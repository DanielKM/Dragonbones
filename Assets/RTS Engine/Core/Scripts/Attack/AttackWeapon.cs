using UnityEngine;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackWeapon : AttackSubComponent
    {
        #region Attributes
        [SerializeField, Tooltip("Only rotate the weapon when the target is inside the attacking range?")]
        private bool rotateInRangeOnly = false; 

        //to the freeze the weapon's rotation on the Y axis then you should enable freezeRotationX and freezeRotationZ
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the X axis.")]
        private bool freezeRotationX = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Y axis.")]
        private bool freezeRotationY = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Z axis.")]
        private bool freezeRotationZ = false;

        [SerializeField, Tooltip("Is the weapon's object rotation smooth?")]
        private bool smoothRotation = true;
        [SerializeField, Tooltip("How smooth is the weapon's rotation? Only if smooth rotation is enabled!")]
        private float rotationDamping = 2.0f; 

        [SerializeField, Tooltip("Force the weapon object to get back to an idle rotation when the attacker does not have an active target?")]
        private bool forceIdleRotation = true; 
        [SerializeField, Tooltip("In case idle rotation is enabled, this represents the idle rotation euler angles.")]
        private Vector3 idleAngles = Vector3.zero; 
        // Used to store the weapon's idle rotation so it is not calculated everytime through its euler angles
        private Quaternion idleRotation = Quaternion.identity;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit ()
        {
            if (source.WeaponTransform != null)
                idleRotation.eulerAngles = idleAngles;
        }

        public void Toggle (bool enable) 
        {
            source.WeaponTransform?.gameObject.SetActive(enable);
        }
        #endregion

        #region Handling Active/Idle Rotation
        public void Update()
        {
            if (source.WeaponTransform == null)
                return;

            //if the attacker does not have an active target
            //or it does but we are not allowed to start weapon rotation until the target is in range
            if (!source.HasTarget
                || (!source.IsInTargetRange && rotateInRangeOnly))
                UpdateIdleRotation();
            else
                UpdateActiveRotation();
        }

        public void UpdateIdleRotation ()
        {
            //can not force idle rotation, stop here
            if (!forceIdleRotation)
                return;

            source.WeaponTransform.localRotation = smoothRotation
                ? Quaternion.Slerp(source.WeaponTransform.localRotation, idleRotation, Time.deltaTime * rotationDamping)
                : idleRotation;
        }

        public void UpdateActiveRotation ()
        {
            Vector3 lookAt = RTSHelper.GetAttackTargetPosition(source.Target) - source.WeaponTransform.position;

            //which axis should not be rotated? 
            if (freezeRotationX == true)
                lookAt.x = 0.0f;
            if (freezeRotationY == true)
                lookAt.y = 0.0f;
            if (freezeRotationZ == true)
                lookAt.z = 0.0f;

            Quaternion targetRotation = Quaternion.LookRotation(lookAt);
            if (smoothRotation == false) //make the weapon instantly look at target
                source.WeaponTransform.rotation = targetRotation;
            else //smooth rotation
                source.WeaponTransform.rotation = Quaternion.Slerp(source.WeaponTransform.rotation, targetRotation, Time.deltaTime * rotationDamping);
        }
        #endregion
    }
}
