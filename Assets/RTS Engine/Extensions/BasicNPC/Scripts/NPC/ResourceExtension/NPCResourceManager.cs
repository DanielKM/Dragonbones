using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.BuildingExtension;
using RTSEngine.ResourceExtension;
using System.Linq;

namespace RTSEngine.NPC.ResourceExtension
{
    public class NPCResourceManager : NPCComponentBase, INPCResourceManager
    {
        #region Attributes 
        [SerializeField, Tooltip("How safe does the NPC faction use their resources? The higher, the safer. For example, if the need ratio is set to 2.0 and the faction needs 200 woods. Only when 400 (200 x 2) wood is available, the 200 can be used. Must be >= 1.0!")]
        private FloatRange resourceNeedRatioRange = new FloatRange(1.0f, 1.2f);

        [SerializeField, Tooltip("Ratio of the resources to be exploited by default by the NPC faction. 0.0 means no resources to exploit by default and 1.0 means all available resources will be exploited by default.")]
        private FloatRange resourceDefaultExploitRatioRange = new FloatRange(0.8f, 1.0f);

        // Key: IBuilding instance that has a IBorder component
        // Value: NPCBorderResourceTracker instance that handles idle and exploited resources within the border's territory
        private Dictionary<IBuilding, NPCBorderResourceTracker> borderResourceTrackers = new Dictionary<IBuilding, NPCBorderResourceTracker>();

        // NPC Components
        protected INPCResourceCollector npcResourceCollector { private set; get; }
        protected IEnumerable<INPCCapacityResourceManager> npcCapacityResourceMgrs { private set; get; }

        // Game services
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            this.npcResourceCollector = npcMgr.GetNPCComponent<INPCResourceCollector>();
            this.npcCapacityResourceMgrs = npcMgr.GetNPCComponentSet<INPCCapacityResourceManager>();

            this.resourceMgr = gameMgr.GetService<IResourceManager>();
        }

        protected override void OnPostInit()
        {
            // Set the resource need ratio for the faction:
            resourceMgr.FactionResources[factionMgr.FactionID].ResourceNeedRatio = resourceNeedRatioRange.RandomValue;

            // Go through the spawned building centers and init their registered resources.
            // Can't really rely on the custom events for initializing since the IBorder components and IResource components will get initialiazed before the events are fired.
            foreach(IBuilding buildingCenter in factionMgr.BuildingCenters)
                foreach(IResource resource in buildingCenter.BorderComponent.ResourcesInRange)
                    AddBorderResource (buildingCenter, resource);

            globalEvent.BorderResourceAddedGlobal += HandleBorderResourceAddedGlobal;
            globalEvent.BorderResourceRemovedGlobal += HandleBorderResourceRemovedGlobal;

            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;

            globalEvent.ResourceDeadGlobal += HandleResourceDeadGlobal;
        }

        protected override void OnDestroyed()
        { 
            globalEvent.BorderResourceAddedGlobal -= HandleBorderResourceAddedGlobal;
            globalEvent.BorderResourceRemovedGlobal -= HandleBorderResourceRemovedGlobal;

            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;

            globalEvent.ResourceDeadGlobal -= HandleResourceDeadGlobal;
        }
        #endregion

        #region Faction Resources Manipulation
        private void AddBorderResource (IBuilding buildingCenter, IResource newResource)
        {
            // when border is activated then we'd send some of them to the resources to collect list of the NPC Resource Collector ...
            // ...and leave the rest idle and that is all depending on the resource exploit ratio defined in this component:

            if (!borderResourceTrackers.TryGetValue(buildingCenter, out NPCBorderResourceTracker nextBorderResourceTracker))
            {
                nextBorderResourceTracker = new NPCBorderResourceTracker();

                borderResourceTrackers.Add(
                    buildingCenter,
                    nextBorderResourceTracker);
            }

            if (nextBorderResourceTracker.Add(newResource, resourceDefaultExploitRatioRange.RandomValue))
                npcResourceCollector.AddResourceToCollect(newResource);
        }

