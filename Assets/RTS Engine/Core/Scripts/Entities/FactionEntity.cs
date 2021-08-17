using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Health;
using RTSEngine.ResourceExtension;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.BuildingExtension;
using RTSEngine.UnitExtension;
using RTSEngine.Determinism;

namespace RTSEngine.Entities
{
    public abstract class FactionEntity : Entity, IFactionEntity
    {
        #region Class Attributes
        public IFactionManager FactionMgr { private set; get; }

        [SerializeField, Tooltip("Main entities are entities that need to be destroyed to eliminate the faction when the defeat condition is set to 'Eliminate Main'")]
        private bool isMainEntity = false;
        public bool IsMainEntity => isMainEntity;

        [SerializeField, Tooltip("Can the faction be changed during the game?")]
        private bool isFactionLocked = false;
        public bool IsFactionLocked => isFactionLocked;

        //resources to add when the faction entity is added or removed
        [Space(), SerializeField, Tooltip("Resources added/removed to/from the owner faction when the faction entity is successfully initialized.")]
        private ResourceInput[] initResources = new ResourceInput[0];
        public IEnumerable<ResourceInput> InitResources => initResources;

        [SerializeField, Tooltip("Resources added/removed to/from the owner faction when the faction entity is disabled (disabled (destroyed/converted)")]
        private ResourceInput[] disableResources = new ResourceInput[0];
        public IEnumerable<ResourceInput> DisableResources => disableResources;

        [Space(), SerializeField, Tooltip("What parts of the model will be colored with the faction colors?")]
        private ColoredRenderer[] coloredRenderers = new ColoredRenderer[0];

        public new IFactionEntityHealth Health { private set; get; }

        public IRallypoint Rallypoint { private set; get; }
        public IDropOffTarget DropOffTarget { private set; get; }
        public IUnitCarrier UnitCarrier { private set; get; }

        // Services
        protected IResourceManager resourceMgr { private set; get; }
        protected IUnitManager unitMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public sealed override void Init(IGameManager gameMgr, InitEntityParameters initParams)
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();

            base.Init(gameMgr, initParams);

            if (!IsFree) //if the entity belongs to a faction
                FactionMgr = gameMgr.GetFactionSlot(FactionID).FactionMgr;
        }

        protected override void FetchComponents()
        {
            //since the Health property overwrites the original Health property in IEntity, it needs to be fetched first.
            //so that other IEntityInitiziable components can use the Health property when being initialized in the IEntity component.
            Health = transform.GetComponentInChildren<IFactionEntityHealth>();

            // The main health component for any faction entity is the one that handles its damage and health.
            base.Health = Health;

            Rallypoint = transform.GetComponentInChildren<IRallypoint>();
            DropOffTarget = transform.GetComponentInChildren<IDropOffTarget>();
            UnitCarrier = transform.GetComponentInChildren<IUnitCarrier>();

            base.FetchComponents();
        }

        protected override void Disable(bool isUpgrade, bool isFactionUpdate)
        {
            base.Disable(isUpgrade, isFactionUpdate);

            if(IsInitialized && !IsFree)
                resourceMgr.UpdateResource(FactionID, DisableResources, add:true); //add the disable resources to the entity's faction.
        }
        #endregion

        #region Updating Faction Colors
        protected sealed override void UpdateColors()
        {
            if(IsFree)
                SelectionColor = Type == EntityType.unit ? unitMgr.FreeUnitColor : buildingMgr.FreeBuildingColor;
            else
                SelectionColor = gameMgr.GetFactionSlot(FactionID).Data.color;

            foreach (ColoredRenderer cr in coloredRenderers)
                cr.UpdateColor(IsFree ? Color.white : gameMgr.GetFactionSlot(FactionID).Data.color, logger);
        }
        #endregion

        #region Updating Faction
        //when targetFactionID < 0 then the faction entity will be a free entity. 
        public sealed override ErrorMessage SetFaction (IEntity targetFactionEntity, int targetFactionID)
        {
            if (targetFactionID == FactionID)
                return ErrorMessage.factionIsFriendly;
            else if (IsFactionLocked)
                return ErrorMessage.factionLocked;

            return inputMgr.SendInput(
                new CommandInput

                {
                    sourceMode = (byte)InputMode.entity,
                    targetMode = (byte)InputMode.setFaction,

                    intValues = inputMgr.ToIntValues(targetFactionID)
                },
                source: this,
                target: targetFactionEntity);
        }

        public sealed override ErrorMessage SetFactionLocal (IEntity source, int targetFactionID)
        {
            var eventArgs = new FactionUpdateArgs(source, targetFactionID);
            globalEvent.RaiseEntityFactionUpdateStartGlobal(this, eventArgs);

            selectionMgr.Remove(this);

            //to disable the entity components for its ex-faction
            Disable(isUpgrade: false, isFactionUpdate: true);

            if(RTSHelper.IsValidFaction(targetFactionID))
            {
                FactionMgr = gameMgr.GetFactionSlot(targetFactionID).FactionMgr;
                FactionID = targetFactionID;
                IsFree = false;
            }
            else
            {
                FactionMgr = null;
                FactionID = -1;
                IsFree = true;
            }

            //update colors.
            UpdateColors();

            RaiseFactionUpdateComplete(eventArgs);
            globalEvent.RaiseEntityFactionUpdateCompleteGlobal(this, eventArgs);

            OnFactionUpdated();

            return ErrorMessage.none;
        }

        protected virtual void OnFactionUpdated () { }
        #endregion
    }
}
