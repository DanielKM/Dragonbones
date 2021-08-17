using UnityEngine;
using UnityEngine.AI;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Movement
{
    public class NavMeshAgentController : MonoBehaviour, IMovementController
    {
        #region Attributes
        public bool Enabled
        {
            set { navAgent.enabled = value; }
            get => navAgent.enabled;
        }

        public bool IsActive
        {
            set
            {
                if (navAgent.isOnNavMesh)
                    navAgent.isStopped = !value;
            }
            get => !navAgent.isStopped;
        }

        private NavMeshAgent navAgent; 
        private NavMeshPath navPath;

        private MovementControllerData data;
        public MovementControllerData Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;

                navAgent.speed = data.speed;
                navAgent.acceleration = data.acceleration;

                navAgent.angularSpeed = data.angularSpeed;

                navAgent.stoppingDistance = data.stoppingDistance;
            }
        }

        /// <summary>
        /// The navigation mesh area mask in which the unit can move.
        /// </summary>
        public LayerMask NavigationAreaMask => navAgent.areaMask;

        /// <summary>
        /// The size that the unit occupies in the navigation mesh while moving.
        /// </summary>
        public float Radius => navAgent.radius;

        /// <summary>
        /// The position of the next corner of the unit's active path.
        /// </summary>
        public Vector3 NextPathTarget { get { return navAgent.steeringTarget; } }

        /// <summary>
        /// The position of the last corner tof the unit's active path, AKA, the path's destination.
        /// </summary>
        public Vector3 FinalTarget { get { return navAgent.destination; } }

        public bool IsLastPlayerCommand { private set; get; }

        // Game services
        protected IGameLoggingService logger { private set; get; }

        // Other components
        protected IGameManager gameMgr { private set; get; }
        protected IMovementComponent mvtComponent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IMovementComponent mvtComponent, MovementControllerData data)
        {
            this.gameMgr = gameMgr;
            this.mvtComponent = mvtComponent;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            IEntity entity = mvtComponent?.Entity;
            if (!logger.RequireValid(entity
                , $"[{GetType()}] Can not initialize without a valid Unit instance."))
                return;

            navAgent = entity.gameObject.GetComponent<NavMeshAgent>();
            if (!logger.RequireValid(navAgent,
                $"[NavMeshAgentController - '{entity.Code}'] NavMeshAgent component must be attached to the unit."))
                return;
            navAgent.enabled = true;

            this.Data = data;

            navPath = new NavMeshPath();

            // Always set to none as Navmesh's obstacle avoidance desyncs multiplayer game since it is far from deterministic
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            // Make sure the NavMeshAgent component updates our unit's position when active.
            navAgent.updatePosition = true;
        }
        #endregion

        #region Preparing/Launching Movement
        /// <summary>
        /// Attempts to calculate a valid path for the specified destination position.
        /// </summary>
        /// <param name="destination">Vector3 that represents the movement's target position.</param>
        /// <returns>True if the path is valid and complete, otherwise false.</returns>
        public void Prepare(Vector3 destination, bool playerCommand)
        {
            this.IsLastPlayerCommand = playerCommand;

            navAgent.CalculatePath(destination, navPath);

            if (navPath != null && navPath.status == NavMeshPathStatus.PathComplete)
                mvtComponent.OnPathPrepared();
            else
                mvtComponent.OnPathFailure();
        }

        /// <summary>
        /// Starts the unit movement using the last calculated path from the "Prepare()" method.
        /// </summary>
        public void Launch ()
        {
            IsActive = true;

            navAgent.SetPath(navPath);
        }
        #endregion
    }
}
