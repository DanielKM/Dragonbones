using UnityEngine;

using RTSEngine.Effect;
using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public struct AttackObjectSource
    {
        [EnforceType(typeof(IAttackObject)), Tooltip("The attack object to launch.")]
        public GameObject attackObject;

        [Tooltip("This is where the attack object will be launched from.")]
        public Transform launchPosition; 
        [Tooltip("The initial rotation that the attack object will have as soon as it is spawned.")]
        public Vector3 launchRotationAngles;

        [Tooltip("The higher the absolute value in an axis, the less accurate the attack object movement is on that axis.")]
        public Vector3 accuracyModifier; 

        [Tooltip("Delay time before the attack object is created.")]
        public float preDelayTime; 
        [Tooltip("Delay time after the attack object is created.")]
        public float postDelayTime; 

        [Tooltip("Delay time that starts exactly when the attack object is created and is used to block the movement of the attack object.")]
        public float launchDelayTime; 
        [Tooltip("Deal damage with the attack object when it is in delay mode?")]
        public bool damageInDelay;
        [Tooltip("A parent object can be assigned to the attack object when it is in delay mode.")]
        public Transform delayParentObject;

        public IAttackObject Launch(IGameManager gameMgr, IAttackComponent source)
        {
            IAttackObject newAttackObject = gameMgr.GetService<IEffectObjectPool>().Spawn(
                attackObject.GetComponent<IEffectObject>(),
                launchPosition.position,
                Quaternion.identity).GetComponent<IAttackObject>();

            Vector3 targetPosition = RTSHelper.GetAttackTargetPosition(source.Target);

            /*if (GameManager.MultiplayerGame == false) // If this is a singleplayer game, we can play with accuracy:
                targetPosition += new Vector3(
                    Random.Range(-accuracyModifier.x, accuracyModifier.x),
                    Random.Range(-accuracyModifier.y, accuracyModifier.y),
                    Random.Range(-accuracyModifier.z, accuracyModifier.z));*/

            newAttackObject.Launch(
                new AttackObjectLaunchData
                {
                    source = source,
                    sourceFactionID = source.Entity.FactionID,

                    target = source.Target.instance,
                    targetPosition = targetPosition,

                    initialRotationAngles = launchRotationAngles,

                    delayTime = launchDelayTime,
                    damageInDelay = damageInDelay,
                    delayParent = delayParentObject,

                    damageFriendly = source.EngageOptions.engageFriendly
                });

            return newAttackObject;
        }
    }
}

