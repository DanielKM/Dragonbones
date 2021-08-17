using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.UnitExtension;
using RTSEngine.Logging;

namespace RTSEngine.Animation
{
    public class UnitAnimatorController : MonoBehaviour, IAnimatorController, IEntityPostInitializable
    {
        #region Class Attributes
        public IUnit Unit { private set; get; }

        [SerializeField, Tooltip("Animator responsible for playing the unit animaiton clips."), Header("General")]
        private Animator animator = null;
        public Animator Animator => animator;
        private TimeModifiedFloat animatorSpeed;

        public AnimatorState CurrState { private set; get; }

        [SerializeField, Tooltip("The default animator override controller of the unit.")]
        private AnimatorOverrideControllerFetcher animatorOverrideController = new AnimatorOverrideControllerFetcher();

        public bool LockState { set; get; }

        /// <summary>
        /// Using a parameter in the Animator component, this determines whether the unit is currently in the moving animator state or not.
        /// This allows other components to handle movement related actions smoothly and sync them correctly with the unit's movement
        /// </summary>
        public bool IsInMvtState => animator.GetBool(UnitAnimator.Parameters[AnimatorState.movingState]);

        [SerializeField, Tooltip("Play the take damage animation when the unit is damaged?"), Header("Damage Animation")]
        private bool damageAnimationEnabled = false;
        [SerializeField, Tooltip("How long does the take damage animation last for?")]
        private float damageAnimationDuration = 0.2f;

        public bool IsDamageAnimationEnabled => damageAnimationEnabled;

        //used for the override reset coroutine which waits for the state to get to idle before resetting the animator state
        private Coroutine overrideResetCoroutine;

        // Game services
        protected IUnitManager unitMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected ITimeModifier timeModifier { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.timeModifier = gameMgr.GetService<ITimeModifier>(); 

            this.Unit = entity as IUnit;

            if (!logger.RequireValid(animator,
                $"[{GetType().Name} - {Unit.Code}] The 'Animator' field must be assigned!"))
                return;

            animatorSpeed = new TimeModifiedFloat(animator.speed);
            animator.speed = animatorSpeed.Value;

            ResetOverrideController();

            SetState(AnimatorState.idle);

            Unit.Health.EntityHealthUpdated += HandleUnitHealthUpdated;

            timeModifier.ModifierUpdated += HandleModifierUpdated;
        }

        public void Disable()
        {
            SetState(AnimatorState.dead);

            Unit.Health.EntityHealthUpdated -= HandleUnitHealthUpdated;

            timeModifier.ModifierUpdated -= HandleModifierUpdated;
        }
        #endregion

        #region Handling Event: Time Modifier Update
        private void HandleModifierUpdated(ITimeModifier sender, EventArgs args)
        {
            animator.speed = animatorSpeed.Value;
        }
        #endregion

        #region Handling Events: Unit
        private void HandleUnitHealthUpdated(IEntity unit, HealthUpdateEventArgs e)
        {
            //only deal with the case where the unit receives damage.
            if (e.Value >= 0)
                return;

            if (damageAnimationEnabled)
            {
                SetState(AnimatorState.takeDamage);

                StartCoroutine(DisableTakeDamageAnimation(damageAnimationDuration));
            }

        }
        #endregion

        #region Updating Animator State
        public void SetState(AnimatorState newState)
        {
            if (LockState == true || !animator.IsValid())
                return;

            if (CurrState == AnimatorState.dead
                // If the damage animation is active, only allow to change the animation if the next one is a death animation.
                || (CurrState == AnimatorState.takeDamage && newState != AnimatorState.dead))
                return;

            CurrState = newState;

            animator.SetBool(UnitAnimator.Parameters[AnimatorState.takeDamage], CurrState == AnimatorState.takeDamage);

            // Stop the idle animation in case take damage animation is played since the take damage animation is broken by the idle anim
            animator.SetBool(UnitAnimator.Parameters[AnimatorState.idle], CurrState == AnimatorState.idle);

            // If the new animator state is the taking damage one then do not disable the rest of animations since as soon as the take damage animation is disabled, we want to get back to the last active state
            if (CurrState == AnimatorState.takeDamage)
                return;

            foreach (KeyValuePair<AnimatorState, string> animParameter in UnitAnimator.Parameters)
                if(animParameter.Key != AnimatorState.movingState)
                    animator.SetBool(animParameter.Value, animParameter.Key == CurrState);
        }

        private IEnumerator DisableTakeDamageAnimation (float delay)
        {
            yield return new WaitForSeconds(delay);

            SetState(AnimatorState.idle);
        }
        #endregion

        #region Updating Animator Override Controller
        public void SetOverrideController(AnimatorOverrideController newOverrideController)
        {
            // Only if the unit is not in its dead animation state do we reset the override controller
            // And since all parameters reset when the unit is dead and the unit is locked in its death state
            // Reseting the controller makes it start from its "entry state" back to "idle" state, this makes the unit leave its death state while still marked as dead in the currAnimatorState
            if (!newOverrideController.IsValid()
                || CurrState == AnimatorState.dead)
                return;

            animator.runtimeAnimatorController = newOverrideController;
            // Finish playing the current clip at the animator so that the switch to the new controller can be done.
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);

            // Since changing the override controller resets all parameters, we need to re-set the current animator state
            SetState(CurrState);
        }

        public void ResetAnimatorOverrideControllerOnIdle()
        {
            overrideResetCoroutine = StartCoroutine(HandleResetAnimatorOverrideControllerOnIdle());
        }

        private IEnumerator HandleResetAnimatorOverrideControllerOnIdle()
        {
            yield return new WaitWhile(() => CurrState != AnimatorState.idle);

            ResetOverrideController();
        }

        public void ResetOverrideController ()
        {
            if (overrideResetCoroutine.IsValid())
                StopCoroutine(overrideResetCoroutine);

            AnimatorOverrideController nextController = animatorOverrideController.Fetch();
            SetOverrideController(nextController.IsValid() ? nextController : unitMgr.DefaultAnimController);
        }
        #endregion
    }
}
