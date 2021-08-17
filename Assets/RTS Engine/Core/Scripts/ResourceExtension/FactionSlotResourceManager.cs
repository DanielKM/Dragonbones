using RTSEngine.Faction;
using RTSEngine.Game;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.ResourceExtension
{
    public class FactionSlotResourceManager : IFactionSlotResourceManager
    {
        //key: resource (unique) name/identifier.
        //value: FactionResourceHandler instance used to update the resource's amount and UI.
        public IReadOnlyDictionary<ResourceTypeInfo, IFactionResourceHandler> ResourceHandlers { private set; get; }

        //value is used to check for required resources for NPC factions
        //if a NPC faction requires X amount of a resource, it must have X * resourceNeedRatio before it can spend it
        //for a player faction, this value is always 1.
        private float resourceNeedRatio = 1.0f;

        public float ResourceNeedRatio
        {
            set
            {
                resourceNeedRatio = Mathf.Min(value, 1.0f);
            }
            get
            {
                return resourceNeedRatio;
            }
        }

        public FactionSlotResourceManager(
            IFactionSlot factionSlot,
            IGameManager gameMgr,
            float resourceNeedRatio,
            IEnumerable<ResourceTypeInfo> mapResources,
            IReadOnlyDictionary<ResourceTypeInfo, ResourceTypeValue> resourceStartingAmount)
        {
            ResourceNeedRatio = resourceNeedRatio;

            ResourceHandlers = mapResources.
                ToDictionary(
                mapResource => mapResource,
                mapResource => new FactionResourceHandler(
                    factionSlot,
                    gameMgr,
                    mapResource,
                    resourceStartingAmount.IsValid() && resourceStartingAmount.ContainsKey(mapResource)
                        ? resourceStartingAmount[mapResource]
                        : mapResource.StartingAmount) as IFactionResourceHandler);
        }
    }
}
