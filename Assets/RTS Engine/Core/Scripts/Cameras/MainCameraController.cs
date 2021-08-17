using System;

using UnityEngine;
using UnityEngine.EventSystems;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;
using RTSEngine.BuildingExtension;
using RTSEngine.Controls;

namespace RTSEngine.Cameras
{
    public class MainCameraController : MonoBehaviour, IMainCameraController
    {
        #region General Attributes
        [SerializeField, Header("General"), Tooltip("The main camera in the scene.")]
        private Camera mainCamera = null;
        /// <summary>
        /// Gets the main camera in the game.
        /// </summary>
        public Camera MainCamera => mainCamera;

        [SerializeField, Tooltip("Child of the main camera object, used to display UI elements only. This UI camear is optional but it is always recommended to separate rendering UI elements and the rest of the game elements.")]
        private Camera mainCameraUI = null;

        // Speed struct that allows to control a movement/rotation speed by accelerating it towards a max value and decelerating it towards 0
        [System.Serializable]
        public struct SmoothSpeed
        {
            public float value;
            public float smoothFactor;
        }

        [SerializeField, Tooltip("X-axis camera position offset value.")]
        // Camera look at offset values, when the camera is looking at a position, this value is the offset that the camera position will have on the x and z axis
        // The value of the offset depends on the camera's rotation on the x-axis
        private float offsetX = -15;
        [SerializeField, Tooltip("Z-axis camera position offset value.")]
        private float offsetZ = -15;
        // The above two floats are the initial offset values used for the initial height of the main camera, the curr offsets are updated when the player changes the height
        public float CurrOffsetX { private set; get; }
        public float CurrOffsetZ { private set; get; }

        #endregion

        #region Panning Attributes
        [Header("Panning")]
        [SerializeField, Tooltip("How fast does the camera pan?")]
        private SmoothSpeed panningSpeed = new SmoothSpeed { value = 20.0f, smoothFactor = 0.1f };

        // If the input axis panning is enabled, the defined axis can be used to move the camera
        [System.Serializable]
        public struct InputAxisPanning
        {
            public bool enabled;
            public string horizontal;
            public string vertical;
        }
        [SerializeField, Tooltip("Pan the camera using input axis.")]
        private InputAxisPanning inputAxisPanning = new InputAxisPanning { enabled = true, horizontal = "Horizontal", vertical = "Vertical" };

        // If the keyboard button panning is enabling, player will be able to use keyboard keys to move the camera
        [System.Serializable]
        public struct KeyPanning
        {
            public bool enabled;
            public ControlType up;
            public ControlType down;
            public ControlType right;
            public ControlType left;
        }
        [SerializeField, Tooltip("Pan the camera using keys.")]
        private KeyPanning keyPanning = new KeyPanning { enabled = false };

        // When the player's mouse cursor is on the edge of the screen, should the camera move or not?
        [System.Serializable]
        public struct ScreenEdgePanning
        {
            public bool enabled;
            public float size;
            [Tooltip("When enabled, screen edge panning would be disabled when the player's mouse cursor is on a UI element even if it was on the defined screen edge.")]
            public bool ignoreUI;
        }
        [SerializeField, Tooltip("Pan the camera when the mouse is over the screen edge.")]
        private ScreenEdgePanning screenEdgePanning = new ScreenEdgePanning { enabled = true, size = 25.0f, ignoreUI = false };

        // Limit the pan of the camera on the x and z axis? 
        [System.Serializable]
        public struct PanningLimit
        {
            public bool enabled;
            [Tooltip("The minimum allowed position values on the x and z axis.")]
            public Vector2 minPosition;
            [Tooltip("The maximum allowed position values on the x and z axis.")]
            public Vector2 maxPosition;
        }
        [SerializeField, Tooltip("Limit the position that the camera can pan to.")]
        private PanningLimit panLimit = new PanningLimit { enabled = true, minPosition = new Vector2(-20.0f, -20.0f), maxPosition = new Vector2(120.0f, 120.0f) };

        private Vector3 currPanDirection = Vector3.zero;
        private Vector3 lastPanDirection = Vector3.zero;

        public bool IsPanning => currPanDirection != Vector3.zero;
        #endregion

        #region Follow Target Attributes
        [Header("Follow Target")]
        private Transform followTarget = null;
        public bool IsFollowingTarget => followTarget.IsValid();

        [SerializeField, Tooltip("Does the camera follow its target smoothly?")]
        private bool smoothFollow = true;
        [SerializeField, Tooltip("How smooth does the camera follow its target?")]
        private float smoothFollowFactor = 0.1f;
        [SerializeField, Tooltip("Does the camera stop following its target when it moves?")]
        private bool stopFollowingOnMovement = true;
        #endregion

