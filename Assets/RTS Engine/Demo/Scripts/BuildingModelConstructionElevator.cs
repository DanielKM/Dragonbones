using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Determinism;

namespace RTSEngine.Demo
{
    public class BuildingModelConstructionElevator : MonoBehaviour, IEntityPreInitializable
    {
        #region Attributes
        public bool IsInitialized { private set; get; } = false;

        protected IBuilding building { private set; get; }

        [SerializeField, Tooltip("Maximum height (position on the y axis) that the building model can reach.")]
        private float maxHeight = 2.0f; 

        [SerializeField, Tooltip("The height (position on the y axis) that the building model start with.")]
        private float initialHeight = -1.0f; 
        // The position on the y axis that the construction model attempts to reach.
        private float targetHeight;

        [SerializeField, Tooltip("How fast is the movement of elevating the building model?")]
        private TimeModifiedFloat speed = new TimeModifiedFloat(1.0f);
        // Required for SmoothDamp
        private float currentVelocity = 0.0f;
        #endregion

        #region Initializing/Terminating
        private void Start()
        {
            // If the Start() Unity message is called while this entity service was not initialized (since it iniitalizes post entity init and that is only after building placement for buildings).
            // We keep this component inactive so the FixedUpdate() method is not called
            if (!IsInitialized)
                enabled = false;
        }

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.building = entity as IBuilding;

            targetHeight = initialHeight;
            maxHeight -= initialHeight;

            Toggle(false);

            if (building.IsPlacementInstance || building.IsBuilt)
                return;

            building.EntityInitiated += HandleEntityInitiated;
            building.BuildingBuilt += HandleBuildingBuilt;

            IsInitialized = true;
        }

        public void Disable()
        {
            building.EntityInitiated -= HandleEntityInitiated;
            building.BuildingBuilt -= HandleBuildingBuilt;
        }
        #endregion

        #region Handling Event: Building Placed
        private void HandleEntityInitiated(IEntity entity, EventArgs args)
        {
            // Building already consturcted, do not enable this effect
            if (building.IsBuilt)
                return;

            // Start this component when the building is placed.
            Toggle(true);
        }
        #endregion

        #region Handling Event: Building Built
        private void HandleBuildingBuilt(IBuilding sender, EventArgs args)
        {
            // Stop this elevator effect as soon as the building is completely built.
            Toggle(false);
        }
        #endregion

        #region Handling Construction Elvator Effect
        private void FixedUpdate()
        {
            Vector3 nextPosition = building.Model.transform.localPosition;
            nextPosition.y = Mathf.SmoothDamp(nextPosition.y, targetHeight, ref currentVelocity, 1 / speed.Value);

            building.Model.transform.localPosition = nextPosition;
        }

        private void Toggle(bool enable)
        {
            enabled = enable;

            if (!enabled)
            {
                building.Health.EntityHealthUpdated -= HandleEntityHealthUpdated;

                building.Model.transform.localPosition = new Vector3(building.Model.transform.localPosition.x, initialHeight + maxHeight, building.Model.transform.localPosition.z); //set the construction object height to the max

                return;
            }

            // Enabling building construction elevator effect
            building.Model.transform.localPosition = new Vector3(building.Model.transform.localPosition.x, initialHeight, building.Model.transform.localPosition.z);

            building.Health.EntityHealthUpdated += HandleEntityHealthUpdated;
        }

        private void HandleEntityHealthUpdated(IEntity entity, HealthUpdateEventArgs e) => UpdateTargetHeight();

        private void UpdateTargetHeight()
        {
            targetHeight = building.Health.HealthRatio * maxHeight + initialHeight;
        }
        #endregion
    }
}
