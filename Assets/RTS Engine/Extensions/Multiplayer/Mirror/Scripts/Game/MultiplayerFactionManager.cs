using System;
using System.Collections.Generic;
using System.Linq;

using Mirror;

using RTSEngine.Determinism;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Multiplayer.Game;
using RTSEngine.Multiplayer.Logging;
using RTSEngine.Multiplayer.Utilities;
using UnityEngine;

namespace RTSEngine.Multiplayer.Mirror.Game
{
    public class MultiplayerFactionManager : NetworkBehaviour, IMultiplayerFactionManager
    {
        #region Attributes
        /// <summary>
        /// Has the multiplayer faction manager asked to get validated after the local game has been initialized?
        /// </summary>
        public bool IsInitialized { private set; get; } = false;

        /// <summary>
        /// Is the multiplayer faction manager validated by the server?
        /// </summary>
        public bool IsValidated { private set; get; } = false;

        public IFactionSlot GameFactionSlot { private set; get; }

        public int CurrTurn { private set; get; } = -1;
        public int LastInputID { private set; get; } = -1;

        private List<CommandInput> relayedInputs = new List<CommandInput>();
        public bool IsSimPaused { private set; get; }

        // Holds added inputs received before all clients are cached to re-send them as soon as the server informs this instance that all clients are ready to get inputs
        private List<CommandInput> preValidationInputCache = new List<CommandInput>();

        public double LastRTT => NetworkTime.rtt;

        // Multiplayer Services
        protected IMultiplayerLoggingService logger { private set; get; }

        // Game Services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected ITimeModifier timeModifier { private set; get; }

        // Other components
        protected IMultiplayerManager multiplayerMgr { private set; get; }
        protected IGameManager gameMgr => this.multiplayerMgr.CurrentGameMgr;
        #endregion

        #region Pre-Initializing/Post-Terminating: Server Only
        public override void OnStartServer()
        {
            if (IsInitialized)
                return;

            // Find the multiplayer manager and only proceed if this the server since initializing the faction slots on host/clients uses OnClientEnterRoom() callback.
            IMultiplayerManager multiplayerMgr = (NetworkManager.singleton as IMultiplayerManager);
            if (multiplayerMgr.Role != MultiplayerRole.server)
                return;

            InitServer(multiplayerMgr);
        }

        public override void OnStopServer()
        {
            if (multiplayerMgr.Role != MultiplayerRole.server)
                return;
        }
        #endregion

        #region Pre-Initializing/Post-Terminating: Host/Client Only
        public override void OnStartClient()
        {
            if (IsInitialized)
                return;

            // The RoomNetworkManager (Mirror) handles spawning this lobby player object.
            // Therefore, we use this callback to know when the client enters the room and initialize their lobby slot here
            InitClient(NetworkManager.singleton as IMultiplayerManager);
        }

        public override void OnStopClient()
        {
        }
        #endregion

        #region Initializing/Terminating
        private void InitServer(IMultiplayerManager multiplayerMgr)
        {
            Init(multiplayerMgr);
        }

        private void InitClient(IMultiplayerManager multiplayerMgr)
        {
            Init(multiplayerMgr);
        }

        private void Init(IMultiplayerManager multiplayerMgr)
        {
            this.multiplayerMgr = multiplayerMgr;
            this.logger = multiplayerMgr.GetService<IMultiplayerLoggingService>(); 

            IsValidated = false;
            IsInitialized = false;
            IsSimPaused = true;

            // TODO
            // Handle initializing NPC factions?
        }

        // Use the Update method to complete the initialization process since we have to wait for the game manager to load
        // while this object is created by the multiplayer manager before the demo scene is loaded.
        private void Update()
        {
            if (!multiplayerMgr.IsValid())
                return;

            if(!IsInitialized)
            {
                if(!multiplayerMgr.CurrentGameMgr.IsValid())
                    return;

                this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
                this.inputMgr = gameMgr.GetService<IInputManager>();
                this.timeModifier = gameMgr.GetService<ITimeModifier>();

                CurrTurn = 0;
                LastInputID = -1;

                IsInitialized = true;

                if (isLocalPlayer)
                    CmdValidate(gameMgr.LocalFactionSlot.ID);
            }
        }
        #endregion

