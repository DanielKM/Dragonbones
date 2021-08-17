using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Terrain;
using RTSEngine.BuildingExtension;
using RTSEngine.Event;
using RTSEngine.NPC.ResourceExtension;

namespace RTSEngine.NPC.BuildingExtension
{
    public class NPCBuildingPlacer : NPCComponentBase, INPCBuildingPlacer
    {
        #region Attributes
        // The current pending building that's being placed by the NPC faction.
        private NPCPendingBuildingHandler currPendingBuilding;
        // All of next the pending buildings that have been instantiated but the NPC faction has not got to placing them are held here. 
        private Queue<NPCPendingBuildingHandler> pendingBuildings;

        [SerializeField, Tooltip("Define the terrain area types where the NPC faction can place its buildings. Leave empty to allow all terrain areas defined in this map to have buildings placed on them.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];

        [SerializeField, Tooltip("NPC faction will only consider placing each new building after a delay sampled from this range has passed. This allows to introduce randomness into the building placement process of NPC factions.")]
        private FloatRange placementDelayRange = new FloatRange(7.0f, 20.0f);
        private float placementDelayTimer;

        [SerializeField, Tooltip("NPC pending building placement is a process where the building rotates around a build around position and gradually moves away from it until an appropriate placement position is found. This field represents how fast will the building rotation speed be?")]
        private float rotationSpeed = 50.0f;

        [SerializeField, Tooltip("Time before the NPC faction decides to try another position to place the building at. Currently, this component just moves the to be placed building from its build around position a distance that can keep specified in the 'Move Distance' field.")]
        private FloatRange placementMoveReload = new FloatRange(8.0f, 12.0f);
        private float placementMoveTimer;
        [SerializeField, Tooltip("Every time the 'Placement Move Reload' time is through, the pending building will be moved away from its build around position by a distance sampled from this field.")]
        private FloatRange moveDistance = new FloatRange(0.5f, 1.5f);

        [SerializeField, Tooltip("Each time the NPC faction attempts another position to place a building, this value is added to the 'Placement Mvt Reload' field0")]
        private FloatRange placementMoveReloadInc = new FloatRange(1.5f, 2.5f);
        //this will be added to the move timer each time the building moves.
        private int placementMoveReloadIncCount = 0;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("How often is the height of a building sampled from the terrain's height per second? We do not need to do this every frame as it may become an expensive computation but we can do it often enough to get good results. This depends on the height variations in your map, so the more height variations you have, the more often you want to sample the height of the map when placing buildings.")]
        private float heightCheckReload = 0.2f;
        // This coroutine is running as long as there's a building to be placed and it allows NPC factions to place buildings on different heights
        private IEnumerator heightCheckCoroutine;
        private float rotationMultiplier;

        // NPC components
        protected INPCResourceManager npcResourceMgr { private set; get; }

        // Other components
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            npcResourceMgr = npcMgr.GetNPCComponent<INPCResourceManager>();

            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();

            pendingBuildings = new Queue<NPCPendingBuildingHandler>();
        }

        protected override void OnPostInit()
        {
            globalEvent.BuildingUpgradedGlobal += HandleBuildingUpgradedGlobal;

            globalEvent.BuildingDeadGlobal += HandleBuildingDeadGlobal;
        }

        protected override void OnDestroyed()
        {
            globalEvent.BuildingUpgradedGlobal -= HandleBuildingUpgradedGlobal;

            globalEvent.BuildingDeadGlobal -= HandleBuildingDeadGlobal;
        }
        #endregion

        #region Handling Event: Building Dead
        private void HandleBuildingDeadGlobal(IBuilding building, DeadEventArgs args)
        {
            if (!factionMgr.IsSameFaction(building)
                || !building.BorderComponent.IsValid())
                return;

            // Consider building centers that are destroyed and remove all pending buildings that are supposed to be placed around them
            StopPlacingAllBuildings(pendingBuildingHandler => pendingBuildingHandler.BuildingCenter == building);
        }
        #endregion

        #region Handling Event: Building Upgrades
        private void HandleBuildingUpgradedGlobal(IBuilding building, UpgradeEventArgs<IEntity> args)
        {
            if (!factionMgr.IsSameFaction(args.FactionID))
                return;

            // In case a building type that is scheduled to be placed has been upraded then destroy it and allow the NPC faction to place the upgraded instance
            StopPlacingAllBuildings(pendingBuildingHandler => pendingBuildingHandler.Instance.Code == building.Code);
        }
        #endregion

