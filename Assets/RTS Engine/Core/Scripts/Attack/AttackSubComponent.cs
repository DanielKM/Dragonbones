using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public abstract class AttackSubComponent
    {
        protected IAttackComponent source { private set; get; }

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 

        public void Init (IGameManager gameMgr, IAttackComponent source)
        {
            this.gameMgr = gameMgr;
            this.source = source;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            OnInit();
        }

        protected virtual void OnInit() { }
    }
}
