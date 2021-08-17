using RTSEngine.EntityComponent;

namespace RTSEngine.NPC.EntityComponent
{
    public class NPCEntityComponentTracker : NPCComponentBase, INPCEntityComponentTracker
    {
        #region Attributes
        public IEntityComponentTracker<IUnitCreator> UnitCreatorTracker { private set; get; }
        public IEntityComponentTracker<IUpgradeLauncher> UpgradeLauncherTracker { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            UnitCreatorTracker = new PendingTaskEntityComponentTracker<IUnitCreator>(gameMgr, factionMgr);
            UpgradeLauncherTracker = new PendingTaskEntityComponentTracker<IUpgradeLauncher>(gameMgr, factionMgr);
        }

        protected override void OnDestroyed()
        {
            UnitCreatorTracker.Disable();
            UpgradeLauncherTracker.Disable();
        }
        #endregion
    }
}