        #region Post-Initializing: Validation from Server
        /// <summary>
        /// Validate whether the player can remain in this lobby and set their permission inside the lobby.
        /// </summary>
        /// <param name="factionSlotID"></param>
        [Command]
        private void CmdValidate(int factionSlotID)
        {
            // In case this is the server, this object might not have been initialized yet so fetch it directly.
            if (!multiplayerMgr.IsValid())
                multiplayerMgr = NetworkManager.singleton as IMultiplayerManager;

            // If this game instance is the headless server then update the input directly as the RPC call will not be called on the headless server
            if (multiplayerMgr.Role == MultiplayerRole.server)
                OnValidationComplete(factionSlotID);

            RpcOnValidated(factionSlotID);
        }

        [ClientRpc]
        private void RpcOnValidated(int factionSlotID)
        {
            OnValidationComplete(factionSlotID);
        }

        private void OnValidationComplete(int factionSlotID)
        {
            GameFactionSlot = gameMgr.GetFactionSlot(factionSlotID);

            if (isLocalPlayer)
            {
                InitLocalPlayer();

                CmdInitComplete(factionSlotID);
            }
            else
                InitNonLocalPlayer();
        }

        [Command]
        private void CmdInitComplete(int factionSlotID)
        {
            // If this game instance is the headless server then update the input directly as the RPC call will not be called on the headless server
            if (multiplayerMgr.Role == MultiplayerRole.server)
                OnInitComplete(factionSlotID);

            RpcOnInitComplete(factionSlotID);
        }

        [ClientRpc]
        private void RpcOnInitComplete(int factionSlotID)
        {
            OnInitComplete(factionSlotID);
        }

        private void OnInitComplete(int factionSlotID)
        {
            multiplayerMgr.OnMultiplayerFactionManagerValidated(this, (float)LastRTT);
        }

        protected virtual void InitLocalPlayer() { }

        protected virtual void InitNonLocalPlayer() { }

        public void OnAllClientsValidatedServer()
        {
            // Only allow to go through if this has been called on the server instance
            if (!multiplayerMgr.ServerGameMgr.IsValid())
                return;

            TargetOnAllClientsValidatedLocal();

            // If this is a headless server instance then it would have to push its cached inputs to all clients to start the game
            // Therefore we call it directly as the above TargetRpc method would only be called on the clients side
            if (multiplayerMgr.Role == MultiplayerRole.server)
                OnAllClientsValidated();
        }

        [TargetRpc]
        private void TargetOnAllClientsValidatedLocal()
        {
            logger.Log($"[{GetType().Name} - Faction ID: {GameFactionSlot.ID}] All players have been validated on the server. Simulation has now started!");

            OnAllClientsValidated();
        }

        private void OnAllClientsValidated()
        {
            IsValidated = true;
            IsSimPaused = false;

            // Re-send the cached input that was added and attempted to be sent before all clients were validated
            AddInput(preValidationInputCache);
            preValidationInputCache.Clear();
        }
        #endregion

        #region Adding Input
        public void AddInput(CommandInput input)
        {
            AddInput(Enumerable.Repeat(input, 1));
        }

        public void AddInput(IEnumerable<CommandInput> inputs)
        {
            if (IsSimPaused)
            {
                preValidationInputCache.AddRange(inputs);
                return;
            }

            // If we are dealing with the instance where the server is then directly add the input to the server
            if (RTSHelper.IsMasterInstance())
                AddInputToMaster(inputs);
            else 
                CmdAddInput(inputs.ToArray());
        }

        [Command]
        private void CmdAddInput(CommandInput[] inputs)
        {
            AddInputToMaster(inputs);
        }

        private void AddInputToMaster(IEnumerable<CommandInput> inputs)
        {
            multiplayerMgr.ServerGameMgr.AddInput(inputs, GameFactionSlot.ID);
        }
        #endregion

