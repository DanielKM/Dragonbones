using System.Collections.Generic;

namespace RTSEngine.Animation
{
    /// <summary>
    /// Used to keep a constant reference to the parameters used in the unit's animator controller.
    /// </summary>
    public static class UnitAnimator
    {
        public static IReadOnlyDictionary<AnimatorState, string> Parameters = new Dictionary<AnimatorState, string>
        {
            {AnimatorState.idle, "IsIdle" },

            {AnimatorState.takeDamage, "IsTakeDamage" },
            {AnimatorState.dead, "IsDead" },

            {AnimatorState.moving, "IsMoving" },
            {AnimatorState.movingState, "InMvtState" },

            {AnimatorState.inProgress, "IsInProgress"}
        };
    }
}
