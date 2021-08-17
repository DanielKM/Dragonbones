using UnityEngine;
using UnityEngine.EventSystems;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.EntityComponent;
using RTSEngine.UI;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Terrain;
using RTSEngine.Audio;
using RTSEngine.Selection;
using RTSEngine.Cameras;
using RTSEngine.Logging;
using RTSEngine.Controls;

namespace RTSEngine.BuildingExtension
{
    public class BuildingPlacement : MonoBehaviour, IBuildingPlacement
    {
        #region Attributes
        public struct BuildingPlacementData
        {
            public BuildingCreationTask creationTask;
            public IBuilding instance;
        }
        private BuildingPlacementData currentBuilding;

        public bool IsPlacingBuilding => currentBuilding.instance.IsValid();

        [SerializeField, Tooltip("This value is added to the building'S position on the Y axis.")]
        private float buildingPositionYOffset = 0.01f; 
        public float BuildingPositionYOffset => buildingPositionYOffset;

        [SerializeField, Tooltip("The maximum distance that a building and the closest terrain area that it can be placed on can have.")]
        private float terrainMaxDistance = 1.5f; 
        public float TerrainMaxDistance => terrainMaxDistance;

        [SerializeField, Tooltip("Input the terrain areas where buildings can be placed.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];

        // This would include the layers defined in the placableTerrainAreas
        private LayerMask placableLayerMask = new LayerMask();

        //audio clips
        [SerializeField, Tooltip("Audio clip to play when the player places a building.")]
        private AudioClipFetcher placeBuildingAudio = new AudioClipFetcher(); 

        [Header("Rotation")]
        [SerializeField, Tooltip("Enable to allow the player to rotate buildings while placing them.")]
        private bool canRotate = true;
        [SerializeField, Tooltip("Key used to increment the building's euler rotation on the y axis.")]
        private ControlType positiveRotationKey = null;
        //private KeyCode positiveRotationKey = KeyCode.H;
        [SerializeField, Tooltip("Key used to decrement the building's euler rotation on the y axis.")]
        private ControlType negativeRotationKey = null;
        //private KeyCode negativeRotationKey = KeyCode.G;
        [SerializeField, Tooltip("How fast would the building rotate?")]
        private float rotationSpeed = 1f; 

        [Header("Hold And Spawn")]
        [SerializeField, Tooltip("Enable to allow the player to hold a key to keep placing the same building type multiple times.")]
        private bool holdAndSpawnEnabled = false;
        [SerializeField, Tooltip("Key used to keep placing the same building type multiple times when the option to do so is enabled")]
        private ControlType holdAndSpawnKey = null;
        //private KeyCode holdAndSpawnKey = KeyCode.LeftShift;
        [SerializeField, Tooltip("Perserve last building placement rotation when holding and spawning buildings?")]
        private bool perserveBuildingRotation = true;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            placableLayerMask = new LayerMask();

            if (!logger.RequireTrue(placableTerrainAreas.Length > 0,
              $"[{GetType().Name}] No building placement terrain areas have been defined in the 'Placable Terrain Areas'. You will not be able to place buildings!",
              type: LoggingType.warning))
                return;
            else if (!logger.RequireValid(placableTerrainAreas,
              $"[{GetType().Name}] 'Placable Terrain Areas' field has some invalid elements!"))
                return; 

            foreach(TerrainAreaType area in placableTerrainAreas)
                placableLayerMask |= area.Layers;
        }

        private void OnDestroy()
        {
        }
        #endregion

        #region Handling Placement Movement/Rotation
        private void Update()
        {
            if (!IsPlacingBuilding)
                return;

            // Right mouse button stops building placement
            if (Input.GetMouseButtonUp(1))
            {
                Stop(); 
                return;
            }

            MoveBuilding();

            RotateBuilding();

            // Left mouse button allows to place the building
            if (Input.GetMouseButtonUp(0)
                && currentBuilding.instance.PlacerComponent.CanPlace
                && !EventSystem.current.IsPointerOverGameObject())
            {
                if (!Complete())
                {
                    globalEvent.RaiseShowPlayerMessageGlobal(
                        this,
                        new MessageEventArgs(
                            type: MessageType.error,
                            message: "Building placement requirements are not met!"
                            )
                        );
                }
            }
        }

        // Keep moving the building by following the player's mouse
        private void MoveBuilding()
        {
            // Using a raycheck, we will make the current building follow the mouse position and stay on top of the terrain.
            if (Physics.Raycast(
                mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition),
                out RaycastHit hit,
                Mathf.Infinity,
                placableLayerMask))
            {
                // Depending on the height of the terrain, we will place the building on it
                Vector3 nextBuildingPos = hit.point;

                // Make sure that the building position on the y axis stays inside the min and max height interval
                nextBuildingPos.y += buildingPositionYOffset;

                if (currentBuilding.instance.transform.position != nextBuildingPos)
                {
                    currentBuilding.instance.transform.position = nextBuildingPos;

                    // Check if the building can be placed in this new position
                    currentBuilding.instance.PlacerComponent.OnPositionUpdate();
                }

            }
        }

