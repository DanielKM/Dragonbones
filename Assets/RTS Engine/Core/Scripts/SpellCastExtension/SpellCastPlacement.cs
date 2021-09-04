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

namespace RTSEngine.SpellCastExtension
{
    public class SpellCastPlacement : MonoBehaviour, ISpellCastPlacement
    {
        #region Attributes
        public struct SpellCastPlacementData
        {
            public SpellCastTask creationTask;
            public ISpell instance;
        }
        private SpellCastPlacementData currentSpell;

        public bool IsPlacingSpell => currentSpell.instance.IsValid();

        [SerializeField, Tooltip("This value is added to the spellcast'S position on the Y axis.")]
        private float spellCastPositionYOffset = 0.01f; 
        public float SpellCastPositionYOffset => spellCastPositionYOffset;

        [SerializeField, Tooltip("The maximum distance that a spellcast and the closest terrain area that it can be placed on can have.")]
        private float terrainMaxDistance = 1.5f; 
        public float TerrainMaxDistance => terrainMaxDistance;

        [SerializeField, Tooltip("Input the terrain areas where spellcast can be placed.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];

        // This would include the layers defined in the placableTerrainAreas
        private LayerMask placableLayerMask = new LayerMask();

        //audio clips
        [SerializeField, Tooltip("Audio clip to play when the player casts a spell.")]
        private AudioClipFetcher placeSpellAudio = new AudioClipFetcher(); 

        [Header("Rotation")]
        [SerializeField, Tooltip("Enable to allow the player to rotate spells while placing them.")]
        private bool canRotate = true;
        [SerializeField, Tooltip("Key used to increment the spell's euler rotation on the y axis.")]
        private ControlType positiveRotationKey = null;
        //private KeyCode positiveRotationKey = KeyCode.H;
        [SerializeField, Tooltip("Key used to decrement the spell's euler rotation on the y axis.")]
        private ControlType negativeRotationKey = null;
        //private KeyCode negativeRotationKey = KeyCode.G;
        [SerializeField, Tooltip("How fast would the spell rotate?")]
        private float rotationSpeed = 1f; 

        [Header("Hold And Spawn")]
        [SerializeField, Tooltip("Enable to allow the player to hold a key to keep placing the same spell type multiple times.")]
        private bool holdAndSpawnEnabled = false;
        [SerializeField, Tooltip("Key used to keep placing the same spell type multiple times when the option to do so is enabled")]
        private ControlType holdAndSpawnKey = null;
        //private KeyCode holdAndSpawnKey = KeyCode.LeftShift;
        [SerializeField, Tooltip("Perserve last spell placement rotation when holding and spawning spells?")]
        private bool perserveSpellRotation = true;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected ISpellCastManager spellCastManager { private set; get; }
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
            this.spellCastManager = gameMgr.GetService<ISpellCastManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            placableLayerMask = new LayerMask();

            if (!logger.RequireTrue(placableTerrainAreas.Length > 0,
              $"[{GetType().Name}] No spell placement terrain areas have been defined in the 'Placable Terrain Areas'. You will not be able to place spells!",
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
            if (!IsPlacingSpell)
                return;

            // Right mouse button stops spell placement
            if (Input.GetMouseButtonUp(1))
            {
                Stop(); 
                return;
            }

            MoveSpell();

            RotateSpell();

            // Left mouse button allows to place the spell
            if (Input.GetMouseButtonUp(0)
                // && currentSpell.instance.PlacerComponent.CanPlace
                && !EventSystem.current.IsPointerOverGameObject()
                )
            {
                if (!Complete())
                {       
                    Debug.Log("broken");
                    // GlobalEvent.RaiseShowPlayerMessageGlobal(
                    //     this,
                    //     new MessageEventArgs(
                    //         type: MessageType.error,
                    //         message: "Spell placement requirements are not met!"
                    //         )
                    //     );
                }
            }
        }

        // Keep moving the spell by following the player's mouse
        private void MoveSpell()
        {
            // Using a raycheck, we will make the current spell follow the mouse position and stay on top of the terrain.
            if (Physics.Raycast(
                mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition),
                out RaycastHit hit,
                Mathf.Infinity,
                placableLayerMask))
            {
                // Depending on the height of the terrain, we will place the spell on it
                Vector3 nextSpellPos = hit.point;

                // Make sure that the spell position on the y axis stays inside the min and max height interval
                nextSpellPos.y += spellCastPositionYOffset;

                if (currentSpell.instance.transform.position != nextSpellPos)
                {
                    currentSpell.instance.transform.position = nextSpellPos;

                    // Check if the spell can be placed in this new position
                    currentSpell.instance.PlacerComponent.OnPositionUpdate();
                }

            }
        }

        private void RotateSpell()
        {
            if (!canRotate)
                return;

            Vector3 nextEulerAngles = currentSpell.instance.transform.rotation.eulerAngles;
            // Only rotate if one of the keys is pressed down (check for direction) and rotate on the y axis only.
            nextEulerAngles.y += rotationSpeed * (controls.Get(positiveRotationKey) ? 1.0f : (controls.Get(negativeRotationKey) ? -1.0f : 0.0f));

            currentSpell.instance.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }
        #endregion

        #region Start, Cancelling & Completing Placement
        private bool Complete()
        {
            ErrorMessage errorMsg;
            // If the spell can not be placed, do not continue and display reason to player with UI message
            if ((errorMsg = currentSpell.creationTask.CanComplete()) != ErrorMessage.none)
            {
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = currentSpell.instance
                });
                return false;
            }

            currentSpell.creationTask.OnComplete();

            spellCastManager.CreatePlacedSpell(
                currentSpell.instance,
                currentSpell.instance.transform.position,
                currentSpell.instance.transform.rotation,
                new InitSpellParameters
                {
                    factionID = currentSpell.instance.FactionID,
                    free = false,

                    setInitialHealth = false,

                    playerCommand = true
                });

            audioMgr.PlaySFX(placeSpellAudio.Fetch(), false);

            Quaternion lastSpellRotation = currentSpell.instance.transform.rotation;

            // To reset the spell placement state
            Stop();

            if (holdAndSpawnEnabled == true && controls.Get(holdAndSpawnKey))
                StartPlacement(
                    currentSpell.creationTask,
                    new SpellCastPlacementOptions 
                    {
                        setInitialRotation = perserveSpellRotation,
                        initialRotation = lastSpellRotation 
                    });

            return true;
        }

