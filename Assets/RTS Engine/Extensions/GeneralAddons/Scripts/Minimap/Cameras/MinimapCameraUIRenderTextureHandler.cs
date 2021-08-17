using UnityEngine;
using UnityEngine.EventSystems;

using RTSEngine.Game;

namespace RTSEngine.Minimap.Cameras
{
    public class MinimapCameraUIRenderTextureHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IMinimapCameraHandler
    {
        // Pointer related fields
        private bool isPointerDown = false;
        private PointerEventData lastEventData;

        // Rect transform of the raw image that is showing the render texture
        private RectTransform rectTransform;

        public void Init(IGameManager gameMgr)
        {
            isPointerDown = false;
            rectTransform = gameObject.GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            lastEventData = eventData;
            isPointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
        }

        public bool TryGetMinimapViewportPoint(out Vector2 point)
        {
            rectTransform = gameObject.GetComponent<RectTransform>();
            point = default;

            if (!isPointerDown)
                return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
               rectTransform,
               lastEventData.position,
               null,
               out point
            );

            var rect = rectTransform.rect;
            point.x = (point.x / rect.width) + rectTransform.pivot.x;
            point.y = (point.y / rect.height) + rectTransform.pivot.y;

            return true;
        }
    }
}
