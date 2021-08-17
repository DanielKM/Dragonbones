using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Health;
using RTSEngine.Search;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackDamage : AttackSubComponent
    {
        #region Attributes
        [SerializeField, Tooltip("Deal damage to targets? If disabled, the attack will still be carried and custom events are triggered but no damage will be dealt.")]
        private bool enabled = true;

        [SerializeField, Tooltip("Data that defines the damage to deal in a regular attack.")]
        private DamageData data = new DamageData { unit = 10, building = 10, custom = new CustomDamageData[0] };
        public DamageData Data => data;

        [SerializeField, Tooltip("Enable or disable area of attack damage.")]
        private bool areaAttackEnabled = false; 
        [SerializeField, Tooltip("When area of attack is enabled, this defines the ranges and damage values per range. Define the elements of this field with increasing range size.")]
        private DamageRangeData[] areaAttackData = new DamageRangeData[0];
        public IEnumerable<DamageRangeData> AreaAttackData => areaAttackData;

        [SerializeField, Tooltip("Enable or disable damage over time.")]
        private bool dotEnabled = false; 
        [SerializeField, Tooltip("When damage over time is enabled, this defines the parameters of the DoT.")]
        private DamageOverTimeData dotData = new DamageOverTimeData();
        public DamageOverTimeData DotData => dotData;

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Effect object that appears on the target when the attack is triggered.")]
        private GameObject effect = null;

        [SerializeField, Tooltip("Enable to reset the logged dealt damage value everytime damage is dealt to a new target.")]
        private bool resetDamageDealt = false;

        /// <summary>
        /// Last target that was dealt damage.
        /// </summary>
        public IFactionEntity LastTarget { private set; get; } = null;
        /// <summary>
        /// Amount of damage dealt.
        /// </summary>
        public int DamageDealt { private set; get; } = 0;

        // Game services
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; } 
        #endregion

        #region Raising Events
        [SerializeField, Tooltip("Triggered when damage is dealt to a target.")]
        private UnityEvent damageDealtEvent = null;

        public CustomEventHandler<IAttackComponent, HealthUpdateEventArgs> AttackDamageDealt;

        public void RaiseAttackDamageDealt (HealthUpdateEventArgs e)
        {
            var handler = AttackDamageDealt;
            handler?.Invoke(source, e);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>(); 
        }
        #endregion

        #region Updating Damage Values
        // Make sure that the calls to these methods are syncable to all clients
        public void UpdateDamage(DamageData newDamageData)
        {
            this.data = newDamageData;
        }

        public void UpdateAreaAttackDamage(DamageRangeData[] newAreaAttackDamageData)
        {
            if (newAreaAttackDamageData == null)
                return;

            this.areaAttackData = newAreaAttackDamageData;
        }

        public void UpdateDotData(DamageOverTimeData newDotData)
        {
            this.dotData = newDotData;
        }
        #endregion

        #region Triggering Damage
        public void Trigger (IFactionEntity target, Vector3 targetPosition)
        {
            if (areaAttackEnabled == true) 
                TriggerArea(target.IsValid() ? target.transform.position : targetPosition, sourceFactionID: source.Entity.FactionID);
            // Apply damage directly
            else if(target.IsValid())
                Deal(target, data.Get(target));
        }

        private void TriggerArea (Vector3 center, int sourceFactionID)
        {
            gridSearch.Search(
                center,
                areaAttackData[areaAttackData.Length - 1].range,
                -1,
                source.IsTargetValid,
                // Set to true because we do not want to tie the target to the LOS parameters.
                playerCommand: true, 
                out IEnumerable<IFactionEntity> targetsInRange);

            foreach(IFactionEntity target in targetsInRange)
            {
                float distance = Vector3.Distance(target.transform.position, center);

                for (int i = 0; i < areaAttackData.Length; i++)
                {
                    // As long as the right range for this faction entity isn't found, move to the next one
                    if (distance > areaAttackData[i].range) 
                        continue;

                    Deal(target, areaAttackData[i].data.Get(target)); 

                    // Area attack range found, move to the next target.
                    break;
                }
            }
        }
        #endregion

        #region Dealing Damage
        private void Deal (IFactionEntity target, int value)
        {
            if (enabled == false || target == null) // Can't deal damage then stop here
                return;

            if (dotEnabled == true) 
                target.Health.AddDamageOverTime(dotData, value, source.Entity);
            else
                target.Health.Add(-value, source.Entity);

            effectObjPool.Spawn(effect, target.transform.position, Quaternion.identity, target.transform);

            // If this is a new target to deal damage to and the damage dealt is only logged for a single target
            if (resetDamageDealt && target != LastTarget)
                DamageDealt = 0;

            DamageDealt += value;

            LastTarget = target;

            RaiseAttackDamageDealt(new HealthUpdateEventArgs(value, target));
            damageDealtEvent.Invoke();
        }
        #endregion
    }
}
