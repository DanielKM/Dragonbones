using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Health
{
    public class BuildingHealth : FactionEntityHealth, IBuildingHealth
    {
        #region Attributes
        public IBuilding Building { private set; get; }
        public override EntityType EntityType => EntityType.building;


        [SerializeField, Tooltip("Possible health states that the building can have while it is being constructed.")]
        private List<EntityHealthState> constructionStates = new List<EntityHealthState>();  

        [SerializeField, Tooltip("State to activate when the building completes construction, a transition state from construction states to regular building states.")]
        private EntityHealthState constructionCompleteState = new EntityHealthState();
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            Building = Entity as IBuilding;

            // Show the construction state only if this is not the placement instance
            // We also check for whether the building has been built or not because in case of a faction conversion, components are re-initiated and this would cause the construction states to appear.
            if(!Building.IsPlacementInstance && !Building.IsBuilt) 
                stateHandler.Reset(constructionStates, CurrHealth);
        }
        #endregion

        #region Updating Health
        protected override void OnHealthUpdated(int updateValue, IEntity source)
        {
            base.OnHealthUpdated(updateValue, source);

            globalEvent.RaiseBuildingHealthUpdatedGlobal(Building, new HealthUpdateEventArgs(updateValue, source));
        }

        protected override void OnMaxHealthReached(int updateValue, IEntity source)
        {
            if(!Building.IsBuilt)
            {
                stateHandler.Activate(constructionCompleteState);

                stateHandler.Reset(states, CurrHealth);
            }

            base.OnMaxHealthReached(updateValue, source);
        }
        #endregion

        #region Destroying Building
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseBuildingDeadGlobal(Building, new DeadEventArgs(upgrade, source));
        }
        #endregion
    }
}
