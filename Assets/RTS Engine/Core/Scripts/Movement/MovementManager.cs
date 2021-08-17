using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Search;
using RTSEngine.Attack;
using RTSEngine.Audio;
using RTSEngine.Utilities;

namespace RTSEngine.Movement
{
    public class MovementManager : MonoBehaviour, IMovementManager
    {
        #region Attributes
        [SerializeField, Tooltip("Determines the distance at which a unit stops before it reaches its movement target position.")]
        private float stoppingDistance = 0.3f;
        public float StoppingDistance => stoppingDistance;

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Visible to the local player when they command unit(s) to move to a location.")]
        private GameObject movementTargetEffectPrefab = null;
        public IEffectObject MovementTargetEffect { get; private set; }

        /// <summary>
        /// Handles connecting the pathfinding system and the RTS Engine movement system.
        /// </summary>
        public IMovementSystem MvtSystem { private set; get; }

        private IReadOnlyDictionary<MovementFormationType, IMovementFormationHandler> formationHandlers = null;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IAttackManager attackMgr { private set; get; } 
        protected IGameAudioManager audioMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>(); 

            MvtSystem = gameObject.GetComponent<IMovementSystem>();
            if (!logger.RequireValid(MvtSystem,
                $"[{GetType().Name}] A component that implements the '{typeof(IMovementSystem).Name}' interface must be attached to the object."))
                return;

            if (movementTargetEffectPrefab.IsValid())
                this.MovementTargetEffect = movementTargetEffectPrefab.GetComponent<IEffectObject>();

            formationHandlers = gameObject
                .GetComponents<IMovementFormationHandler>()
                .ToDictionary(handler =>
                {
                    handler.Init(gameMgr);

                    return handler.FormationType;
                });
        }
        #endregion

        #region Setting Path Destination Helper Methods
        private void OnPathDestinationCalculationStart (IEntity entity)
        {
            // Disable the target position marker so it won't intefer in determining the target positions
            entity.MovementComponent.TargetPositionMarker.Toggle(false);
        }
        private void OnPathDestinationCalculationStart (IEnumerable<IEntity> entities)
        {
            foreach(IEntity entity in entities)
                // Disable the target position marker so it won't intefer in determining the target positions
                entity.MovementComponent.TargetPositionMarker.Toggle(false);
        }

        private void OnPathDestinationCalculationInterrupted (IEntity entity)
        {
            entity.MovementComponent.TargetPositionMarker.Toggle(true);
        }
        private void OnPathDestinationCalculationInterrupted (IEnumerable<IEntity> entities)
        {
            foreach(IEntity entity in entities)
                entity.MovementComponent.TargetPositionMarker.Toggle(true);
        }
        #endregion

        #region Setting Path Destination: Single Entity
        public ErrorMessage SetPathDestination(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source)
        {
            return inputMgr.SendInput(
                new CommandInput()
                {
                    sourceMode = (byte)InputMode.entity,
                    targetMode = (byte)InputMode.movement,

                    sourcePosition = entity.transform.position,
                    targetPosition = destination,

                    floatValue = offsetRadius,

                    // MovementSource:
                    code = $"{source.component?.Code}.{source.targetAddableUnit?.Code}",
                    opPosition = source.targetAddableUnitPosition,
                    playerCommand = source.playerCommand,
                    intValues = inputMgr.ToIntValues(source.isAttackMove ? 1 : 0, source.isOriginalAttackMove ? 1 : 0)
                },
                source: entity,
                target: target);
        }

