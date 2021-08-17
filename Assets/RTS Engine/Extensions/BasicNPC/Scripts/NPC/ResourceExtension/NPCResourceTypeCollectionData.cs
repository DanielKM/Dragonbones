using UnityEngine;

using RTSEngine.ResourceExtension;

namespace RTSEngine.NPC.ResourceExtension
{
    [System.Serializable]
    public struct NPCResourceTypeCollectionData
    {
        [Tooltip("The resource type whose collection will be regulated by the next settings.")]
        public ResourceTypeInfo type; 

        [Tooltip("How many collector will be assigned to collect this resource type? (Per resource instance!)")]
        public FloatRange instanceCollectorsRatio;

        [Tooltip("The total maximum amount ratio of collectors that can collect this resource at the same time.")]
        public FloatRange maxCollectorsRatioRange;

        [Tooltip("To ensure that the NPC faction collects this resource type, you can set a minimum collector amount which the NPC faction will prioritize"), Min(0)]
        public int minCollectorsAmount;
    }
}
