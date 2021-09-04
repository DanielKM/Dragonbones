using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Mirror;

using RTSEngine.Lobby;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Faction;

using RTSEngine.Multiplayer.Event;
using RTSEngine.Multiplayer.Utilities;
using RTSEngine.Multiplayer.Server;
using RTSEngine.Multiplayer.Lobby;
using RTSEngine.Multiplayer.Game;
using RTSEngine.Multiplayer.Service;
using RTSEngine.Multiplayer.Logging;
using kcp2k;
using RTSEngine.Scene;

namespace RTSEngine.Multiplayer.Mirror
{
    public class MultiplayerManager : NetworkRoomManager, IMultiplayerManager
    {
        #region Attributes
        [SerializeField, Scene, Tooltip("Scene loaded when leaving this lobby menu."), Header("RTS Engine")]
        private string prevScene = "main_menu";

        public MultiplayerRole Role { private set; get; }

        public MultiplayerState State { private set; get; }

        // Lobby related
        public IMultiplayerLobbyManager CurrentLobby { get; private set; }

        [SerializeField, Tooltip("Delay time after the host player requests to launch the game.")]
        private float startDelayTime = 2.0f;
        private Coroutine startLobbyDelayedCoroutine;

        // Game related
        public IGameManager CurrentGameMgr { get; private set; }
        private List<IMultiplayerFactionManager> multiplayerFactionMgrs = null;
        public IEnumerable<IMultiplayerFactionManager> MultiplayerFactionMgrs => multiplayerFactionMgrs.Where(mgr => mgr.IsValid());
        public IMultiplayerFactionManager LocalMultiplayerFactionMgr { private set; get; }

        // Server related
        public bool IsServerOnly => Role == MultiplayerRole.server;
        [SerializeField, EnforceType(typeof(IMultiplayerServerGameManager)), Tooltip("Prefab of the server game manager which gets spawned on the server instance only to run the lockstep simulation.")]
        private GameObject serverGameMgrPrefab = null;
        public IMultiplayerServerManager ServerMgr { private set; get; }
        public IMultiplayerServerGameManager ServerGameMgr { private set; get; }

        // Transport related
        public ushort Port
        {
            get
            {
                if (transport is TelepathyTransport)
                    return (transport as TelepathyTransport).port;
                else if (transport is KcpTransport)
                    return (transport as KcpTransport).Port;
                else
                {
                    logger.LogError($"[{GetType().Name}] Only Telepathy and KCP Transports are supported currently.");
                    return 7777;
                }
            }
            private set
            {
                if (transport is TelepathyTransport)
                    (transport as TelepathyTransport).port = value;
                else if (transport is KcpTransport)
                    (transport as KcpTransport).Port = value;
                else
                {
                    logger.LogError($"[{GetType().Name}] Only Telepathy and KCP Transports are supported currently.");
                }
            }
        }

        [SerializeField, Tooltip("Define properties for loading target scenes from this scene.")]
        private SceneLoader sceneLoader = new SceneLoader();

        // Other components
        public IMultiplayerManagerUI UIMgr { private set; get; }

