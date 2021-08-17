using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Task;

namespace RTSEngine.Demo
{
    public class BarracksDoorSystem : MonoBehaviour, IEntityPostInitializable
    {
        [SerializeField, EnforceType(typeof(IUnitCarrier)), Tooltip("Drag and drop the Unit Carrier component into this field to open the door when units leave the carrier.")]
        private GameObject unitCarrier = null;
        [SerializeField, EnforceType(typeof(IUnitCreator)), Tooltip("Drag and drop the Unit Creator component into this field to open the door when units are created.")]
        private GameObject unitCreator = null;

        [SerializeField, Tooltip("Door object to open/close when units are created or removed from the carrier.")]
        private Transform door = null;
        [SerializeField, Tooltip("Time (in seconds) before the door closes after it is open."), Min(0.0f)]
        private float closeTime = 1.5f;
        private TimeModifiedTimer closeTimer;

        // Is the door currently open?
        private bool isOpen;

        [Space(), SerializeField, Tooltip("Position of the door object when it is open.")]
        private Vector3 openPosition = Vector3.zero;
        [SerializeField, Tooltip("Euler angles (for rotation) of the door object when it is open.")]
        private Vector3 openEulerAngles = Vector3.zero;

        [Space(), SerializeField, Tooltip("Position of the door object when it is closed.")]
        private Vector3 closedPosition = Vector3.zero;
        [SerializeField, Tooltip("Euler angles (for rotation) of the door object when it is closed.")]
        private Vector3 closedEulerAngles = Vector3.zero;

        protected IBuilding building { private set; get; }

        protected IGameLoggingService logger { private set; get; } 
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.building = entity as IBuilding;

            this.logger = gameMgr.GetService<IGameLoggingService>();

            if (!logger.RequireValid(door,
              $"[{GetType().Name}] The 'Door' field must be assigned!"))
                return; 

            if (this.building.IsPlacementInstance)
                return;

            if(unitCarrier.IsValid())
                unitCarrier.GetComponent<IUnitCarrier>().UnitRemoved += HandleUnitRemoved;

            if(unitCreator.IsValid())
                unitCreator.GetComponent<IUnitCreator>().PendingTaskAction += HandleUnitCreatorPendingTaskAction;

            Close();
        }

        public void Disable()
        {
            if(unitCarrier.IsValid())
                unitCarrier.GetComponent<IUnitCarrier>().UnitRemoved -= HandleUnitRemoved;

            if(unitCreator.IsValid())
                unitCreator.GetComponent<IUnitCreator>().PendingTaskAction -= HandleUnitCreatorPendingTaskAction;
        }

        private void HandleUnitCreatorPendingTaskAction(IPendingTaskEntityComponent sender, PendingTaskEventArgs args)
        {
            if(args.State == PendingTaskState.completed)
                Open();
        }

        private void HandleUnitRemoved(IUnitCarrier sender, EntityEventArgs<IUnit> args)
        {
            Open();
        }

        private void Open()
        {
            door.localPosition = openPosition;
            door.localRotation = Quaternion.Euler(openEulerAngles);

            closeTimer = new TimeModifiedTimer(closeTime);

            isOpen = true;
        }

        private void Close()
        {
            door.localPosition = closedPosition;
            door.localRotation = Quaternion.Euler(closedEulerAngles);

            isOpen = false;
        }

        private void Update()
        {
            if (!isOpen)
                return;

            if (closeTimer.ModifiedDecrease())
                Close();
        }
    }
}
