using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Serialization;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.ResourceExtension;
using RTSEngine.NPC.UnitExtension;

namespace RTSEngine.NPC.ResourceExtension
{
    public class NPCResourceCollector : NPCComponentBase, INPCResourceCollector
    {
        #region Attributes 
        [SerializeField, EnforceType(typeof(IUnit), prefabOnly: true), Tooltip("List of units with an IResourceCollector component that the NPC faction can use to collect resources.")]
        private GameObject[] collectors = new GameObject[0];

        [SerializeField, Tooltip("A list of resource types and settings regarding regulating their collection by the NPC faction."), FormerlySerializedAs("collectionSettings")]
        private List<NPCResourceTypeCollectionData> collectionData = new List<NPCResourceTypeCollectionData>();

        // key: tracked resource type.
        // value: tracker class instance used to track the collection of the key resource type
        private Dictionary<ResourceTypeInfo, NPCResourceTypeCollectionTracker> collectionTrackers;

        [SerializeField, Tooltip("How often does the NPC faction check for resources to collect?")]
        private FloatRange collectionTimerRange = new FloatRange(3.0f, 5.0f);
        private TimeModifiedTimer collectionTimer;

        [SerializeField, Tooltip("How many collection timer ticks are required for the NPC faction to enforce the minimum collectors amount for each resource type by rearranging collectors?")]
        private int enforceMinCollectorsTimerTicks = 5;
        private int collectionTimerTicks;

        [SerializeField, Tooltip("When enabled, other NPC components can request to collect resources.")]
        private bool collectOnDemand = true;

        // NPC Components
        private INPCUnitCreator npcUnitCreator;
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            this.npcUnitCreator = npcMgr.GetNPCComponent<INPCUnitCreator>();

            collectionTrackers = new Dictionary<ResourceTypeInfo, NPCResourceTypeCollectionTracker>();
            collectionTimer = new TimeModifiedTimer(collectionTimerRange);

            collectionTimerTicks = 0;

            InitCollectionTrackers();
        }

        protected override void OnPostInit()
        {
            IsActive = true;

            InitCollectorRegulators();

            globalEvent.UnitInitiatedGlobal += HandleUnitInitiatedGlobal;

            globalEvent.ResourceDeadGlobal += HandleResourceDeadGlobal;
        }

        protected override void OnDestroyed()
        {
            globalEvent.UnitInitiatedGlobal -= HandleUnitInitiatedGlobal;

            globalEvent.ResourceDeadGlobal -= HandleResourceDeadGlobal;

            foreach (NPCResourceTypeCollectionTracker nextCollectionTracker in collectionTrackers.Values)
            {
                nextCollectionTracker.CollectorSlotFreed -= HandleCollectorSlotFreed;

                nextCollectionTracker.Disable();
            }
        }

        private void InitCollectionTrackers()
        {
            collectionTrackers.Clear();

            foreach (NPCResourceTypeCollectionData nextCollectionData in collectionData)
            {
                if (!logger.RequireValid(nextCollectionData.type,
                    $"[{GetType().Name} - {factionMgr.FactionID}] The field 'Collection Data' has element(s) where the resource type 'Type' is not assigned!"))
                    continue;

                NPCResourceTypeCollectionTracker newCollectionTracker = new NPCResourceTypeCollectionTracker(gameMgr, npcMgr, nextCollectionData);
                collectionTrackers.Add(nextCollectionData.type, newCollectionTracker);

                newCollectionTracker.CollectorSlotFreed += HandleCollectorSlotFreed;
            }
        }

        private void InitCollectorRegulators()
        {
            foreach(NPCResourceTypeCollectionTracker nextCollectionTracker in collectionTrackers.Values)
                ActivateCollectorRegulator(nextCollectionTracker.CollectionData.type, nextCollectionTracker.CollectorMonitor);
        }

        private void ActivateCollectorRegulator(ResourceTypeInfo resourceType, NPCActiveRegulatorMonitor collectorMonitor)
        {
            foreach (GameObject unitObj in collectors)
            {
                IUnit collectorUnit = unitObj?.GetComponent<IUnit>();
                if (!logger.RequireValid(collectorUnit,
                    $"[{GetType().Name} - {factionMgr.FactionID}] 'Collectors' field has some unassigned elements."))
                    return;

                IResourceCollector collectorComponent = collectorUnit.gameObject.GetComponentInChildren<IResourceCollector>();
                if (!logger.RequireValid(collectorComponent,
                    $"[{GetType().Name} - {factionMgr.FactionID}] 'Collectors' field has some assigned units with no component that implements '{typeof(IResourceCollector).Name}' interface attached to them."))
                    return;

                NPCUnitRegulator nextRegulator;
                if (collectorComponent.IsResourceTypeCollectable(resourceType)
                    && (nextRegulator = npcUnitCreator.ActivateUnitRegulator(collectorUnit)).IsValid())
                    collectorMonitor.AddCode(nextRegulator.Prefab.Code);
            }

            if (collectorMonitor.Count == 0)
            {
                logger.LogWarning($"[{GetType().Name} - {factionMgr.FactionID}] No resource collector regulators have been assigned for resource type '{resourceType.Key}'!");
                IsActive = false;
            }
        }
        #endregion

        #region Adding/Removing Resources to Collect 
        public void AddResourceToCollect(IResource resource)
        {
            if (collectionTrackers.TryGetValue(resource.ResourceType, out NPCResourceTypeCollectionTracker nextCollectionTracker))
                nextCollectionTracker.AddResource(resource);
        }

