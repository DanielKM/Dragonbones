using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.NPC.ResourceExtension
{
    public class NPCResourceTypeCollectionTracker
    {
        #region Attributes
        public NPCResourceTypeCollectionData CollectionData { private set; get; }

        // Current amount of the tracked resource type collectors.
        public int CollectorsAmount { private set; get; } = 0;
        public bool IsMinAmountReached => CollectorsAmount >= CollectionData.minCollectorsAmount;

        // The instances of the same resource type that are being actively collected and tracked.
        private readonly List<IResource> resourceInstances;
        public IEnumerable<IResource> ResourceInstances => resourceInstances.ToArray();

        // Actively monitors the instances of NPCUnitRegulator for collector units that are able to collect the tracked resource type.
        public NPCActiveRegulatorMonitor CollectorMonitor { private set; get; } 

        private readonly INPCManager npcMgr;
        #endregion

        #region Raising Events
        public event CustomEventHandler<NPCResourceTypeCollectionTracker, EventArgs> CollectorSlotFreed;

        private void RaiseCollectorSlotFreed()
        {
            var handler = CollectorSlotFreed;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public NPCResourceTypeCollectionTracker(IGameManager gameMgr, INPCManager npcMgr, NPCResourceTypeCollectionData collectionData)
        {
            this.CollectionData = collectionData;
            this.npcMgr = npcMgr;

            CollectorsAmount = 0;
            resourceInstances = new List<IResource>();

            CollectorMonitor = new NPCActiveRegulatorMonitor(gameMgr, npcMgr.FactionMgr);
        }

        public void Disable()
        {
            CollectorMonitor.Disable();
        }
        #endregion

        #region Adding/Removing Resources
        public void AddResource(IResource resource)
        {
            if (resourceInstances.Contains(resource))
                return;

            resourceInstances.Add(resource);
            resource.WorkerMgr.WorkerAdded += HandleWorkerAdded;
            resource.WorkerMgr.WorkerRemoved += HandleWorkerRemoved;
        }

        public void RemoveResource(IResource resource)
        {
            if (!resourceInstances.Remove(resource))
                return;

            resource.WorkerMgr.WorkerAdded -= HandleWorkerAdded;
            resource.WorkerMgr.WorkerRemoved -= HandleWorkerRemoved;
        }
        #endregion

        #region Handling Event: Resource Collector Added/Removed - Updating CollectorsAmount
        private void HandleWorkerAdded(IEntity sender, EntityEventArgs<IUnit> args)
        {
            if (npcMgr.FactionMgr.IsSameFaction(args.Entity))
                CollectorsAmount++;
        }

        private void HandleWorkerRemoved(IEntity sender, EntityEventArgs<IUnit> args)
        {
            if (npcMgr.FactionMgr.IsSameFaction(args.Entity))
            {
                CollectorsAmount--;

                CollectorsAmount = Mathf.Max(CollectorsAmount, 0);

                RaiseCollectorSlotFreed();
            }
        }
        #endregion

        #region Collectors Amount Helper Methods
        public bool CanAddCollector (int availableCollectorsAmount)
        {
            return CollectionData.minCollectorsAmount > CollectorsAmount
                || (availableCollectorsAmount * CollectionData.maxCollectorsRatioRange.RandomValue) > CollectorsAmount;
        }

        public int GetTargetCollectorsAmount (IResource resource)
        {
            int targetCollectorsAmount = (int)(resource.WorkerMgr.MaxAmount * CollectionData.instanceCollectorsRatio.RandomValue);

            return Mathf.Max(targetCollectorsAmount, 1);
        }
        #endregion
    }
}
