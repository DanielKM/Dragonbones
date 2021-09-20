using RTSEngine.BuildingExtension;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Faction;
using RTSEngine.Game;
using System;

namespace RTSEngine.Event
{
    public interface IGlobalEventPublisher : IPreRunGameService
    {
        event CustomEventHandler<IAttackComponent, EventArgs> AttackSwitchCompleteGlobal;
        event CustomEventHandler<IAttackComponent, EventArgs> AttackSwitchStartGlobal;

        event CustomEventHandler<IBorder, EventArgs> BorderActivatedGlobal;
        event CustomEventHandler<IBorder, EventArgs> BorderDisabledGlobal;
        event CustomEventHandler<IBorder, ResourceEventArgs> BorderResourceAddedGlobal;
        event CustomEventHandler<IBorder, ResourceEventArgs> BorderResourceRemovedGlobal;

        event CustomEventHandler<IBuilding, EventArgs> BuildingBuiltGlobal;
        event CustomEventHandler<IBuilding, DeadEventArgs> BuildingDeadGlobal;
        event CustomEventHandler<IBuilding, HealthUpdateEventArgs> BuildingHealthUpdatedGlobal;
        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacedGlobal;
        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStartGlobal;
        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStatusUpdatedGlobal;
        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStopGlobal;
        event CustomEventHandler<IBuilding, UpgradeEventArgs<IEntity>> BuildingUpgradedGlobal;

        event CustomEventHandler<IEffectObject, EventArgs> EffectObjectCreatedGlobal;
        event CustomEventHandler<IEffectObject, EventArgs> EffectObjectDestroyedGlobal;

        event CustomEventHandler<IEntity, TaskUIReloadEventArgs> EntityComponentPendingTaskUIReloadRequestGlobal;
        event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> EntityComponentTargetLockedGlobal;
        event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> EntityComponentTargetStartGlobal;
        event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> EntityComponentTargetStopGobal;
        event CustomEventHandler<IEntityComponent, TaskUIReloadEventArgs> EntityComponentTaskUIReloadRequestGlobal;
        event CustomEventHandler<IEntity, UpgradeEventArgs<IEntityComponent>> EntityComponentUpgradedGlobal;

        event CustomEventHandler<IEntity, DeadEventArgs> EntityDeadGlobal;
        event CustomEventHandler<IEntity, HealthUpdateEventArgs> EntityHealthUpdatedGlobal;
        event CustomEventHandler<IEntity, EventArgs> EntityInitiatedGlobal;
        event CustomEventHandler<IEntity, UpgradeEventArgs<IEntity>> EntityInstanceUpgradedGlobal;
        event CustomEventHandler<IEntity, EventArgs> EntityMouseEnterGlobal;
        event CustomEventHandler<IEntity, EventArgs> EntityMouseExitGlobal;
        event CustomEventHandler<IEntity, UpgradeEventArgs<IEntity>> EntityUpgradedGlobal;
        event CustomEventHandler<IEntity, FactionUpdateArgs> EntityFactionUpdateCompleteGlobal;
        event CustomEventHandler<IEntity, FactionUpdateArgs> EntityFactionUpdateStartGlobal;

        event CustomEventHandler<IFactionEntity, DeadEventArgs> FactionEntityDeadGlobal;
        event CustomEventHandler<IFactionEntity, HealthUpdateEventArgs> FactionEntityHealthUpdatedGlobal;

        event CustomEventHandler<IFactionSlot, ResourceUpdateEventArgs> FactionSlotResourceAmountUpdatedGlobal;

        event CustomEventHandler<IGameManager, EventArgs> GameStateUpdatedGlobal;

        event CustomEventHandler<object, EventArgs> HideTooltipGlobal;

        event CustomEventHandler<IMovementComponent, EventArgs> MovementStartGlobal;
        event CustomEventHandler<IMovementComponent, EventArgs> MovementStopGlobal;

