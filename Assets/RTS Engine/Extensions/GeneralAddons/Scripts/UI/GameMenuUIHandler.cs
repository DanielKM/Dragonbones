using System;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Controls;

namespace RTSEngine.UI
{
    public class GameMenuUIHandler : MonoBehaviour, IPreRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Shown when the local player wins the game.")]
        private GameObject winMenu = null; 
        [SerializeField, Tooltip("Shown when the local player loses the game.")]
        private GameObject loseMenu = null; 

        [SerializeField, Tooltip("Shown when the local player pauses the game.")]
        private GameObject pauseMenu = null;
        [SerializeField, Tooltip("Key used to toggle the pause menu during the game.")]
        private ControlType pauseKey = null;

        [SerializeField, Tooltip("Shown when a multiplayer game is frozen.")]
        private GameObject freezeMenu = null;

        // Game services
        protected IGameManager gameMgr { private set; get; } 
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameControlsManager controls { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.controls = gameMgr.GetService<IGameControlsManager>(); 

            globalEvent.GameStateUpdatedGlobal += HandleGameStateUpdatedGlobal;
        }

        private void OnDestroy()
        {
            Disable();
        }
        #endregion

        #region Disabling Handler
        public void Disable()
        {
            globalEvent.GameStateUpdatedGlobal -= HandleGameStateUpdatedGlobal;
        }
        #endregion

        #region Handling Pause Menu
        public void TogglePauseMenu ()
        {
            if (gameMgr.State == GameStateType.running)
                gameMgr.SetState(GameStateType.pause);
            else if (gameMgr.State == GameStateType.pause)
                gameMgr.SetState(GameStateType.running);
        }

        private void Update()
        {
            if (controls.GetDown(pauseKey))
                TogglePauseMenu();
        }
        #endregion

        #region Handling Event: Game State Updated
        private void HandleGameStateUpdatedGlobal(IGameManager sender, EventArgs e)
        {
            UpdateMenu();
        }

        private void UpdateMenu ()
        {
            winMenu.SetActive(gameMgr.State == GameStateType.won);
            loseMenu.SetActive(gameMgr.State == GameStateType.lost);
            pauseMenu.SetActive(gameMgr.State == GameStateType.pause);
            freezeMenu.SetActive(gameMgr.State == GameStateType.frozen);
        }
        #endregion
    }
}
