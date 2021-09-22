using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Selection;
using RTSEngine.Faction;

namespace RTSEngine.Event
{
    public delegate void CustomEventHandler<T, E>(T sender, E args) where E : EventArgs;

    public class GlobalEventPublisher : MonoBehaviour, IGlobalEventPublisher
    {
        #region Attributes
        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.selectionMgr = this.gameMgr.GetService<ISelectionManager>();

            // Handling entity type specific selection events
            selectedEvents = new Dictionary<EntityType, CustomEventHandler<IEntity, EntitySelectionEventArgs>>
            {
                {EntityType.unit, UnitSelectedGlobal },
                {EntityType.building, BuildingSelectedGlobal },
                {EntityType.resource, ResourceSelectedGlobal },
            };

            deselectedEvents = new Dictionary<EntityType, CustomEventHandler<IEntity, EventArgs>>
            {
                {EntityType.unit, UnitDeselectedGlobal },
                {EntityType.building, BuildingDeselectedGlobal },
                {EntityType.resource, ResourceDeselectedGlobal },
            };
        }
        #endregion

        #region Selection
        public event CustomEventHandler<IEntity, EntitySelectionEventArgs> EntitySelectedGlobal = delegate {};
        private IReadOnlyDictionary<EntityType, CustomEventHandler<IEntity, EntitySelectionEventArgs>> selectedEvents;
		public event CustomEventHandler<IEntity, EntitySelectionEventArgs> UnitSelectedGlobal = delegate {};
		public event CustomEventHandler<IEntity, EntitySelectionEventArgs> BuildingSelectedGlobal = delegate {};
		public event CustomEventHandler<IEntity, EntitySelectionEventArgs> ResourceSelectedGlobal = delegate {};

        public void RaiseEntitySelectedGlobal(IEntity entity, EntitySelectionEventArgs args)
        {
            var handler = EntitySelectedGlobal;
            handler?.Invoke(entity, args);

            var specificHandler = selectedEvents[entity.Type];
            specificHandler?.Invoke(entity, args);
        }

		public event CustomEventHandler<IEntity, EventArgs> EntityDeselectedGlobal = delegate {};
        private IReadOnlyDictionary<EntityType, CustomEventHandler<IEntity, EventArgs>> deselectedEvents;
		public event CustomEventHandler<IEntity, EventArgs> UnitDeselectedGlobal = delegate {};
		public event CustomEventHandler<IEntity, EventArgs> BuildingDeselectedGlobal = delegate {};
		public event CustomEventHandler<IEntity, EventArgs> ResourceDeselectedGlobal = delegate {};

        public void RaiseEntityDeselectedGlobal(IEntity entity)
        {
            var handler = EntityDeselectedGlobal;
            handler?.Invoke(entity, EventArgs.Empty);

            var specificHandler = deselectedEvents[entity.Type];
            specificHandler?.Invoke(entity, EventArgs.Empty);
        }
        #endregion

