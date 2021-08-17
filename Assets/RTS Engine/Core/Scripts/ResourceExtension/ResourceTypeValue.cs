using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct ResourceTypeValue
    {
        [Tooltip("Amount to add/remove or to check whether it is available.")]
        public int amount;
        [Tooltip("Capacity to add/remove or to check whether it is available. Only valid for capacity-enabled resource types.")]
        public int capacity;
    }
}
