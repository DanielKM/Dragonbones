using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Health
{
    [System.Serializable]
    public struct DamageOverTimeData
    {
        [Tooltip("Does the DoT keep going until the target is destroyed?")]
        public bool infinite;
        [Tooltip("If the DoT is not infinite, this presents how long it will last for.")]
        public float duration;
        [Tooltip("How frequent will damage be dealt?")]
        public float cycleDuration;
    }
}