        #region Requesting Building Placement
        public bool OnBuildingPlacementRequest(BuildingCreationTask creationTask, IBuilding buildingCenter, IEnumerable<BuildingPlaceAroundData> placeAroundDataSet, bool canRotate)
        {
            // If the building center hasn't been specified, do not proceed.
            if (!logger.RequireTrue(buildingCenter.IsValid(),
                $"[{GetType().Name} - {factionMgr.FactionID}] Building Center for building prefab '{creationTask.Prefab.Code}' hasn't been specified in the Building Placement Request!"))
                return false;

            ErrorMessage errorMessage;
            if ((errorMessage = creationTask.CanStart()) != ErrorMessage.none)
            {
                switch(errorMessage)
                {
                    case ErrorMessage.taskMissingResourceRequirements:
                        npcResourceMgr.OnIncreaseMissingResourceRequest(creationTask.RequiredResources);
                        break;
                }

                return false;
            }

            creationTask.OnStart();

            IBuilding placementInstance = Instantiate(creationTask.Prefab.gameObject, Vector3.zero, creationTask.Prefab.transform.rotation).GetComponent<IBuilding>();
            // Initialize the building instance for placement
            placementInstance.InitPlacementInstance(
                gameMgr,
                new InitBuildingParameters
                {
                    factionID = factionMgr.FactionID,
                    free = false,

                    setInitialHealth = false,

                    buildingCenter = buildingCenter.BorderComponent,
                });

            NPCPendingBuildingHandler newPendingBuildingHandler = new NPCPendingBuildingHandler(
                gameMgr,
                creationTask,
                placementInstance,
                buildingCenter,
                placeAroundDataSet,
                canRotate);

            // We need to hide the building initially, when its turn comes to be placed, it will be activated to be able to receive collision.
            newPendingBuildingHandler.Instance.gameObject.SetActive(false);
            newPendingBuildingHandler.Instance.Model.SetActive(false);

            globalEvent.RaiseBuildingPlacementStartGlobal(newPendingBuildingHandler.Instance);

            pendingBuildings.Enqueue(newPendingBuildingHandler);

            // If there is no current building being placed then activate this component to start placing this newly added pending building
            if (!IsActive)
                StartPlacingNextBuilding();

            return true;
        }
        #endregion

        #region Starting/Stopping Building Placement - Activating/Deactivating Component
        private void StartPlacingNextBuilding()
        {
            if (pendingBuildings.Count == 0)
            {
                StopPlacingBuilding();
                return;
            }

            IsActive = true;

            // Start placing the next pending building
            currPendingBuilding = pendingBuildings.Dequeue();

            // If we are unable to set the place around data for the next pending building then we discard it and move to the next one.
            // And make sure that the building is allowed inside its currently assigned border
            if (!currPendingBuilding.TrySetNextPlaceAroundData()
                || !currPendingBuilding.Instance.PlacerComponent.IsBuildingInBorder())
            {
                StopPlacingBuilding();
                return;
            }

            currPendingBuilding.Instance.gameObject.SetActive(true);

            currPendingBuilding.Instance.PlacerComponent.OnPlacementStart();

            ResetMovementState();

        }

        private void ResetMovementState ()
        {
            // Reset building rotation/movement state:
            placementDelayTimer = placementDelayRange.RandomValue;

            placementMoveTimer = placementMoveReload.RandomValue;
            placementMoveReloadIncCount = 0;

            rotationMultiplier = UnityEngine.Random.value > 0.5f ? 1 : -1;
        }

        protected override void OnActivtated()
        {
            // Start the height check coroutine to keep the building always on top of the terrain.
            // This coroutine is only active when this component is placing a pending building.
            if (heightCheckCoroutine.IsValid())
                return;

            heightCheckCoroutine = HeightCheck(heightCheckReload);
            StartCoroutine(heightCheckCoroutine);
        }

        private void StopPlacingBuilding()
        {
            if (!IsActive)
                return;

            // In case a pending building was being placed by this component
            if (currPendingBuilding.IsValid())
            {
                currPendingBuilding.CreationTask.OnCancel();

                globalEvent.RaiseBuildingPlacementStopGlobal(currPendingBuilding.Instance);

                Destroy(currPendingBuilding.Instance.gameObject);
            }

            currPendingBuilding = null;
            IsActive = false;

            // Attempt to start placement of next building if there is one.
            StartPlacingNextBuilding();
        }

        private void StopPlacingAllBuildings (Func<NPCPendingBuildingHandler, bool> condition)
        {
            pendingBuildings = new Queue<NPCPendingBuildingHandler>(pendingBuildings
                .Where(nextPendingBuildingHandler =>
                {
                    if (!condition(nextPendingBuildingHandler))
                        return true;

                    nextPendingBuildingHandler.CreationTask.OnCancel();

                    globalEvent.RaiseBuildingPlacementStopGlobal(nextPendingBuildingHandler.Instance);

                    Destroy(nextPendingBuildingHandler.Instance.gameObject);

                    return false;
                }));

            if (currPendingBuilding.IsValid() && condition(currPendingBuilding))
                StopPlacingBuilding();

        }

        protected override void OnDeactivated()
        {
            // This component is no longer handling placing pending buildings so there is no need to have a height check coroutine
            if(heightCheckCoroutine.IsValid())
                StopCoroutine(heightCheckCoroutine);

            heightCheckCoroutine = null;
        }
        #endregion

