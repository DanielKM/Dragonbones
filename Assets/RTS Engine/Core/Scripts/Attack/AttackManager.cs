using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Audio;
using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Controls;
using RTSEngine.Utilities;

namespace RTSEngine.Attack
{
    public class AttackManager : MonoBehaviour, IAttackManager
    {
        #region Attributes
        [Header("Terrain Attack")]
        [SerializeField, Tooltip("Allow faction entities that do not require a target to launch attacks on the terrain?")]
        private bool terrainAttackEnabled = true;
        [SerializeField, Tooltip("If terrain attack is enabled, this represents the key that the player can use while selecting an attack faction entity to directly launch a terrain attack.")]
        private ControlType terrainAttackKey = null;
        //private KeyCode terrainAttackKey = KeyCode.T;
        public bool IsTerrainAttackKeyDown => controls.Get(terrainAttackKey);

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Visible to the local player when they command unit(s) to perform a terrain attack on a location.")]
        private GameObject terrainAttackTargetEffectPrefab = null;
        public IEffectObject TerrainAttackTargetEffect { get; private set; }

        [Header("Move-Attack")]
        [SerializeField, Tooltip("Allow movable units with an Attack component to search for attack targets while moving towards their destinations when a movement command is set by the player while holding down the below key?")]
        private bool moveAttackEnabled = true;
        [SerializeField, Tooltip("Key that must be held down by the local player while launching a movement command into a movable unit to allow it to search for attack targets while moving.")]
        private ControlType moveAttackKey = null;
        //private KeyCode moveAttackKey = KeyCode.M;
        public bool CanMoveAttack { private set; get; } 
        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Visible to the local player when they command unit(s) to perform a move-attack.")]
        private GameObject moveAttackTargetEffectPrefab = null;
        public IEffectObject MoveAttackTargetEffect { get; private set; }

        // Game services
        protected IMovementManager mvtMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();

            if (terrainAttackTargetEffectPrefab.IsValid())
                this.TerrainAttackTargetEffect = terrainAttackTargetEffectPrefab.GetComponent<IEffectObject>();

            if (moveAttackTargetEffectPrefab.IsValid())
                this.MoveAttackTargetEffect = moveAttackTargetEffectPrefab.GetComponent<IEffectObject>();

            // Move attack initial state
            CanMoveAttack = false;
            if (!moveAttackEnabled)
                enabled = false;

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Handling Terrain Attack
        public bool CanLaunchTerrainAttack<T>(LaunchAttackData<T> data)
        {
            return terrainAttackEnabled && (IsTerrainAttackKeyDown || data.allowTerrainAttack);
        }
        #endregion

        #region Handling Attack-Move
        private void Update()
        {
            if (!moveAttackEnabled)
                return;

            CanMoveAttack = controls.Get(moveAttackKey);
        }
        #endregion

        #region Launching Attack: Multiple Attackers
        public ErrorMessage LaunchAttack(LaunchAttackData<IEnumerable<IEntity>> data)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entityGroup,
                targetMode = (byte)InputMode.attack,

                targetPosition = data.targetPosition,

