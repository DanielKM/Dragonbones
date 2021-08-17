using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.Logging;
using RTSEngine.UnitExtension;
using System;
using RTSEngine.Selection;

namespace RTSEngine.Upgrades
{ 
    public class EntityUpgradeManager : MonoBehaviour, IEntityUpgradeManager
    {
        #region Attributes
        // Holds the elements that define the entity upgrades for each faction slot
        private List<UpgradeElement<IEntity>>[] elements;

        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IUnitManager unitMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; } 
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        protected ISelectionManager selectionMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>(); 

            gameMgr.GameBuilt += HandleGameBuilt;
        }

        private void OnDestroy()
        {
            gameMgr.GameBuilt -= HandleGameBuilt;
        }

        private void HandleGameBuilt(IGameManager sender, EventArgs args)
        {
            // When the game is built, the factions slots are ready.
            elements = gameMgr.FactionSlots
                .Select(factionSlot => new List<UpgradeElement<IEntity>>())
                .ToArray();

            gameMgr.GameBuilt -= HandleGameBuilt;
        }
        #endregion

        #region Fetching Entity Upgrade Data
        public bool TryGet (int factionID, out UpgradeElement<IEntity>[] upgradeElements)
        {
            upgradeElements = null;

            if (!logger.RequireTrue(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to get entity upgrade elements for faction ID: {factionID} is not allowed!",
                    source: this))
                return false;

            upgradeElements = elements[factionID].ToArray();
            return true;
        }

        public bool IsLaunched(EntityUpgrade upgrade, int factionID)
        {
            if (!logger.RequireValid(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to check entity upgrade for invalid faction ID: {factionID} is not allowed!"))
                return false;

            return elements[factionID]
                .Where(element => element.sourceCode == upgrade.SourceCode
                    && element.target == upgrade.UpgradeTarget)
                .Any();
        }
        #endregion

        #region Launching Entity Upgrades
        public ErrorMessage LaunchLocal(EntityUpgrade upgrade, int factionID)
        {
            if (!logger.RequireTrue(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name} - {upgrade}] Attempting to launch entity upgrade for invalid faction ID: {factionID} is not allowed!")
                || !logger.RequireValid(upgrade.UpgradeTarget,
                    $"[{GetType().Name} - {upgrade}] Attempting to launch upgrade for invalid target entity is not allowed!"))
                return ErrorMessage.invalid;

            else if (!logger.RequireTrue(!IsLaunched(upgrade, factionID),
                $"[{GetType().Name}] Attempting to launch entity upgrade for entity of code '{upgrade.SourceCode}' that has been already launched!"))
                return ErrorMessage.upgradeLaunched;

            else if(!logger.RequireTrue(!upgrade.SourceEntity.IsValid() || upgrade.SourceEntity.Type == upgrade.UpgradeTarget.Type,
                    $"[{GetType().Name}] Upgrade source entity ('{upgrade.SourceEntity?.Type}') and target entity ('{(upgrade.UpgradeTarget).Type}') have different types!"))
                return ErrorMessage.upgradeTypeMismatch;

            UpgradeElement<IEntity> newElement = new UpgradeElement<IEntity>
            {
                sourceCode = upgrade.SourceCode,
                target = upgrade.UpgradeTarget
            };

            if (upgrade.SourceInstanceOnly)
            {
                if(upgrade.SourceEntity.IsValid())
                    UpgradeInstance(upgrade.SourceEntity, newElement, factionID, upgrade.EntityComponentMatcher, upgrade.UpgradeEffect);
            }
            else
            {
                elements[factionID].Add(newElement);

                // Upgrade all spawned instances of the same type of the upgrade source?
                if (upgrade.UpdateSpawnedInstances && upgrade.SourceEntity.IsValid())
                    // Why use ToArray() here? because we do not want to update the FactionManager's FactionEntities collection directly since we will be destroying/creating new entities
                    foreach (IFactionEntity factionEntity in gameMgr.GetFactionSlot(factionID).FactionMgr.FactionEntities.ToArray())
                        if (factionEntity.Code == upgrade.SourceCode)
                            UpgradeInstance(factionEntity, newElement, factionID, upgrade.EntityComponentMatcher, upgrade.UpgradeEffect);

                switch (upgrade.UpgradeTarget.Type)
                {
                    case EntityType.building:

                        globalEvent.RaiseBuildingUpgradedGlobal(
                            upgrade.SourceEntity as IBuilding,
                            new UpgradeEventArgs<IEntity>(newElement, factionID, null));

                        break;

                    case EntityType.unit:

                        globalEvent.RaiseUnitUpgradedGlobal(
                            upgrade.SourceEntity as IUnit,
                            new UpgradeEventArgs<IEntity>(newElement, factionID, null));

                        break;

                    case EntityType.resource:
                        logger.LogError($"[{GetType().Name}] Upgrading 'resource' instances is currently not allowed!");
                        break;
                }

                globalEvent.RaiseEntityUpgradedGlobal(
                    upgrade.SourceEntity,
                    new UpgradeEventArgs<IEntity>(newElement, factionID, null));
            }

            //if there are upgrades that get triggerd from this one, launch them
            foreach (Upgrade triggerUpgrade in upgrade.TriggerUpgrades)
                triggerUpgrade.LaunchLocal(gameMgr, factionID);

            return ErrorMessage.none;
        }

        // Upgrades a faction entity instance locally
        private void UpgradeInstance(IFactionEntity sourceInstance, UpgradeElement<IEntity> upgradeElement, int factionID, IEnumerable<EntityUpgradeComponentMatcherElement> entityComponentMatcher, IEffectObject upgradeEffect)
        {
            // Upgraded instances get the same curr health to max health ratio when they are created as the instnaces that they were upgraded from
            float healthRatio = sourceInstance.Health.CurrHealth / (float)sourceInstance.Health.MaxHealth;

            // We want to re-select the upgraded instance after creating if this is the case
            bool wasSelected = sourceInstance.Selection.IsSelected;

            IEntity upgradedInstance = null;

            switch (sourceInstance.Type)
            {
                case EntityType.building:

                    IBuilding currBuilding = sourceInstance as IBuilding;

                    // Get the current builders of this building if there are any
                    // And make them stop building the instance of the building since it will be destroyed.
                    IEnumerable<IUnit> currBuilders = currBuilding.WorkerMgr.Workers.ToArray();
                    foreach (IUnit unit in currBuilders)
                        unit.BuilderComponent.Stop();

                    // Create upgraded instance of the building
                    upgradedInstance = buildingMgr.CreatePlacedBuildingLocal(
                        upgradeElement.target as IBuilding,
                        sourceInstance.transform.position,
                        sourceInstance.transform.rotation,
                        new InitBuildingParameters
                        {
                            free = sourceInstance.IsFree,
                            factionID = factionID,

                            setInitialHealth = true,
                            initialHealth = (int)(healthRatio * upgradeElement.target.gameObject.GetComponent<IEntityHealth>().MaxHealth),

                            buildingCenter = currBuilding.CurrentCenter,
                        });

                    foreach (IUnit unit in currBuilders)
                        unit.SetTargetFirst(RTSHelper.ToTargetData<IEntity>(upgradedInstance), playerCommand: false);

                    break;

                case EntityType.unit:

                    IUnit unitPrefab = upgradeElement.target as IUnit;

                    // Create upgraded instance of the unit
                    upgradedInstance = unitMgr.CreateUnitLocal(
                        unitPrefab,
                        sourceInstance.transform.position,
                        sourceInstance.transform.rotation,
                        new InitUnitParameters
                        {
                            free = sourceInstance.IsFree,
                            factionID = factionID,

                            setInitialHealth = true,
                            initialHealth = (int)(healthRatio * upgradeElement.target.gameObject.GetComponent<IEntityHealth>().MaxHealth),

                            rallypoint = unitPrefab.SpawnRallypoint,
                            gotoPosition = sourceInstance.transform.position,
                        });

                    break;

                case EntityType.resource:
                    logger.LogError($"[{GetType().Name}] Upgrading 'resource' instances is currently not allowed!");
                    return;
            }

            foreach (EntityUpgradeComponentMatcherElement matcherElement in entityComponentMatcher)
                if (sourceInstance.EntityComponents.TryGetValue(matcherElement.sourceComponentCode, out IEntityComponent sourceComponent)
                    && upgradedInstance.EntityComponents.TryGetValue(matcherElement.targetComponentCode, out IEntityComponent targetComponent))
                    targetComponent.HandleComponentUpgrade(sourceComponent);

            globalEvent.RaiseEntityInstanceUpgradedGlobal(
                sourceInstance,
                new UpgradeEventArgs<IEntity>(upgradeElement, factionID, upgradedInstance));

            // Show the upgrade effect for the player:
            effectObjPool.Spawn(upgradeEffect,
                sourceInstance.transform.position,
                upgradeEffect.IsValid() ? upgradeEffect.transform.rotation : default,
                parent: upgradedInstance.transform);

            // Destroy the upgraded instance
            sourceInstance.Health.DestroyLocal(true, null);

            if (wasSelected)
                selectionMgr.Add(upgradedInstance, SelectionType.multiple);
        }
        #endregion
    }
}
