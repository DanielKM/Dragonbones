using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;

namespace RTSEngine.Movement
{
    public abstract class BaseMovementFormationHandler : MonoBehaviour, IMovementFormationHandler 
    {
        #region Attributes
        [SerializeField, Tooltip("Select the movement formation type handled by this component.")]
        private MovementFormationType formationType = null;
        public MovementFormationType FormationType => formationType;

        [SerializeField, Tooltip("Define a formation type to switch to when this handler fails to produce the path destinations using the above formation. Make sure that properties of the main formation can be also applied to the fall back formation!")]
        private MovementFormationType fallbackFormationType = null;
        public MovementFormationType FallbackFormationType => fallbackFormationType;

        [SerializeField, Tooltip("The maximum amount of empty attempts (no generated path destinations) before the Movement Manager moves to the fallback formation type or stopping if no fallback formation is specified.")]
        private int maxEmptyAttempts = 3;
        public int MaxEmptyAttempts => maxEmptyAttempts;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init (IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>(); 

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Generating Path Destinations
        public abstract ErrorMessage GeneratePathDestinations(PathDestinationInputData input, ref int amount,
            ref float offset, ref List<Vector3> pathDestinations, out int generatedAmount);

        protected ErrorMessage IsConditionFulfilled(PathDestinationInputData input, Vector3 testPosition)
        {
            // No condition has been defined for the movement path destination generation
            if (input.condition == null)
                return ErrorMessage.none;

            return input.condition(input, testPosition);
        }
        #endregion
    }
}
