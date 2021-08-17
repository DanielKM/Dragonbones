using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
    public interface IEntityComponentUpgradeManager : IPreRunGameService
    {
        bool TryGet(IEntity entity, int factionID, out List<UpgradeElement<IEntityComponent>> componentUpgrades);

        bool IsLaunched(EntityComponentUpgrade upgrade, int factionID);

        ErrorMessage LaunchLocal(EntityComponentUpgrade upgrade, int factionID);
    }
}