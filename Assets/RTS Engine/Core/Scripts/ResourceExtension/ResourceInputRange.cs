using UnityEngine;
using UnityEngine.Serialization;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct ResourceInputRange
    {
        [Tooltip("Type of the resource."), FormerlySerializedAs("name")]
        public ResourceTypeInfo type;
        [Tooltip("Amount (and/or capacity amount) to add/remove or to check whether it is available.")]
        public ResourceTypeValueRange value;

        [Tooltip("Enable this option to disallow the faction from adding/removing the given resource value.")]
        public bool nonConsumable;
    }
}
