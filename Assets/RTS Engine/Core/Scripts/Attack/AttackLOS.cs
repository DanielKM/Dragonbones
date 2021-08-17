using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackLOS : AttackSubComponent
    {
        #region Attributes
        [SerializeField, Tooltip("Can the attacker engage only if the target is in its line of sight?")]
        private bool enabled = true; 

        [SerializeField, Tooltip("Enable to use the weapon object's rotation for the line of sight calculations instead of the main attcker object.")]
        private bool useWeaponObject = false; 

        [SerializeField, Tooltip("How wide is the line of sight angle (in degrees) of the attacker? the less, the closer the attacker must face its target to engage it."), Min(0)]
        private float angle = 40.0f; 

        [SerializeField, Tooltip("Define layers for obstacles that block the line of sight.")]
        private LayerMask obstacleLayerMask = new LayerMask();

        // Ignore one or more axis while considering LOS?
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the X axis.")]
        private bool ignoreRotationX = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Y axis.")]
        private bool ignoreRotationY = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Z axis.")]
        private bool ignoreRotationZ = false;
        #endregion

        #region Handling Angle and Obstacle LOS
        public ErrorMessage IsInSight (TargetData<IFactionEntity> target, bool ignoreAngle = false, bool ignoreObstacle = false)
        {
            if (!enabled)
                return ErrorMessage.none;

            // Use the weapon object or the attacker's object as the reference for the line of sight:
            Transform sourceTransform = (useWeaponObject == true && source.WeaponTransform.IsValid()) 
                ? source.WeaponTransform 
                : source.Entity.transform;

            Vector3 targetPosition = RTSHelper.GetAttackTargetPosition(target);

            if (!ignoreAngle && IsAngleBlocked(sourceTransform, targetPosition))
                return ErrorMessage.LOSAngleBlocked;
            else if (!ignoreObstacle && IsObstacleBlocked(sourceTransform.position, targetPosition))
                return ErrorMessage.LOSObstacleBlocked;

            return ErrorMessage.none;
        }

        public bool IsAngleBlocked (Transform sourceTransform, Vector3 targetPosition)
        {
            if (!enabled) 
                return true;

            Vector3 lookAt = targetPosition - sourceTransform.transform.position;

            // Which axis to ignore when checking for LOS?
            if (ignoreRotationX == true)
                lookAt.x = 0.0f;
            if (ignoreRotationY == true)
                lookAt.y = 0.0f;
            if (ignoreRotationZ == true)
                lookAt.z = 0.0f;

            // if the angle is below the allowed LOS Angle then the attacker is in line of sight of the target
            return Vector3.Angle(sourceTransform.forward, lookAt) >= angle;
        }

        public bool IsObstacleBlocked (Vector3 sourcePosition, Vector3 targetPosition)
        {
            if (!enabled)
                return false;

            return Physics.Linecast(sourcePosition, targetPosition, obstacleLayerMask);
        }
        #endregion
    }
}
