using System;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI.Utilities;

namespace RTSEngine.UI
{
    public class TooltipUIHandler : MonoBehaviour, IPreRunGameService, IMonoBehaviour
    {
        #region Attributes
        [SerializeField, Tooltip("Handles displaying the player message.")]
        private TextMessage message = new TextMessage();

        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            message.Init(this, logger);

            globalEvent.ShowTooltipGlobal += HandleShowTooltipGlobal;
            globalEvent.HideTooltipGlobal += HandleHideTooltipGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.ShowTooltipGlobal -= HandleShowTooltipGlobal;
            globalEvent.HideTooltipGlobal -= HandleHideTooltipGlobal;
        }
        #endregion

        #region Handling Events: Show/Hide Tooltip
        private void HandleShowTooltipGlobal(object sender, MessageEventArgs args)
        {
            message.Display(args);
        }

        private void HandleHideTooltipGlobal(object sender, EventArgs e)
        {
            message.Hide();
        }
        #endregion
    }
}
