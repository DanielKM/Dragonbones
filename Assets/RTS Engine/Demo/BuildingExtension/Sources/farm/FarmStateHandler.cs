using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.ResourceExtension;

namespace RTSEngine.Demo
{
    public class FarmStateHandler : MonoBehaviour, IEntityPreInitializable
    {
        #region Attributes
        protected IResource resource { private set; get; }

        [SerializeField, Tooltip("When the height of the crop reaches this distance in relation to the current crop state height, the state will be marked as reached.")]
        private float heightStoppingDistance = 0.02f;

        [System.Serializable]
        public struct FarmState
        {
            [Min(1), Tooltip("Required collected resources amount to trigger the activiation of this state.")]
            public int requiredResources;

            [SerializeField, Tooltip("Assign how crops would behave in this state.")]
            public CropState[] crops;
        }

        [System.Serializable]
        public class CropState
        {
            [SerializeField, Tooltip("Crop game objects that will be shown when this state is active.")]
            private Transform[] show = new Transform[0];
            [SerializeField, Tooltip("Crop game objects that will be hidden when this state is active.")]
            private Transform[] hide = new Transform[0];

            [SerializeField, Tooltip("The target height that the shown crop objects will reach when this state is active.")]
            private float targetHeight = 0.0f;
            [SerializeField, Tooltip("The speed of updating the crop objects' heights.")]
            private TimeModifiedFloat speed = new TimeModifiedFloat(1.0f);
            [SerializeField, Tooltip("Enable to directly snap the height of the crop objects as soon as the state is active.")]
            private bool snapHeight = true;

            private float velocity;

            public void Enable()
            {
                foreach (Transform t in hide)
                    t.gameObject.SetActive(false);

                foreach (Transform t in show)
                {
                    t.gameObject.SetActive(true);

                    if(snapHeight)
                        t.localPosition = new Vector3(t.localPosition.x, targetHeight, t.localPosition.z);
                }
            }

            public bool Update(float heightStoppingDistance)
            {
                if (show.Length == 0
                    || snapHeight)
                    return true;

                float nextHeight = show[0].localPosition.y;

                if(Mathf.Abs(nextHeight - targetHeight) <= heightStoppingDistance)
                    return true;

                nextHeight = Mathf.SmoothDamp(nextHeight, targetHeight, ref velocity, 1/speed.Value);

                foreach (Transform t in show)
                    t.localPosition = new Vector3(t.localPosition.x, nextHeight, t.localPosition.z);

                return false;
            }
        }
        [SerializeField, Tooltip("Assign different states for the farm depending on the amount of the collected resources.")]
        private FarmState[] farmStates = new FarmState[0];

        // Keeps track of the amount of collected resources before the farm state is updated.
        private int lastCollectedResources;

        // State attributes
        private int currStateID;
        private LinkedList<int> targetStateIDs;

        // When set to true, it means that the farm is currently updating its state
        private bool isUpdatingState = false;

        // Moving the collector inside the farm to simulate farming
        [Space(), SerializeField, Tooltip("Positions that the farm resource collector can take while working in the farm.")]
        private Transform[] workingPositions = new Transform[0];
        [SerializeField, Tooltip("Time before changing the collector's farming position.")]
        private float mvtReloadTime = 3.0f;
        private TimeModifiedTimer mvtTimer;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.resource = entity as IResource;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            if (!logger.RequireValid(resource,
              $"[{GetType().Name}] This component must be spawned for a {typeof(IResource).Name} type of entity."))
                return;

            isUpdatingState = false;

            // Initial collector mvt settings
            mvtTimer = new TimeModifiedTimer(mvtReloadTime);

            // Initial crop state settings
            lastCollectedResources = 0;

            currStateID = -1;
            targetStateIDs = new LinkedList<int>();

            if((entity as IBuilding).IsPlacementInstance)
            {
                enabled = false;
                return;
            }    

            TryAddNextState(force: true);

            resource.Health.EntityHealthUpdated += HandleResourceHealthUpdate;
        }

        public void Disable()
        {
            resource.Health.EntityHealthUpdated -= HandleResourceHealthUpdate;
        }
        #endregion

        #region Updating Farm State
        private void TryAddNextState(bool force = false)
        {
            int nextStateID = currStateID.GetNextIndex(farmStates);

            if (farmStates.Length < 2
                || (!force && lastCollectedResources < farmStates[nextStateID].requiredResources))
                return;

            if(!force)
                lastCollectedResources -= farmStates[nextStateID].requiredResources;

            // Add the next farm state as the state that the farm is attempting to reach
            targetStateIDs.AddLast(currStateID.GetNextIndex(farmStates));

            if (!isUpdatingState)
                TryMoveNextState();
        }

        private void HandleResourceHealthUpdate(IEntity sender, HealthUpdateEventArgs args)
        {
            // In the case where a resource collector picked this resource
            if(args.Value < 0.0f && (args.Source as IUnit)?.CollectorComponent.IsValid() == true)
            {
                lastCollectedResources -= args.Value;

                // See if we can add the next state as a target for this farm
                TryAddNextState();
            }
        }

        private void Update()
        {
            if (resource.WorkerMgr.Amount <= 0)
                return;

            if(workingPositions.Length > 0 && mvtTimer.ModifiedDecrease())
            {
                IUnit collector = resource.WorkerMgr.Workers.First();

                if (collector.IsValid()
                    && (!collector.DropOffSource.IsValid() || collector.DropOffSource.State == DropOffState.inactive))
                    {
                    collector.MovementComponent.SetTarget(
                        workingPositions[UnityEngine.Random.Range(0, workingPositions.Length)].position,
                        stoppingDistance: 0.0f,
                        new MovementSource
                        {
                            component = collector.CollectorComponent,

                            playerCommand = false
                        });

                    collector.MovementComponent.UpdateRotationTarget(resource, resource.transform.position);
                }

                mvtTimer.Reload();
            }

            if(isUpdatingState 
                && farmStates[currStateID].crops.All(crop => crop.Update(heightStoppingDistance)))
                TryMoveNextState();
        }

        private void TryMoveNextState()
        {
            if (farmStates.Length < 2
                || targetStateIDs.Count == 0)
            {
                isUpdatingState = false;
                return;
            }

            currStateID = targetStateIDs.First();
            targetStateIDs.RemoveFirst();

            foreach (CropState crop in farmStates[currStateID].crops)
                crop.Enable();

            isUpdatingState = true;
        }
        #endregion
    }
}
