using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
    public interface IEntityUpgradeManager : IPreRunGameService
    {
        bool TryGet(int factionID, out UpgradeElement<IEntity>[] upgradeElements);

        bool IsLaunched(EntityUpgrade upgrade, int factionID);

        ErrorMessage LaunchLocal(EntityUpgrade upgrade, int factionID);
    }
}