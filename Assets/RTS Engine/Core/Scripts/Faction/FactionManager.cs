using System;
using System.Linq;
using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.BuildingExtension;
using RTSEngine.Game;

namespace RTSEngine.Faction
{
    public class FactionManager : IFactionManager
    {
        #region Attributes
        public int FactionID { private set; get; }

        public IFactionSlot Slot {private set; get;}

        private List<IFactionEntity> factionEntities; 
        public IEnumerable<IFactionEntity> FactionEntities => factionEntities.ToArray();

        private List<IFactionEntity> mainEntities;
        public IEnumerable<IFactionEntity> MainEntities => mainEntities.ToArray();

        private List<IUnit> units; 
        public IEnumerable<IUnit> Units => units.ToArray();

        private List<IUnit> attackUnits;
        public IEnumerable<IUnit> GetAttackUnits(float range = 1.0f)
            => attackUnits.GetRange(0, (int)(attackUnits.Count * (range >= 0.0f && range <= 1.0f ? range : 1.0f)));

        private List<IBuilding> buildings;
        public IEnumerable<IBuilding> Buildings => buildings.ToArray();

        private List<IBuilding> buildingCenters;
        public IEnumerable<IBuilding> BuildingCenters => buildingCenters.ToArray();

        private List<FactionEntityAmountLimit> limits = new List<FactionEntityAmountLimit>();

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IFactionManager, EntityEventArgs<IFactionEntity>> OwnFactionEntityAdded;
        public event CustomEventHandler<IFactionManager, EntityEventArgs<IFactionEntity>> OwnFactionEntityRemoved;

