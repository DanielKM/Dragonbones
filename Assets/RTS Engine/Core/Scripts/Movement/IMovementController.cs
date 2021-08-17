using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Movement
{
    /// <summary>
    /// Defines properties and methods required for a movement controller that allows an Entity instance to navigate the map.
    /// </summary>
    public interface IMovementController : IMonoBehaviour
    {
        bool Enabled { get; set; }

        bool IsActive { set; get; }

        MovementControllerData Data { set;  get; }

        LayerMask NavigationAreaMask { get; }

        float Radius { get; }

        Vector3 FinalTarget { get; }

        Vector3 NextPathTarget { get; }

        /// <summary>
        /// Was the last movement prepare call by a direct player command?
        /// </summary>
        bool IsLastPlayerCommand { get; }

        void Init(IGameManager gameMgr, IMovementComponent mvtComponent, MovementControllerData data);

        void Prepare(Vector3 destination, bool playerCommand);

        void Launch();
    }
}