        #region Handling Actual Building Placement, Height Check
        protected override void OnActiveUpdate()
        {
            placementDelayTimer -= Time.deltaTime;
            placementMoveTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (!IsActive)
                return;

            // Invalid pending building instance
            if (!currPendingBuilding.IsValid())
            {
                StopPlacingBuilding();
                return;
            }

            // If the pending building leaves the allowed maximum range to the build around position, then try if we can get another place around data to be active for this pending building
            // If none is found then we can discard this pending building and move to the next one.
            if(Vector3.Distance(currPendingBuilding.Instance.transform.position, currPendingBuilding.PlaceAroundPosition) >= currPendingBuilding.MaxPlacementRange)
            {
                if (currPendingBuilding.TrySetNextPlaceAroundData())
                {
                    ResetMovementState();
                    currPendingBuilding.Instance.PlacerComponent.OnPlacementStart();
                }
                else
                    StopPlacingBuilding();

                return;
            }

            if (placementMoveTimer <= 0.0f)
            {
                // Reset timer
                placementMoveTimer = placementMoveReload.RandomValue + (placementMoveReloadInc.RandomValue * placementMoveReloadIncCount);
                placementMoveReloadIncCount++;

                // Move building away from build around position by the defined movement distance
                Vector3 mvtDir = (currPendingBuilding.Instance.transform.position - currPendingBuilding.PlaceAroundPosition).normalized;
                mvtDir.y = 0.0f;
                if (mvtDir == Vector3.zero)
                    mvtDir = new Vector3(1.0f, 0.0f, 0.0f);
                currPendingBuilding.Instance.transform.position += mvtDir * moveDistance.RandomValue;
            }

            // Keep rotating the building around its build around position
            // Make sure to cache the building's local rotation as the 'RotateAround' method will affect that while we need it unchanged
            Quaternion nextBuildingRotation = currPendingBuilding.CanRotate
                ? RTSHelper.GetLookRotation(currPendingBuilding.Instance.transform, currPendingBuilding.PlaceAroundPosition, true)
                : currPendingBuilding.Instance.transform.rotation;

            currPendingBuilding.Instance.transform.RotateAround(currPendingBuilding.PlaceAroundPosition, Vector3.up, rotationMultiplier * rotationSpeed * Time.deltaTime);

            currPendingBuilding.Instance.transform.rotation = nextBuildingRotation;

            // NPC faction is only allowed to finalize building placement when the delay is through
            if (placementDelayTimer <= 0.0f)
            {
                currPendingBuilding.Instance.PlacerComponent.OnPositionUpdate();

                if (currPendingBuilding.Instance.PlacerComponent.CanPlace
                    && currPendingBuilding.IsPlaceAroundValid)
                    FinalizeNextPlacement();
            }
        }

        private IEnumerator HeightCheck(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);

                if (currPendingBuilding.IsValid())
                    currPendingBuilding.Instance.transform.position = new Vector3(
                            currPendingBuilding.Instance.transform.position.x,
                            terrainMgr.SampleHeight(currPendingBuilding.Instance.transform.position, placableTerrainAreas) + placementMgr.BuildingPositionYOffset,
                            currPendingBuilding.Instance.transform.position.z);
            }
        }
        #endregion

        #region Finalizing Building Placement
        private bool FinalizeNextPlacement()
        {
            ErrorMessage errorMessage = ErrorMessage.none;
            if (!currPendingBuilding.IsValid()
                || (errorMessage = currPendingBuilding.CreationTask.CanComplete()) != ErrorMessage.none)
            {
                switch(errorMessage)
                {
                    case ErrorMessage.taskMissingResourceRequirements:
                        npcResourceMgr.OnIncreaseMissingResourceRequest(currPendingBuilding.CreationTask.RequiredResources);
                        break;
                }

                StopPlacingBuilding();

                return false;
            }

            currPendingBuilding.CreationTask.OnComplete();

            buildingMgr.CreatePlacedBuilding(
                currPendingBuilding.CreationTask.Prefab,
                currPendingBuilding.Instance.transform.position,
                currPendingBuilding.Instance.transform.rotation,
                new InitBuildingParameters
                {
                    factionID = factionMgr.FactionID,
                    free = false,

                    setInitialHealth = false,

                    buildingCenter = currPendingBuilding.Instance.PlacerComponent.PlacementCenter,

                    playerCommand = false
                });

            // Destroy the building instance that was supposed to be placed and create a new placed one
            Destroy(currPendingBuilding.Instance.gameObject);

            // Reset the current pending building handler
            currPendingBuilding = null;

            // This will deactivate the component in case there is no other pending building
            StartPlacingNextBuilding();

            return true;
        }
        #endregion

#if UNITY_EDITOR
        [Header("Logs")]
        [SerializeField, ReadOnly, Space()]
        private GameObject[] placementQueue = new GameObject[0];

        protected override void UpdateLogStats()
        {
            if (!currPendingBuilding.IsValid())
            {
                placementQueue = new GameObject[0];
                return;
            }

            placementQueue = Enumerable.Repeat(currPendingBuilding.Instance.gameObject, 1)
                .Concat(pendingBuildings.Select(pendingBuilding => pendingBuilding.Instance.gameObject))
                .ToArray();
        }
#endif
    }
}
