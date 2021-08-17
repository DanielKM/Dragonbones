using RTSEngine.Determinism;
using UnityEngine;

namespace RTSEngine.NPC.UnitExtension
{
    [System.Serializable]
    public struct NPCFactionEntityForceCreationData
    {
        [Tooltip("Allow to periodically update the target amount of the faction entity type(s) to force the NPC faction to create more of it?")]
        public bool enabled;

        [Tooltip("Delays updating the target count of the faction entity type(s) after the game starts.")]
        public FloatRange targetCountUpdateDelay;

        [Tooltip("How often to increase the target count of the faction entity type(s)?")]
        public FloatRange targetCountUpdatePeriod;
        [HideInInspector]
        public TimeModifiedTimer timer;

        [Tooltip("Amount to update the target count of the faction entity type(s) each period.")]
        public int targetCountUpdateAmount;
    }
}
