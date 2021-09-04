using UnityEngine;
using UnityEngine.EventSystems;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.BuildingExtension;
using RTSEngine.SpellCastExtension;
using RTSEngine.Cameras;
using RTSEngine.Terrain;
using RTSEngine.Task;
using RTSEngine.EntityComponent;
using RTSEngine.UI;
using RTSEngine.Controls;
using RTSEngine.Logging;
using RTSEngine.Search;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RTSEngine.Selection
{
    public class MouseSelector : MonoBehaviour, IMouseSelector
    {
        #region Attributes
        [SerializeField, Tooltip("Enable to allow the player to double click on an entity to select all entities of the same type within a defined range.")]
        private bool enableDoubleClickSelect = true;
        [SerializeField, Tooltip("Entities of the same type within this range of the original double click will be selected."), Min(0.0f)]
        private float doubleClickSelectRange = 10.0f;

        [Space(), SerializeField, Tooltip("Define the key used to select multiple entities when held down.")]
        private ControlType multipleSelectionKey = null;
        public bool MultipleSelectionKeyDown => controls.Get(multipleSelectionKey);

        [Header("Layers"), SerializeField, Tooltip("Input the layer's name to be used for entity selection objects.")]
        private string entitySelectionLayer = "EntitySelection";
        public string EntitySelectionLayer => entitySelectionLayer;

        [SerializeField, Tooltip("Input the terrain areas that are clickable for the player.")]
        private TerrainAreaType[] clickableTerrainAreas = new TerrainAreaType[0];

        // This would incldue the layers defined in the clickableTerrainAreas and entitySelectionLayer
        private LayerMask clickableLayerMask;

        [Header("Selection Flash")]
        [SerializeField, Tooltip("Duration of the selection marker flash.")]
        private float flashTime = 1.0f;
        [SerializeField, Tooltip("How often does the selection marker flash?")]
        private float flashRepeatTime = 0.2f;

        [SerializeField, Tooltip("Color used when the selection marker of a friendly entity is flashing.")]
        private Color friendlyFlashColor = Color.green;
        [SerializeField, Tooltip("Color used when the selection marker of an enemy entity is flashing.")]
        private Color enemyFlashColor = Color.red; 

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameUIManager gameUIMgr { private set; get; } 
        protected ISelectionManager selectionMgr { private set; get; } 
        protected IBuildingPlacement placementMgr { private set; get; }
        protected ISpellCastPlacement spellCastMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected ITaskManager taskMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.gameUIMgr = gameMgr.GetService<IGameUIManager>(); 
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.spellCastMgr = gameMgr.GetService<ISpellCastPlacement>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.taskMgr = gameMgr.GetService<ITaskManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>(); 

            if (!logger.RequireValid(multipleSelectionKey,
              $"[{GetType().Name}] Field 'Multiple Selection Key' has not been assigned! Functionality will be disabled.",
              type: LoggingType.warning))
                return; 

            clickableLayerMask = new LayerMask();

            clickableLayerMask |= (1 << LayerMask.NameToLayer(entitySelectionLayer));

            if (!logger.RequireValid(clickableTerrainAreas,
              $"[{GetType().Name}] 'Clickable Terrain Areas' field has some invalid elements!"))
                return; 

            foreach(TerrainAreaType area in clickableTerrainAreas)
                clickableLayerMask |= area.Layers;
        }
        #endregion

        #region Handling Mouse Selection
        private void Update()
        {
            if (gameMgr.State != GameStateType.running 
                || spellCastMgr.IsPlacingSpell
                || placementMgr.IsPlacingBuilding
                || !gameUIMgr.HasPriority(this)
                || EventSystem.current.IsPointerOverGameObject())
                return;

            bool leftButtonDown = Input.GetMouseButtonDown(0);
            bool rightButtonDown = Input.GetMouseButtonDown(1);

            // If the mouse pointer is over a UI element or the minimap, we will not detect entity selection
            // In addition, we make sure that one of the mouse buttons are down
            if (!leftButtonDown && !rightButtonDown)
                return;

            if (Physics.Raycast(mainCameraController.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, clickableLayerMask.value))
            {
                // Get the entity that the player clicked in or see if the player clicked on a terrain area.
                IEntity hitEntity = hit.transform.gameObject.GetComponent<EntitySelectionCollider>()?.Entity; 
                bool hitTerrain = terrainMgr.IsTerrainArea(hit.transform.gameObject);

                if (rightButtonDown) 
                {
                    // If there was an active awaiting task then disable it since it can only be completed with a left mouse button click
                    if (taskMgr.AwaitingTask.IsEnabled)
                    {
                        taskMgr.AwaitingTask.Disable();
                        return;
                    }

                    if (hitEntity.IsValid())
                        hitEntity.Selection.OnDirectAction();
                    else if (hitTerrain)
                        selectionMgr.GetEntitiesList(EntityType.all, false, true).SetTargetFirstMany(hit.point, playerCommand: true, includeMovement: true);
                }
                else if (leftButtonDown)
                {
                    if (!hitEntity.IsValid()) 
                    {
                        // Complete awaiting task on terrain click.
                        if (hitTerrain && taskMgr.AwaitingTask.IsEnabled)
                            foreach (IEntityTargetComponent sourceComp in taskMgr.AwaitingTask.Current.sourceTracker.EntityTargetComponents)
                                sourceComp.SetTarget(hit.point, playerCommand: true);
                        // No awaiting task and terrain click = deselecting currently selected entities
                        else
                            selectionMgr.RemoveAll();

                        taskMgr.AwaitingTask.Disable();
                        return;
                    }

                    // Awaiting task is active with a valid hit entity
                    if (taskMgr.AwaitingTask.IsEnabled)
                    {
                        hitEntity.Selection.OnAwaitingTaskAction(taskMgr.AwaitingTask.Current);
                        taskMgr.AwaitingTask.Disable();
                    }
                    // If no awaiting task is active then proceed with regular selection
                    else 
                        selectionMgr.Add(hitEntity, SelectionType.single); 
                }
            }
        }
        #endregion

        #region Handling Double Click Selection
        private IEntity nextRangeSelectionSource = null;

        private ErrorMessage IsTargetValidForRangeSelection(TargetData<IEntity> target, bool playerCommand)
        {
            if (!nextRangeSelectionSource.IsValid())
                return ErrorMessage.invalid;
            else if (target.instance.Code != nextRangeSelectionSource.Code)
                return ErrorMessage.entityCodeMismatch;
            else if (!nextRangeSelectionSource.IsSameFaction(target.instance))
                return ErrorMessage.factionMismatch;

            return ErrorMessage.none;
        }

        public void SelectEntitisInRange(IEntity source, bool playerCommand)
        {
            if (!enableDoubleClickSelect 
                || !source.IsValid()
                || source.IsFree)
                return;

            nextRangeSelectionSource = source;

            gridSearch.Search(
                source.transform.position,
                doubleClickSelectRange,
                -1,
                IsTargetValidForRangeSelection,
                playerCommand,
                out IEnumerable<IEntity> entitiesInRange);

            selectionMgr.RemoveAll();
            selectionMgr.Add(entitiesInRange);
        }
        #endregion

        #region Handling Selection Flash
        public void FlashSelection(IEntity entity, bool isFriendly)
        {
            if (!entity.IsValid()
                || !entity.SelectionMarker.IsValid())
                return;

            entity.SelectionMarker.StartFlash(
                flashTime,
                flashRepeatTime,
                (isFriendly == true) ? friendlyFlashColor : enemyFlashColor);
        }
        #endregion
    }
}