        event CustomEventHandler<IPendingTaskEntityComponent, EventArgs> PendingTaskEntityComponentAdded;
        event CustomEventHandler<IPendingTaskEntityComponent, EventArgs> PendingTaskEntityComponentRemoved;
        event CustomEventHandler<IPendingTaskEntityComponent, EventArgs> PendingTaskEntityComponentUpdated;

        event CustomEventHandler<IResource, DeadEventArgs> ResourceDeadGlobal;
        event CustomEventHandler<IResource, HealthUpdateEventArgs> ResourceHealthUpdatedGlobal;
        event CustomEventHandler<IResource, EventArgs> ResourceInitiatedGlobal;

        event CustomEventHandler<object, MessageEventArgs> ShowPlayerMessageGlobal;

        event CustomEventHandler<object, MessageEventArgs> ShowTooltipGlobal;

        event CustomEventHandler<IUnit, DeadEventArgs> UnitDeadGlobal;
        event CustomEventHandler<IUnit, HealthUpdateEventArgs> UnitHealthUpdatedGlobal;
        event CustomEventHandler<IUnit, EventArgs> UnitInitiatedGlobal;
        event CustomEventHandler<IEntity, ResourceEventArgs> UnitResourceDropOffCompleteGlobal;
        event CustomEventHandler<IEntity, ResourceEventArgs> UnitResourceDropOffStartGlobal;
        event CustomEventHandler<IUnit, UpgradeEventArgs<IEntity>> UnitUpgradedGlobal;

        event CustomEventHandler<IEntity, EntitySelectionEventArgs> EntitySelectedGlobal;
        event CustomEventHandler<IEntity, EventArgs> EntityDeselectedGlobal;
        event CustomEventHandler<IEntity, EntitySelectionEventArgs> BuildingSelectedGlobal;
        event CustomEventHandler<IEntity, EntitySelectionEventArgs> ResourceSelectedGlobal;
        event CustomEventHandler<IEntity, EntitySelectionEventArgs> UnitSelectedGlobal;
        event CustomEventHandler<IEntity, EventArgs> UnitDeselectedGlobal;
        event CustomEventHandler<IEntity, EventArgs> BuildingDeselectedGlobal;
        event CustomEventHandler<IEntity, EventArgs> ResourceDeselectedGlobal;
        event CustomEventHandler<IFactionSlot, DefeatConditionEventArgs> FactionSlotDefeatConditionTriggeredGlobal;
        event CustomEventHandler<IEntity, VisibilityEventArgs> EntityVisibilityUpdateGlobal;
        event CustomEventHandler<IFactionSlot, DefeatConditionEventArgs> FactionSlotDefeatedGlobal;

