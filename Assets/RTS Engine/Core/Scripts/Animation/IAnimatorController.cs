using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Animation
{
    public interface IAnimatorController : IMonoBehaviour
    {
        IUnit Unit { get; }
        Animator Animator { get; }

        AnimatorState CurrState { get; }
        bool LockState { get; set; }
        bool IsInMvtState { get; }

        void SetState(AnimatorState newState);

        void SetOverrideController(AnimatorOverrideController newOverrideController);

        void ResetAnimatorOverrideControllerOnIdle();
        void ResetOverrideController();
    }
}
