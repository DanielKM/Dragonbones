using UnityEngine;

namespace RTSEngine.NPC.UnitExtension
{
    [System.Serializable]
    public struct NPCAttackEngageOrderUnitBehaviourData
    {
        [Tooltip("Enable to send the unit instances to the attack target/destination when one is provided.")]
        public bool send;
        [Tooltip("Only send unit instances that are in an idle state?")]
        public bool sendIdleOnly;
        [Tooltip("Only send unit instances who have an active target that can not attack back?")]
        public bool sendNoTargetThreatOnly;

        [Tooltip("Ratio of the unit instances to send over all available to send instances that will be tasked with moving to the attack target/destination. <=0.0f means that no instances will be sent and >=1.0f means all available instances will be sent")]
        public FloatRange sendRatioRange;

        [Tooltip("Delay time before the available and eligible unit instances are sent to the attack target/destination?")]
        public FloatRange sendDelay;

        [Tooltip("Enable to launch an actual attack with the unit instances to send. Or disable to only move the units towards the attack target/destination.")]
        public bool attack;

        [Tooltip("Send back units to their spawn positions when the attack is cancelled?")]
        public bool sendBackOnAttackCancel;
    }
}
