using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Cameras;
using RTSEngine.Logging;
using RTSEngine.BuildingExtension;
using RTSEngine.Event;
using RTSEngine.UI;
using RTSEngine.Search;
using RTSEngine.Terrain;
using System.Collections.Generic;
using RTSEngine.EntityComponent;
using System.Linq;

namespace RTSEngine.Selection.Box
{
    public class BoxSelector : MonoBehaviour, IBoxSelector
    {
        #region Attributes
        [SerializeField, Tooltip("Assign an independent UI canvas to handle displaying the selection box.")]
        private RectTransform canvas = null;
        [SerializeField, Tooltip("The selection box UI element, child of the above canvas, goes here.")]
        private RectTransform image = null;

        [SerializeField, Tooltip("The minimum allowed size of the selection box to draw it on the screen and enable it.")]
        private float minSize = 10.0f;

        // Is the player currently drawing the selection box (enabled even when the min size is not reached yet).
        private bool isDrawing = false;
        /// <summary>
        /// Is the player currently drawing a selection box that satisifies the minimum size condition?
        /// </summary>
        public bool IsActive { private set; get; }

        // Initial mouse position recorded when the player starts drawing the selection box
        private Vector3 initialMousePosition;
        // Final mouse position recorded when the player stops drawing the selection box
        private Vector3 finalMousePosition;

        private Vector2 lowerLeftCorner;
        private Vector2 upperRightCorner;
        private Rect screenRect;
        private GameObject center;
        private GameObject edge2;
        private GameObject edge1;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameUIManager gameUIMgr { private set; get; } 
        protected ISelectionManager selectionMgr { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 
        protected IGameUIManager UIMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IMainCameraController mainCameraCtrl { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.gameUIMgr = gameMgr.GetService<IGameUIManager>(); 
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.mouseSelector = gameMgr.GetService<IMouseSelector>(); 
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 
            this.UIMgr = gameMgr.GetService<IGameUIManager>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.mainCameraCtrl = gameMgr.GetService<IMainCameraController>(); 

            if (!logger.RequireValid(canvas,
                $"[{GetType().Name}] The 'Canvas' field must be assigned")
                || !logger.RequireValid(image,
                $"[{GetType().Name} The 'Image' field must be assigned"))
                return;

            minSize = Mathf.Clamp(minSize, 0.0f, minSize);

            globalEvent.BuildingPlacementStartGlobal += HandleBuildingPlacementStartGlobal;

            // By default the selection box is disabled.
            Disable();
        }

        private void OnDestroy()
        {
            globalEvent.BuildingPlacementStartGlobal -= HandleBuildingPlacementStartGlobal;
        }
        #endregion

        #region Handling Event: Building Placement
        private void HandleBuildingPlacementStartGlobal(IBuilding building, EventArgs args)
        {
            if (!building.IsLocalPlayerFaction())
                return;

            Disable();
        }
        #endregion

        #region Drawing Selection Box
        private void Update()
        {
            if (gameMgr.State != GameStateType.running 
                || placementMgr.IsPlacingBuilding)
                return;

            // If the player is holding the left mouse button -> potentially drawing a selection box
            if (Input.GetMouseButton(0))
                OnDrawingProgress();

            // If the player releases the left mouse button while they were drawing the selection box
            else if (isDrawing && Input.GetMouseButtonUp(0))
            {
                OnDrawingComplete();
                Disable();
            }
        }

        private void OnDrawingProgress()
        {
            // Just started drawing?
            if (!isDrawing)
            {
                // Do not start drawing the box if this component does not have priority to use the UI.
                if (!gameUIMgr.HasPriority(this))
                    return;

                // Initial selection box position edges
                initialMousePosition = finalMousePosition = Input.mousePosition;
                isDrawing = true;
            }
            // Continue drawing
            else
                finalMousePosition = Input.mousePosition;

            // Do not display if we haven't reached the minimum size
            if (Vector3.Distance(finalMousePosition, initialMousePosition) < minSize)
                return;

            // Passed the minimum required size

            // Start displaying the selection box
            if (!IsActive)
            {
                image.gameObject.SetActive(true);
                IsActive = true;

                // Reserve a slot to prioritize this service to handle its box drawing UI.
                UIMgr.PrioritizeServiceUI(this);
            }

            image.sizeDelta = new Vector2(Mathf.Abs(finalMousePosition.x - initialMousePosition.x), Mathf.Abs(finalMousePosition.y - initialMousePosition.y));

            // Center the selection box position between initial and final mouse positions and offset by the canvas position
            image.localPosition = (initialMousePosition + finalMousePosition) / 2.0f - canvas.localPosition;
        }

        private void OnDrawingComplete()
        {
            if (!IsActive || Vector3.Distance(finalMousePosition, initialMousePosition) < minSize)
                return;

            // If the player is not holding down the multiple selection key down then deselect all currently selected entities
            if (!mouseSelector.MultipleSelectionKeyDown)
                selectionMgr.RemoveAll();

            if (!gameMgr.LocalFactionSlot.IsValid())
                return;

            lowerLeftCorner = new Vector2(Mathf.Min(finalMousePosition.x, initialMousePosition.x), Mathf.Min(finalMousePosition.y, initialMousePosition.y));
            upperRightCorner = new Vector2(Mathf.Max(finalMousePosition.x, initialMousePosition.x), Mathf.Max(finalMousePosition.y, initialMousePosition.y));

            selectionMgr.Add(gameMgr.LocalFactionSlot.FactionMgr.Units.Where(
                unit =>
                {
                    Vector3 unitScreenPosition = mainCameraController.MainCamera.WorldToScreenPoint(unit.Selection.transform.position);

                    // Make sure the unit screen position fits inside the current selection box
                    return unitScreenPosition.x >= lowerLeftCorner.x && unitScreenPosition.x <= upperRightCorner.x
                        && unitScreenPosition.y >= lowerLeftCorner.y && unitScreenPosition.y <= upperRightCorner.y;
                }
                ));
        }

        public void Disable()
        {
            isDrawing = false;
            IsActive = false;

            image.gameObject.SetActive(false);

            UIMgr.DeprioritizeServiceUI(this);
        }
        #endregion
    }
}
