using System;
using System.Linq;

using Mirror;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Lobby;
using RTSEngine.Lobby.Logging;
using RTSEngine.Multiplayer.Game;
using RTSEngine.Multiplayer.Utilities;
using RTSEngine.Lobby.UI;
using RTSEngine.UI;

namespace RTSEngine.Multiplayer.Mirror.Lobby
{
    public class MultiplayerLobbyFactionSlot : NetworkRoomPlayer, ILobbyFactionSlot
    {
        #region Attributes
        // Has this lobby slot been initialized?
        public bool IsInitialized { private set; get; } = false;

        public FactionSlotData Data => new FactionSlotData
        {
            role = Role,

            name = inputData.name,
            color = lobbyMgr.FactionColorSelector.Get(inputData.colorID),

            type = lobbyMgr.CurrentMap.GetFactionType(inputData.factionTypeID),
            npcType = lobbyMgr.CurrentMap.GetNPCType(inputData.npcTypeID),

            isLocalPlayer = isLocalPlayer
        };
        private LobbyFactionSlotInputData inputData = new LobbyFactionSlotInputData();

        public FactionSlotRole Role { get; private set; } = FactionSlotRole.client;

        public bool IsInteractable { private set; get; }

        [SerializeField, Tooltip("UI Image to display the faction's color.")]
        private Image factionColorImage = null; 
        [SerializeField, Tooltip("UI Input Field to display and change the faction's name.")]
        private InputField factionNameInput = null; 
        [SerializeField, Tooltip("UI Dropdown menu used to display the list of possible faction types that the slot can have.")]
        private Dropdown factionTypeMenu = null; 
        [SerializeField, Tooltip("UI Dropdown menu used to display the list of possible NPC faction types that the slot can have")]
        private Dropdown npcTypeMenu = null; 
        [SerializeField, Tooltip("Button used to remove the faction slot from the lobby.")]
        private Button removeButton = null;
        [SerializeField, Tooltip("Button used to allow the player to announce they are ready to start the game or not.")]
        private Button readyToBeginButton = null; 
        [SerializeField, Tooltip("Image that is activated whenever the player announces that they are ready to start the game.")]
        private Image readyImage = null; 

        // Active game
        public IMultiplayerFactionManager MultiplayerFactionMgr { get; private set; }
        public IFactionSlot GameFactionSlot { get; private set; }

        // Lobby Services
        protected ILobbyManager lobbyMgr { private set; get; }
        protected ILobbyLoggingService logger { private set; get; }
        protected ILobbyManagerUI lobbyUIMgr { private set; get; }
        protected ILobbyPlayerMessageUIHandler playerMessageUIHandler { private set; get; } 

