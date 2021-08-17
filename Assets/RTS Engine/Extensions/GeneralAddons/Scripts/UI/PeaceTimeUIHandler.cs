using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI.Utilities;
using RTSEngine.Event;

namespace RTSEngine.UI
{
    public class PeaceTimeUIHandler : MonoBehaviour, IPreRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Handles displaying the peace timer.")]
        private TextMessage message = new TextMessage();

        // Game services
        protected IGameManager gameMgr { private set; get; } 
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.logger = gameMgr.GetService<IGameLoggingService>();

            message.Init(this, logger);
        }

        private void OnDestroy()
        {
            message.Hide();
        }
        #endregion

        #region Displaying/Hiding Peace Time
        private void Update()
        {
            if(!gameMgr.InPeaceTime)
            {
                message.Hide();
                return;
            }

            message.Display(new MessageEventArgs(MessageType.info, RTSHelper.TimeToString(gameMgr.PeaceTimer.CurrValue)));
        }
        #endregion
    }
}
