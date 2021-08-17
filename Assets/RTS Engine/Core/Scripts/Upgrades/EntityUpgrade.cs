using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
    [RequireComponent(typeof(IEntity))]
    public class EntityUpgrade : Upgrade
    {
        public override string SourceCode => SourceEntity.IsValid() ? SourceEntity.Code : "";

        [Space(), SerializeField, EnforceType(typeof(IEntity)), Tooltip("The upgrade entity target.")]
        private GameObject upgradeTarget = null;
        public IEntity UpgradeTarget => upgradeTarget.IsValid() ? upgradeTarget.GetComponent<IEntity>() : null;

        [SerializeField, Tooltip("Pick the entity components of the upgrade source that match the upgrade target.")]
        private EntityUpgradeComponentMatcherElement[] entityComponentMatcher = new EntityUpgradeComponentMatcherElement[0];
        public IEnumerable<EntityUpgradeComponentMatcherElement> EntityComponentMatcher => entityComponentMatcher;

        public override void LaunchLocal(IGameManager gameMgr, int factionID)
        {
            gameMgr.GetService<IEntityUpgradeManager>().LaunchLocal(this, factionID);
        }
    }
}
