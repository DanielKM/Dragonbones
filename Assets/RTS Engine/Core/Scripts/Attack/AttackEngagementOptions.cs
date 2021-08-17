using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public struct AttackEngagementOptions
    {
        [Tooltip("Engage targets that the local player assigns?")]
        public bool engageOnAssign; 
        [Tooltip("When damage is dealt to the faction entity, it will attempt to engage with the source of the damage.")]
        public bool engageWhenAttacked;
        [Tooltip("Trigger one attack iteration and then stop the attack?")]
        public bool engageOnce;
        [Tooltip("Engage friendly faction entities?")]
        public bool engageFriendly;
        [Tooltip("Auto-engage entities that are blocked by the LOS angle (attacker not looking at target)? This only applies for auto-targeting entities.")]
        public bool autoIgnoreAngleLOS;
        [Tooltip("Auto-engage entities that are blocked by obstacles? This only applies for auto-targeting entities.")]
        public bool autoIgnoreObstacleLOS;
    }
}
