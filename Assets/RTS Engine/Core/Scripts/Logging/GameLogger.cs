using RTSEngine.Game;

namespace RTSEngine.Logging
{
    public class GameLogger : LoggerBase, IGameLoggingService 
    {
        protected IGameManager gameMgr { private set; get; }

        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }
    }
}
