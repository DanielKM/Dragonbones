using RTSEngine.Game;

using UnityEngine;

namespace RTSEngine.Minimap.Cameras
{
    public interface IMinimapCameraHandler
    {
        void Init(IGameManager gameMgr);

        bool TryGetMinimapViewportPoint(out Vector2 point);
    }
}