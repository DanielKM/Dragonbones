using RTSEngine.Animation;
using UnityEngine;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct CollectableResourceData
    {
        [Tooltip("Type of the resource to collect.")]
        public ResourceTypeInfo type;

        [Space(), Tooltip("Amount of resources to be collected per progress OR maximum capacity of the collected resource.")]
        public int amount;

        [Space(), Tooltip("Child object of the collector that gets activated when the above resource type is being actively collected/dropped off."), Space()]
        public GameObject obj;
        [Tooltip("Allows to have a custom resource collection/drop off animaiton for the above resource type.")]
        public AnimatorOverrideControllerFetcher animatorOverrideController;
    }
}