        #region IEntity
        public event CustomEventHandler<IEntity, EventArgs> EntityInitiatedGlobal;
        public void RaiseEntityInitiatedGlobal(IEntity sender)
        {
            var handler = EntityInitiatedGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, HealthUpdateEventArgs> EntityHealthUpdatedGlobal;
        public event CustomEventHandler<IEntity, DeadEventArgs> EntityDeadGlobal;
        public void RaiseEntityHealthUpdatedGlobal(IEntity sender, HealthUpdateEventArgs e)
        {
            var handler = EntityHealthUpdatedGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseEntityDeadGlobal(IEntity sender, DeadEventArgs e)
        {
            var handler = EntityDeadGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IEntity, UpgradeEventArgs<IEntityComponent>> EntityComponentUpgradedGlobal;
        public void RaiseEntityComponentUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntityComponent> e)
        {
            var handler = EntityComponentUpgradedGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IEntity, UpgradeEventArgs<IEntity>> EntityInstanceUpgradedGlobal;
        public void RaiseEntityInstanceUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntity> e)
        {
            var handler = EntityInstanceUpgradedGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IEntity, TaskUIReloadEventArgs> EntityComponentPendingTaskUIReloadRequestGlobal;
        // Conditions to raise this event: Sender entity is a player faction entity and it is the only entity selected.
        public void RaiseEntityComponentPendingTaskUIReloadRequestGlobal(IEntity sender, TaskUIReloadEventArgs e = null)
        {
            if (!selectionMgr.IsSelectedOnly(sender, localPlayerFaction: true))
                return;

            var handler = EntityComponentPendingTaskUIReloadRequestGlobal;
            handler?.Invoke(sender, e ?? new TaskUIReloadEventArgs());
        }

        public event CustomEventHandler<IEntity, EventArgs> EntityMouseEnterGlobal;
        public event CustomEventHandler<IEntity, EventArgs> EntityMouseExitGlobal;
        public void RaiseEntityMouseEnterGlobal(IEntity sender)
        {
            var handler = EntityMouseEnterGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseEntityMouseExitGlobal(IEntity sender)
        {
            var handler = EntityMouseExitGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, UpgradeEventArgs<IEntity>> EntityUpgradedGlobal;
        public void RaiseEntityUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntity> e)
        {
            CustomEventHandler<IEntity, UpgradeEventArgs<IEntity>> handler = EntityUpgradedGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IEntity, FactionUpdateArgs> EntityFactionUpdateStartGlobal;
        public event CustomEventHandler<IEntity, FactionUpdateArgs> EntityFactionUpdateCompleteGlobal;
        public void RaiseEntityFactionUpdateStartGlobal(IEntity sender, FactionUpdateArgs e)
        {
            var handler = EntityFactionUpdateStartGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseEntityFactionUpdateCompleteGlobal(IEntity sender, FactionUpdateArgs e)
        {
            var handler = EntityFactionUpdateCompleteGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IEntity, VisibilityEventArgs> EntityVisibilityUpdateGlobal;
        public void RaiseEntityVisibilityUpdateGlobal(IEntity sender, VisibilityEventArgs args)
        {
            var handler = EntityVisibilityUpdateGlobal;
            handler?.Invoke(sender, args);
        }
        #endregion

        #region IFactionEntity
        public event CustomEventHandler<IFactionEntity, HealthUpdateEventArgs> FactionEntityHealthUpdatedGlobal;
        public event CustomEventHandler<IFactionEntity, DeadEventArgs> FactionEntityDeadGlobal;
        public void RaiseFactionEntityHealthUpdatedGlobal(IFactionEntity sender, HealthUpdateEventArgs e)
        {
            var handler = FactionEntityHealthUpdatedGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseFactionEntityDeadGlobal(IFactionEntity sender, DeadEventArgs e)
        {
            var handler = FactionEntityDeadGlobal;
            handler?.Invoke(sender, e);
        }
        #endregion

        #region IEntityComponent
        public event CustomEventHandler<IEntityComponent, TaskUIReloadEventArgs> EntityComponentTaskUIReloadRequestGlobal;
        public void RaiseEntityComponentTaskUIReloadRequestGlobal(IEntityComponent sender, TaskUIReloadEventArgs e = null)
        {
            // Conditions to raise this event: Sender entity must be selected.
            if (!sender.Entity.Selection.IsSelected)
                return;

            var handler = EntityComponentTaskUIReloadRequestGlobal;
            handler?.Invoke(sender, e ?? new TaskUIReloadEventArgs());
        }
        #endregion

        #region IEntityTargetComponent
        public event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> EntityComponentTargetLockedGlobal;
        public event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> EntityComponentTargetStartGlobal;
        public event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> EntityComponentTargetStopGobal;

        public void RaiseEntityComponentTargetLockedGlobal(IEntityTargetComponent sender, TargetDataEventArgs e)
        {
            var handler = EntityComponentTargetLockedGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseEntityComponentTargetStartGlobal(IEntityTargetComponent sender, TargetDataEventArgs e)
        {
            var handler = EntityComponentTargetStartGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseEntityComponentTargetStopGlobal(IEntityTargetComponent sender, TargetDataEventArgs e)
        {
            var handler = EntityComponentTargetStopGobal;
            handler?.Invoke(sender, e);
        }
        #endregion

        #region IMovementComponent
        public event CustomEventHandler<IMovementComponent, EventArgs> MovementStartGlobal;
        public event CustomEventHandler<IMovementComponent, EventArgs> MovementStopGlobal;
        public void RaiseMovementStartGlobal(IMovementComponent sender)
        {
            var handler = MovementStartGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseMovementStopGlobal(IMovementComponent sender)
        {
            var handler = MovementStopGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        #endregion

        #region IAttackComponent
        public event CustomEventHandler<IAttackComponent, EventArgs> AttackSwitchStartGlobal;
        public event CustomEventHandler<IAttackComponent, EventArgs> AttackSwitchCompleteGlobal;
        public void RaiseAttackSwitchStartGlobal(IAttackComponent sender)
        {
            var handler = AttackSwitchStartGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseAttackSwitchCompleteGlobal(IAttackComponent sender)
        {
            var handler = AttackSwitchCompleteGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        #endregion

        #region IPendingTaskEntityComponent
        public event CustomEventHandler<IPendingTaskEntityComponent, EventArgs> PendingTaskEntityComponentAdded;
        public event CustomEventHandler<IPendingTaskEntityComponent, EventArgs> PendingTaskEntityComponentRemoved;
        public event CustomEventHandler<IPendingTaskEntityComponent, EventArgs> PendingTaskEntityComponentUpdated;

        public void RaisePendingTaskEntityComponentAdded(IPendingTaskEntityComponent source)
        {
            var handler = PendingTaskEntityComponentAdded;
            handler?.Invoke(source, EventArgs.Empty);
        }
        public void RaisePendingTaskEntityComponentRemoved(IPendingTaskEntityComponent source)
        {
            var handler = PendingTaskEntityComponentRemoved;
            handler?.Invoke(source, EventArgs.Empty);
        }
        public void RaisePendingTaskEntityComponentUpdated(IPendingTaskEntityComponent source)
        {
            var handler = PendingTaskEntityComponentUpdated;
            handler?.Invoke(source, EventArgs.Empty);
        }
        #endregion

        #region IUnit
        public event CustomEventHandler<IUnit, EventArgs> UnitInitiatedGlobal;
        public void RaiseUnitInitiatedGlobal(IUnit sender)
        {
            var handler = UnitInitiatedGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<IUnit, HealthUpdateEventArgs> UnitHealthUpdatedGlobal;
        public event CustomEventHandler<IUnit, DeadEventArgs> UnitDeadGlobal;
        public void RaiseUnitHealthUpdatedGlobal(IUnit sender, HealthUpdateEventArgs e)
        {
            var handler = UnitHealthUpdatedGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseUnitDeadGlobal(IUnit sender, DeadEventArgs e)
        {
            var handler = UnitDeadGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IEntity, ResourceEventArgs> UnitResourceDropOffStartGlobal;
        public event CustomEventHandler<IEntity, ResourceEventArgs> UnitResourceDropOffCompleteGlobal;
        public void RaiseUnitResourceDropOffStartGlobal(IUnit sender, ResourceEventArgs e)
        {
            var handler = UnitResourceDropOffStartGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseUnitResourceDropOffCompleteGlobal(IUnit sender, ResourceEventArgs e)
        {
            var handler = UnitResourceDropOffCompleteGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IUnit, UpgradeEventArgs<IEntity>> UnitUpgradedGlobal;
        public void RaiseUnitUpgradedGlobal(IUnit sender, UpgradeEventArgs<IEntity> e)
        {
            CustomEventHandler<IUnit, UpgradeEventArgs<IEntity>> handler = UnitUpgradedGlobal;
            handler?.Invoke(sender, e);
        }
        #endregion

        #region IBuilidng
        public event CustomEventHandler<IBuilding, HealthUpdateEventArgs> BuildingHealthUpdatedGlobal;
        public event CustomEventHandler<IBuilding, DeadEventArgs> BuildingDeadGlobal;
        public void RaiseBuildingHealthUpdatedGlobal(IBuilding sender, HealthUpdateEventArgs e)
        {
            var handler = BuildingHealthUpdatedGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseBuildingDeadGlobal(IBuilding sender, DeadEventArgs e)
        {
            var handler = BuildingDeadGlobal;
            handler?.Invoke(sender, e);
        }

        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStartGlobal;
        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStopGlobal;
        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStatusUpdatedGlobal;
        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacedGlobal;
        public void RaiseBuildingPlacementStartGlobal(IBuilding sender)
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacementStartGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseBuildingPlacementStopGlobal(IBuilding sender)
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacementStopGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }


        public void RaiseBuildingPlacementStatusUpdatedGlobal(IBuilding sender)
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacementStatusUpdatedGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseBuildingPlacedGlobal(IBuilding sender)
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacedGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<IBuilding, EventArgs> BuildingBuiltGlobal;
        public void RaiseBuildingBuiltGlobal(IBuilding sender)
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingBuiltGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<IBuilding, UpgradeEventArgs<IEntity>> BuildingUpgradedGlobal;
        public void RaiseBuildingUpgradedGlobal(IBuilding sender, UpgradeEventArgs<IEntity> args)
        {
            CustomEventHandler<IBuilding, UpgradeEventArgs<IEntity>> handler = BuildingUpgradedGlobal;
            handler?.Invoke(sender, args);
        }

        #endregion

        #region IBorder
        public event CustomEventHandler<IBorder, EventArgs> BorderActivatedGlobal;
        public event CustomEventHandler<IBorder, EventArgs> BorderDisabledGlobal;
        public event CustomEventHandler<IBorder, ResourceEventArgs> BorderResourceAddedGlobal;
        public event CustomEventHandler<IBorder, ResourceEventArgs> BorderResourceRemovedGlobal;

        public void RaiseBorderActivatedGlobal(IBorder sender)
        {
            CustomEventHandler<IBorder, EventArgs> handler = BorderActivatedGlobal;

            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseBorderDisabledGlobal(IBorder sender)
        {
            CustomEventHandler<IBorder, EventArgs> handler = BorderDisabledGlobal;

            handler?.Invoke(sender, EventArgs.Empty);
        }
        public void RaiseBorderResourceAddedGlobal(IBorder sender, ResourceEventArgs e)
        {
            CustomEventHandler<IBorder, ResourceEventArgs> handler = BorderResourceAddedGlobal;

            handler?.Invoke(sender, e);
        }
        public void RaiseBorderResourceRemovedGlobal(IBorder sender, ResourceEventArgs e)
        {
            CustomEventHandler<IBorder, ResourceEventArgs> handler = BorderResourceRemovedGlobal;

            handler?.Invoke(sender, e);
        }
        #endregion

        #region IResource
        public event CustomEventHandler<IResource, EventArgs> ResourceInitiatedGlobal;

        public event CustomEventHandler<IResource, HealthUpdateEventArgs> ResourceHealthUpdatedGlobal;
        public event CustomEventHandler<IResource, DeadEventArgs> ResourceDeadGlobal;

        public void RaiseResourceInitiatedGlobal(IResource sender)
        {
            var handler = ResourceInitiatedGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public void RaiseResourceHealthUpdatedGlobal(IResource sender, HealthUpdateEventArgs e)
        {
            var handler = ResourceHealthUpdatedGlobal;
            handler?.Invoke(sender, e);
        }
        public void RaiseResourceDeadGlobal(IResource sender, DeadEventArgs e)
        {
            var handler = ResourceDeadGlobal;
            handler?.Invoke(sender, e);
        }
        #endregion

        #region Faction Slot
        public event CustomEventHandler<IFactionSlot, ResourceUpdateEventArgs> FactionSlotResourceAmountUpdatedGlobal;

        public void RaiseFactionSlotResourceAmountUpdatedGlobal(IFactionSlot factionSlot, ResourceUpdateEventArgs args)
        {
            var handler = FactionSlotResourceAmountUpdatedGlobal;

            handler?.Invoke(factionSlot, args);
        }

        public event CustomEventHandler<IFactionSlot, DefeatConditionEventArgs> FactionSlotDefeatConditionTriggeredGlobal;

        public void RaiseFactionSlotDefeatConditionTriggeredGlobal(IFactionSlot factionSlot, DefeatConditionEventArgs args)
        {
            var handler = FactionSlotDefeatConditionTriggeredGlobal;

            handler?.Invoke(factionSlot, args);
        }

        public event CustomEventHandler<IFactionSlot, DefeatConditionEventArgs> FactionSlotDefeatedGlobal;

        public void RaiseFactionSlotDefeatedGlobal(IFactionSlot factionSlot, DefeatConditionEventArgs args)
        {
            var handler = FactionSlotDefeatedGlobal;

            handler?.Invoke(factionSlot, args);
        }

        #endregion

        #region UI
        public event CustomEventHandler<object, MessageEventArgs> ShowTooltipGlobal;
        public event CustomEventHandler<object, EventArgs> HideTooltipGlobal;
        public void RaiseShowTooltipGlobal(object sender, MessageEventArgs args)
        {
            var handler = ShowTooltipGlobal;
            handler?.Invoke(sender, args);
        }
        public void RaiseHideTooltipGlobal(object sender)
        {
            var handler = HideTooltipGlobal;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<object, MessageEventArgs> ShowPlayerMessageGlobal;
        public void RaiseShowPlayerMessageGlobal(object sender, MessageEventArgs e)
        {
            var handler = ShowPlayerMessageGlobal;
            handler?.Invoke(sender, e);
        }
        #endregion

        #region Game State
        public event CustomEventHandler<IGameManager, EventArgs> GameStateUpdatedGlobal;

        public void RaiseGameStateUpdatedGlobal()
        {
            var handler = GameStateUpdatedGlobal;
            handler?.Invoke(gameMgr, EventArgs.Empty);
        }
        #endregion

        #region IEffectObject
        public event CustomEventHandler<IEffectObject, EventArgs> EffectObjectCreatedGlobal;
        public event CustomEventHandler<IEffectObject, EventArgs> EffectObjectDestroyedGlobal;

        public void RaiseEffectObjectCreatedGlobal(IEffectObject source)
        {
            var handler = EffectObjectCreatedGlobal;
            handler?.Invoke(source, EventArgs.Empty);
        }
        public void RaiseEffectObjectDestroyedGlobal(IEffectObject source)
        {
            var handler = EffectObjectDestroyedGlobal;
            handler?.Invoke(source, EventArgs.Empty);
        }
        #endregion
    }
}