        public bool StartPlacement(SpellCastTask creationTask, SpellCastPlacementOptions options = default)
        {
            ErrorMessage errorMsg;
            //if the spell can not be placed, do not continue and display reason to player with UI message
            if ((errorMsg = creationTask.CanStart()) != ErrorMessage.none)
            {
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = currentSpell.instance
                });
                return false;
            }

            creationTask.OnStart();

            currentSpell = new SpellCastPlacementData
            {
                creationTask = creationTask,
                instance = Instantiate(
                    creationTask.Prefab.gameObject, 
                    Vector3.zero,
                    options.setInitialRotation ? options.initialRotation : creationTask.Prefab.transform.rotation
                    ).GetComponent<ISpell>()
            };

            currentSpell.instance.InitPlacementInstance(
                gameMgr,
                new InitSpellParameters
                {
                    factionID = creationTask.Entity.FactionID,
                    free = false,

                    setInitialHealth = false,
                });

            currentSpell.instance.SelectionMarker?.Enable();

            // Set the position of the new spell (and make sure it's on the terrain)
            if (Physics.Raycast(mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                Vector3 nextSpellPos = hit.point;
                nextSpellPos.y += spellCastPositionYOffset;
                currentSpell.instance.transform.position = nextSpellPos;
            }

            currentSpell.instance.PlacerComponent.OnPlacementStart();
            // globalEvent.RaiseSpellPlacementStartGlobal(currentSpell.instance);

            return true;
        }

        public bool Stop()
        {
            if (!IsPlacingSpell)
                return false;

            // globalEvent.RaiseSpellPlacementStopGlobal(currentSpell.instance);

            currentSpell.creationTask.OnCancel();

            // if (currentSpell.instance.IsValid())
            //     currentSpell.instance.Health.DestroyLocal(false, null);

            currentSpell.instance = null;

            return true;
        }
        #endregion
    }
}
