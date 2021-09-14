using System;

using UnityEngine;

using RTSEngine.Attack;
using RTSEngine.Entities;
using RTSEngine.Movement;
using RTSEngine.Event;

namespace RTSEngine.EntityComponent
{
    public class SpellAttack : FactionEntityAttack
    {
        #region Attributes
        private ISpell unit;

        [SerializeField, Tooltip("Defines the unit's stopping distance and movement formation when engaging in an attack.")]
        private AttackFormationSelector formation = new AttackFormationSelector(); 
        public override AttackFormationSelector Formation => formation;

        [SerializeField, Tooltip("Enable to allow the unit to engage its target while moving.")]
        private bool moveOnAttack = false; 
        [SerializeField, Tooltip("If the target leaves the attack range then this represents how far is the attacker willing to follow their target before giving up on them.")]
        private float followDistance = 15.0f;
        // Holds the distance between the target and the attacker when the attacker first enters in range of the target.
        private float initialEngagementDistance = 0.0f;

        // Caches the unit's movement target and source when the unit is moving with attack-move enabled so that the unit can continue its movement after potentially stopping to attack on the way.
        private TargetData<IEntity> lastAttackMoveTarget;
        private MovementSource lastAttackMoveSource;
        // True only if the unit is in an attack-move mode
        private bool inAttackMoveMode;
        #endregion

        #region Initializing/Terminating
        protected override void OnAttackInit()
        {
            this.unit = factionEntity as ISpell;

            formation.Init(this);

            // unit.MovementComponent.MovementStart += HandleMovementStart;
            // unit.MovementComponent.MovementStop += HandleMovementStop;
        }

        protected override void OnAttackStop(TargetData<IFactionEntity> lastTarget, bool wasInProgress)
        {
            // unit.MovementComponent.UpdateRotationTarget(null, lastTarget.opPosition);

            // if (inAttackMoveMode)
            //     unit.MovementComponent.SetTarget(lastAttackMoveTarget, mvtMgr.StoppingDistance, lastAttackMoveSource);
        }

        protected override void OnAttackDisabled()
        {
            // unit.MovementComponent.MovementStart -= HandleMovementStart;
            // unit.MovementComponent.MovementStop -= HandleMovementStop;
        }
        #endregion

        #region Handling Event: Movement Start/Stop & Attack-Move Handling
        // Must be a player command movement to launch
        // If player command movement is launched then, it can also disable attack move mode if canMoveAttack is disabled
        private void HandleMovementStart(IMovementComponent sender, MovementEventArgs args)
        {
            // In case the movement source is not one that is part of an attack-move chain
            // While it is a player issued command or the attack-move can not be initiated
            // Then disable the attack-move mode in the case that it was disabled before.
            if(!args.Source.isAttackMove
                && (!args.Source.playerCommand || !attackMgr.CanMoveAttack))
            { 
                DisableAttackMove();
                return;
            }

            // If the movement source is part of an attack-move chain but it is not the initiator of that chain
            // See if this movement (target) is the original attack-move chain initiator or not, if yes then update the target destination but not the source (since the movement manager recalculates it slightly differently)
            if (args.Source.isAttackMove && !attackMgr.CanMoveAttack)
            {
                if(args.Source.isOriginalAttackMove)
                    lastAttackMoveTarget = unit.MovementComponent.Target;
                return;
            }

            // At this point this movement start command is the initiator of a new attack-move chain
            // Therefore, we set the "isOriginalAttackMove" boolean to true and cache the target and source of this attack-move initiator command
            lastAttackMoveSource = new MovementSource
            {
                component = args.Source.component,

                targetAddableUnit = args.Source.targetAddableUnit,
                targetAddableUnitPosition = args.Source.targetAddableUnitPosition,

                playerCommand = false,

                isAttackMove = true,
                isOriginalAttackMove = true,
            };
            lastAttackMoveTarget = unit.MovementComponent.Target;

            // Disable idle only target search so that the attack unit can move and search for potential attack targets.
            TargetFinder.IdleOnly = false;

            inAttackMoveMode = true;
        }

        private void HandleMovementStop(IMovementComponent sender, EventArgs args)
        {
            if (!inAttackMoveMode
                || !unit.MovementComponent.IsPositionReached(lastAttackMoveTarget.position))
                return;

            // When the unit is stopping movement command that is the initiator of an attack-move chain (by checking if the initiator attack-move command's destination is reached)
            // Then stop the attack-move chain for this unit and revert its idle target finder options
            DisableAttackMove();
        }
        private void DisableAttackMove()
        {
            TargetFinder.IdleOnly = TargetFinderData.idleOnly;
            inAttackMoveMode = false;
        }
        #endregion