        public ErrorMessage SetPathDestinationLocal(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source)
        {
            if (!logger.RequireValid(entity,
              $"[{GetType().Name}] Can not move an invalid entity!"))
                return ErrorMessage.invalid;
            else if (!entity.CanMove)
                return ErrorMessage.mvtDisabled;

            OnPathDestinationCalculationStart(entity);

            // Used for the movement target effect and rotation look at of the unit
            Vector3 originalDestination = destination; 

            // First check if the actual destination is a valid target position, if it can't be then search for a valid one depending on the movement formation
            // If the offset radius is not zero, then the unit will be moving towards a target entity and a calculation for a path destination around that target is required
            if (offsetRadius > 0.0f
                || IsPositionClear(ref destination, entity.MovementComponent, source.playerCommand) != ErrorMessage.none)
            {
                GeneratePathDestination(
                    entity,
                    destination,
                    offsetRadius,
                    source.playerCommand,
                    out List<Vector3> pathDestinations);

                if (pathDestinations.Count == 0)
                {
                    OnPathDestinationCalculationInterrupted(entity);
                    return ErrorMessage.mvtTargetPositionNotFound;
                }

                // Get the closest target position
                destination = pathDestinations.OrderBy(pos => (pos - entity.transform.position).sqrMagnitude).First();
            }

            if (source.playerCommand && !target.IsValid() && RTSHelper.IsLocalPlayerFaction(entity))
            {
                SpawnMovementTargetEffect(originalDestination);

                audioMgr.PlaySFX(entity.MovementComponent.OrderAudio, false); //play the movement audio.
            }

            return entity.MovementComponent.OnPathDestination(
                new TargetData<IEntity>
                {
                    instance = target,
                    position = destination,
                    opPosition = originalDestination
                },
                source);
        }
        #endregion

        #region Setting Path Destination: Multiple Entities
        public ErrorMessage SetPathDestination(IEnumerable<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, bool playerCommand)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entityGroup,
                targetMode = (byte)InputMode.movement,

                targetPosition = destination,
                floatValue = offsetRadius,