        // Services
        protected IMultiplayerLoggingService logger { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IMultiplayerManager, MultiplayerStateEventArgs> MultiplayerStateUpdated;

        private void RaiseMultiplayerStateUpdated(MultiplayerStateEventArgs args)
        {
            this.State = args.State;

            var handler = MultiplayerStateUpdated;
            handler?.Invoke(this, args);
        }

        public event CustomEventHandler<IMultiplayerFactionManager, EventArgs> MultiplayerFactionManagerValidated;

        private void RaiseMultiplayerFactionManagerValidated(IMultiplayerFactionManager newMultiFactionMgr)
        {
            var handler = MultiplayerFactionManagerValidated;
            handler?.Invoke(newMultiFactionMgr, EventArgs.Empty);
        }
        #endregion

        #region Services
        private IReadOnlyDictionary<System.Type, IMultiplayerService> services = null;

        public T GetService<T>() where T : IMultiplayerService
        {
            if(!services.ContainsKey(typeof(T)))
                Debug.LogError ($"[GameManager] No service of type: '{typeof(T)}' has been registered!");

            if (services.TryGetValue(typeof(T), out IMultiplayerService value))
                return (T)value;

            return default;
        }

        private void RegisterServices()
        {
            // Only services that are attached to the same game object are recognized
            // Register the services when the game starts.
            services = GetComponents<IMultiplayerService>()
                .ToDictionary(service => service.GetType().GetSuperInterfaceType<IMultiplayerService>(), service => service);

            // Initialize services.
            foreach (IMultiplayerService service in services.Values)
                service.Init(this);
        }
        #endregion

        #region Initializing
        public override void Awake()
        {
            base.Awake();

            this.logger = GetComponent<IMultiplayerLoggingService>();

            RTSHelper.Init(this);

            RegisterServices();

            // Initial State
            ResetState();

            UIMgr = GetComponent<IMultiplayerManagerUI>();
            if (!logger.RequireValid(UIMgr,
                $"[{GetType().Name}] A component that extends the interface '{typeof(IMultiplayerManagerUI).Name}' must be attached to the same game object to handle UI."))
                return;
            UIMgr.Init(this);

            ServerMgr = MultiplayerServerManager.Singleton;
        }

        public override void Start()
        {
            base.Start();

            // If there is a server manager component this means that this is a server build, in this case, start the server when this component is loaded.
            if (ServerMgr.IsValid())
                ServerMgr.Execute(this);
        }
        #endregion

        #region Terminating
        private void ResetState ()
        {
            Role = MultiplayerRole.none; 

            if (CurrentLobby.IsValid())
                Destroy(CurrentLobby.gameObject);
            CurrentLobby = null;

            if (CurrentGameMgr.IsValid())
                sceneLoader.LoadScene(offlineScene, source: this);

            CurrentGameMgr = null;
            multiplayerFactionMgrs = null;
            LocalMultiplayerFactionMgr = null;
            if (ServerGameMgr.IsValid())
            {
                Destroy(ServerGameMgr as UnityEngine.Object);
                ServerGameMgr = null;
            }
        }

        public void OnNormalStop()
        {
            Stop(DisconnectionReason.normal);
        }

        public void Stop(DisconnectionReason reason)
        {
            switch(Role)
            {
                case MultiplayerRole.none:
                    LoadPrevScene();
                    return;

                case MultiplayerRole.client:
                    StopClient();
                    break;

                case MultiplayerRole.host:
                    StopHost();
                    break;

                case MultiplayerRole.server:
                    StopServer();
                    break;
            }

            ResetState();

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.main));

            switch(reason)
            {
                case DisconnectionReason.normal:
                    UIMgr.Message.Display("You left the room/game.");
                    break;

                case DisconnectionReason.timeout:
                    UIMgr.Message.Display("Your session timedout!");
                    break;

                    // Lobby related
                case DisconnectionReason.lobbyNotFound:
                    UIMgr.Message.Display("Lobby can not be found!");
                    break;

                case DisconnectionReason.lobbyNotAvailable:
                    UIMgr.Message.Display("Lobby is no longer available!");
                    break;

                case DisconnectionReason.lobbyHostKick:
                    UIMgr.Message.Display("You were kicked by the lobby host!");
                    break;

                case DisconnectionReason.gameCodeMismatch:
                    UIMgr.Message.Display("Your game does not match with the server!");
                    break;
            }

            // Attempt to restart headless server when it stops.
            if(ServerMgr.IsValid())
                ServerMgr.Execute(this);
        }

        private void LoadPrevScene()
        {
            if (CurrentLobby.IsValid() || CurrentGameMgr.IsValid())
                return;

            sceneLoader.LoadScene(prevScene, source: this);

            Destroy(this.gameObject);
        }
        #endregion

        #region Starting Lobby: Client, Host or Server
        public ServerAccessData UpdateServerAccessData(ServerAccessData accessData)
        {
            print(accessData.networkAddress);
            if(!string.IsNullOrEmpty(accessData.networkAddress))
                networkAddress = accessData.networkAddress;
            if (ushort.TryParse(accessData.port, out ushort nextPort))
                Port = nextPort;

            // In case the input access data were faulty (example: port has characters in it), return the currently valid set ones.
            return new ServerAccessData
            {
                networkAddress = networkAddress,

                port = Port.ToString()
            };
        }