        private bool RemoveBorderResource (IBuilding buildingCenter, IResource removedResource)
        {
            if (!borderResourceTrackers.TryGetValue(buildingCenter, out NPCBorderResourceTracker nextBorderResourceTracker))
            {
                nextBorderResourceTracker.Remove(removedResource);

                npcResourceCollector.RemoveResourceToCollect(removedResource);

                return true;
            }

            return false;
        }

        private void RemoveAllResourcesInBorder (IBuilding buildingCenter)
        {
            borderResourceTrackers.Remove(buildingCenter);
        }

        private void ReplaceEmptyResource (IResource emptyResource)
        {
            foreach (NPCBorderResourceTracker nextBorderResourceTracker in borderResourceTrackers.Values)
            {
                if(nextBorderResourceTracker.AttemptReplaceResource(emptyResource, out IResource replacementResource))
                    npcResourceCollector.AddResourceToCollect(replacementResource);
            }
        }
        #endregion

        #region Handling Events: IBorder
        private void HandleBorderResourceAddedGlobal(IBorder border, ResourceEventArgs e)
        {
            if(factionMgr.IsSameFaction(border.Building))
                AddBorderResource(border.Building, e.Resource);
        }

        private void HandleBorderResourceRemovedGlobal (IBorder border, ResourceEventArgs e)
        {
            if(factionMgr.IsSameFaction(border.Building))
                RemoveBorderResource(border.Building, e.Resource);
        }

        private void HandleBorderDisabledGlobal(IBorder border, EventArgs e)
        {
            if(factionMgr.IsSameFaction(border.Building))
                RemoveAllResourcesInBorder(border.Building);
        }
        #endregion

        #region Handling Events: IResource
        private void HandleResourceDeadGlobal(IResource resource, DeadEventArgs e)
        {
            if(factionMgr.IsSameFaction(resource))
                ReplaceEmptyResource(resource);
        }
        #endregion

        #region Handling Missing Resources
        public void OnIncreaseMissingResourceRequest(IEnumerable<ResourceInput> resourceInputs)
        {
            // Currently, this only treats capacity resources.
            // FUTURE: Handle missing non-capacity resources
            foreach(ResourceInput nextInput in resourceInputs)
            {
                if (!nextInput.type.HasCapacity
                    || resourceMgr.HasResources(nextInput, factionMgr.FactionID))
                    continue;

                INPCCapacityResourceManager nextCapacityResourceMgr = npcCapacityResourceMgrs.Where(element => element.TargetCapacityResource == nextInput.type).FirstOrDefault();
                if (!nextCapacityResourceMgr.IsValid())
                    continue;

                nextCapacityResourceMgr.OnIncreaseCapacityRequest();
            }
        }
        #endregion

#if UNITY_EDITOR
        [System.Serializable]
        private struct NPCBuildingCenterActiveResourcesLogData
        {
            public GameObject center;

            public GameObject[] exploitedResources;
            public GameObject[] idleResources;
        }

        [Header("Logs")]
        [SerializeField, ReadOnly, Space()]
        private NPCBuildingCenterActiveResourcesLogData[] targetResourcesLog = new NPCBuildingCenterActiveResourcesLogData[0];

        // Current resource amount
        [SerializeField, ReadOnly, Space()]
        private ResourceTypeInput[] factionResources = new ResourceTypeInput[0];

        protected override void UpdateLogStats()
        {
            targetResourcesLog = borderResourceTrackers
                .Select(tracker => new NPCBuildingCenterActiveResourcesLogData
                {
                    center = tracker.Key.gameObject,

                    exploitedResources = tracker.Value.ExploitedResources.Select(resource => resource.gameObject).ToArray(),
                    idleResources = tracker.Value.IdleResources.Select(resource => resource.gameObject).ToArray(),
                })
                .ToArray();

            factionResources = resourceMgr.FactionResources[factionMgr.FactionID].ResourceHandlers
                .Select(resourceHandler => new ResourceTypeInput
                {
                    type = resourceHandler.Key,

                    value = new ResourceTypeValue
                    {
                        amount = resourceHandler.Value.Amount,
                        capacity = resourceHandler.Value.Capacity
                    }
                })
                .ToArray();
        }
#endif
    }
}
