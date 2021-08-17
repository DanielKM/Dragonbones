using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Health
{
    public class UnitHealth : FactionEntityHealth, IUnitHealth
    {
        #region Attributes
        public IUnit Unit { private set; get; }
        public override EntityType EntityType => EntityType.unit;

        [SerializeField, Tooltip("Stop the unit's movement when it receives damage?"), Header("Unit Health")]
        private bool stopMovingOnDamage = false;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            Unit = Entity as IUnit;

            stateHandler.Reset(states, CurrHealth);
        }
        #endregion

        #region Updating Health
        protected override void OnHealthUpdated(int updateValue, IEntity source)
        {
            base.OnHealthUpdated(updateValue, source);

            if (updateValue < 0)
            {
                if (stopMovingOnDamage)
                    Unit.MovementComponent.Stop();
            }

            globalEvent.RaiseUnitHealthUpdatedGlobal(Unit, new HealthUpdateEventArgs(updateValue, source));
        }
        #endregion

        #region Destroying Unit
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseUnitDeadGlobal(Unit, new DeadEventArgs(upgrade, source));
        }
        #endregion
    }
}
