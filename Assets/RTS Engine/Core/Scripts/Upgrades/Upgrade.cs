using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
    public abstract class Upgrade : MonoBehaviour, IMonoBehaviour
    {
        [SerializeField, EnforceType(typeof(IEntity)), Tooltip("Source entity whose upgrade is handled by this component (required for entity component upgrades but optional for entity upgrades).")]
        private GameObject sourceEntity = null;
        public IFactionEntity SourceEntity => sourceEntity.IsValid() ? sourceEntity.GetComponent<IFactionEntity>() : null;
        public abstract string SourceCode { get; }

        [Space(), SerializeField, Tooltip("Upgrade only the source instance that has this component attached to it?")]
        private bool sourceInstanceOnly = false;
        public bool SourceInstanceOnly => sourceInstanceOnly;

        [SerializeField, Tooltip("Upgrade already spawned instances?")]
        private bool updateSpawnedInstances = true;
        public bool UpdateSpawnedInstances => updateSpawnedInstances;

        [Space(), SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Effect shown when spawned instances are upgraded.")]
        private GameObject upgradeEffect = null;
        public IEffectObject UpgradeEffect => upgradeEffect.IsValid() ? upgradeEffect.GetComponent<IEffectObject>() : null;

        [Space(), SerializeField, Tooltip("Upgrades to trigger/launch when this upgrade is completed.")]
        private Upgrade[] triggerUpgrades = new Upgrade[0];
        public IEnumerable<Upgrade> TriggerUpgrades => triggerUpgrades;

        // Why provide the IGameManager instance as an input? Because Upgrade components are not always attached to spawned instances but sometimes to prefabs
        public abstract void LaunchLocal(IGameManager gameMgr, int factionID);
    }
}