        #region Getting Relayed Input
        public void RelayInput(IEnumerable<MultiplayerInputWrapper> relayedInputs, int lastRelayedInputID, int serverTurn)
        {
            // Only allow to go through if this has been called on the server instance
            if (!multiplayerMgr.ServerGameMgr.IsValid())
                return;

            RpcRelayInput(connectionToClient, relayedInputs.ToArray(), lastRelayedInputID, serverTurn);
        }

        [TargetRpc]
        private void RpcRelayInput(NetworkConnection targetClient, MultiplayerInputWrapper[] inputs, int lastRelayedInputID, int serverTurn)
        {
            if(IsSimPaused && serverTurn == CurrTurn - 1)
                logger.Log($"[{GetType().Name} - Faction ID: {GameFactionSlot.ID}] Server is resending inputs of last turn due to locked simulation (game is paused)!");
            else if (!logger.RequireTrue(serverTurn == CurrTurn,
              $"[{GetType().Name}] Expected to get server turn {CurrTurn} but received {serverTurn} instead! This MUST NOT happen!"))
                return; 

            if (inputs.Any())
                foreach (MultiplayerInputWrapper input in inputs)
                {
                    // Client has already received this input.
                    if (input.ID < LastInputID + 1)
                        continue;
                    // Next input that the client is expecting
                    else if (input.ID == LastInputID + 1)
                    {
                        LastInputID++;
                        this.relayedInputs.Add(input.input);
                    }
                    else
                    {
                        logger.LogError($"[{GetType().Name}] Bad relayed input on client! Expected input of ID {LastInputID + 1} but got input of ID {input.ID}. This MUST NOT happen!");
                        return;
                    }
                }

            // Either no inputs were sent or we can confirm all inputs were received.
            if(!inputs.Any() || LastInputID == lastRelayedInputID)
            {
                // Play the actual inputs
                foreach (CommandInput input in this.relayedInputs)
                    inputMgr.LaunchInput(input);

                this.relayedInputs.Clear();

                /*
                 * Issue of random disconnection used to happen in case the simulation pauses and the confirmation does not get to the server before the server actually resends the input.
                 * When this happens, the received inputs is empty. this needs further investigating. This is now fixed with the server ignoring double input relay confirmation
                 * But a more robust solution should be implemented to avoid unnecessary network traffic.
                 * If the transport used is reliable then it probably makes sense to not resend the confirmation on empty inputs?? but what if the turn has no inputs to relay??
                 * Or maybe do not tie this to the inputs being empty or not but rather the serverTurn and CurrTurn
                if(LastInputID != lastRelayedInputID && !inputs.Any())
                    logger.LogError($"Double confirmed input of server turn {serverTurn} with local turn {CurrTurn}");
                */

                // Let the server know we received it!
                CmdOnRelayedInputReceived(serverTurn, (float)LastRTT);
            }

            // Only increase the current turn in case we receive input on the expected server turn.
            if(serverTurn == CurrTurn)
                CurrTurn++;
        }

        [Command]
        private void CmdOnRelayedInputReceived(int turnID, float lastRTT)
        {
            multiplayerMgr.ServerGameMgr.OnRelayedInputReceived(GameFactionSlot.ID, turnID, lastRTT);
        }
        #endregion

        #region Pausing Simulation
        public void PauseSimulation(bool enable)
        {
            // Only allow to go through if this has been called on the server instance
            if (!multiplayerMgr.ServerGameMgr.IsValid())
                return;

            RpcPauseSimulation(connectionToClient, enable);

            IsSimPaused = enable;
        }

        [TargetRpc]
        private void RpcPauseSimulation(NetworkConnection connectionToClient, bool enable)
        {
            this.IsSimPaused = enable;

            if (IsSimPaused)
                gameMgr.SetState(GameStateType.frozen);
            else
                gameMgr.SetState(GameStateType.running);
        }
        #endregion
    }
}
