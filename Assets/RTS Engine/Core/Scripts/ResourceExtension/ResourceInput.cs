using UnityEngine;
using UnityEngine.Serialization;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct ResourceInput
    {
        [Tooltip("Type of the resource."), FormerlySerializedAs("name")]
        public ResourceTypeInfo type;
        [Tooltip("Amount (and/or capacity amount) to add/remove or to check whether it is available.")]
        public ResourceTypeValue value;

        [Tooltip("Enable this option to disallow the faction from adding/removing the given resource value.")]
        public bool nonConsumable;
    }
}
