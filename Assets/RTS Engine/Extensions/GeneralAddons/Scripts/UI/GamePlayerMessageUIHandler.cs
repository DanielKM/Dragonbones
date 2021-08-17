
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Audio;

namespace RTSEngine.UI
{
    public class GamePlayerMessageUIHandler : PlayerMessageUIHandlerBase, IGamePlayerMessageUIHandler 
    {
        #region Attributes
        // Game Services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            InitBase(logger: gameMgr.GetService<IGameLoggingService>(),
                audioMgr: gameMgr.GetService<IGameAudioManager>());

            globalEvent.ShowPlayerMessageGlobal += HandleShowPlayerMessageGlobal;
        }

        protected override void OnDisabled()
        {
            globalEvent.ShowPlayerMessageGlobal -= HandleShowPlayerMessageGlobal;
        }
        #endregion

        #region Handling Event: Show Player Message
        private void HandleShowPlayerMessageGlobal(object sender, MessageEventArgs args)
        {
            DisplayMessage(args);
        }
        #endregion
    }
}