                playerCommand = playerCommand
            },
            source: entities,
            target: target);
        }

        public ErrorMessage SetPathDestinationLocal(IEnumerable<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, bool playerCommand)
        {
            if (!logger.RequireValid(entities,
              $"[{GetType().Name}] Some or all entities that are attempting to move are invalid!"))
                return ErrorMessage.invalid;
            // Only one entity to move? use the dedicated method instead!
            else if (!entities.ElementAtOrDefault(1).IsValid()) 
                return SetPathDestinationLocal(entities.FirstOrDefault(), destination, offsetRadius, target, new MovementSource { playerCommand = playerCommand });

            // Sort the attack units based on their codes, we assume that units that share the same code (which is the defining property of an entity in the RTS Engine) are identical.
            // Additionally, filter out any units that are not movable.
            ChainedSortedList<string, IEntity> sortedMvtSources = RTSHelper.SortEntitiesByCode(
                entities,
                entity => 
                {
                    if(entity.CanMove)
                    {
                        // While filtering for the entities that can move...
                        OnPathDestinationCalculationStart(entity);
                        return true;
                    }
                    return false;
                });

            foreach (List<IEntity> mvtSourceSet in sortedMvtSources.Values) 
            {
                GeneratePathDestination(
                    mvtSourceSet,
                    destination,
                    mvtSourceSet[0].MovementComponent.Formation,
                    offsetRadius,
                    playerCommand,
                    out List<Vector3> pathDestinations);

                if (pathDestinations.Count == 0)
                {
                    OnPathDestinationCalculationInterrupted(mvtSourceSet);
                    return ErrorMessage.mvtTargetPositionNotFound;
                }

                // Compute the directions of the units we have so we know the direction they will face in regards to the target.
                Vector3 unitsDirection = RTSHelper.GetEntitiesDirection(entities, destination);
                unitsDirection.y = 0;

                // Index counter for the generated path destinations.
                int destinationID = 0;
                // Index for the entities in the current set
                int i = 0;

                for (i = 0; i < mvtSourceSet.Count; i++) 
                {
                    IEntity mvtSource = mvtSourceSet[i];

                    // If this movement is towards a target, pick the closest position to the target for each unit
                    if (target.IsValid()) 
                        pathDestinations = pathDestinations.OrderBy(pos => (pos - mvtSource.transform.position).sqrMagnitude).ToList();

                    if (mvtSource.MovementComponent.OnPathDestination(
                        new TargetData<IEntity>
                        {
                            instance = target,
                            position = pathDestinations[destinationID],

                            opPosition = pathDestinations[destinationID] + unitsDirection // Rotation look at position
                        },
                        new MovementSource { playerCommand = playerCommand }) != ErrorMessage.none)
                    {
                        OnPathDestinationCalculationInterrupted(mvtSource);
                        continue;
                    }

                    // Only move to the next path destination if we're moving towards a non target, if not keep removing the first element of the list which was the closest to the last unit
                    if (target.IsValid())
                        pathDestinations.RemoveAt(0);
                    else
                        destinationID++;

                    if (destinationID >= pathDestinations.Count) // No more paths to test, stop moving units.
                        break;
                }

                // If no path destinations could be assigned to the rest of the units, interrupt their path calculation state
                if(i < mvtSourceSet.Count)
                    OnPathDestinationCalculationInterrupted(mvtSourceSet.GetRange(i + 1, mvtSourceSet.Count - (i + 1)));
            }


            if (playerCommand && !target.IsValid() && RTSHelper.IsLocalPlayerFaction(entities.FirstOrDefault()))
            {
                SpawnMovementTargetEffect(destination);

                audioMgr.PlaySFX(entities.First().MovementComponent.OrderAudio, false); //play the movement audio.
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Generating Path Destinations
        public ErrorMessage GeneratePathDestination(IEntity entity, Vector3 targetPosition, float offset, bool playerCommand, out List<Vector3> pathDestinations, System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null)
            => GeneratePathDestination(Enumerable.Repeat(entity, 1), targetPosition, entity.MovementComponent.Formation, offset, playerCommand, out pathDestinations, condition);

        public ErrorMessage GeneratePathDestination(IEnumerable<IEntity> entities, Vector3 targetPosition, MovementFormationSelector formationSelector, float offset, bool playerCommand, out List<Vector3> pathDestinations, System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null)
        {
            // assumptions: All entities are of the same type.

            pathDestinations = new List<Vector3>(); 

            // The unit that will be used as a reference to the rest of the units of the same type.
            IEntity refMvtSource = entities.First(); 

            if (!logger.RequireValid(formationSelector.type,
                $"[{GetType().Name}] Requesting path destinations for entity of code '{refMvtSource.Code}' with invalid formation type!")
                || !logger.RequireTrue(formationHandlers.ContainsKey(formationSelector.type),
                $"[{GetType().Name}] Requesting path destinations for formation of type: '{formationSelector.type.Key}' but no suitable component that implements '{typeof(IMovementFormationHandler).Name}' is found!"))
                return ErrorMessage.invalid;

            // The amount of path destinations that we want to produce.
            int amount = entities.Count();

            // Depending on the ref entity's movable terrain areas, adjust the target position
            terrainMgr.GetTerrainAreaPosition(targetPosition, refMvtSource.MovementComponent.TerrainAreas, out targetPosition);

            ErrorMessage errorMessage;

            // First we need to compute the directions of the units we have so we know the direction they will face in regards to the target.
            Vector3 direction = RTSHelper.GetEntitiesDirection(entities, targetPosition);
            // We also want to handle setting the height by sampling the terrain to get the correct height since there's no way to know it directly.
            direction.y = 0;

            // Holds the amount of attempts made to generate path destinations but resulted in no generated positions.
            int emptyAttemptsCount = 0;
            // In case the attack formation is switched due to max empty attempts or an error then we want to reset the offset.
            float originalOffset = offset;

            while (amount > 0)
            {
                // In case the path destination generation methods result into a failure, return with the failure's error code.
                if ((errorMessage = formationHandlers[formationSelector.type].GeneratePathDestinations(
                    new PathDestinationInputData
                    {
                        refMvtComp = refMvtSource.MovementComponent,

                        targetPosition = targetPosition,
                        direction = direction,

                        formationSelector = formationSelector,

                        condition = condition,
                        
                        playerCommand = playerCommand
                    },
                    ref amount,
                    ref offset,
                    ref pathDestinations,
                    out int generatedAmount)) != ErrorMessage.none || emptyAttemptsCount >= formationHandlers[formationSelector.type].MaxEmptyAttempts)
                {
                    // Reset empty attemps count and offset for next fallback formation type
                    emptyAttemptsCount = 0;
                    offset = originalOffset;

                    // Current formation type could not compute all path destinations then generate path destinations with the fall back formation if there's one
                    if (formationHandlers[formationSelector.type].FallbackFormationType != null)
                    {
                        formationSelector = new MovementFormationSelector
                        {
                            type = formationHandlers[formationSelector.type].FallbackFormationType,
                            properties = formationSelector.properties
                        };

                        continue;
                    }

                    // No fallback formation? exit!
                    return errorMessage;
                }

                // Only if the last attempt resulted in no generated path destinations.
                if (generatedAmount == 0)
                    emptyAttemptsCount++;
            }

            // We have computed at least one path destination, the count of the list is either smaller or equal to the initial value of the "amount" argument.
            return ErrorMessage.none; 
        }
        #endregion

        #region Generating Path Destinations Helper Methods
        public ErrorMessage IsPositionClear(ref Vector3 targetPosition, IMovementComponent refMvtComp, bool playerCommand)
            => IsPositionClear(ref targetPosition, refMvtComp.Controller.Radius, refMvtComp.Controller.NavigationAreaMask, refMvtComp.TerrainAreas, playerCommand);

        public ErrorMessage IsPositionClear(ref Vector3 targetPosition, float agentRadius, LayerMask navAreaMask, IEnumerable<TerrainAreaType> terrainAreas, bool playerCommand)
        {
            ErrorMessage errorMessage;
            if ((errorMessage = gridSearch.IsPositionReserved(targetPosition, agentRadius, terrainAreas, playerCommand)) != ErrorMessage.none)
                return errorMessage;

            else if (TryGetMovablePosition(targetPosition, agentRadius, navAreaMask, out targetPosition))
                return ErrorMessage.none;

            return ErrorMessage.mvtPositionNavigationOccupied;
        }

        public bool TryGetMovablePosition(Vector3 center, float radius, LayerMask areaMask, out Vector3 movablePosition)
            => MvtSystem.TryGetValidPosition(center, radius, areaMask, out movablePosition);

        public bool GetRandomMovablePosition(IEntity entity, Vector3 origin, float range, out Vector3 targetPosition, bool playerCommand)
        {
            targetPosition = entity.transform.position;
            if (entity == null || !entity.CanMove)
                return false;

            // Pick a random direction to go to
            Vector3 randomDirection = Random.insideUnitSphere * range; 
            randomDirection += origin;
            randomDirection.y = terrainMgr.SampleHeight(randomDirection, entity.MovementComponent);

            // Get the closet movable point to the randomly chosen direction
            if (MvtSystem.TryGetValidPosition(randomDirection, range, entity.MovementComponent.Controller.NavigationAreaMask, out targetPosition)
                && IsPositionClear(ref targetPosition, entity.MovementComponent, playerCommand) == ErrorMessage.none)
                return true;

            return false;
        }
        #endregion

        #region Movement Helper Methods
        private void SpawnMovementTargetEffect(Vector3 position)
        {
            effectObjPool.Spawn(
                attackMgr.CanMoveAttack && attackMgr.MoveAttackTargetEffect.IsValid() 
                ? attackMgr.MoveAttackTargetEffect
                : MovementTargetEffect,
                position);
        }
        #endregion
    }
}