                playerCommand = data.playerCommand
            },
            source: data.source,
            target: data.targetEntity);
        }

        public ErrorMessage LaunchAttackLocal(LaunchAttackData<IEnumerable<IEntity>> data)
        {
            if (!logger.RequireValid(data.source,
              $"[{GetType().Name}] Some or all entities that are attempting to attack are invalid!"))
                return ErrorMessage.invalid;
            else if (!data.source.ElementAtOrDefault(1).IsValid())
                return LaunchAttackLocal(
                    new LaunchAttackData<IEntity>
                    {
                        source = data.source.FirstOrDefault(),
                        targetEntity = data.targetEntity,
                        targetPosition = data.targetPosition,
                        playerCommand = data.playerCommand
                    });
            else if (!data.targetEntity.IsValid() && !CanLaunchTerrainAttack(data))
                return ErrorMessage.attackTerrainDisabled;

            // Take out the attack entities which do not use a movement component, for those, a direct target set is done where the attack position is the current entity position.
            IEnumerable<IEntity> nonMovableAttackers = data.source.Where(a => !a.CanMove);
            IEnumerable<IEntity> movableAttackers = data.source.Except(nonMovableAttackers);

            // We first start by handling the movable attackers

            // Sort the attack entities based on their codes, we assume that units that share the same code (which is the defining property of an entity in the RTS Engine) are identical.
            // And filter out any units that do not have an attack component.
            ChainedSortedList<string, IEntity> sortedAttackers = RTSHelper.SortEntitiesByCode(movableAttackers, x => x.CanAttack);

            // At least one attacker to get the attack order audio from.
            IEntity refAttacker = null; 

            foreach (List<IEntity> attackerSet in sortedAttackers.Values)
            {
                // If the current unit type is unable to have the entity as the target, move to the next unit type list
                if (attackerSet[0].AttackComponent.IsTargetValid(RTSHelper.ToTargetData(data.targetEntity), data.playerCommand) != ErrorMessage.none)
                    continue;

                // Generate movement path destinations for the current list of identical unit types:
                mvtMgr.GeneratePathDestination(
                    attackerSet,
                    data.targetPosition,
                    attackerSet[0].AttackComponent.Formation.MovementFormation,
                    attackerSet[0].AttackComponent.Formation.GetStoppingDistance(data.targetEntity, min: true),
                    data.playerCommand,
                    out List<Vector3> pathDestinations,
                    condition: RTSHelper.IsAttackLOSBlocked);

                // No valid path destinations generated? do not continue as there is nowhere to move to
                if (pathDestinations.Count == 0)
                    continue;

                // Index counter for the generated path destinations.
                int destinationID = 0;

                foreach (IEntity attacker in attackerSet)
                {
                    // If this attack movement is towards a target, pick the closest position to the target for each unit
                    if (data.targetEntity.IsValid())
                        pathDestinations = pathDestinations.OrderBy(pos => (pos - attacker.transform.position).sqrMagnitude).ToList();

                    // If current unit is able to engage with its target using the computed path then move to the next path, if not, test the path on the next unit.
                    // The last argument of the SetTarget method is set to the playerCommand because we still want to move the units to computed attack position...
                    // ...even if it is out of the attack range because the player issued the attack/movement command.
                    if (attacker.AttackComponent.SetTargetLocal(
                        new TargetData<IFactionEntity>
                        {
                            instance = data.targetEntity,
                            opPosition = data.targetPosition,

                            position = pathDestinations[destinationID]
                        }, data.playerCommand)
                        .In(PlayerMessageHandler.SuccessErrorMessages.Append(ErrorMessage.attackMoveToTargetOnly)))
                    {
                        // Assign the reference unit from which the attack order will be played.
                        if (!refAttacker.IsValid()) 
                            refAttacker = attacker;

                        // Only move to the next path destination if we're not attacking a valid target (terrain attack?), if not keep removing the first element of the list which was the closest to the last unit
                        if (!data.targetEntity.IsValid())
                            destinationID++;
                        else
                            pathDestinations.RemoveAt(0);

                        // No more paths to test, stop moving units to attack.
                        if (destinationID >= pathDestinations.Count)
                            break;
                    }
                }
            }

            // Finally handle setting targets for the non movable attackers
            foreach (IEntity attacker in nonMovableAttackers)
            {
                // Assign the reference unit from which the attack order will be played, if none has been assigned yet.
                if (!refAttacker.IsValid())
                    refAttacker = attacker;

                attacker.AttackComponent?.SetTargetLocal(
                    new TargetData<IFactionEntity>
                    {
                        instance = data.targetEntity,
                        opPosition = data.targetEntity.IsValid() ? data.targetEntity.transform.position : data.targetPosition,

                        position = attacker.transform.position
                    },
                    data.playerCommand);
            }

            if (data.playerCommand && refAttacker.IsValid() && refAttacker.IsLocalPlayerFaction())
            {
                if (!data.targetEntity.IsValid())
                    effectObjPool.Spawn(TerrainAttackTargetEffect, data.targetPosition);

                audioMgr.PlaySFX(refAttacker.AttackComponent.OrderAudio, false);
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Launching Attack: Single Attacker
        public ErrorMessage LaunchAttack(LaunchAttackData<IEntity> data)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.attack,
                sourcePosition = data.source.transform.position,
                targetPosition = data.targetPosition,
                playerCommand = data.playerCommand
            },
            source: data.source,
            target: data.targetEntity);
        }

        public ErrorMessage LaunchAttackLocal(LaunchAttackData<IEntity> data)
        {
            if (!logger.RequireValid(data.source,
              $"[{GetType().Name}] Can not attack with an invalid entity!"))
                return ErrorMessage.invalid;
            else if (!data.source.CanAttack)
                return ErrorMessage.attackDisabled;
            else if (!data.targetEntity.IsValid() && !CanLaunchTerrainAttack(data))
                return ErrorMessage.attackTerrainDisabled;

            ErrorMessage errorMsg;
            if ((errorMsg = data.source.AttackComponent.IsTargetValid(RTSHelper.ToTargetData(data.targetEntity), data.playerCommand)) != ErrorMessage.none) //check whether the new target is valid for this attack type.
            {
                if (data.playerCommand && data.source.IsLocalPlayerFaction())
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = data.source,
                        target = data.targetEntity
                    });
                return errorMsg;
            }

            // If the attack order was issued by the local player and this is the local player's instance.
            if (data.playerCommand && data.source.IsLocalPlayerFaction())
            {
                if (!data.targetEntity.IsValid())
                    effectObjPool.Spawn(TerrainAttackTargetEffect, data.targetPosition);

                audioMgr.PlaySFX(data.source.AttackComponent.OrderAudio, false);
            }

            // Calculate a target attack position and attempt to set a new attack target for the source unit.
            return data.source.AttackComponent.SetTargetLocal(
                new TargetData<IFactionEntity>
                {
                    instance = data.targetEntity,
                    opPosition = data.targetPosition,

                    position = data.source.CanMove && TryGetAttackPosition(data.source, data.targetEntity, data.targetPosition, data.playerCommand, out Vector3 attackPosition)
                        ? attackPosition
                        : data.source.transform.position
                },
                data.playerCommand);
        }
        #endregion

        #region Generating Attack Position
        public bool TryGetAttackPosition(IEntity attacker, IFactionEntity target, Vector3 targetPosition, bool playerCommand, out Vector3 attackPosition)
        {
            attackPosition = Vector3.positiveInfinity;

            if(!logger.RequireTrue(attacker.IsValid() && attacker.CanAttack,
                $"[{GetType().Name} - {attacker.Code}] Can not calculate an attack position with an invalid entity instance or a non attack entity!"))
                return false;

            // Generate movement attack path destination for the new target
            mvtMgr.GeneratePathDestination(
                attacker,
                targetPosition,
                attacker.AttackComponent.Formation.GetStoppingDistance(target, min: true),
                playerCommand,
                out List<Vector3> pathDestinations,
                condition: RTSHelper.IsAttackLOSBlocked);

            // If there's a valid attack movement destination produced, get the closest target position
            if (logger.RequireTrue(pathDestinations.Count > 0,
                $"[{GetType().Name} - {attacker.Code}] Unable to locate a path destination for the position: {targetPosition} and target: {target}!"))
            {
                attackPosition = pathDestinations.OrderBy(pos => (pos - attacker.transform.position).sqrMagnitude).First();
                return true;
            }

            return false;
        }
        #endregion
    }
}