        // Multiplayer Services
        protected IMultiplayerManager multiplayerMgr { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<ILobbyFactionSlot, EventArgs> RoleUpdated;

        private void RaiseRoleUpdated(FactionSlotRole role)
        {
            this.Role = role;

            var handler = RoleUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Pre-Initializing: Server Only
        public override void OnStartServer()
        {
            if (IsInitialized)
                return;

            // Find the multiplayer manager and only proceed if this the server since initializing the faction slots on host/clients uses OnClientEnterRoom() callback.
            IMultiplayerManager multiplayerMgr = (NetworkManager.singleton as IMultiplayerManager);
            if (!multiplayerMgr.IsServerOnly)
                return;

            this.multiplayerMgr = multiplayerMgr;
            InitServer(multiplayerMgr.CurrentLobby, isPlayerControlled: true);
        }
        #endregion

        #region Pre-Initializing: Host/Client Only
        public override void OnClientEnterRoom()
        {
            if (IsInitialized)
                return;

            // The RoomNetworkManager (Mirror) handles spawning this lobby player object.
            // Therefore, we use this callback to know when the client enters the room and initialize their lobby slot here
            this.multiplayerMgr = NetworkManager.singleton as IMultiplayerManager;
            InitClient(multiplayerMgr.CurrentLobby, isPlayerControlled:true);
        }
        #endregion

        #region Initializing/Terminating
        private void InitServer(ILobbyManager lobbyMgr, bool isPlayerControlled)
        {
            Init(lobbyMgr, isPlayerControlled);
        }

        private void InitClient(ILobbyManager lobbyMgr, bool isPlayerControlled)
        {
            Init(lobbyMgr, isPlayerControlled);
        }

        public void Init (ILobbyManager lobbyMgr, bool isPlayerControlled)
        {
            // The 'multiplayerMgr' is set from the ClientStart or ServerStart methods, depending on the nature of this instance.
            this.lobbyMgr = lobbyMgr;

            // Get services
            this.logger = lobbyMgr.GetService<ILobbyLoggingService>();
            this.lobbyUIMgr = lobbyMgr.GetService<ILobbyManagerUI>();
            this.playerMessageUIHandler = lobbyMgr.GetService<ILobbyPlayerMessageUIHandler>();

            if (!logger.RequireValid(factionColorImage, $"[{GetType().Name}] The field 'Faction Color Image' is required!")
                || !logger.RequireValid(factionTypeMenu, $"[{GetType().Name}] The field 'Faction Type Menu' is required!")
                || !logger.RequireValid(npcTypeMenu, $"[{GetType().Name}] The field 'NPC Type Menu' is required!")
                || !logger.RequireValid(removeButton, $"[{GetType().Name}] The field 'Remove Button' is required!")
                || !logger.RequireValid(readyToBeginButton, $"[{GetType().Name}] The field 'Ready Button' is required!")
                || !logger.RequireValid(readyImage, $"[{GetType().Name}] The field 'Ready Image' is required!"))
                return; 

            // Since this Init method is called either from a client or headless server instance and not created in the lobby manager and then initiated here
            // We need to add the faction slot so that the lobby manager can keep track of it.
            this.lobbyMgr.AddFactionSlot(this);

            this.lobbyMgr.LobbyGameDataUpdated += HandleLobbyGameDataUpdated;
            this.lobbyMgr.FactionSlotRemoved += HandleFactionSlotRemoved;
            this.multiplayerMgr.MultiplayerFactionManagerValidated += HandleMultiplayerFactionManagerValidated;

            // Every faction slot starts with the same default input data
            ResetInputData();

            // By default, the faction slot is not interactable until it is validated.
            SetInteractable(false);

            if(isPlayerControlled)
            {
                // Make the new player validate themselves in the server if this their local faction slot.
                if (isLocalPlayer)
                    CmdValidateRequest(localGameCode: lobbyMgr.GameCode);
            }
            else
            {
                // Handle adding NPC faction slot.
            }

            IsInitialized = true;
        }

        private void OnDestroy()
        {
            this.lobbyMgr.LobbyGameDataUpdated -= HandleLobbyGameDataUpdated;
            this.lobbyMgr.FactionSlotRemoved -= HandleFactionSlotRemoved;

            this.multiplayerMgr.MultiplayerFactionManagerValidated -= HandleMultiplayerFactionManagerValidated;
        }
        #endregion

        #region Post-Initializing: Validating Client
        /// <summary>
        /// Validate whether the player can remain in this lobby and set their permission inside the lobby.
        /// </summary>
        /// <param name="localGameCode"></param>
        [Command]
        private void CmdValidateRequest(string localGameCode)
        {
            // In case this is the server, this object might not have been initialized yet so fetch it directly.
            if (!multiplayerMgr.IsValid())
                multiplayerMgr = NetworkManager.singleton as IMultiplayerManager;
            if (!lobbyMgr.IsValid())
                lobbyMgr = multiplayerMgr.CurrentLobby;

            // If the game is already starting then no new clients are allowed to join.
            if(lobbyMgr.IsStartingLobby)
            {
                RpcKickRelay(factionSlotID:index, reason:DisconnectionReason.lobbyAlreadyStarting);
                return;
            }
            // If the client's game code does not match with the server, kick the client.
            else if (localGameCode != lobbyMgr.GameCode)
            {
                RpcKickRelay(factionSlotID:index, reason:DisconnectionReason.gameCodeMismatch);
                return;
            }

            // If this player lobby object is the first player lobby object to be added then mark it as the host.
            FactionSlotRole validatedRole = lobbyMgr.FactionSlotCount > 1 ? FactionSlotRole.client : FactionSlotRole.host;

            // If this game instance is the headless server then update the input directly as the RPC call will not be called on the headless server
            // Add the faction slot to the manager here for the same above reason
            if (multiplayerMgr.IsServerOnly)
                RaiseRoleUpdated(validatedRole);

            RpcValidateRelay(validatedRole);

            RpcUpdateLobbyGameDataRelay(lobbyMgr.CurrentLobbyGameData);
        }

        [ClientRpc]
        private void RpcValidateRelay(FactionSlotRole validatedRole)
        {
            RaiseRoleUpdated(validatedRole);

            if (isLocalPlayer)
                ValidateLocalPlayer();
            else
                ValidateNonLocalPlayer();
        }

        public void ValidateLocalPlayer()
        {
            // Host player sets the default map.
            if (Role == FactionSlotRole.host)
                CmdUpdateLobbyGameDataRequest(lobbyMgr.CurrentLobbyGameData);

            // Only the host is able to pick the lobby game data
            lobbyUIMgr.SetInteractable(lobbyMgr.LocalFactionSlot.Role == FactionSlotRole.host);

            SetInteractable(true);

            CmdUpdateInputDataRequest(this.inputData);
        }

        public void ValidateNonLocalPlayer()
        {
            // New faction slot added by a new player -> sync the local faction slot input data to this one.
            lobbyMgr.LocalFactionSlot.OnFactionSlotValidated(this);
        }
        #endregion

        #region Setting Faction Slot Role
        public void UpdateRoleRequest(FactionSlotRole newRole)
        {
            // Only the headless server instance can update the role of a faction slot
            if (!multiplayerMgr.IsServerOnly)
                return;

            UpdateRoleComplete(newRole); // Call on headless server
            RpcUpdateRoleRelay(newRole); // Relay call to clients
        }

        [ClientRpc]
        private void RpcUpdateRoleRelay(FactionSlotRole newRole)
        {
            UpdateRoleComplete(newRole);
        }

        private void UpdateRoleComplete(FactionSlotRole newRole)
        {
            RaiseRoleUpdated(newRole);

            // If the game has already started then reassign the host in the game faction slots.
            if (multiplayerMgr.State == MultiplayerState.game)
            {
                MultiplayerFactionMgr.GameFactionSlot.UpdateRole(newRole);

                // Stop here as we do not need to update interactibility on the lobby UI since an active game is underway.
                return;
            }

            // Keep the lobby map UI disabled for the headless server instance
            if (multiplayerMgr.IsServerOnly)
                return;

            // Only the host is able to pick the lobby game data
            lobbyUIMgr.SetInteractable(lobbyMgr.LocalFactionSlot.Role == FactionSlotRole.host);
        }
        #endregion

        #region Handling Faction Slot Input Data
        private void ResetInputData()
        {
            inputData = new LobbyFactionSlotInputData
            {
                name = "new_faction",

                colorID = 0,

                factionTypeID = 0,
                npcTypeID = 0
            };
        }

        /// <summary>
        /// Called when a non local faction slot is validated on the local player's instance.
        /// The call is to allow the local player to share their input data (and the host to share the map settings) with the newly validated player.
        /// </summary>
        /// <param name="newFactionSlot"></param>
        public void OnFactionSlotValidated(ILobbyFactionSlot newFactionSlot)
        {
            // The host is responsible for relaying the game's lobby data to the newly added faction slot
            if(Role == FactionSlotRole.host)
                CmdUpdateLobbyGameDataRequest(lobbyMgr.CurrentLobbyGameData);

            // Sync the local faction's input data so that the new faction slot can see them
            UpdateInputDataAttempt(this.inputData);
        }

        private void UpdateInputDataAttempt(LobbyFactionSlotInputData inputData)
        {
            if (!IsInteractable || !isLocalPlayer)
                return;

            CmdUpdateInputDataRequest(inputData);
        }

        [Command]
        private void CmdUpdateInputDataRequest(LobbyFactionSlotInputData inputData)
        {
            if(multiplayerMgr.IsServerOnly)
                UpdateInputDataComplete(inputData);

            RpcUpdateInputDataRelay(inputData);
        }

        [ClientRpc]
        private void RpcUpdateInputDataRelay(LobbyFactionSlotInputData inputData)
        {
            UpdateInputDataComplete(inputData);
        }

        private void UpdateInputDataComplete(LobbyFactionSlotInputData inputData)
        {
            this.inputData = inputData;
            RefreshInputDataUI();
        }

        private void RefreshInputDataUI ()
        {
            factionNameInput.text = inputData.name;

            factionColorImage.color = this.lobbyMgr.FactionColorSelector.Get(inputData.colorID);

            factionTypeMenu.value = inputData.factionTypeID;
            npcTypeMenu.value = inputData.npcTypeID;
        }
        #endregion

        #region General UI Handling
        public void SetInteractable (bool interactable)
        {
            factionNameInput.interactable = interactable; 
            factionTypeMenu.interactable = interactable;

            npcTypeMenu.gameObject.SetActive(interactable && Role == FactionSlotRole.npc);
            npcTypeMenu.interactable = interactable && Role == FactionSlotRole.npc;

            readyToBeginButton.interactable = interactable;

            removeButton.gameObject.SetActive(lobbyMgr.LocalFactionSlot?.Role == FactionSlotRole.host && !isLocalPlayer);
            removeButton.interactable = lobbyMgr.LocalFactionSlot?.Role == FactionSlotRole.host && !isLocalPlayer;

            IsInteractable = interactable;
        }
        #endregion

        #region Updating Lobby Game Data
        // Called by the IMultiplayerManager instance of the host when they request to update the lobby game data
        public void UpdateLobbyGameDataAttempt(LobbyGameData newLobbyGameData)
        {
            if(!IsInteractable || !isLocalPlayer)
                return;

            CmdUpdateLobbyGameDataRequest(newLobbyGameData);
        }

        [Command]
        private void CmdUpdateLobbyGameDataRequest(LobbyGameData gameData)
        {
            if(multiplayerMgr.IsServerOnly)
                lobbyMgr.UpdateLobbyGameDataComplete(gameData);

            RpcUpdateLobbyGameDataRelay(gameData);
        }

        [ClientRpc]
        private void RpcUpdateLobbyGameDataRelay(LobbyGameData gameData)
        {
            lobbyMgr.UpdateLobbyGameDataComplete(gameData);
        }

        private void HandleLobbyGameDataUpdated (LobbyGameData prevLobbyGameData, EventArgs args)
        {
            ResetFactionType(prevMapID:prevLobbyGameData.mapID);
            ResetNPCType(prevMapID:prevLobbyGameData.mapID);
        }
        #endregion

        #region Updating Faction Name
        public void OnFactionNameUpdated ()
        {
            if (!IsInteractable || factionNameInput.text.Trim() == "") 
            {
                factionNameInput.text = inputData.name;
                return;
            }

            inputData.name = factionNameInput.text.Trim();

            UpdateInputDataAttempt(inputData);
        }
        #endregion

        #region Updating Faction Type
        private void ResetFactionType(int prevMapID)
        {
            RTSHelper.UpdateDropdownValue(ref factionTypeMenu,
                lastOption: lobbyMgr.GetMap(prevMapID).GetFactionType(inputData.factionTypeID).Name,
                newOptions: lobbyMgr.CurrentMap.factionTypes.Select(type => type.Name).ToList());

            inputData.factionTypeID = factionTypeMenu.value;
        }

        public void OnFactionTypeUpdated ()
        {
            if(!IsInteractable)
            {
                factionTypeMenu.value = inputData.factionTypeID;
                return;
            }

            inputData.factionTypeID = factionTypeMenu.value;

            UpdateInputDataAttempt(inputData);
        }
        #endregion

        #region Updating Color
        public void OnFactionColorUpdated ()
        {
            if(!IsInteractable)
                return;

            inputData.colorID = lobbyMgr.FactionColorSelector.GetNextIndex(inputData.colorID);

            UpdateInputDataAttempt(inputData);
        }
        #endregion

        #region Updating NPC Type
        private void ResetNPCType(int prevMapID)
        {
            RTSHelper.UpdateDropdownValue(ref npcTypeMenu,
                lastOption: lobbyMgr.GetMap(prevMapID).GetNPCType(inputData.npcTypeID).Name,
                newOptions: lobbyMgr.CurrentMap.npcTypes.Select(type => type.Name).ToList());

            inputData.npcTypeID = npcTypeMenu.value;
        }

        public void OnNPCTypeUpdated ()
        {
            if(!IsInteractable)
            {
                npcTypeMenu.value = inputData.npcTypeID;
                return;
            }

            inputData.npcTypeID = npcTypeMenu.value;

            UpdateInputDataAttempt(inputData);
        }
        #endregion

        #region Updating Ready Status
        public void ToggleReadyStatus()
        {
            if (!IsInteractable || !isLocalPlayer)
                return;

            CmdChangeReadyState(!readyToBegin);
        }

        public override void ReadyStateChanged(bool _, bool newReadyState)
        {
            readyImage.gameObject.SetActive(newReadyState);
        }
        #endregion

        #region Removing Faction Slot
        public void OnRemove()
        {
            lobbyMgr.RemoveFactionSlotRequest(index);
        }

        public void KickAttempt (int factionSlotID)
        {
            if (multiplayerMgr.IsServerOnly)
            {
                RpcKickRelay(factionSlotID, DisconnectionReason.serverKick);
                return;
            }

            CmdKickRequest(factionSlotID, DisconnectionReason.lobbyHostKick);
        }

        [Command]
        private void CmdKickRequest(int factionSlotID, DisconnectionReason reason)
        {
            RpcKickRelay(factionSlotID, reason); 
        }

        [ClientRpc]
        private void RpcKickRelay(int factionSlotID, DisconnectionReason reason)
        {
            ILobbyFactionSlot nextSlot = lobbyMgr.FactionSlots.ElementAtOrDefault(factionSlotID);

            // Only apply the lobby departure for the local player since we will close their connection from their end to complete the kick.
            // This is in case the local player is still connected...
            if (nextSlot == lobbyMgr.LocalFactionSlot)
                multiplayerMgr.Stop(reason);
        }

        private void HandleFactionSlotRemoved(ILobbyFactionSlot removedSlot, EventArgs args)
        {
            // Only if the game is active and this is the host instance.
            if (!multiplayerMgr.CurrentGameMgr.IsValid()
                || !multiplayerMgr.ServerGameMgr.IsValid())
                return;

            multiplayerMgr.CurrentGameMgr.OnFactionDefeated(removedSlot.GameFactionSlot.ID);
        }
        #endregion

        #region Starting Lobby
        public void OnStartLobbyRequest()
        {
            if (!IsInteractable || !isLocalPlayer)
                return;

            if (Role != FactionSlotRole.host)
            {
                playerMessageUIHandler.Message.Display("Only host is allowed to start the game!");
                return;
            }

            CmdStartLobbyRequest();
        }

        [Command]
        private void CmdStartLobbyRequest()
        {
            // Called by another faction slot other than the host? deny it.
            if (Role != FactionSlotRole.host)
                return;

            ErrorMessage startLobbyError = multiplayerMgr.StartLobby();

            RpcOnStartLobbyReply(startLobbyError);
        }

        [ClientRpc]
        private void RpcOnStartLobbyReply(ErrorMessage errorMessage)
        {
            switch(errorMessage)
            {
                case ErrorMessage.none:

                    // Disable allowing any input on all faction slots and wait for the game to start.
                    foreach(ILobbyFactionSlot slot in lobbyMgr.FactionSlots)
                        slot.SetInteractable(false);

                    lobbyUIMgr.SetInteractable(false);

                    playerMessageUIHandler.Message.Display("Starting game...");
                    break;

                case ErrorMessage.lobbyMinSlotsUnsatisfied:
                case ErrorMessage.lobbyMaxSlotsUnsatisfied:

                    if(Role == FactionSlotRole.host)
                        playerMessageUIHandler.Message.Display($"Amount of faction slots must be between {lobbyMgr.CurrentMap.factionsAmount.min} and {lobbyMgr.CurrentMap.factionsAmount.max}!");
                    break;

                case ErrorMessage.lobbyPlayersNotAllReady:

                    if(Role == FactionSlotRole.host)
                        playerMessageUIHandler.Message.Display($"Not all faction slots are ready!");
                    break;

                default:

                    // Only display failure error to the host since it was the one that attempted to start the game.
                    if(Role == FactionSlotRole.host)
                        playerMessageUIHandler.Message.Display(errorMessage.ToString(), MessageType.error);
                    break;
            }
        }

        public void OnStartLobbyInterrupted()
        {
            // Only called via the server.
            if (!multiplayerMgr.IsServerOnly)
                return;

            RpcHandleStartLobbyInterrupted();
        }

        [ClientRpc]
        private void RpcHandleStartLobbyInterrupted()
        {
            // Allow players to edit their faction slots again.
            lobbyMgr.LocalFactionSlot.SetInteractable(true);
        }
        #endregion

        #region Handling Active Game
        private void HandleMultiplayerFactionManagerValidated(IMultiplayerFactionManager newMultiFactionMgr, EventArgs args)
        {
            if (!multiplayerMgr.CurrentGameMgr.IsValid())
                return;

            this.MultiplayerFactionMgr = newMultiFactionMgr;
        }

        public void OnGameBuilt(IFactionSlot gameFactionSlot)
        {
            this.GameFactionSlot = gameFactionSlot;
        }
        #endregion
    }
}
