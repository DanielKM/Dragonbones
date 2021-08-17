using System.Collections;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Health
{
    public abstract class FactionEntityHealth : EntityHealth, IFactionEntityHealth
    {
        #region Attributes
        public IFactionEntity FactionEntity { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            FactionEntity = Entity as IFactionEntity;
        }
        #endregion

        #region Updating Health
        public override ErrorMessage CanAdd (int updateValue, IEntity source)
        {
            if (IsDead)
                return ErrorMessage.dead;
            if (updateValue > 0 && !CanIncrease)
                return ErrorMessage.healthNoIncrease;
            else if (updateValue < 0 && !CanDecrease)
                return ErrorMessage.healthNoDecrease;

            return ErrorMessage.none;
        }
        protected override void OnHealthUpdated(int updateValue, IEntity source)
        {
            globalEvent.RaiseFactionEntityHealthUpdatedGlobal(FactionEntity, new HealthUpdateEventArgs(updateValue, source));
        }
        #endregion

        #region Destroying Faction Entity
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseFactionEntityDeadGlobal(FactionEntity, new DeadEventArgs(upgrade, source));
        }
        #endregion

        #region Handling Damage Over Time
        public void AddDamageOverTime (DamageOverTimeData dotData, int damage, IEntity source)
        {
            StartCoroutine(DamageOverTime(dotData, damage, source));
        }

        private IEnumerator DamageOverTime (DamageOverTimeData dotData, int damage, IEntity source)
        {
            float duration = dotData.duration;
            while(true)
            {
                Add(-damage, source);

                yield return new WaitForSeconds(dotData.cycleDuration);

                if(!dotData.infinite)
                {
                    duration -= dotData.duration;
                    if (duration <= 0.0f)
                        yield break;
                }
            }
        }
        #endregion
    }
}