        private void RotateBuilding()
        {
            if (!canRotate)
                return;

            Vector3 nextEulerAngles = currentBuilding.instance.transform.rotation.eulerAngles;
            // Only rotate if one of the keys is pressed down (check for direction) and rotate on the y axis only.
            nextEulerAngles.y += rotationSpeed * (controls.Get(positiveRotationKey) ? 1.0f : (controls.Get(negativeRotationKey) ? -1.0f : 0.0f));

            currentBuilding.instance.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }
        #endregion

        #region Start, Cancelling & Completing Placement
        private bool Complete()
        {
            ErrorMessage errorMsg;
            // If the building can not be placed, do not continue and display reason to player with UI message
            if ((errorMsg = currentBuilding.creationTask.CanComplete()) != ErrorMessage.none)
            {
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = currentBuilding.instance
                });
                return false;
            }

            currentBuilding.creationTask.OnComplete();

            buildingMgr.CreatePlacedBuilding(
                currentBuilding.instance,
                currentBuilding.instance.transform.position,
                currentBuilding.instance.transform.rotation,
                new InitBuildingParameters
                {
                    factionID = currentBuilding.instance.FactionID,
                    free = false,

                    setInitialHealth = false,

                    buildingCenter = currentBuilding.instance.PlacerComponent.PlacementCenter,

                    playerCommand = true
                });

            audioMgr.PlaySFX(placeBuildingAudio.Fetch(), false);

            Quaternion lastBuildingRotation = currentBuilding.instance.transform.rotation;

            // To reset the building placement state
            Stop();

            if (holdAndSpawnEnabled == true && controls.Get(holdAndSpawnKey))
                StartPlacement(
                    currentBuilding.creationTask,
                    new BuildingPlacementOptions 
                    {
                        setInitialRotation = perserveBuildingRotation,
                        initialRotation = lastBuildingRotation 
                    });

            return true;
        }

        public bool StartPlacement(BuildingCreationTask creationTask, BuildingPlacementOptions options = default)
        {
            ErrorMessage errorMsg;
            //if the building can not be placed, do not continue and display reason to player with UI message
            if ((errorMsg = creationTask.CanStart()) != ErrorMessage.none)
            {
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = currentBuilding.instance
                });
                return false;
            }

            creationTask.OnStart();

            currentBuilding = new BuildingPlacementData
            {
                creationTask = creationTask,
                instance = Instantiate(
                    creationTask.Prefab.gameObject, 
                    Vector3.zero,
                    options.setInitialRotation ? options.initialRotation : creationTask.Prefab.transform.rotation
                    ).GetComponent<IBuilding>()
            };

            currentBuilding.instance.InitPlacementInstance(
                gameMgr,
                new InitBuildingParameters
                {
                    factionID = creationTask.Entity.FactionID,
                    free = false,

                    setInitialHealth = false,

                    buildingCenter = null,
                });

            currentBuilding.instance.SelectionMarker?.Enable();

            // Set the position of the new building (and make sure it's on the terrain)
            if (Physics.Raycast(mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                Vector3 nextBuildingPos = hit.point;
                nextBuildingPos.y += buildingPositionYOffset;
                currentBuilding.instance.transform.position = nextBuildingPos;
            }

            currentBuilding.instance.PlacerComponent.OnPlacementStart();
            globalEvent.RaiseBuildingPlacementStartGlobal(currentBuilding.instance);

            return true;
        }

        public bool Stop()
        {
            if (!IsPlacingBuilding)
                return false;

            globalEvent.RaiseBuildingPlacementStopGlobal(currentBuilding.instance);

            currentBuilding.creationTask.OnCancel();

            if (currentBuilding.instance.IsValid())
                currentBuilding.instance.Health.DestroyLocal(false, null);

            currentBuilding.instance = null;

            return true;
        }
        #endregion
    }
}
