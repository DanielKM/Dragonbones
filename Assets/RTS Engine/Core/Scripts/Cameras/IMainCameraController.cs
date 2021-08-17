using System;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.Cameras
{
    public interface IMainCameraController : IPreRunGameService
    {
        Camera MainCamera { get; }

        float CurrOffsetX { get; }
        float CurrOffsetZ { get; }

        bool IsPanning { get; }
        bool IsFollowingTarget { get; }

        Vector3 InitialEulerAngles { get; }

        event CustomEventHandler<IMainCameraController, EventArgs> CameraPositionUpdated;

        void SetFollowTarget(Transform transform);
        void LookAt(Vector3 targetPosition, bool smooth, float smoothFactor = 0.1F);

        Vector3 ScreenToViewportPoint(Vector3 position);
        Vector3 ScreenToWorldPoint(Vector3 position, bool applyOffset = true);
        Ray ScreenPointToRay(Vector3 position);
    }
}