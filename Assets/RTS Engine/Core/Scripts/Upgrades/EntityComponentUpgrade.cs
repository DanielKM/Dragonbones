using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
    public class EntityComponentUpgrade : Upgrade
    {
        [Space(), SerializeField, EntityComponentCode(), Tooltip("Code of the component to upgrade (if there is one).")]
        private string sourceComponentCode = "component_code";
        public override string SourceCode => SourceComponent.IsValid() ? SourceComponent.Code : "";
        public IEntityComponent SourceComponent
        {
            get
            {
                RTSHelper.TryGetEntityComponentWithCode(SourceEntity, sourceComponentCode, out IEntityComponent component);
                return component;
            }
        }

        [SerializeField, EnforceType(typeof(IEntityComponent)), Tooltip("The upgrade entity component target.")]
        private GameObject upgradeTarget = null;
        public IEntityComponent UpgradeTarget => upgradeTarget.IsValid() ? upgradeTarget.GetComponent<IEntityComponent>() : null;

        public override void LaunchLocal(IGameManager gameMgr, int factionID)
        {
            gameMgr.GetService<IEntityComponentUpgradeManager>().LaunchLocal(this, factionID);
        }
    }
}
