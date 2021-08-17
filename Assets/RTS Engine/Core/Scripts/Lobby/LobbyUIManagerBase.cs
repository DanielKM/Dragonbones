using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.UI.Utilities;
using RTSEngine.Logging;
using RTSEngine.Lobby.Logging;

namespace RTSEngine.Lobby
{
    public class LobbyUIManagerBase : MonoBehaviour, ILobbyManagerUI
    {
        #region Attributes
        [SerializeField, Tooltip("Main canvas that is the parent object of all lobby UI elements.")]
        private Canvas canvas = null;

        [SerializeField, Tooltip("Parent object of all the faction lobby slots objects.")]
        private GridLayoutGroup lobbyFactionSlotParent = null; 

        [SerializeField, Tooltip("UI Dropdown Menu used to represent the maps that can be selected in the lobby.")]
        private Dropdown mapDropdownMenu = null;

        [SerializeField, Tooltip("UI Text used to display the selected map's description.")]
        private Text mapDescriptionText = null; 
        [SerializeField, Tooltip("UI Text used to display the selected map's min and max allowed faction amount.")]
        private Text mapFactionAmountText = null;

        [SerializeField, Tooltip("UI Button used to add a faction slot to the lobby.")]
        protected Button addFactionButton = null;

        [SerializeField, Tooltip("UI Button used to start the game.")]
        protected Button startGameButton = null;

        // Services
        protected ILoggingService logger { private set; get; }

        // Other components
        protected ILobbyManager lobbyMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(ILobbyManager manager)
        {
            this.lobbyMgr = manager;

            this.logger = lobbyMgr.GetService<ILobbyLoggingService>();


            if (!logger.RequireValid(canvas, $"[{GetType().Name}] The 'Canvas' field must be assigned")
                || !logger.RequireValid(lobbyFactionSlotParent, $"[{GetType().Name}] The 'Lobby Fation Slot Parent' field must be assigned")
                || !logger.RequireValid(mapDropdownMenu, $"[{GetType().Name}] The 'Map Dropdown menu' field must be assigned"))
                return;

            mapDropdownMenu.ClearOptions();
            mapDropdownMenu.AddOptions(manager.Maps.Select(map => map.name).ToList());

            HandleLobbyGameDataUpdated(manager.CurrentLobbyGameData, EventArgs.Empty);

            manager.FactionSlotAdded += HandleFactionSlotAdded;
            manager.LobbyGameDataUpdated += HandleLobbyGameDataUpdated;

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            lobbyMgr.FactionSlotAdded -= HandleFactionSlotAdded;
            lobbyMgr.LobbyGameDataUpdated -= HandleLobbyGameDataUpdated;

            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }

        public void Toggle(bool show)
        {
            canvas.gameObject.SetActive(show);
        }
        #endregion

        #region General UI Handling
        public void SetInteractable (bool interactable)
        {
            mapDropdownMenu.interactable = interactable;

            lobbyMgr.DefeatConditionSelector.Interactable = interactable;
            lobbyMgr.TimeModifierSelector.Interactable = interactable;
            lobbyMgr.InitialResourcesSelector.Interactable = interactable;

            if (startGameButton.IsValid())
            {
                startGameButton.interactable = interactable;
                startGameButton.gameObject.SetActive(interactable);
            }

            if (addFactionButton.IsValid())
            {
                addFactionButton.interactable = interactable;
                addFactionButton.gameObject.SetActive(interactable);
            }

            OnInteractableUpdate();
        }

        protected virtual void OnInteractableUpdate() { }
        #endregion

        #region Updating Lobby Game Data
        public void OnLobbyGameDataUIUpdated()
        {
            if (!lobbyMgr.IsLobbyGameDataMaster())
                return;

            lobbyMgr.UpdateLobbyGameDataRequest(
                new LobbyGameData
                {
                    mapID = mapDropdownMenu.value,

                    defeatConditionID = lobbyMgr.DefeatConditionSelector.CurrentOptionID,
                    timeModifierID = lobbyMgr.TimeModifierSelector.CurrentOptionID,
                    initialResourcesID = lobbyMgr.InitialResourcesSelector.CurrentOptionID
                });
        }

        private void HandleLobbyGameDataUpdated(LobbyGameData prevLobbyGameData, EventArgs args)
        {
            if(!lobbyMgr.IsLobbyGameDataMaster())
                mapDropdownMenu.value = lobbyMgr.CurrentLobbyGameData.mapID;

            if(mapDescriptionText)
                mapDescriptionText.text = lobbyMgr.CurrentMap.description;
            if(mapFactionAmountText)
                mapFactionAmountText.text = $"{lobbyMgr.CurrentMap.factionsAmount.min} - {lobbyMgr.CurrentMap.factionsAmount.max}";
        }
        #endregion

        #region Updating Faction Slots
        private void HandleFactionSlotAdded(ILobbyFactionSlot newSlot, EventArgs args)
        {
            newSlot.transform.SetParent(lobbyFactionSlotParent.transform, false);
            newSlot.transform.localScale = Vector3.one;
        }
        #endregion
    }
}