        #region Rotation Attributes
        [Header("Rotation")]
        [SerializeField, Tooltip("Defines the initial rotation of the main camera.")]
        private Vector3 initialEulerAngles = new Vector3(45.0f, 45.0f, 0.0f);
        public Vector3 InitialEulerAngles => initialEulerAngles;
        private Quaternion initialRotation;

        [SerializeField, Tooltip("Have a fixed rotation when the camera is panning? When enabled, the camera rotation will be reset when the camera pans.")]
        private bool fixPanRotation = true;
        [SerializeField, Min(0), Tooltip("How far can the camera move before reverting to the initial rotation (if above field is enabled).")]
        private float allowedRotationPanSize = 0.2f;

        [SerializeField, Tooltip("How fast can the camera rotate?")]
        private SmoothSpeed rotationSpeed = new SmoothSpeed { value = 40.0f, smoothFactor = 0.1f };

        [System.Serializable]
        public struct RotationLimit
        {
            public bool enabled;
            public float min;
            public float max;
        }
        [SerializeField, Tooltip("Limit the rotation of the main camera.")]
        private RotationLimit rotationLimit = new RotationLimit { enabled = true, min = 0.0f, max = 90.0f };

        [System.Serializable]
        public struct KeyRotation
        {
            public bool enabled;
            public ControlType positive;
            public ControlType negative;
        }
        [SerializeField, Tooltip("Rotate the camera with keys")]
        protected KeyRotation keyRotation = new KeyRotation { enabled = false };

        [System.Serializable]
        public struct MouseWheelRotation
        {
            public bool enabled;
            public float smoothFactor;
        }
        [SerializeField, Tooltip("Rotate the camera with the mouse wheel.")]
        private MouseWheelRotation mouseWheelRotation = new MouseWheelRotation { enabled = true, smoothFactor = 0.1f };

        // The current and last rotation value that is determined using the different rotation inputs.
        private float currRotationValue;
        private float lastRotationValue;

        // Keeps track of the mouse position in the last frame to determine rotation.
        private Vector3 lastMousePosition;
        #endregion

        #region Zoom Attributes
        [SerializeField, Header("Zoom"), Tooltip("How fast can the main camera zoom?")]
        private SmoothSpeed zoomSpeed = new SmoothSpeed { value = 1.0f, smoothFactor = 0.1f };

        [System.Serializable]
        public struct MouseWheelZoom
        {
            public bool enabled;
            public bool invert;
            public string name;
            public float sensitivity;
        }
        [SerializeField, Tooltip("Use the mouse wheel to zoom.")]
        private MouseWheelZoom mouseWheelZoom = new MouseWheelZoom { enabled = true, invert = false, name = "Mouse ScrollWheel", sensitivity = 20.0f };

        [System.Serializable]
        public struct KeyZoom
        {
            public bool enabled;
            public ControlType inKey;
            public ControlType outKey;
        }
        [SerializeField, Tooltip("Zoom using keys.")]
        private KeyZoom keyZoom = new KeyZoom { enabled = false };

        [SerializeField, Tooltip("Enable to zoom using the camera's field of view instead of the height of the camera.")]
        private bool zoomUseFOV = false;
        // Gets either incremented or decremented depending on the zoom inputs
        private float zoomValue = 0.0f;

        [SerializeField, Tooltip("The height that the main camera starts with.")]
        private float initialHeight = 20.0f;
        [SerializeField, Tooltip("The minimum height the main camera is allowed to have.")]
        private float minHeight = 20.0f;
        [SerializeField, Tooltip("The maximum height the main camera is allowed to have.")]
        private float maxHeight = 50.0f;

        [SerializeField, Tooltip("Allow the player to zoom the camera when they are placing a building?")]
        private bool allowBuildingPlaceZoom = true;
        #endregion

        #region Services
        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IMainCameraController, EventArgs> CameraPositionUpdated;

        private void RaiseCameraPositionUpdated()
        {
            var handler = CameraPositionUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.controls = gameMgr.GetService<IGameControlsManager>();

            if (!logger.RequireValid(mainCamera,
                $"[{GetType().Name}] The field 'Main Camera' must be assigned!")
                || !logger.RequireTrue(initialHeight >= minHeight && initialHeight <= maxHeight,
                $"[{GetType().Name}] The 'Initial Height' value must be between the minimum and maximum allowed height values."))
                return;

            initialRotation = Quaternion.Euler(initialEulerAngles);
            mainCamera.transform.rotation = initialRotation;

            zoomValue = (initialHeight - minHeight) / (maxHeight - minHeight);
            if (zoomUseFOV)
                UpdateCameraFOV(initialHeight);
            else
                mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, initialHeight, mainCamera.transform.position.y);

