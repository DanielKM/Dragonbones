using System;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using UnityEngine.Serialization;

using RTSEngine.Utilities;

namespace RTSEngine.DevTools.ResourceExtension
{
    public class FactionResourceSetter : DevToolComponentBase 
    {
        [SerializeField, Tooltip("The resources that will be given to the faction that passes the above filter"), FormerlySerializedAs("initialResources")]
        private ResourceInput[] resources = new ResourceInput[0];

        protected IResourceManager resourceMgr { private set; get; }

        protected override void OnPostRunInit()
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();

            if(Label)
                Label.text = $"Reset Resources";

            if (IsActive)
                Set();
        }

        public void Set()
            => Set(RoleFilter);

        public void Set(FactionSlotRoleFilter filter)
        {
            for (int factionID = 0; factionID < gameMgr.FactionCount; factionID++)
            {
                if (!filter.IsAllowed(factionID.ToFactionSlot()))
                    continue;

                foreach (ResourceInput input in resources)
                {
                    if (!resourceMgr.FactionResources[factionID].ResourceHandlers.TryGetValue(input.type, out IFactionResourceHandler resourceTypeHandler))
                        continue;

                    // Reset current faction resources
                    if(!input.type.HasCapacity)
                        resourceMgr.UpdateResource(
                            factionID,
                            new ResourceInput
                            {
                                type = input.type,
                                value = new ResourceTypeValue
                                {
                                    amount = resourceTypeHandler.Amount,
                                    capacity = resourceTypeHandler.Capacity
                                }
                            },
                            add: false);

                    // And add the current resources.
                    resourceMgr.UpdateResource(factionID, input, add: true);
                }
            }
        }

        public override void OnUIInteraction() 
        {
            Set();
        }
    }
}
