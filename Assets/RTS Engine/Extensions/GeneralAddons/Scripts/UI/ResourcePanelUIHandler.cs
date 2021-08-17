using System.Collections.Generic;

using UnityEngine;

using RTSEngine.ResourceExtension;
using RTSEngine.Faction;

namespace RTSEngine.UI
{
    public class ResourcePanelUIHandler : BaseTaskPanelUIHandler<ResourceTaskUIAttributes>
    {
        [System.Serializable]
        public struct ResourceUIData
        {
            [Tooltip("Type of the resource that can be displayed in the UI panel.")]
            public ResourceTypeInfo type;

            [Tooltip("Define the faction types for which is it allowed to display this resource type in the UI panel.")]
            public FactionTypeTargetPicker allowedUIFactionTypes;
        }

        [SerializeField, Tooltip("Set the types of resources that can be displayed in the UI panel. Only resource types that are defined in the ResourceManager will be considered.")]
        private ResourceUIData[] data = new ResourceUIData[0];

        [SerializeField, Tooltip("Parent transform of the active task UI elements that display resources.")]
        private Transform panel = null;

        [SerializeField, Tooltip("Allow to have the individual resource type descriptions as the tooltip text when the player's mouse hovers over the resource UI tasks?")]
        private bool resourceDescriptionAsTooltip = true;

        [SerializeField, Tooltip("Color of the resource icon if it is a capacity resource and it reaches its maximum capacity.")]
        private Color maxCapacityColor = Color.red;

        private List<ITaskUI<ResourceTaskUIAttributes>> activeTasks;

        // Game services
        protected IResourceManager resourceMgr { private set; get; } 

        protected override void OnInit()
        {
            // If there is no local faction slot then we have no resources to display.
            if (!gameMgr.LocalFactionSlot.IsValid())
                return;
            else if (!logger.RequireValid(panel,
              $"[{GetType().Name}] The 'Panel' field must be assigned!"))
                return; 

            this.resourceMgr = gameMgr.GetService<IResourceManager>();

            activeTasks = new List<ITaskUI<ResourceTaskUIAttributes>>();

            foreach(ResourceUIData nextResourceUIData in data)
            {
                if (!nextResourceUIData.allowedUIFactionTypes.IsValidTarget(gameMgr.LocalFactionSlot.Data.type)
                    || !resourceMgr.FactionResources[gameMgr.LocalFactionSlot.ID].ResourceHandlers.TryGetValue(nextResourceUIData.type, out IFactionResourceHandler nextResourceHandler))
                    continue;

                ITaskUI<ResourceTaskUIAttributes> nextTask = Create(activeTasks, panel);

                nextTask.Reload(new ResourceTaskUIAttributes
                {
                    resourceHandler = nextResourceHandler,

                    tooltipEnabled = resourceDescriptionAsTooltip,
                    tooltipText = nextResourceUIData.type.Description,

                    maxCapacityColor = maxCapacityColor
                });
            }
        }

        public override void Disable()
        {
        }
    }
}
