using System;

using RTSEngine.Entities;

namespace RTSEngine.Selection
{
    public class BuildingSelection : EntitySelection
    {
        #region Attributes
        protected IBuilding building { private set; get; }

        protected override bool extraSelectCondition => !building.IsPlacementInstance;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            building = Entity as IBuilding;

            building.EntityInitiated += HandleEntityInitiated;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            building.EntityInitiated -= HandleEntityInitiated;
        }
        #endregion

        #region Handling Event: Entity Initiated
        private void HandleEntityInitiated(IEntity entity, EventArgs args)
        {
            // Can only select building after they are placed.
            IsActive = true;
        }
        #endregion
    }
}
