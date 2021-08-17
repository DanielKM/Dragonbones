using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Animation;
using RTSEngine.Determinism;
using RTSEngine.Audio;
using RTSEngine.Effect;

namespace RTSEngine.EntityComponent
{
    public abstract class FactionEntityTargetProgressComponent<T> : FactionEntityTargetComponent<T> where T : IEntity
    {
        #region Attributes
        public override bool IsIdle => !HasTarget;

        // Active Progress:
        [SerializeField, Tooltip("What audio clip to play when the component enters the progress state and starts affecting the target?")]
        private AudioClipFetcher progressEnabledAudio = new AudioClipFetcher();

        [SerializeField, Tooltip("Allows to have a custom progress animaiton when the component enters progress state and starts affecting the target.")]
        protected AnimatorOverrideControllerFetcher progressOverrideController = new AnimatorOverrideControllerFetcher();

        [SerializeField, Tooltip("When having an active target, how long does it take for the component to progress and affect the target?")]
        protected float progressDuration = 1.0f;
        private TimeModifiedTimer progressTimer;

        [SerializeField, Tooltip("The maximum allowed distance between the faction entity and its target so that progress remains active."), Min(0.0f)]
        private float progressMaxDistance = 1.0f;
        public float ProgressMaxDistance => progressMaxDistance;

        /// <summary>
        /// Is the faction entity currently actively working the entity component?
        /// </summary>
        public bool InProgress { get; private set; } = false;

        [SerializeField, Tooltip("Activated when the faction entity's component is in progress.")]
        protected GameObject inProgressObject;

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Triggered on the source faction entity when the component is in progress.")]
        private GameObject sourceEffect = null; 
        private IEffectObject currSourceEffect;

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Triggered on the target when the component is in progress.")]
        private GameObject targetEffect = null; 
        private IEffectObject currTargetEffect;

        #endregion

        #region Initializing/Terminating
        protected sealed override void OnInit()
        {
            progressTimer = new TimeModifiedTimer(progressDuration);

            OnProgressInit();
        }

        protected virtual void OnProgressInit() { }
        #endregion

        #region Updating Component State
        private void Update()
        {
            if (!IsInitialized
                || !IsActive
                || factionEntity.Health.IsDead) //if the faction entity is dead, do not proceed.
                return;

            OnUpdate();

            if (HasTarget) //unit has target -> active
                TargetUpdate(); //on active update
            else //no target? -> inactive
                NoTargetUpdate();
        }

        protected virtual void OnUpdate() { }

        protected abstract bool MustStopProgress();
        protected abstract bool CanEnableProgress();
        protected abstract bool CanProgress();
        protected abstract bool MustDisableProgress();

        private void TargetUpdate()
        {
            if (Target.instance?.IsInteractable == false
                || MustStopProgress())
            {
                Stop();
                return;
            }

            if (!InProgress && CanEnableProgress())
                EnableProgress();

            if (InProgress && CanProgress())
            {
                if(progressTimer.ModifiedDecrease())
                {
                    OnProgress();
                    progressTimer.Reload();
                }

                if (MustDisableProgress())
                    DisableProgress();
            }

            OnTargetUpdate();
        }

        protected virtual void OnTargetUpdate() { }

        private void NoTargetUpdate()
        {
            if (InProgress == true)
                Stop(); //cancel job

            OnNoTargetUpdate();
        }

        protected virtual void OnNoTargetUpdate() { }
        #endregion

        #region Stopping
        protected sealed override bool CanStop() => InProgress;

        protected sealed override void OnStop(TargetData<T> lastTarget)
        {
            bool wasInProgress = InProgress;

            DisableProgress();

            if (factionEntity.AnimatorController.IsValid())
            {
                factionEntity.AnimatorController.ResetAnimatorOverrideControllerOnIdle();
                factionEntity.AnimatorController.SetState(AnimatorState.idle);
            }

            OnStop(lastTarget, wasInProgress);
        }

        protected virtual void OnStop(TargetData<T> lastTarget, bool wasInProgress) { }
        #endregion

        #region Handling Progress
        private void EnableProgress()
        {
            audioMgr.PlaySFX(factionEntity.AudioSourceComponent, progressEnabledAudio.Fetch(), loop: true);

            progressTimer.Reload(); //start timer
            InProgress = true; //the unit's job is now in progress

            OnInProgressEnabledEffects();

            if (factionEntity.CanMove)
                factionEntity.MovementComponent.Stop();

            factionEntity.AnimatorController?.SetOverrideController(progressOverrideController.Fetch());

            factionEntity.AnimatorController?.SetState(AnimatorState.inProgress);

            OnInProgressEnabled();
        }

        protected virtual void OnInProgressEnabledEffects ()
        {
            if (inProgressObject.IsValid()) //show the in progress object
                inProgressObject.SetActive(true);

            ToggleSourceTargetEffect(true); //enable the source and target effect objects
        }

        protected virtual void OnInProgressEnabled () { }

        protected virtual void OnProgress() { }

        protected void DisableProgress()
        {
            InProgress = false;

            OnInProgressDisabledEffects();

            OnProgressDisabled();
        }

        protected virtual void OnInProgressDisabledEffects()
        {
            if (inProgressObject.IsValid())
                inProgressObject.SetActive(false);

            ToggleSourceTargetEffect(false);
        }

        protected virtual void OnProgressDisabled() { }
        #endregion

        #region Progress Effects
        protected void ToggleSourceTargetEffect (bool enable)
        {
            if (!enable)
            {
                if (currSourceEffect.IsValid()) //if the source unit effect was assigned and it's still valid
                {
                    currSourceEffect.Deactivate(); //stop it
                    currSourceEffect = null;
                }

                if (currTargetEffect.IsValid()) //if a target effect was assigned and it's still valid
                {
                    currTargetEffect.Deactivate(); //stop it
                    currTargetEffect = null;
                }

                return;
            }

            if (sourceEffect.IsValid())
                currSourceEffect = effectObjPool.Spawn(
                    sourceEffect,
                    factionEntity.transform.position,
                    sourceEffect.transform.rotation,
                    factionEntity.transform,
                    false); //spawn the source effect on the source unit and don't enable the life timer

            if (targetEffect.IsValid())
                currTargetEffect = effectObjPool.Spawn(
                    targetEffect,
                    Target.instance.transform.position,
                    targetEffect.transform.rotation,
                    Target.instance.transform,
                    false); //spawn the target effect on the target and don't enable the life timer
        }
        #endregion

        #region Searching/Updating Target
        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            return Vector3.Distance(sourcePosition, target.instance.transform.position) <= progressMaxDistance + target.instance.Radius;
        }

        protected sealed override void OnTargetPreLocked(bool playerCommand, TargetData<IEntity> newTarget, bool sameTarget) 
        {
            DisableProgress();
        }
        #endregion
    }
}
