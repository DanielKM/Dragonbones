using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Minimap.Cameras
{
    public interface IMinimapCameraController : IPreRunGameService
    {
        Camera MinimapCamera { get; }

        bool IsMouseOverMinimap();

        Vector2 WorldToScreenPoint(Vector3 targetPosition);
    }
}