        #region Engaging Target
        protected override bool MustStopProgress()
        {
            if (base.MustStopProgress())
                return true;

            //attacker can not move
            else if(!unit.CanMove)
            {
                // Target has already entered the attacking range but it is no longer there or it is blocked by an obstacle.
                if (IsInTargetRange
                    && (!IsTargetInRange(transform.position, Target)
                        || LineOfSight.IsObstacleBlocked(transform.position, Target.instance.transform.position)))
                    return true;
            }
            //attacker unit is movable
            else
            {
                //attacker has a unit as a target (movable target) and it is currently moving.
                if (Target.instance.IsValid()
                    && Target.instance.CanMove)
                {
                    //the target leaves the allowed follow distance of the attacker after the target being once in the attack range.
                    if(IsInTargetRange
                        && Vector3.Distance(transform.position, RTSHelper.GetAttackTargetPosition(Target)) > Mathf.Max(followDistance, initialEngagementDistance))
                    {
                        //stop attack as the attacker can not follow its target anymore.
                        return true;
                    }

                    // Either attacker is not moving and it is not inside the attack range.
                    // Or target is now blocked by an obstacle
                    // Or Target might have moved but it is still inside the attacking range but it might have moved enough to trigger a re-calculation for the attack position
                    if( (!unit.MovementComponent.HasTarget && !IsTargetInRange(transform.position, Target))
                        || LineOfSight.IsInSight(Target) != ErrorMessage.none 
                        || formation.MustUpdateAttackPosition(Target.opPosition, RTSHelper.GetAttackTargetPosition(Target), unit.MovementComponent.Destination, Target.instance))
                    {
                        TargetData<IFactionEntity> lastTarget = new TargetData<IFactionEntity> { instance = Target.instance, position = Target.instance.transform.position };

                        SetTarget(lastTarget, playerCommand: false);

                        return false;
                    }
                    else if(LineOfSight.IsAngleBlocked(transform, Target.instance.transform.position))
                    {
                        unit.MovementComponent.UpdateRotationTarget(Target.instance, Target.instance.transform.position);

                        return false;
                    }
                }
            }

            return false;
        }

        protected override bool CanEnableProgress()
        {
            return base.CanEnableProgress()
                //make sure that if the unit is moving then it is allowed to attack on movement
                && (moveOnAttack || !unit.MovementComponent.HasTarget)
                && IsTargetInRange(transform.position, Target);
        }

        protected override void OnEnterTargetRange()
        {
            base.OnEnterTargetRange();

            initialEngagementDistance = Vector3.Distance(transform.position, RTSHelper.GetAttackTargetPosition(Target));
        }

        protected override void OnInProgressEnabled()
        {
            base.OnInProgressEnabled();
        }

        protected override void OnComplete()
        {
            base.OnComplete();
            unit.AnimatorController.LockState = false;
        }
        #endregion

        #region Handling/Calculating Attack Position
        private ErrorMessage TryUpdateValidAttackPosition (IFactionEntity potentialTarget, bool forceInRange, out Vector3 nextAttackPosition)
        {
            nextAttackPosition = default;

            if (potentialTarget == null)
                return ErrorMessage.invalid;

            attackMgr.TryGetAttackPosition(unit, potentialTarget, potentialTarget.transform.position, playerCommand: false, out nextAttackPosition);

            // If we are forcing the attack position to be in the attack's range.
            if (forceInRange && !IsTargetInRange(nextAttackPosition, RTSHelper.ToTargetData(potentialTarget)))
                return ErrorMessage.attackPositionNotFound;

            return ErrorMessage.none;
        }

        public override bool IsTargetInRange (Vector3 attackPosition, TargetData<IEntity> target)
        {
            return formation.IsTargetInRange(attackPosition, target);
        }
        #endregion

        #region Searching/Updating Target
        public override ErrorMessage SetTargetLocal (TargetData<IEntity> newTarget, bool playerCommand)
        {
            // newTarget.position: Generated attack path destination
            // newTarget.opPosition: Target position when the attack order is made
            ErrorMessage errorMessage = ErrorMessage.none;

            // Unit can already attack from its position, inform AttackManager about it (which might have called this method).
            if (IsTargetInRange(transform.position, newTarget)
                && !LineOfSight.IsObstacleBlocked(transform.position, RTSHelper.GetAttackTargetPosition(newTarget))) 
                errorMessage = ErrorMessage.attackAlreadyInPosition;
            else if (!IsTargetInRange(newTarget.position, newTarget)) //check if the attack position is outside the unit's attacking range.
            {
                //if we're allowed to move even if the attack position is out of range then do it.
                if (unit.MovementComponent.IsActive && playerCommand)
                {
                    //move towards attack position without attacking the target.
                    //bypass MovementManager and directly move unit (even in multiplayer games) since this is called in a local synced method.
                    unit.MovementComponent.OnPathDestination(newTarget, new MovementSource { playerCommand = false });

                    return ErrorMessage.attackMoveToTargetOnly; //if an attack unit is supposed to move even if it is out of range then no error is produced.
                }

                return ErrorMessage.attackPositionOutOfRange;
            }
            else if (LineOfSight.IsObstacleBlocked(newTarget.position, RTSHelper.GetAttackTargetPosition(newTarget)))
            {
                return ErrorMessage.LOSObstacleBlocked;
            }

            base.SetTargetLocal(newTarget, playerCommand);

            bool updateRotation = true; //update rotation directly instead of allowing the UnitMovement component to update it (in case unit should not be moved)

            if (unit.MovementComponent.IsActive && errorMessage != ErrorMessage.attackAlreadyInPosition) //only if the current unit's position is not valid for the attack
            {
                updateRotation = false;
                //move towards attack position and supply attack-move mode
                unit.MovementComponent.OnPathDestination(newTarget, new MovementSource { component = this, playerCommand = playerCommand, isAttackMove = inAttackMoveMode });
            }
            else //current unit position is valid for attack, do not move but set rotation and mark attack range as entered.
                unit.MovementComponent.Stop(); //stop unit from moving in case they were already moving.

            if (updateRotation)
                unit.MovementComponent.UpdateRotationTarget(Target.instance, Target.instance.IsValid() ? Target.instance.transform.position : Target.opPosition);

            return errorMessage;
        }
        #endregion
    }
}
