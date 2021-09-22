using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Movement;
using RTSEngine.Game;
using RTSEngine.UnitExtension;
using RTSEngine.Logging;
using RTSEngine.Terrain;

namespace RTSEngine.EntityComponent
{
    public abstract class EntityWorkerManager : MonoBehaviour, IEntityWorkerManager, IEntityPostInitializable
    {
        #region Attributes
        public IEntity Entity {private set; get;}

        [SerializeField, Tooltip("Code to identify this component, unique within the entity")]
        private string code = "unique_code";
        public string Code => code;

        [SerializeField, Tooltip("Size of this array is the max. amount of workers, worker positions can be static if the array's elements are assigned.")]
        private Transform[] workerPositions = new Transform[0];

        [SerializeField, Tooltip("For static worker position, populate to define the types of terrain areas where the fixed worker positions can be placed at.")]
        private TerrainAreaType[] forcedTerrainAreas = new TerrainAreaType[0];

        // Key: index of the worker position in the "workerPositions" array
        // Value: IEntity instance that is occupying that worker position
        private IDictionary<int, IUnit> workers = null;
        public IEnumerable<IUnit> Workers => workers.Values.Where(worker => worker.IsValid());

        public int Amount => Workers.Count();
        public int MaxAmount => workerPositions.Length;
        public bool HasMaxAmount => Amount >= MaxAmount;

        // Game services
        protected IMovementManager mvtMgr { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerAdded;
        public event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerRemoved;
        public void RaiseWorkerAdded (IEntity sender, EntityEventArgs<IUnit> e)
        {
            var handler = WorkerAdded;
            handler?.Invoke(sender, e);
        }
        public void RaiseWorkerRemoved (IEntity sender, EntityEventArgs<IUnit> e)
        {
            var handler = WorkerRemoved;
            handler?.Invoke(sender, e);
        }

        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.mvtMgr = gameMgr.GetService<IMovementManager>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();

            this.Entity = entity;

            workers = Enumerable.Range(0, workerPositions.Length)
                .ToDictionary<int, int, IUnit>(index => index, index => null);

            foreach (Transform workerPosTransform in workerPositions)
            {
                if (workerPosTransform == null)
                    return;

                Vector3 nextWorkerPosition = default;
                if (!logger.RequireTrue(terrainMgr.GetTerrainAreaPosition(workerPosTransform.position, forcedTerrainAreas, out nextWorkerPosition),
                          $"[{GetType().Name} - {Entity.Code}] Unable to update the goto transform position as it is initial position does not comply with the forced terrain areas!"))
                    return;

                workerPosTransform.position = nextWorkerPosition;
            }
        }

        public void Disable() { }
        #endregion

        #region Adding Workers
        public Vector3 GetAddablePosition(IUnit worker) => GetOccupiedPosition(worker, out _);

        // Returns the source entity's position if the worker is not registed as worker in this component or it is registered but no static positions are provided.
        public Vector3 GetOccupiedPosition(IUnit worker, out bool isStaticPosition)
        {
            Transform positionTransform = null;
            if(workers.Where(pair => pair.Value == worker).Any())
                positionTransform = workerPositions[workers.Where(pair => pair.Value == worker).First().Key];

            if (positionTransform)
            {
                isStaticPosition = true;
                return positionTransform.position;
            }
            else
            {
                isStaticPosition = false;
                return Entity.transform.position;
            }
        }

        public ErrorMessage CanMove(IUnit worker, AddableUnitData addableData = default)
        {
            if (!worker.IsValid())
                return ErrorMessage.invalid;
            else if (!worker.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (worker.Health.IsDead)
                return ErrorMessage.dead;

            else if (!addableData.allowDifferentFaction && !RTSHelper.IsSameFaction(worker, Entity))
                return ErrorMessage.factionMismatch;

            // Already reached maxmimum amount and this is a new worker attempting to be added
            else if (HasMaxAmount && !workers.Values.Contains(worker))
            {
                // If no possible destination is available then stop all entity target components of the worker
                worker.SetIdle(); 
                return ErrorMessage.workersMaxAmountReached;
            }

            return ErrorMessage.none;
        }

        public ErrorMessage Move(IUnit worker, AddableUnitData addableData)
        {
            ErrorMessage errorMsg;
            if((errorMsg = CanMove(worker, addableData)) != ErrorMessage.none)
            {
                if (addableData.playerCommand && RTSHelper.IsLocalPlayerFaction(Entity))
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = Entity,
                        target = worker
                    });

                return errorMsg;
            }

            // Getting the addable position is handled here and not in the GetAddablePosition() method
            // This is because when an addable position is determined, the worker entity gets added to the workers list of this worker manager
            // The reason behind that is that we want the worker to reserve a spot in this worker manager before they get to their addable position
            int positionIndex = -1;
            if (workers.Where(pair => pair.Value == worker).Any())
                positionIndex = workers.Where(pair => pair.Value == worker).First().Key;
            else
            {
                positionIndex = workers.Where(pair => !pair.Value.IsValid()).First().Key;

                workers[positionIndex] = worker;

                RaiseWorkerAdded(Entity, new EntityEventArgs<IUnit>(worker));
            }

            Vector3 destination = workerPositions[positionIndex] ? workerPositions[positionIndex].position : Entity.transform.position;
            float radius = workerPositions[positionIndex] ? 0.0f : Entity.Radius;

            return mvtMgr.SetPathDestinationLocal(
                worker,
                destination,
                radius,
                Entity,
                new MovementSource
                {
                    playerCommand = addableData.playerCommand,

                    component = addableData.sourceComponent,

                    targetAddableUnit = this,
                    targetAddableUnitPosition = destination
                });
        }

        // Registering the workers occurs at the Move() methods instead of the Add() methods here
        // It is because we want the worker to reserve a working slot before they get into the working position.
        public ErrorMessage CanAdd(IUnit worker, AddableUnitData addableData = default) => ErrorMessage.undefined;
        public ErrorMessage Add(IUnit worker, AddableUnitData addable = default) => ErrorMessage.undefined;
        #endregion

        #region Removing Workers
        public void Remove(IUnit worker)
        {
            if(workers.Where(pair => pair.Value == worker).Any())
            {
                int positionIndex = workers.Where(pair => pair.Value == worker).First().Key;

                RaiseWorkerRemoved(Entity, new EntityEventArgs<IUnit>(worker));

                workers[positionIndex] = null;
            }
        }
        #endregion
    }
}