            CurrOffsetX = offsetX;
            CurrOffsetZ = offsetZ;
        }
        #endregion

        #region Getting/Applying Input
        private void Update()
        {
            UpdatePanInput();
            UpdateRotationInput();
            UpdateZoomInput();

            lastMousePosition = Input.mousePosition;
        }

        private void LateUpdate()
        {
            Pan();
            Rotate();
            Zoom();
        }
        #endregion

        #region Handling Camera Panning
        private void UpdatePanInput()
        {
            currPanDirection = Vector3.zero;

            // If the pan on screen edge is enabled and we either are ignoring UI elements on the edge of the screen or the player's mouse is not over one
            if (screenEdgePanning.enabled && (screenEdgePanning.ignoreUI || !EventSystem.current.IsPointerOverGameObject()))
            {
                // If the mouse is in either one of the 4 edges of the screen then move it accordinly  
                if (Input.mousePosition.x <= screenEdgePanning.size && Input.mousePosition.x >= 0.0f)
                    currPanDirection.x = -1.0f;
                else if (Input.mousePosition.x >= Screen.width - screenEdgePanning.size && Input.mousePosition.x <= Screen.width)
                    currPanDirection.x = 1.0f;

                if (Input.mousePosition.y <= screenEdgePanning.size && Input.mousePosition.y >= 0.0f)
                    currPanDirection.z = -1.0f;
                else if (Input.mousePosition.y >= Screen.height - screenEdgePanning.size && Input.mousePosition.y <= Screen.height)
                    currPanDirection.z = 1.0f;
            }

            // Camera pan on key input (overwrites the screen edge pan if it has been enabled and had effect on this frame)
            if (keyPanning.enabled)
            {
                if(controls.Get(keyPanning.up))
                    currPanDirection.z = 1.0f;
                else if(controls.Get(keyPanning.down))
                    currPanDirection.z = -1.0f;

                if(controls.Get(keyPanning.right))
                    currPanDirection.x = 1.0f;
                else if(controls.Get(keyPanning.left))
                    currPanDirection.x = -1.0f;
            }

            // Camera pan on axis input (overwrites the screen edge pan/key input axis if it has been enabled and had effect on this frame)
            if (inputAxisPanning.enabled)
            {
                if (Mathf.Abs(Input.GetAxis(inputAxisPanning.horizontal)) > 0.25f)
                    currPanDirection.x = Mathf.Sign(Input.GetAxis(inputAxisPanning.horizontal)) * 1.0f;
                if (Mathf.Abs(Input.GetAxis(inputAxisPanning.vertical)) > 0.25f)
                    currPanDirection.z = Mathf.Sign(Input.GetAxis(inputAxisPanning.vertical)) * 1.0f;
            }
        }

        private void Pan()
        {
            // Prioritize following target
            if (followTarget.IsValid())
            {
                LookAt(followTarget.position, smoothFollow, smoothFollowFactor);

                if (currPanDirection != Vector3.zero && stopFollowingOnMovement)
                    SetFollowTarget(null);
            }
            // Regular camera movement
            else
            {
                // Smoothly update the last panning direction towards the current one
                lastPanDirection = Vector3.Lerp(lastPanDirection, currPanDirection, panningSpeed.smoothFactor);

                // Moving the actual camera
                mainCamera.transform.Translate(Quaternion.Euler(new Vector3(0f, mainCamera.transform.eulerAngles.y, 0f)) * lastPanDirection * panningSpeed.value * Time.deltaTime, Space.World);

                if (lastPanDirection != Vector3.zero)
                    RaiseCameraPositionUpdated();
            }

            mainCamera.transform.position = ApplyPanLimit(mainCamera.transform.position);
        }

        private Vector3 ApplyPanLimit(Vector3 position)
        {
            return panLimit.enabled
                ? new Vector3(
                    Mathf.Clamp(position.x, panLimit.minPosition.x, panLimit.maxPosition.x),
                    position.y,
                    Mathf.Clamp(position.z, panLimit.minPosition.y, panLimit.maxPosition.y))
                : position;
        }
        #endregion

        #region Handling Follow Target
        /// <summary>
        /// Updates the target that the camera will be following
        /// </summary>
        /// <param name="transform"></param>
        public void SetFollowTarget(Transform transform)
        {
            followTarget = transform;

            // Reset movement inputs
            currPanDirection = Vector3.zero;
            lastPanDirection = Vector3.zero;
        }

