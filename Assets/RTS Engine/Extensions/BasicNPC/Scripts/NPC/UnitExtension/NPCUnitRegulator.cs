using System.Collections.Generic;
using System.Linq;
using System;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.NPC.ResourceExtension;
using RTSEngine.NPC.EntityComponent;

namespace RTSEngine.NPC.UnitExtension
{
    /// <summary>
    /// Regulates the creation of a unit type for NPC factions.
    /// </summary>
    public class NPCUnitRegulator : NPCRegulator<IUnit>
    {
        #region Attributes 
        public NPCUnitRegulatorData Data { private set; get; }

        // Amount of spawned instances of the regulated unit type to the total available population slots of the NPC faction ratio
        // Inferred from the regulator data
        private readonly float ratio = 0;

        // Component used by the NPC faction to track all of its IUnitCreator instances
        private readonly IEntityComponentTracker<IUnitCreator> unitCreatorTracker;

        // key: IUnitCreator instance responsible for creating the unit in its creation task of index 'value'
        private readonly Dictionary<IPendingTaskEntityComponent, int> creators = new Dictionary<IPendingTaskEntityComponent, int>();
        public IEnumerable<IPendingTaskEntityComponent> Creators => creators.Keys.ToArray();
        public int CreatorsCount => creators.Count;

        // NPC components
        private readonly INPCUnitCreator npcUnitCreator;
        private readonly INPCResourceManager npcResourceMgr;

        // Game services
        protected readonly IResourceManager resourceMgr;
        #endregion

        #region Raising Events

        #endregion

        #region Initializing/Terminating
        public NPCUnitRegulator (NPCUnitRegulatorData data, IUnit prefab, IGameManager gameMgr, INPCManager npcMgr)
            : base(data, prefab, gameMgr, npcMgr)
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();

            this.Data = data;

            this.npcUnitCreator = npcMgr.GetNPCComponent<INPCUnitCreator>();
            this.npcResourceMgr = npcMgr.GetNPCComponent<INPCResourceManager>();

            ratio = Data.Ratio;

            // Add the existing units that can be regulated by this component
            foreach (IUnit unit in this.factionMgr.Units)
                AddExisting(unit);

            unitCreatorTracker = this.npcMgr.GetNPCComponent<INPCEntityComponentTracker>().UnitCreatorTracker;
            foreach (IUnitCreator creator in unitCreatorTracker.Components)
                AddUnitCreator(creator);

            unitCreatorTracker.ComponentAdded += HandleUnitCreatorAdded;
            unitCreatorTracker.ComponentUpdated += HandleUnitCreatorUpdated;
            unitCreatorTracker.ComponentRemoved += HandleUnitCreatorRemoved;

            globalEvent.UnitInitiatedGlobal += HandleUnitInitiatedGlobal;

            if (npcUnitCreator.PopulationResource.IsValid())
            {
                IFactionResourceHandler populationResourceHandler = resourceMgr.FactionResources[factionMgr.FactionID].ResourceHandlers[npcUnitCreator.PopulationResource];
                UpdateTargetCount((int)(populationResourceHandler.Capacity * ratio));

                populationResourceHandler.FactionResourceAmountUpdated += HandlePopulationResourceAmountUpdated;
            }
        }

        protected override void OnDisabled()
        {
            unitCreatorTracker.ComponentAdded += HandleUnitCreatorAdded;
            unitCreatorTracker.ComponentUpdated += HandleUnitCreatorUpdated;
            unitCreatorTracker.ComponentRemoved += HandleUnitCreatorRemoved;

            globalEvent.UnitInitiatedGlobal -= HandleUnitInitiatedGlobal;

            if (npcUnitCreator.PopulationResource.IsValid())
            {
                IFactionResourceHandler populationResourceHandler = resourceMgr.FactionResources[factionMgr.FactionID].ResourceHandlers[npcUnitCreator.PopulationResource];
                populationResourceHandler.FactionResourceAmountUpdated -= HandlePopulationResourceAmountUpdated;
            }
        }
        #endregion

