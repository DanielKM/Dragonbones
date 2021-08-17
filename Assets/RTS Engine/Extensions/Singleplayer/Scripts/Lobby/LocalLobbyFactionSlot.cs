using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Lobby;
using RTSEngine.Event;
using System;
using RTSEngine.Faction;
using RTSEngine.Lobby.Logging;
using RTSEngine.Lobby.UI;

namespace RTSEngine.SinglePlayer.Lobby
{
    public class LocalLobbyFactionSlot : MonoBehaviour, ILobbyFactionSlot
    {
        #region Attributes
        public bool IsInitialized { private set; get; } = false;

        public FactionSlotData Data => new FactionSlotData
        {
            role = Role,

            name = inputData.name,
            color = lobbyMgr.FactionColorSelector.Get(inputData.colorID),

            type = lobbyMgr.CurrentMap.GetFactionType(inputData.factionTypeID),
            npcType = lobbyMgr.CurrentMap.GetNPCType(inputData.npcTypeID),

            isLocalPlayer = Role == FactionSlotRole.host
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

        // Active game
        public IFactionSlot GameFactionSlot { private set; get; }

        // Lobby Services
        protected ILobbyManager lobbyMgr { private set; get; }
        protected ILobbyLoggingService logger { private set; get; }
        protected ILobbyManagerUI lobbyUIMgr { private set; get; }
        protected ILobbyPlayerMessageUIHandler playerMessageUIHandler { private set; get; } 
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

        #region Initializing/Terminating
        public void Init (ILobbyManager lobbyMgr, bool playerControlled)
        {
            this.lobbyMgr = lobbyMgr;

            this.logger = lobbyMgr.GetService<ILobbyLoggingService>(); 
            this.lobbyUIMgr = lobbyMgr.GetService<ILobbyManagerUI>();
            this.playerMessageUIHandler = lobbyMgr.GetService<ILobbyPlayerMessageUIHandler>();

            if (!logger.RequireValid(factionColorImage, $"[{GetType().Name}] The field 'Faction Color Image' is required!")
                || !logger.RequireValid(factionTypeMenu, $"[{GetType().Name}] The field 'Faction Type Menu' is required!")
                || !logger.RequireValid(npcTypeMenu, $"[{GetType().Name}] The field 'NPC Type Menu' is required!")
                || !logger.RequireValid(removeButton, $"[{GetType().Name}] The field 'Remove Button' is required!"))
                return;

            this.lobbyMgr.LobbyGameDataUpdated += HandleLobbyGameDataUpdated;
            this.lobbyMgr.FactionSlotAdded += HandleFactionSlotAddedOrRemoved;
            this.lobbyMgr.FactionSlotRemoved += HandleFactionSlotAddedOrRemoved;

            ResetFactionType(prevMapID:-1);
            ResetNPCType(prevMapID:-1);

            // Every faction slot starts with the same default input data
            ResetInputData();
            RefreshInputDataUI();


            RaiseRoleUpdated(playerControlled ? FactionSlotRole.host : FactionSlotRole.npc);

            // By default, the faction slot is not interactable until it is validated and initialized.
            SetInteractable(true);

            IsInitialized = true;
        }

        private void OnDestroy()
        {
            this.lobbyMgr.LobbyGameDataUpdated -= HandleLobbyGameDataUpdated;
            this.lobbyMgr.FactionSlotAdded -= HandleFactionSlotAddedOrRemoved;
            this.lobbyMgr.FactionSlotRemoved -= HandleFactionSlotAddedOrRemoved;
        }

        private void HandleFactionSlotAddedOrRemoved(ILobbyFactionSlot senderSlot, EventArgs args)
        {
            if (this as ILobbyFactionSlot == senderSlot)
                return;

            // Refresh the interactability of the slot UI elements (such as displaying the remove button when there are enough factions to remove).
            SetInteractable(IsInteractable); 
        }
        public void OnFactionSlotValidated (ILobbyFactionSlot newFactionSlot)
        {
        }
        #endregion

        #region Handling Faction Slot Role
        public void UpdateRoleRequest(FactionSlotRole newRole)
        {
            // Can not update roles in a single player lobby.
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

        private void RefreshInputDataUI ()
        {
            factionNameInput.text = inputData.name;

            factionColorImage.color = lobbyMgr.FactionColorSelector.Get(inputData.colorID);

            factionTypeMenu.value = inputData.factionTypeID;
            npcTypeMenu.value = inputData.npcTypeID;
        }
        #endregion

        #region General UI Handling
        public void SetInteractable (bool interactable)
        {
            factionNameInput.interactable = interactable; 
            factionTypeMenu.interactable = interactable;

            npcTypeMenu.gameObject.SetActive(Role == FactionSlotRole.npc);
            npcTypeMenu.interactable = interactable;

            removeButton.gameObject.SetActive(Role == FactionSlotRole.npc);
            removeButton.interactable = Role == FactionSlotRole.npc && lobbyMgr.CanRemoveFactionSlot(this);

            IsInteractable = interactable;
        }

        #endregion

        #region Updating Lobby Game Data

        public void UpdateLobbyGameDataAttempt(LobbyGameData newLobbyGameData)
        {
            lobbyMgr.UpdateLobbyGameDataComplete(newLobbyGameData);
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
        }
        #endregion

        #region Updating Color
        public void OnFactionColorUpdated ()
        {
            if(!IsInteractable)
                return;

            inputData.colorID = lobbyMgr.FactionColorSelector.GetNextIndex(inputData.colorID);
            factionColorImage.color = lobbyMgr.FactionColorSelector.Get(inputData.colorID);
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
        }
        #endregion

        #region Removing Faction Slot
        public void OnRemove()
        {
            lobbyMgr.RemoveFactionSlotComplete(this);
        }

        public void KickAttempt (int factionSlotID)
        {
            lobbyMgr.RemoveFactionSlotRequest(factionSlotID);
        }
        #endregion

        #region Starting Lobby
        public void OnStartLobbyRequest()
        {
            // Disable allowing any input on all faction slots and wait for the game to start.
            foreach(ILobbyFactionSlot slot in lobbyMgr.FactionSlots)
                slot.SetInteractable(false);

            lobbyUIMgr.SetInteractable(false);
            playerMessageUIHandler.Message.Display("Starting game...");
        }

        public void OnStartLobbyInterrupted()
        {
            // Disable allowing any input on all faction slots and wait for the game to start.
            foreach(ILobbyFactionSlot slot in lobbyMgr.FactionSlots)
                slot.SetInteractable(true);

            lobbyUIMgr.SetInteractable(true);

            playerMessageUIHandler.Message.Display("Game start interrupted!");
        }
        #endregion

        #region Handling Active Game
        public void OnGameBuilt(IFactionSlot gameFactionSlot)
        {
            this.GameFactionSlot = gameFactionSlot;
        }
        #endregion
    }
}