        /// <summary>
        /// Make the camera look at a target position and return the final position while considering the offset values
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="smooth"></param>
        /// <param name="smoothFactor"></param>
        public void LookAt(Vector3 targetPosition, bool smooth, float smoothFactor = 0.1f)
        {
            targetPosition = ApplyPanLimit(new Vector3(targetPosition.x + CurrOffsetX, mainCamera.transform.position.y, targetPosition.z + CurrOffsetZ));

            mainCamera.transform.position =
                smooth
                ? Vector3.Lerp(mainCamera.transform.position, targetPosition, smoothFactor)
                : targetPosition;

            RaiseCameraPositionUpdated();
        }
        #endregion

        #region Handling Camera Rotation
        private void UpdateRotationInput()
        {
            currRotationValue = 0.0f;

            // If the keyboard keys rotation is enabled, check for the positive and negative rotation keys and update the current rotation value accordinly
            if (keyRotation.enabled)
            {
                if(controls.Get(keyRotation.positive))
                    currRotationValue = 1.0f;
                else if(controls.Get(keyRotation.negative))
                    currRotationValue = -1.0f;
            }

            // If the mouse wheel rotation is enabled and the player is holding the mouse wheel button, update the rotation value accordinly
            if (mouseWheelRotation.enabled && Input.GetMouseButton(2))
                currRotationValue = (Input.mousePosition.x - lastMousePosition.x) * mouseWheelRotation.smoothFactor;

            // Smoothly update the last rotation value towards the current one
            lastRotationValue = Mathf.Lerp(lastRotationValue, currRotationValue, rotationSpeed.smoothFactor);
        }

        private void Rotate()
        {
            // If the player is moving the camera and the camera's rotation must be fixed during movement...
            //... or if the camera is following a target, lock camera rotation to default value
            if ((fixPanRotation && lastPanDirection.magnitude > allowedRotationPanSize) || followTarget)
            {
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, initialRotation, rotationSpeed.smoothFactor);
                return;
            }

            Vector3 nextEulerAngles = mainCamera.transform.rotation.eulerAngles;
            nextEulerAngles.y += rotationSpeed.value * Time.deltaTime * lastRotationValue;

            // Limit the y euler angle if that's enabled
            if (rotationLimit.enabled)
                nextEulerAngles.y = Mathf.Clamp(nextEulerAngles.y, rotationLimit.min, rotationLimit.max);

            mainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }
        #endregion

        #region Handling Camera Zoom
        private void UpdateZoomInput()
        {
            if (placementMgr.IsPlacingBuilding && !allowBuildingPlaceZoom)
                return;

            // Camera zoom on keys
            if (keyZoom.enabled)
            {
                if(controls.Get(keyZoom.inKey))
                    zoomValue -= Time.deltaTime;
                else if(controls.Get(keyZoom.outKey))
                    zoomValue += Time.deltaTime;
            }

            // Camera zoom when the player is moving the mouse scroll wheel
            if (mouseWheelZoom.enabled)
                zoomValue += Input.GetAxis("Mouse ScrollWheel") * mouseWheelZoom.sensitivity
                    * (mouseWheelZoom.invert ? -1.0f : 1.0f) * Time.deltaTime;
        }

        private void Zoom()
        {
            zoomValue = Mathf.Clamp01(zoomValue);
            float targetHeight = Mathf.Lerp(minHeight, maxHeight, zoomValue);

            // If we're using the field of view for zooming, no need to adjust the offset values
            if (zoomUseFOV)
            {
                UpdateCameraFOV(Mathf.Lerp(mainCamera.fieldOfView, targetHeight, Time.deltaTime * zoomSpeed.value));
                return;
            }

            // Handling zoom using the camera's height
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position,
                new Vector3(
                    mainCamera.transform.position.x, targetHeight, mainCamera.transform.position.z), Time.deltaTime * zoomSpeed.value);

            // Update the current camera offset since the height has ben modified
            CurrOffsetX = (offsetX * mainCamera.transform.position.y) / initialHeight;
            CurrOffsetZ = (offsetZ * mainCamera.transform.position.y) / initialHeight;
        }

        private void UpdateCameraFOV(float value)
        {
            mainCamera.fieldOfView = value;
            if (mainCameraUI.IsValid())
                mainCameraUI.fieldOfView = value;
        }
        #endregion

        #region Main Camera Helper Methods
        public Vector3 ScreenToViewportPoint(Vector3 position) => mainCamera.ScreenToViewportPoint(position);

        public Ray ScreenPointToRay(Vector3 position) => mainCamera.ScreenPointToRay(position);

        public Vector3 ScreenToWorldPoint(Vector3 position, bool applyOffset = true)
        {
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(position);

            return applyOffset
                ? new Vector3(worldPosition.x - CurrOffsetX, worldPosition.y, worldPosition.z - CurrOffsetZ)
                : worldPosition;
        }
        #endregion
    }
}
 