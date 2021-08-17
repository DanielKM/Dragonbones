using RTSEngine.EntityComponent;

namespace RTSEngine.NPC.Upgrades
{
    public interface INPCUpgradeManager
    {
        bool OnUpgradeLaunchRequest(IUpgradeLauncher upgradeLauncher, int upgradeTaskID);
    }
}