using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Upgrades
{
    public class EntityComponentUpgradeManager : MonoBehaviour, IEntityComponentUpgradeManager
    {
        #region Attributes
        // Holds the elements that define the upgrade source and targets for IEntityComponent components.
        // Key: source entity code that defines the entity whose components will be upgraded
        // Value: list of IEntityComponent upgrades
        // Each faction slot gets its own element in the array
        private Dictionary<string, List<UpgradeElement<IEntityComponent>>>[] elements;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

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
                .Select(factionSlot => new Dictionary<string, List<UpgradeElement<IEntityComponent>>>())
                .ToArray();

            gameMgr.GameBuilt -= HandleGameBuilt;
        }
        #endregion

        #region Fetching Entity Component Upgrade Data
        public bool TryGet(IEntity entity, int factionID, out List<UpgradeElement<IEntityComponent>> componentUpgrades)
        {
            componentUpgrades = null;

            if (!logger.RequireTrue(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to get entity component upgrade elements for faction ID: {factionID} is not allowed!",
                    source: this))
                return false;

            return elements[factionID].TryGetValue(entity.Code, out componentUpgrades);
        }

        public bool IsLaunched(EntityComponentUpgrade upgrade, int factionID)
        {
            if (!logger.RequireValid(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to get entity component upgrade elements for faction ID: {factionID} is not allowed!"))
                return false;

            if (elements[factionID].TryGetValue(upgrade.SourceCode, out List<UpgradeElement<IEntityComponent>> componentUpgrades))
                return componentUpgrades
                    .Where(element => element.sourceCode == upgrade.SourceCode && element.target == upgrade.UpgradeTarget)
                    .Any();

            return false;
        }
        #endregion

        #region Launching Entity Component Upgrades
        public ErrorMessage LaunchLocal(EntityComponentUpgrade upgrade, int factionID)
        {
            if (!logger.RequireTrue(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to launch upgrade for invalid faction ID: {factionID} is not allowed!")
                || !logger.RequireValid(upgrade.SourceEntity,
                    $"[{GetType().Name}] Attempting to launch upgrade for invalid source entity is not allowed!")
                || !logger.RequireValid(upgrade.UpgradeTarget,
                    $"[{GetType().Name}] Attempting to launch upgrade for invalid target entity component is not allowed!"))
                return ErrorMessage.invalid;

            else if (!logger.RequireTrue(!IsLaunched(upgrade, factionID),
                $"[{GetType().Name}] Attempting to launch entity component upgrade for entity of code '{(upgrade.SourceEntity.IsValid() ? upgrade.SourceEntity.Code : "DESTROYED")}' that has been already launched!"))
                return ErrorMessage.upgradeLaunched;

            UpgradeElement<IEntityComponent> newElement = new UpgradeElement<IEntityComponent>
            {
                sourceCode = upgrade.SourceCode,
                target = upgrade.UpgradeTarget
            };

            if (upgrade.SourceInstanceOnly)
                upgrade.SourceEntity.UpgradeComponent(newElement);
            else
            {
                if (elements[factionID].TryGetValue(upgrade.SourceEntity.Code, out List<UpgradeElement<IEntityComponent>> componentUpgrades))
                    componentUpgrades.Add(newElement);
                else
                    elements[factionID].Add(upgrade.SourceEntity.Code, new List<UpgradeElement<IEntityComponent>> { newElement });

                // Upgrade all spawned elements of the same type of the upgrade source?
                if (upgrade.UpdateSpawnedInstances)
                    foreach (IFactionEntity factionEntity in gameMgr.GetFactionSlot(factionID).FactionMgr.FactionEntities)
                        if (factionEntity.Code == upgrade.SourceEntity.Code)
                            factionEntity.UpgradeComponent(newElement);

                globalEvent.RaiseEntityComponentUpgradedGlobal(
                    upgrade.SourceEntity,
                    new UpgradeEventArgs<IEntityComponent>(newElement, factionID, null));
            }

            // If there are upgrades that get triggerd from this one, launch them
            foreach (Upgrade triggerUpgrade in upgrade.TriggerUpgrades)
                triggerUpgrade.LaunchLocal(gameMgr, factionID);

            return ErrorMessage.none;
        }
        #endregion
    }
}
