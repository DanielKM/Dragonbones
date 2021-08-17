using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Minimap.Notification
{
    public interface IAttackMinimapNotificationManager : IPreRunGameService
    {
        bool CanSpawn(Vector3 spawnPosition);
        void Spawn(Vector3 targetPosition);
    }
}