using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Selection;
using RTSEngine.UI;
using RTSEngine.Cameras;

namespace RTSEngine.Minimap.Cameras
{
    public class MinimapCameraController : MonoBehaviour, IMinimapCameraController
    {
        #region Attributes
        [Header("General")]
        [SerializeField, EnforceType(sameScene: true), Tooltip("The camera used to render the minimap.")]
        private Camera minimapCamera = null;
        public Camera MinimapCamera => minimapCamera;

        [SerializeField, EnforceType(typeof(IMinimapCameraHandler), sameScene: true), Tooltip("The minimap camera handler is the component responsible for detecting the player's cursor position on the minimap camera. It must implement the 'IMinimapCameraHandler' interface.")]
        private GameObject minimapCameraHandlerObj = null;
        private IMinimapCameraHandler minimapCameraHandler;

        [SerializeField, Tooltip("Layers assigned to the terrain objects that the player is allowed to click in the minimap to move the main camera.")]
        private LayerMask terrainLayerMask = new LayerMask();

        [Header("Movement")]
        [SerializeField, Tooltip("Can the player move the main camera by clicking the minimap?")]
        private bool movementEnabled = true;
        [SerializeField, Tooltip("Can the player drag their mouse on the minimap to move the main camera?")]
        private bool dragMovementEnabled = true;
        [SerializeField, Tooltip("Can the player move selected units by right-clicking on the minimap?")]
        private bool selectedUnitsMovementEnabled = true;

        [SerializeField, Tooltip("Update the minimap's rotation to fit the main camera?")]
        private bool followMainCameraRotation = true;

        [Header("UI")]
        [SerializeField, Tooltip("The UI image used to represent the minimap cursor.")]
        private RectTransform cursor = null;
        [SerializeField, Tooltip("The canvas where the minimap UI is.")]
        private RectTransform canvas = null;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGameUIManager gameUIMgr { private set; get; } 
        protected ISelectionManager selectionMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameUIMgr = gameMgr.GetService<IGameUIManager>(); 
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();

            if (minimapCameraHandlerObj.IsValid())
                minimapCameraHandler = minimapCameraHandlerObj.GetComponent<IMinimapCameraHandler>();

            if (!logger.RequireValid(minimapCamera,
                $"[{GetType().Name}] The field 'Minimap Camera' hasn't been assigned!")
                || !logger.RequireTrue(!cursor.IsValid() || canvas.IsValid(),
                $"[{GetType().Name}] When the 'Cursor' field is assigned, the 'Canvas' field must be assigned as well!")
                || !logger.RequireValid(minimapCameraHandler,
                $"[{GetType().Name}] The field 'Minimap Camera Handler Object' hasn't been assigned!"))
                return;

            minimapCameraHandler.Init(gameMgr);

            // Only subscribe to this event if a valid cursor is assigned to the minimap (since we will be moving that cursor on camera position updated)
            if (cursor.IsValid())
                mainCameraController.CameraPositionUpdated += HandleCameraPositionUpdated;

            if (followMainCameraRotation) //set the rotation of the minimap camera:
                transform.rotation = Quaternion.Euler(new Vector3(90.0f, mainCameraController.InitialEulerAngles.y, 0.0f));

            // Prioritize the UI screen to this service when the mouse is over the minimap camera
            gameUIMgr.PrioritizeServiceUI(new UIPriority
            {
                service = this,
                condition = IsMouseOverMinimap,
            });
        }

        private void OnDestroy()
        {
            mainCameraController.CameraPositionUpdated -= HandleCameraPositionUpdated;
        }
        #endregion

        #region Handling Event: Main Camera Position Updated
        private void HandleCameraPositionUpdated(IMainCameraController source, EventArgs args)
        {
            Vector3 screenCenterWorldPos = mainCameraController.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0.0f), applyOffset: true);

            // Convert the main camera's position to the minimap camera's screen space
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas,
                minimapCamera.WorldToScreenPoint(screenCenterWorldPos),
                minimapCamera,
                out Vector2 localPoint))
            {
                cursor.localPosition = new Vector3(localPoint.x, localPoint.y, cursor.localPosition.z);
            }
        }
        #endregion

        #region Handling Minimap Mouse Click
        /// <summary>
        /// Determines whether the mouse is over the minimap and provides the hitpoint
        /// </summary>
        public bool IsMouseOverMinimap() =>
            minimapCameraHandler.TryGetMinimapViewportPoint(out Vector2 viewportPoint) && IsMouseOverMinimap(viewportPoint, out _);

        public bool IsMouseOverMinimap(Vector2 viewportPoint, out Vector3 terrainHitPosition)
        {
            terrainHitPosition = Vector3.zero;

            //if (minimapCamera.rect.Contains(mainCameraController.ScreenToViewportPoint(Input.mousePosition))

            // Draw a ray using the current mouse position towards the minimap camera and see if it hits the terrain
            if(minimapCamera.rect.Contains(viewportPoint)
                && Physics.Raycast(minimapCamera.ViewportPointToRay(viewportPoint), out RaycastHit hit, terrainLayerMask))
            {
                terrainHitPosition = hit.point;
                return true;
            }

            return false;
        }

        private void Update()
        {
            // Only allow to interact with the minimap camera UI if movement is allowed and no other service is reserving the UI priority.
            if (!movementEnabled)
                return;

            bool leftClickEvent = Input.GetMouseButtonDown(0) || (dragMovementEnabled && Input.GetMouseButton(0));
            bool rightClickEvent = selectedUnitsMovementEnabled && Input.GetMouseButtonDown(1);

            if (!leftClickEvent && !rightClickEvent)
                return;

            minimapCameraHandler.TryGetMinimapViewportPoint(out Vector2 viewportPoint);

            if (IsMouseOverMinimap(viewportPoint, out Vector3 terrainHitPosition))
            {
                if (leftClickEvent)
                    OnLeftMouseClick(terrainHitPosition);
                else
                    OnRightMouseClick(terrainHitPosition);
            }
        }

        // Move main camera
        private void OnLeftMouseClick(Vector3 hitPoint)
        {
            // Stop following the camera's target if there was one
            mainCameraController.SetFollowTarget(null);
            // Make the main camera look at the minimap hit position
            mainCameraController.LookAt(hitPoint, smooth: true, smoothFactor: 0.8f);
        }

        // Move selected units
        private void OnRightMouseClick(Vector3 hitPoint)
        {
            // Get the currently selected units from player faction
            List<IUnit> selectedUnits = selectionMgr.GetEntitiesList(EntityType.unit, exclusiveType: false, localPlayerFaction: true).Cast<IUnit>().ToList();

            if (selectedUnits.Count > 0)
                mvtMgr.SetPathDestination(selectedUnits, hitPoint, 0.0f, null, true);
        }
        #endregion

        #region Minimap Camera Helper Methods
        public Vector2 WorldToScreenPoint(Vector3 position) => minimapCamera.WorldToScreenPoint(position);
        #endregion
    }
}
