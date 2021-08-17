using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.Selection
{
    [System.Serializable]
    public struct EntitySelectionOptions
    {
        [Tooltip("Pick the entity type for which the selection options will be defined.")]
        public EntityType entityType;

        [Tooltip("Allow the above entity type to be multiply selected?")]
        public bool allowMultiple; 
        [Tooltip("When enabled, the above entity type instances can not be selected multiply with different entity types.")]
        public bool exclusive;
    }
}