        private bool OnLobbyLoadStart ()
        {
            if (Role != MultiplayerRole.none)
                return false;

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.loadingLobby));

            return true;
        }

        public void LaunchHost()
        {
            if (!OnLobbyLoadStart())
                return;

            UIMgr.Message.Display("Starting host...");

            Role = MultiplayerRole.host;

            StartHost();
        }

        public void LaunchClient()
        {
            if (!OnLobbyLoadStart())
                return;

            UIMgr.Message.Display("Connecting to lobby...");

            Role = MultiplayerRole.client;

            StartClient();
        }

        public void LaunchServer()
        {
            if (!OnLobbyLoadStart())
                return;

            UIMgr.Message.Display("Starting server...");

            Role = MultiplayerRole.server;

            StartServer();
        }
        #endregion

        #region Active Lobby Handling
        public void OnLobbyLoaded(IMultiplayerLobbyManager currentLobby)
        {
            this.CurrentLobby = currentLobby;

            if (!logger.RequireValid(this.CurrentLobby,
              $"[{GetType().Name}] Attempting to assign an invalid lobby is not allowed!"))
            {
                this.CurrentLobby = null;
                return;
            }

            this.CurrentLobby.LobbyGameDataUpdated += HandleLobbyGameDataUpdated;

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.lobby));
        }

        private void HandleLobbyGameDataUpdated(LobbyGameData prevLobbyGameData, EventArgs args)
        {
            // Update the maximum connections amount to suit the current map maximum faction amount
            maxConnections = CurrentLobby.CurrentMap.factionsAmount.max;

            // Update the gameplay scene to the current map scene.
            GameplayScene = CurrentLobby.CurrentMap.sceneName;
        }

        /// <summary>
        /// This is called on the server when all the players in the room are ready.
        /// <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
        /// </summary>
        public override void OnRoomServerPlayersReady()
        {
            // Override this method because we don't want the game play scene to load as soon as all players are ready
            // The host starts the game when all players are ready
        }

        public ErrorMessage CanStartLobby ()
        {
            if (!this.CurrentLobby.IsValid())
                return ErrorMessage.invalid;
            else if (this.CurrentLobby.CurrentMap.factionsAmount.min > this.CurrentLobby.FactionSlotCount)
                return ErrorMessage.lobbyMinSlotsUnsatisfied;
            else if (this.CurrentLobby.CurrentMap.factionsAmount.max < this.CurrentLobby.FactionSlotCount)
                return ErrorMessage.lobbyMaxSlotsUnsatisfied;
            else if (!roomSlots.All(slot => slot.readyToBegin))
                return ErrorMessage.lobbyPlayersNotAllReady;

            return ErrorMessage.none;
        }

        public ErrorMessage StartLobby ()
        {
            ErrorMessage errorMessage;
            if ((errorMessage = CanStartLobby()) != ErrorMessage.none)
                return errorMessage;

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.startingLobby));
            startLobbyDelayedCoroutine = StartCoroutine(StartLobbyDelayed(delayTime: startDelayTime));

            return ErrorMessage.none;
        }

        private IEnumerator StartLobbyDelayed(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.gameConfirmed));

            ServerChangeScene(GameplayScene);
        }

        public bool InterruptStartLobby ()
        {
            if (State != MultiplayerState.startingLobby)
                return false;

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.lobby));
            StopCoroutine(startLobbyDelayedCoroutine);

            CurrentLobby.LocalFactionSlot.OnStartLobbyInterrupted();

            return true;
        }
        #endregion

        #region Active Game Handling
        public void OnGameLoaded(IGameManager gameMgr)
        {
            this.CurrentGameMgr = gameMgr;

            if (!logger.RequireValid(this.CurrentGameMgr,
              $"[{GetType().Name}] Attempting to assign an invalid game manager is not allowed!"))
            {
                this.CurrentGameMgr = null;
                return;
            }

            multiplayerFactionMgrs = new List<IMultiplayerFactionManager>();

            // Either start client related components, server related components or both (if host)
            switch(Role)
            {
                case MultiplayerRole.client:
                    break;

                case MultiplayerRole.host:
                case MultiplayerRole.server:
                    InitGameServer();
                    break;
            }

            RaiseMultiplayerStateUpdated(new MultiplayerStateEventArgs(state: MultiplayerState.game));
        }

        private void InitGameServer()
        {
            if (!logger.RequireValid(serverGameMgrPrefab,
              $"[{GetType().Name}] The 'Server Game Mgr Prefab' field must be assigned in order to start a server instance!"))
                return; 

            ServerGameMgr = Instantiate(serverGameMgrPrefab).GetComponent<IMultiplayerServerGameManager>();
            ServerGameMgr.transform.SetParent(this.transform);

            ServerGameMgr.Init(this);
        }

        public void OnMultiplayerFactionManagerValidated(IMultiplayerFactionManager newMultiFactionMgr)
        {
            multiplayerFactionMgrs.Add(newMultiFactionMgr);

            // Assign the local faction manager of the local player.
            // If this is the headless server instance then set it up so that the local faction manager is the client host one.
            if ((IsServerOnly && newMultiFactionMgr.GameFactionSlot.Data.role == FactionSlotRole.host)
                || (newMultiFactionMgr as NetworkBehaviour).isLocalPlayer)
            {
                LocalMultiplayerFactionMgr = newMultiFactionMgr;
                // Assign the local multiplayer faction manager as the new IInputAdder instance to handle inputs in the multiplayer game
                CurrentGameMgr.CurrBuilder.OnInputAdderReady(LocalMultiplayerFactionMgr);
            }

            RaiseMultiplayerFactionManagerValidated(newMultiFactionMgr);
        }
        #endregion

        #region Handling Disconnection
        /// <summary>
        /// This is called on the client when disconnected from a server.
        /// </summary>
        /// <param name="conn">The connection that disconnected.</param>
        public override void OnRoomClientDisconnect(NetworkConnection conn) 
        {
            if (!conn.isReady)
                Stop(DisconnectionReason.lobbyNotFound);
            else if (CurrentLobby.IsValid())
                Stop(DisconnectionReason.lobbyNotAvailable);

            // WARNING:
            // If we are in game (game manager instance is not null) then load the multiplayer main menu?
        }

        /// <summary>
        /// This is called on the client when the client stops.
        /// </summary>
        public override void OnRoomStopClient()
        {
            ResetState();
        }

        /// <summary>
        /// Called on the server when a client disconnects.
        /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            if (CurrentLobby.IsValid())
            {
                // If no suitable faction slot for the disconnected player is found then the player must have been disconnected automatically due to maxConnections or some internal reason
                // In this case, no need to handle player disconnection in the lobby.
                ILobbyFactionSlot disconnectedFactionSlot = conn.clientOwnedObjects
                    .Select(identity => identity.gameObject.GetComponent<ILobbyFactionSlot>())
                    .Where(lobbySlot => lobbySlot.IsValid())
                    .FirstOrDefault();

                // If a player leaves while the game is about to start then cancel starting the game.
                InterruptStartLobby();

                // See if the disconnected player is the host, if yes, then it will be updated.
                OnFactionSlotRemoved(disconnectedFactionSlot);
            }

            base.OnServerDisconnect(conn);
        }

        private bool OnFactionSlotRemoved(ILobbyFactionSlot removedSlot)
        {
            // Only the server can change the role of a faction slot.
            if (!removedSlot.IsValid()
                || !IsServerOnly
                || !CurrentLobby.IsValid())
                return false;

            if(removedSlot.Role == FactionSlotRole.host)
            {
                // Update host on all clients because a host client is required to run the game.
                ILobbyFactionSlot nextHostSlot = CurrentLobby.FactionSlots
                    .FirstOrDefault(slot => slot != removedSlot && slot.Role == FactionSlotRole.client);

                // No client can be found to be set as the new host, cancel the game and shutdown the server.
                if (!nextHostSlot.IsValid())
                {
                    Stop(DisconnectionReason.nextHostNotFound);
                    return false;
                }

                // Changing hosts only occurs at a headless server.
                // If the game is running, then we need to re-assign the local multiplayer faction manager
                if (CurrentGameMgr.IsValid())
                {
                    LocalMultiplayerFactionMgr = MultiplayerFactionMgrs.FirstOrDefault(multiFactionMgr => multiFactionMgr.GameFactionSlot == nextHostSlot.GameFactionSlot);
                    // Re-assign the new IInputManager as t he local multiplayer faction manager
                    CurrentGameMgr.CurrBuilder.OnInputAdderReady(LocalMultiplayerFactionMgr);
                }

                nextHostSlot.UpdateRoleRequest(FactionSlotRole.host);
            }

            CurrentLobby.RemoveFactionSlotComplete(removedSlot);

            // Trigger the kick on the player in the tracker server game manager if it was not already kicked.

            return true;
        }
        #endregion

        #region Handling Transport

        #endregion
    }
}
