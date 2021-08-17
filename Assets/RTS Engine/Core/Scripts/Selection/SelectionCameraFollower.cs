using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Entities;
using RTSEngine.Cameras;
using RTSEngine.BuildingExtension;
using RTSEngine.Controls;

namespace RTSEngine.Selection
{
    public class SelectionCameraFollower : MonoBehaviour, ISelectionCameraFollower
    {
        #region Attributes
        [SerializeField, Tooltip("Enable to allow this component to follow selected entities.")]
        public bool isActive = true;
        [SerializeField, Tooltip("What key does the player need to use to follow a selected entity?")]
        public ControlType key = null;
        [SerializeField, Tooltip("Enable to allow the player to iterate through all of their selected entities and follow them with the camera.")]
        public bool iterate = true;

        // Holds a list of the selected entities to iterate through them on camera follow
        private List<IEntity> followedEntities;
        private IEntity currFollowedEntity;
        // Used to know the next selected entity index to camera follow next
        private int nextFollowIndex;

        // Game services
        protected IMainCameraController mainCameraController { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.controls = gameMgr.GetService<IGameControlsManager>();

            // Initial state:
            Reset();
        }
        #endregion

        #region Handling Event: Followed Entitiy Deselected
        private void HandleCurrentFollowedEntityDeselected(IEntity entity, EventArgs args)
        {
            Reset();
        }
        #endregion

        #region Handling Following Selected Entities
        private void Update()
        {
            if (!isActive
                || placementMgr.IsPlacingBuilding
                || !controls.GetDown(key)
                || selectionMgr.Count == 0
                || mainCameraController.IsPanning)
                return;

            FollowNextEntity();
        }

        private void FollowNextEntity()
        {
            // First follow order?
            if (!mainCameraController.IsFollowingTarget)
                Reset();

            // Since a new entity will be followed, unsub to the last entity's deselection event
            if(currFollowedEntity.IsValid())
                currFollowedEntity.Selection.Deselected -= HandleCurrentFollowedEntityDeselected;

            // Handling the index of the selected entities to follow
            if (nextFollowIndex >= selectionMgr.Count)
                nextFollowIndex = 0;

            followedEntities = selectionMgr.GetEntitiesList(EntityType.all, exclusiveType: false, localPlayerFaction: false).ToList();

            currFollowedEntity = followedEntities[nextFollowIndex];

            // Follow the next entity
            mainCameraController.SetFollowTarget(currFollowedEntity.transform);

            // Subscribe to the entity's deselection event because we want to stop following as soon as the entity is deselected
            currFollowedEntity.Selection.Deselected += HandleCurrentFollowedEntityDeselected;

            // If we can iterate through selected entities
            if (iterate)
            {
                nextFollowIndex++;
                if (nextFollowIndex >= followedEntities.Count)
                    nextFollowIndex = 0;
            }
        }

        public void Reset()
        {
            followedEntities = null;

            if(currFollowedEntity.IsValid())
                currFollowedEntity.Selection.Deselected -= HandleCurrentFollowedEntityDeselected;
            currFollowedEntity = null;

            nextFollowIndex = 0;

            mainCameraController.SetFollowTarget(null);
        }
        #endregion
    }
}
