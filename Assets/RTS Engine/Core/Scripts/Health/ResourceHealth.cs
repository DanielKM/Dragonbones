using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Health
{
    public class ResourceHealth : EntityHealth, IResourceHealth
    {
        #region Attributes
        public IResource Resource { private set; get; }
        public override EntityType EntityType => EntityType.resource;

        [SerializeField, Tooltip("Transitional state activated when the first is collected for the first time.")]
        private EntityHealthState collectedState = new EntityHealthState();
        private bool collected = false;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            Resource = Entity as IResource;

            stateHandler.Reset(states, CurrHealth);

            // If the health can not be decreased, meaning that the resource has infinite amount/health
            // Then lock the health value from being updated in the AddLocal method, check the LockHealth definition for more info.
            if (!CanDecrease)
                LockHealth = true;

            collected = false;
        }
        #endregion

        #region Updating Health
        // Allow addition method to pass even if CanDecrease is set to false while updateValue < 0
        // That just means that the resource has infinite amount/health and we make sure that the health does not get decreased by having LockHealth = true on Init.
        public override ErrorMessage CanAdd(int updateValue, IEntity source)
        {
            if (IsDead)
                return ErrorMessage.dead;
            if (updateValue > 0 && !CanIncrease)
                return ErrorMessage.healthNoIncrease;

            return ErrorMessage.none;
        }

        protected override void OnHealthUpdated(int updateValue, IEntity source)
        {
            // If the resource hasn't been collected before now, activate the collected state. This is a unique behaviour for resources.
            if (!collected && updateValue < 0)
            {
                collected = true;
                stateHandler.Activate(collectedState);
            }

            globalEvent.RaiseResourceHealthUpdatedGlobal(Resource, new HealthUpdateEventArgs(updateValue, source));
        }
        #endregion

        #region Destroying Resource
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseResourceDeadGlobal(Resource, new DeadEventArgs(upgrade, source));
        }
        #endregion
    }
}
