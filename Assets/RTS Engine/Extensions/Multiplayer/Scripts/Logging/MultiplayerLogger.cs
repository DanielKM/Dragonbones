using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Multiplayer.Logging
{
    public class MultiplayerLogger : LoggerBase, IMultiplayerLoggingService
    {
        protected IMultiplayerManager multiplayerMgr { private set; get; }

        public void Init(IMultiplayerManager multiplayerMgr)
        {
            this.multiplayerMgr = multiplayerMgr;
        }
    }
}
