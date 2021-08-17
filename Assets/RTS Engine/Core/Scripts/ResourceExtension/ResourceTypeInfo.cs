using UnityEngine;

using RTSEngine.Audio;
using UnityEngine.Serialization;

namespace RTSEngine.ResourceExtension
{
    [CreateAssetMenu(fileName = "NewResourceType", menuName = "RTS Engine/Resource Type", order = 2)]
    public class ResourceTypeInfo : RTSEngineScriptableObject
    {
        [SerializeField, Tooltip("A unique name used to identify the resource type."), FormerlySerializedAs("_name")]
        private string key = "new_resource";
        public override string Key => key;

        [SerializeField, Tooltip("Name used to display the resource in UI elements.")]
        private string displayName = "Name";
        public string DisplayName => displayName;

        [SerializeField, TextArea, Tooltip("Short description used to display the resource in UI elements.")]
        private string description = "Name";
        public string Description => description;

        [Space(), SerializeField, Tooltip("Enable to make the resource type a capacity one where it will have a maximum amount (capacity) property in addition.")]
        private bool hasCapacity = false;
        public bool HasCapacity => hasCapacity;

        [SerializeField, Tooltip("Default starting amount/capacity of the resource for each faction in a game.")]
        private ResourceTypeValue startingAmount = new ResourceTypeValue();
        public ResourceTypeValue StartingAmount => startingAmount;

        [SerializeField, Tooltip("UI icon of the resource.")]
        private Sprite icon = null;
        public Sprite Icon => icon;
    }
}
