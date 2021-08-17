using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Demo
{
    public class InfiniteRotation : MonoBehaviour, IEntityPostInitializable
    {
        protected IBuilding building { private set; get; }

        [SerializeField]
        private Transform target = null;

        [SerializeField]
        private Vector3 rotationAngles = Vector3.one;

        [SerializeField]
        private TimeModifiedFloat rotationSpeed = new TimeModifiedFloat(4.0f);

        private bool isActive = false;

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.building = entity as IBuilding;

            building.BuildingBuilt += HandleBuildingBuilt;
        }

        public void Disable()
        {
            isActive = false;

            if(building.IsValid())
                building.BuildingBuilt -= HandleBuildingBuilt;
        }

        private void HandleBuildingBuilt(IBuilding sender, EventArgs args)
        {
            isActive = true;
        }

        private void Update()
        {
            if (!isActive
                || !target.IsValid())
                return;

            target.Rotate(rotationAngles * rotationSpeed.Value * Time.deltaTime);
        }
    }
}
