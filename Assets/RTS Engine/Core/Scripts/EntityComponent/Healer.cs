using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Movement;

namespace RTSEngine.EntityComponent
{
    public partial class Healer : FactionEntityTargetProgressComponent<IFactionEntity>
    {
        #region Class Attributes
        [SerializeField, Tooltip("When assigned a target, this is the stopping distance that the healer will have when moving towards the target"), Min(0.0f)]
        private float stoppingDistance = 5.0f;

        [SerializeField, Tooltip("Health amount to add to target every progress round.")]
        private int healthPerProgress = 5;
        public int HealthPerProgress
        {
            set
            {
                if (value > 0)
                    healthPerProgress = value;
            }
            get => healthPerProgress;
        }

        [SerializeField, Tooltip("Define the faction entities that can be healed by this healer.")]
        private HealerTargetPicker targetPicker = new HealerTargetPicker();
        #endregion

        #region Updating Component State
        protected override bool MustStopProgress()
        {
            return Target.instance.Health.IsDead
                || Target.instance.Health.CurrHealth >= Target.instance.Health.MaxHealth
                || !Target.instance.IsFriendlyFaction(factionEntity)
                || (InProgress && !IsTargetInRange(factionEntity.transform.position, Target));
        }

        protected override bool CanEnableProgress()
        {
            return IsTargetInRange(factionEntity.transform.position, Target);
        }

        protected override bool CanProgress() => true;

        protected override bool MustDisableProgress() => false;
        #endregion

        #region Handling Progress
        protected override void OnInProgressEnabled()
        {
            base.OnInProgressEnabled();

            globalEvent.RaiseEntityComponentTargetStartGlobal(this, new TargetDataEventArgs(Target));
        }

        protected override void OnProgress()
        {
            Target.instance.Health.Add(healthPerProgress, factionEntity);
        }
        #endregion

        #region Searching/Updating Target
        public override ErrorMessage IsTargetValid (TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IFactionEntity> potentialTarget = testTarget;

            if (!potentialTarget.instance.IsValid() || potentialTarget.instance == factionEntity)
                return ErrorMessage.invalid;
            // In the case of a building that is yet to be constructed, we check using the CanLaunchTask property of the target (which takes into accoun the construction status in case target is a building).
            else if (!potentialTarget.instance.IsInteractable || !potentialTarget.instance.CanLaunchTask)
                return ErrorMessage.uninteractable;
            else if (!potentialTarget.instance.IsFriendlyFaction(factionEntity))
                return ErrorMessage.factionMismatch;
            else if (!targetPicker.IsValidTarget(this, potentialTarget.instance))
                return ErrorMessage.entityCompTargetPickerUndefined;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.dead;
            else if (potentialTarget.instance.Health.HasMaxHealth)
                return ErrorMessage.healthtMaxReached;
            else if (!factionEntity.CanMove && !IsTargetInRange(factionEntity.transform.position, potentialTarget))
                return ErrorMessage.entityCompTargetOutOfRange;

            return ErrorMessage.none;
        }

        protected override void OnTargetPostLocked(bool playerCommand, bool sameTarget)
        {
            globalEvent.RaiseEntityComponentTargetLockedGlobal(this, new TargetDataEventArgs(Target));

            if (!factionEntity.CanMove)
                return;

            if (!IsTargetInRange(factionEntity.transform.position, Target))
                factionEntity.MovementComponent.SetTarget(
                    Target,
                    stoppingDistance,
                    new MovementSource
                    {
                        component = this,

                        playerCommand = false
                    });
            else
                factionEntity.MovementComponent.UpdateRotationTarget(Target.instance, Target.position);
        }
        #endregion
    }
}
