using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct ResourceTypeValueRange
    {
        public IntRange amount;
        public IntRange capacity;
    }
}