        private void RaiseOwnFactionEntityAdded (EntityEventArgs<IFactionEntity> args)
        {
            var handler = OwnFactionEntityAdded;
            handler?.Invoke(this, args);
        }
        private void RaiseOwnFactionEntityRemoved (EntityEventArgs<IFactionEntity> args)
        {
            var handler = OwnFactionEntityRemoved;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public void Init (IGameManager gameMgr, IFactionSlot slot) 
        {
            this.gameMgr = gameMgr;
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            this.Slot = slot;
            this.Slot.FactionSlotStateUpdated += HandleFactionSlotStateUpdated;
            this.FactionID = slot.ID;

            this.limits = new List<FactionEntityAmountLimit>();
            if (slot.Data.type.IsValid() && slot.Data.type.Limits.IsValid())
                limits = slot.Data.type.Limits
                    .Select(limit => new FactionEntityAmountLimit(definer: limit.Definer, maxAmount: limit.MaxAmount))
                    .ToList();

            factionEntities = new List<IFactionEntity>();
            mainEntities = new List<IFactionEntity>();

            units = new List<IUnit>();
            attackUnits = new List<IUnit>();

            buildings = new List<IBuilding>();
            buildingCenters = new List<IBuilding>();

            globalEvent.UnitInitiatedGlobal += HandleUnitInitiatedGlobal;

            globalEvent.BorderActivatedGlobal += HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;
            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;

            globalEvent.FactionEntityDeadGlobal += HandleFactionEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal += HandleEntityFactionUpdateCompleteGlobal;
		}

        private void HandleFactionSlotStateUpdated(IFactionSlot slot, EventArgs args)
        {
            // Disable this component when the faction is eliminated.
            if (slot.State == FactionSlotState.eliminated)
                Disable();
        }

        private void Disable()
        {
            globalEvent.UnitInitiatedGlobal -= HandleUnitInitiatedGlobal;

            globalEvent.BorderActivatedGlobal -= HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;
            globalEvent.BuildingPlacedGlobal -= HandleBuildingPlacedGlobal;

            globalEvent.FactionEntityDeadGlobal -= HandleFactionEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal -= HandleEntityFactionUpdateCompleteGlobal;
        }
        #endregion

        #region Handling Events
        private void HandleBorderActivatedGlobal(IBorder border, EventArgs args)
        {
            if (!RTSHelper.IsFactionEntity(border.Building, FactionID))
                return;

            buildingCenters.Add(border.Building);
        }
        private void HandleBorderDisabledGlobal(IBorder border, EventArgs args)
        {
            if (!RTSHelper.IsFactionEntity(border.Building, FactionID))
                return;

            buildingCenters.Remove(border.Building);
        }

        private void HandleUnitInitiatedGlobal(IUnit sender, EventArgs args) => AddUnit(sender);

        private void HandleBuildingPlacedGlobal(IBuilding sender, EventArgs args) => AddBuilding(sender);

        private void HandleFactionEntityDeadGlobal(IFactionEntity factionEntity, DeadEventArgs args)
        {
            if (factionEntity.Type == EntityType.unit)
                RemoveUnit(factionEntity as IUnit);
            else
                RemoveBuilding(factionEntity as IBuilding);
        }

        private void HandleEntityFactionUpdateStartGlobal (IEntity updatedInstance, FactionUpdateArgs args)
        {
            switch(updatedInstance.Type)
            {
                case EntityType.unit:
                    RemoveUnit(updatedInstance as IUnit);
                    break;

                case EntityType.building:
                    RemoveBuilding(updatedInstance as IBuilding);
                    break;
            }
        }

        private void HandleEntityFactionUpdateCompleteGlobal (IEntity updatedInstance, FactionUpdateArgs args)
        {
            //when the conversion is complete and the faction entity is assigned their new faction, add them back to the faction lists:
            switch(updatedInstance.Type)
            {
                case EntityType.unit:
                    AddUnit(updatedInstance as IUnit);
                    break;

                case EntityType.building:
                    AddBuilding(updatedInstance as IBuilding);
                    break;
            }
        }
        #endregion

        #region Adding/Removing Faction Entities
        private void AddFactionEntity(IFactionEntity factionEntity)
        {
            factionEntities.Add(factionEntity);

            if (factionEntity.IsMainEntity)
                mainEntities.Add(factionEntity);

            if (factionEntity.DropOffTarget != null)
                OnDropOffTargetAdded();

            UpdateLimit(factionEntity.Code, factionEntity.Category, increment:true);

            RaiseOwnFactionEntityAdded(new EntityEventArgs<IFactionEntity>(factionEntity));
        }

        // When a new resource drop off building is spawned, all collectors check if this building can suit them or not.
        private void OnDropOffTargetAdded ()
		{
            foreach (IUnit unit in units)
                unit.DropOffSource?.UpdateTarget();
        }

        private void RemoveFactionEntity (IFactionEntity factionEntity)
        {
            factionEntities.Remove(factionEntity);
            if (factionEntity.IsMainEntity)
                mainEntities.Remove(factionEntity);

            UpdateLimit(factionEntity.Code, factionEntity.Category, increment:false);

            RaiseOwnFactionEntityRemoved(new EntityEventArgs<IFactionEntity>(factionEntity));

            // Check if the faction doesn't have any buildings/units anymore and trigger the faction defeat in that case
            CheckFactionDefeat(); 
        }

        private void AddUnit (IUnit unit)
        {
            if(!RTSHelper.IsFactionEntity(unit, FactionID))
                return;

            AddFactionEntity(unit);

			units.Add (unit);
            if (unit.AttackComponent != null)
                attackUnits.Add(unit);
        }

		private void RemoveUnit (IUnit unit)
		{
            if(!RTSHelper.IsFactionEntity(unit, FactionID))
                return;

            RemoveFactionEntity(unit);

			units.Remove (unit);
            if (unit.AttackComponent != null)
                attackUnits.Remove(unit);
        }

        private void AddBuilding (IBuilding building)
		{
            if(!RTSHelper.IsFactionEntity(building, FactionID))
                return;

            AddFactionEntity(building);

			buildings.Add (building);
		}

		private void RemoveBuilding (IBuilding building)
		{
            if(!RTSHelper.IsFactionEntity(building, FactionID))
                return;

            RemoveFactionEntity(building);

			buildings.Remove (building);
        }
        #endregion

        #region Handling Faction Defeat Conditions
        // A method that checks if the faction doesn't have any more units/buildings and trigger a faction defeat in that case.
        private void CheckFactionDefeat ()
        {
            if (mainEntities.Count == 0)
                globalEvent.RaiseFactionSlotDefeatConditionTriggeredGlobal(Slot, new DefeatConditionEventArgs(DefeatConditionType.eliminateMain));

            if (factionEntities.Count == 0)
                globalEvent.RaiseFactionSlotDefeatConditionTriggeredGlobal(Slot, new DefeatConditionEventArgs(DefeatConditionType.eliminateAll));
        }
        #endregion

        #region Handling Faction Limits
        public bool AssignLimits (IEnumerable<FactionEntityAmountLimit> newLimits)
        {
            if (!newLimits.IsValid())
                return false;

            limits = newLimits.ToList();

            return true;
        }

        public bool HasReachedLimit(IEntity entity)
            => HasReachedLimit(entity.Code, entity.Category);

        public bool HasReachedLimit(string code, IEnumerable<string> category) 
            => limits
                .Any(limit => limit.IsMaxAmountReached(code, category));
        public bool HasReachedLimit(string code, string category) 
            => limits
                .Any(limit => limit.IsMaxAmountReached(code, Enumerable.Repeat(category, 1)));

        public void UpdateLimit(IEntity entity, bool increment)
            => UpdateLimit(entity.Code, entity.Category, increment);

        private void UpdateLimit(string code, IEnumerable<string> category, bool increment)
        {
            foreach(FactionEntityAmountLimit limit in limits)
                if (limit.Contains(code, category))
                {
                    limit.Update(increment ? 1 : -1);
                    return;
                }
        }
        #endregion
    }
}
