using UnityEngine;

using RTSEngine.Cameras;
using RTSEngine.Game;

namespace RTSEngine.Minimap.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class MinimapCameraDirectHandler : MonoBehaviour, IMinimapCameraHandler
    {
        protected IMainCameraController mainCameraController { private set; get; }

        public void Init(IGameManager gameMgr)
        {
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 
        }

        public bool TryGetMinimapViewportPoint(out Vector2 point)
        {
            point = mainCameraController.ScreenToViewportPoint(Input.mousePosition);
            return true;
        }
    }
}
