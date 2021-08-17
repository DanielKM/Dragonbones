using UnityEngine;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public struct DamageRangeData 
    {
        [Tooltip("Range of the Area of Effect attack.")]
        public float range;

        [Tooltip("Data that defines the damage to be dealt inside this range.")]
        public DamageData data;
    }
}