        void RaiseAttackSwitchCompleteGlobal(IAttackComponent sender);
        void RaiseAttackSwitchStartGlobal(IAttackComponent sender);
        void RaiseBorderActivatedGlobal(IBorder sender);
        void RaiseBorderDisabledGlobal(IBorder sender);
        void RaiseBorderResourceAddedGlobal(IBorder sender, ResourceEventArgs e);
        void RaiseBorderResourceRemovedGlobal(IBorder sender, ResourceEventArgs e);
        void RaiseBuildingBuiltGlobal(IBuilding sender);
        void RaiseBuildingDeadGlobal(IBuilding sender, DeadEventArgs e);
        void RaiseBuildingHealthUpdatedGlobal(IBuilding sender, HealthUpdateEventArgs e);
        void RaiseBuildingPlacedGlobal(IBuilding sender);
        void RaiseBuildingPlacementStartGlobal(IBuilding sender);
        void RaiseBuildingPlacementStatusUpdatedGlobal(IBuilding sender);
        void RaiseBuildingPlacementStopGlobal(IBuilding sender);
        void RaiseBuildingUpgradedGlobal(IBuilding sender, UpgradeEventArgs<IEntity> e);
        void RaiseEffectObjectCreatedGlobal(IEffectObject source);
        void RaiseEffectObjectDestroyedGlobal(IEffectObject source);
        void RaiseEntityComponentPendingTaskUIReloadRequestGlobal(IEntity sender, TaskUIReloadEventArgs e = null);
        void RaiseEntityComponentTargetLockedGlobal(IEntityTargetComponent sender, TargetDataEventArgs e);
        void RaiseEntityComponentTargetStartGlobal(IEntityTargetComponent sender, TargetDataEventArgs e);
        void RaiseEntityComponentTargetStopGlobal(IEntityTargetComponent sender, TargetDataEventArgs e);
        void RaiseEntityComponentTaskUIReloadRequestGlobal(IEntityComponent sender, TaskUIReloadEventArgs e = null);
        void RaiseEntityComponentUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntityComponent> e);
        void RaiseEntityDeadGlobal(IEntity sender, DeadEventArgs e);
        void RaiseEntityDeselectedGlobal(IEntity sender);
        void RaiseEntityHealthUpdatedGlobal(IEntity sender, HealthUpdateEventArgs e);
        void RaiseEntityInitiatedGlobal(IEntity sender);
        void RaiseEntityInstanceUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntity> e);
        void RaiseEntityMouseEnterGlobal(IEntity sender);
        void RaiseEntityMouseExitGlobal(IEntity sender);
        void RaiseEntitySelectedGlobal(IEntity sender, EntitySelectionEventArgs args);
        void RaiseEntityUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntity> e);
        void RaiseFactionEntityDeadGlobal(IFactionEntity sender, DeadEventArgs e);
        void RaiseFactionEntityHealthUpdatedGlobal(IFactionEntity sender, HealthUpdateEventArgs e);
        void RaiseEntityFactionUpdateCompleteGlobal(IEntity sender, FactionUpdateArgs e);
        void RaiseEntityFactionUpdateStartGlobal(IEntity sender, FactionUpdateArgs e);
        void RaiseFactionSlotDefeatConditionTriggeredGlobal(IFactionSlot factionSlot, DefeatConditionEventArgs args);
        void RaiseFactionSlotResourceAmountUpdatedGlobal(IFactionSlot factionSlot, ResourceUpdateEventArgs e);
        void RaiseGameStateUpdatedGlobal();
        void RaiseHideTooltipGlobal(object sender);
        void RaiseMovementStartGlobal(IMovementComponent sender);
        void RaiseMovementStopGlobal(IMovementComponent sender);
        void RaisePendingTaskEntityComponentAdded(IPendingTaskEntityComponent source);
        void RaisePendingTaskEntityComponentRemoved(IPendingTaskEntityComponent source);
        void RaisePendingTaskEntityComponentUpdated(IPendingTaskEntityComponent source);
        void RaiseResourceDeadGlobal(IResource sender, DeadEventArgs e);
        void RaiseResourceHealthUpdatedGlobal(IResource sender, HealthUpdateEventArgs e);
        void RaiseResourceInitiatedGlobal(IResource sender);
        void RaiseShowPlayerMessageGlobal(object sender, MessageEventArgs e);
        void RaiseShowTooltipGlobal(object sender, MessageEventArgs e);
        void RaiseUnitDeadGlobal(IUnit sender, DeadEventArgs e);
        void RaiseUnitHealthUpdatedGlobal(IUnit sender, HealthUpdateEventArgs e);
        void RaiseUnitInitiatedGlobal(IUnit sender);
        void RaiseUnitResourceDropOffCompleteGlobal(IUnit sender, ResourceEventArgs e);
        void RaiseUnitResourceDropOffStartGlobal(IUnit sender, ResourceEventArgs e);
        void RaiseUnitUpgradedGlobal(IUnit sender, UpgradeEventArgs<IEntity> e);
        void RaiseEntityVisibilityUpdateGlobal(IEntity sender, VisibilityEventArgs args);
        void RaiseFactionSlotDefeatedGlobal(IFactionSlot factionSlot, DefeatConditionEventArgs args);
    }
}