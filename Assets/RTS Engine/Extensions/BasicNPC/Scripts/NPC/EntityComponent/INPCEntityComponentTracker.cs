using RTSEngine.EntityComponent;

namespace RTSEngine.NPC.EntityComponent
{
    public interface INPCEntityComponentTracker : INPCComponent
    {
        IEntityComponentTracker<IUnitCreator> UnitCreatorTracker { get; }
        IEntityComponentTracker<IUpgradeLauncher> UpgradeLauncherTracker { get; }
    }
}