        public void RemoveResourceToCollect(IResource resource)
        {
            if (collectionTrackers.TryGetValue(resource.ResourceType, out NPCResourceTypeCollectionTracker nextCollectionTracker))
                nextCollectionTracker.RemoveResource(resource);
        }
        #endregion

        #region Handling Event: Collector Slot Freed
        private void HandleCollectorSlotFreed(NPCResourceTypeCollectionTracker sender, EventArgs args)
        {
            // When a resource collector slot is freed, we activate this component (in case it was deactivated) to allow it to fill that slot again.
            IsActive = true;
        }
        #endregion

        #region Event Handling: Unit Init
        private void HandleUnitInitiatedGlobal(IUnit unit, EventArgs args)
        {
            if (factionMgr.IsSameFaction(unit) && unit.CollectorComponent.IsValid())
                IsActive = true;
        }
        #endregion

        #region Event Handling: Resource Dead
        private void HandleResourceDeadGlobal(IResource resource, DeadEventArgs e)
        {
            RemoveResourceToCollect(resource);
        }
        #endregion

        #region Handling Resource Collection
        protected override void OnActiveUpdate()
        {
            if (collectionTimer.ModifiedDecrease())
            {
                collectionTimer.Reload(collectionTimerRange);

                collectionTimerTicks++;

                bool canEnforceMinCollectors = collectionTimerTicks == enforceMinCollectorsTimerTicks;
                if (canEnforceMinCollectors)
                    collectionTimerTicks = 0;

                // Assume that all resource instances have the required amount of collectors and deactivate this component
                IsActive = false;

                //go through the ResourceTypeCollection instances that regulate each resource type
                foreach (NPCResourceTypeCollectionTracker nextCollectionTracker in collectionTrackers.Values)
                {
                    IEnumerable<NPCUnitRegulator> nextCollectorRegulators = nextCollectionTracker.CollectorMonitor
                        .AllCodes
                        .Select(collectorCode => npcUnitCreator.GetActiveUnitRegulator(collectorCode))
                        .Where(collectorRegulator => collectorRegulator.IsValid());

                    if (!nextCollectorRegulators.Any() 
                        || !nextCollectionTracker.CanAddCollector(nextCollectorRegulators.Sum(collectorRegulator => collectorRegulator.Count)))
                        continue;

                    // Set state back to active so we can keep monitoring whether resource collectors have been correctly assigned or not.
                    IsActive = true;

                    foreach (IResource resource in nextCollectionTracker.ResourceInstances)
                    {
                        int targetCollectorsAmount = nextCollectionTracker.GetTargetCollectorsAmount(resource);

                        if (targetCollectorsAmount > resource.WorkerMgr.Amount)
                            OnResourceCollectionRequestInternal(
                                resource,
                                targetCollectorsAmount,
                                out _,
                                forceSwitch: canEnforceMinCollectors && !nextCollectionTracker.IsMinAmountReached);
                    }
                }
            }
        }

        public void OnResourceCollectionRequest(IResource resource, int targetCollectorsAmount, out int assignedCollectors, bool force = false)
        {
            assignedCollectors = 0;

            if (!collectOnDemand)
                return;

            OnResourceCollectionRequestInternal(resource, targetCollectorsAmount, out assignedCollectors, force);
        }

        private void OnResourceCollectionRequestInternal(IResource resource, int targetCollectorsAmount, out int assignedCollectors, bool forceSwitch = false)
        {
            assignedCollectors = 0;
            if (!resource.IsValid()
                || resource.Health.IsDead
                || !collectionTrackers.ContainsKey(resource.ResourceType))
                return;

            int requiredCollectors = targetCollectorsAmount - resource.WorkerMgr.Amount;

            IUnit[] currentCollectors = npcUnitCreator
                .GetActiveUnitRegulator(collectionTrackers[resource.ResourceType].CollectorMonitor.RandomCode)
                .InstancesIdleFirst
                .ToArray();

            foreach (IUnit nextCollector in currentCollectors)
            {
                if (requiredCollectors <= 0)
                    break;

                bool canStillForceSwitch = forceSwitch 
                    && nextCollector.CollectorComponent.HasTarget
                    && (collectionTrackers[nextCollector.CollectorComponent.Target.instance.ResourceType].CollectorsAmount - 1 >= collectionTrackers[nextCollector.CollectorComponent.Target.instance.ResourceType].CollectionData.minCollectorsAmount);

                if (nextCollector.IsValid()
                    && (nextCollector.IsIdle || canStillForceSwitch)
                    && nextCollector.CollectorComponent.Target.instance != resource)
                {
                    nextCollector.CollectorComponent.SetTarget(RTSHelper.ToTargetData(resource), playerCommand: false);

                    requiredCollectors--;
                    assignedCollectors++;
                }
            }
        }
        #endregion

#if UNITY_EDITOR
        [System.Serializable]
        private struct NPCResourceTypeCollectionTrackerLogData
        {
            public ResourceTypeInfo resourceType;

            public int collectorsAmount;

            public GameObject[] targetResources;
        }

        [Header("Logs")]
        [SerializeField, ReadOnly, Space()]
        private List<NPCResourceTypeCollectionTrackerLogData> collectionTrackerLogs = new List<NPCResourceTypeCollectionTrackerLogData>();

        protected override void UpdateLogStats()
        {
            collectionTrackerLogs = collectionTrackers 
                .Select(tracker => new NPCResourceTypeCollectionTrackerLogData
                {
                    resourceType = tracker.Key,

                    collectorsAmount = tracker.Value.CollectorsAmount,

                    targetResources = tracker.Value.ResourceInstances.Select(resource => resource.gameObject).ToArray()
                })
                .ToList();
        }
#endif

    }
}