        #region Handling Events: IUnit
        private void HandleUnitInitiatedGlobal(IUnit unit, EventArgs args) => AddNewlyCreated(unit);
        #endregion

        #region Handling Events: IUnitCreator
        private void HandleUnitCreatorAdded(IEntityComponentTracker<IUnitCreator> sender, EntityComponentEventArgs<IUnitCreator> args)
        {
            AddUnitCreator(args.Component);
        }

        private void HandleUnitCreatorUpdated(IEntityComponentTracker<IUnitCreator> sender, EntityComponentEventArgs<IUnitCreator> args)
        {
            RemoveUnitCreator(args.Component);
            AddUnitCreator(args.Component);
        }

        private void HandleUnitCreatorRemoved(IEntityComponentTracker<IUnitCreator> sender, EntityComponentEventArgs<IUnitCreator> args)
        {
            RemoveUnitCreator(args.Component);
        }
        #endregion

        #region Adding/Removing Unit Creators
        private void AddUnitCreator (IUnitCreator newCreator)
        {
            if (!newCreator.IsValid()
                || !newCreator.Entity.IsFriendlyFaction(factionMgr.FactionID))
                return;

            int taskID = newCreator.FindTaskIndex(Prefab.Code);
            if (taskID < 0)
                return;

            // newCreator is valid and has a task that creates the unit regulated by this component
            creators.Add(newCreator, taskID);

            newCreator.PendingTaskAction += HandleUnitCreatorPendingTaskAction;
        }

        private void RemoveUnitCreator (IUnitCreator removeCreator)
        {
            if (!creators.Remove(removeCreator))
                return;

            removeCreator.PendingTaskAction -= HandleUnitCreatorPendingTaskAction;
        }
        #endregion

        #region Handling Events: Tracked IUnitCreator
        private void HandleUnitCreatorPendingTaskAction(IPendingTaskEntityComponent sender, PendingTaskEventArgs args)
        {
            if (creators[sender] != args.Data.sourceID)
                return;

            switch(args.State)
            {
                case Task.PendingTaskState.added:
                    AddPending(args.Data.sourceTaskInput.PrefabObject.GetComponent<IUnit>());
                    break;

                case Task.PendingTaskState.cancelled:
                    RemovePending(args.Data.sourceTaskInput.PrefabObject.GetComponent<IUnit>());
                    break;

            }
        }
        #endregion

        #region Creating Unit
        public bool Create (ref int amount)
        {
            if (!RTSHelper.TestFactionEntityRequirements(Data.FactionEntityRequirements, factionMgr))
                return false;

            foreach(var nextCreator in creators)
            {
                ErrorMessage errorMessage = ErrorMessage.none;

                while(errorMessage == ErrorMessage.none)
                {
                    if (CurrPendingAmount >= MaxPendingAmount)
                        return false;

                    errorMessage = nextCreator.Key.LaunchTaskAction(nextCreator.Value, false);

                    switch (errorMessage)
                    {
                        case ErrorMessage.none: 
                            amount--;

                            break;

                        case ErrorMessage.taskMissingResourceRequirements:
                            npcResourceMgr.OnIncreaseMissingResourceRequest(nextCreator.Key.Tasks.ElementAt(nextCreator.Value).RequiredResources);
                            return false;

                        default:
                            break;
                    }

                    if (amount <= 0)
                        return true;
                }
            }

            return false;
        }
        #endregion

        #region Target Count Manipulation
        public void HandlePopulationResourceAmountUpdated(IFactionResourceHandler resourceHandler, ResourceUpdateEventArgs args)
        {
            int ratioTargetCount = (int)(resourceHandler.Capacity * ratio);
            if (ratioTargetCount <= TargetCount)
                return;

            UpdateTargetCount(ratioTargetCount);
        }
        #endregion
    }
}