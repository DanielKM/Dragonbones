using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using RTSEngine.Game;
using RTSEngine.Animation;
using RTSEngine.Health;

namespace RTSEngine.Entities
{
    public class Unit : FactionEntity, IUnit
    {
        #region Class Attributes
        public sealed override EntityType Type => EntityType.unit;

        //the component that is responsible for moving the unit when it is created.
        public IRallypoint SpawnRallypoint { private set; get; }

        // Component used to create the unit
        public IEntityComponent CreatorEntityComponent { private set; get; }

        [SerializeField, Tooltip("The Transform from which the look at position is set, when the unit spawns.")]
        private Transform spawnLookAt = null;

        public IDropOffSource DropOffSource { private set; get; }
        public IResourceCollector CollectorComponent { private set; get; }
        public IBuilder BuilderComponent { private set; get; }
        public ICarriableUnit CarriableUnit { private set; get; }
        public new IUnitHealth Health { private set; get; }

        // services
        protected IMovementManager mvtMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, InitUnitParameters initParams)
        {
            this.mvtMgr = gameMgr.GetService<IMovementManager>();

            base.Init(gameMgr, initParams);

            //handling rigidbody:
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if(rigidbody)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            //handling spawn rotation:
            if (spawnLookAt) //if we have a set a position for the unit to look at when it is spawned.
                transform.LookAt(spawnLookAt);
            else if (initParams.rallypoint != null) //if not, see if there is a building creator for the unit and look in the opposite direction of it.
                RTSHelper.LookAwayFrom(transform, initParams.rallypoint.Entity.transform.position, fixYRotation:true);

            if (!IsFree)
                resourceMgr.UpdateResource(FactionID, InitResources, add: true);

            SpawnRallypoint = initParams.rallypoint;
            CreatorEntityComponent = initParams.creatorEntityComponent;

            CompleteInit();
            globalEvent.RaiseUnitInitiatedGlobal(this);

            // Allow CompleteInit() to initialize the movement component since all IEntityComponent components are initialized with that call.
            Radius = MovementComponent.Controller.Radius; //for units, their radius is overwritten by the movement component's controller radius
            SetInitialTargetPosition(initParams.gotoPosition);
        }

        protected sealed override void FetchComponents()
        {
            DropOffSource = transform.GetComponentInChildren<IDropOffSource>();
            CollectorComponent = transform.GetComponentInChildren<IResourceCollector>();
            BuilderComponent = transform.GetComponentInChildren<IBuilder>();
            CarriableUnit = transform.GetComponentInChildren<ICarriableUnit>();

            Health = transform.GetComponentInChildren<IUnitHealth>();

            base.FetchComponents();

            // IEntity component is responsible for getting the movement component
            if (!logger.RequireValid(MovementComponent,
                $"[{GetType().Name} - {Code}] Units must have a component that implements #{typeof(IMovementComponent)}' that handles unit movement")
                || !logger.RequireValid(AnimatorController,
                $"[{GetType().Name} - {Code}] Units must have a component that implements #{typeof(IAnimatorController)}' that handles animation."))
                return;
        }

        // A method that is used to move the unit to its initial position after it spawns
        protected virtual void SetInitialTargetPosition (Vector3 gotoPosition)
        {
            if (!RTSHelper.IsMasterInstance())
                return;

            if (SpawnRallypoint != null)
                SpawnRallypoint.SendAction (this, playerCommand: false);
            else if (Vector3.Distance(gotoPosition, transform.position) > mvtMgr.StoppingDistance) //only if the goto position is not within the stopping distance of this unit
                mvtMgr.SetPathDestination(this, gotoPosition, 0.0f, null, new MovementSource { playerCommand = false });
        }

        protected sealed override void Disable(bool IsUpgrade, bool isFactionUpdate)
        {
            base.Disable(IsUpgrade, isFactionUpdate);

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Editor Only
#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
        }
#endif
        #endregion

    }
}
