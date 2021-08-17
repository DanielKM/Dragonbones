using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Entities;

namespace RTSEngine.Attack
{
    public struct AttackObjectLaunchData
    {
        public IAttackComponent source;
        public int sourceFactionID;

        public IFactionEntity target;
        public Vector3 targetPosition;

        public Vector3 initialRotationAngles;

        public float delayTime;
        public bool damageInDelay;
        public Transform delayParent;

        public bool damageFriendly;
    }
}
