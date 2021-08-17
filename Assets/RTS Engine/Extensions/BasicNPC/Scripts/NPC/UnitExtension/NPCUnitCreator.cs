using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.ResourceExtension;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Determinism;
using RTSEngine.NPC.Event;
using RTSEngine.Logging.NPC;

namespace RTSEngine.NPC.UnitExtension
{
    public partial class NPCUnitCreator : NPCComponentBase, INPCUnitCreator
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IUnit), prefabOnly: true), Tooltip("Unit prefabs that this component is able to create independently from other NPC components. Unit prefabs that are assigned to other NPC components (such as resource collectors or builders) are not required to be added to this list.")]
        private List<GameObject> independentUnits = new List<GameObject>();

        [SerializeField, Tooltip("Capacity resource type that is used as the population resource. If this is not assigned, the NPC faction will aim to create just the minimum amount of each unit type.")]
        private FactionTypeFilteredResourceType populationResource = new FactionTypeFilteredResourceType();
        public ResourceTypeInfo PopulationResource { private set; get; } = null;

        // Key: unit type/code
        // Value: ActiveUnitRegulator that manages the unit type.
        private Dictionary<string, NPCActiveUnitRegulatorData> activeUnitRegulators;
        #endregion

        #region Initiliazing/Terminating
        protected override void OnPreInit()
        {
            PopulationResource = populationResource.GetFiltered(gameMgr.GetFactionSlot(factionMgr.FactionID).Data.type);

            if(logger.RequireValid(PopulationResource, $"[{GetType().Name} - Faction ID: {factionMgr.FactionID}] No population resource has been set, only the minimum amount of units will be auto-created!", type: Logging.LoggingType.warning))
                logger.RequireTrue(PopulationResource.HasCapacity, $"[{GetType().Name} - Faction ID: {factionMgr.FactionID}] Resource '{PopulationResource.Key}' Must be a capacity resource to be used as the main population resource!");

            activeUnitRegulators = new Dictionary<string, NPCActiveUnitRegulatorData>();
        }

        protected override void OnPostInit()
        {
            // Activate the independent unit types
            foreach (IUnit unit in independentUnits.Select(obj => obj.IsValid() ? obj.GetComponent<IUnit>() : null))
                ActivateUnitRegulator(unit);

            globalEvent.UnitUpgradedGlobal += HandleUnitUpgradeGlobal;
        }

        protected override void OnDestroyed()
        {
            globalEvent.UnitUpgradedGlobal -= HandleUnitUpgradeGlobal;

            DestroyAllActiveRegulators();
        }
        #endregion

        #region Handling Events: Unit Upgrade
        private void HandleUnitUpgradeGlobal(IUnit unit, UpgradeEventArgs<IEntity> args)
        {
            if (!factionMgr.FactionID.IsSameFaction(args.FactionID))
                return;

            if(unit.IsValid())
                DestroyActiveRegulator(unit.Code);
            ActivateUnitRegulator(args.UpgradeElement.target as IUnit);
        }
        #endregion

        #region Handling Unit Regulators 
        public NPCUnitRegulator ActivateUnitRegulator(IUnit unitPrefab)
        {
            if (!logger.RequireValid(unitPrefab,
                $"[{GetType().Name} - Faction ID: {factionMgr.FactionID}] Can not activate a regulator for an invalid unit prefab, check the 'Independent Units' list for unassigned elements or any other unit input field in other NPC components."))
                return null;

            // If no valid regulator data for the unit is returned then do not continue
            NPCUnitRegulatorData regulatorData = unitPrefab.GetComponent<NPCUnitRegulatorDataInput>()?.GetFiltered(factionType: factionSlot.Data.type, npcType: npcMgr.Type);
            if (!regulatorData.IsValid())
                return null;

            // If there is an active regulator for the current unit type
            NPCUnitRegulator activeInstance = GetActiveUnitRegulator(unitPrefab.Code);
            if (activeInstance.IsValid())
                return activeInstance;

            // At this stage, there is valid data for the regulator and it has not been created yet
            NPCActiveUnitRegulatorData newUnitRegulator = new NPCActiveUnitRegulatorData()
            {
                instance = new NPCUnitRegulator(regulatorData, unitPrefab, gameMgr, npcMgr),

                // Initial spawning timer: regular spawn reload + start creating after delay
                spawnTimer = new TimeModifiedTimer(regulatorData.CreationDelayTime + regulatorData.SpawnReload)
            };

            newUnitRegulator.instance.AmountUpdated += HandleUnitRegulatorAmountUpdated;

            activeUnitRegulators.Add(unitPrefab.Code, newUnitRegulator);

            // Whenever a new regulator is added to the active regulators list, then move the unit creator into the active state
            IsActive = true;

            return newUnitRegulator.instance;
        }

        public NPCUnitRegulator GetActiveUnitRegulator(string unitCode)
        {
            if (activeUnitRegulators.TryGetValue(unitCode, out NPCActiveUnitRegulatorData nextUnitRegulator))
                return nextUnitRegulator.instance;

            return null;
        }

        private void DestroyAllActiveRegulators()
        {
            foreach (NPCActiveUnitRegulatorData nextUnitRegulator in activeUnitRegulators.Values)
            {
                nextUnitRegulator.instance.AmountUpdated -= HandleUnitRegulatorAmountUpdated;

                nextUnitRegulator.instance.Disable();
            }

            activeUnitRegulators.Clear();
        }

        private void DestroyActiveRegulator(string unitCode)
        {
            NPCUnitRegulator nextUnitRegulator = GetActiveUnitRegulator(unitCode);
            if (nextUnitRegulator.IsValid())
            {
                nextUnitRegulator.AmountUpdated += HandleUnitRegulatorAmountUpdated;

                nextUnitRegulator.Disable();
            }

            activeUnitRegulators.Remove(unitCode);
        }
        #endregion

        #region Handling Events: Unit Regulator
        private void HandleUnitRegulatorAmountUpdated(NPCRegulator<IUnit> unitRegulator, NPCRegulatorUpdateEventArgs args)
        {
            // In case this component is inactive and one of the existing (not pending) units is removed ...
            // Or when the target count of the unit type is higher than the current count
            if (!IsActive
                && !unitRegulator.HasTargetCount)
                IsActive = true;
        }
        #endregion

        #region Handling Unit Creation
        protected override void OnActiveUpdate()
        {
            // Assume that the unit creator has finished its job with the current active unit regulators.
            IsActive = false;

            foreach (NPCActiveUnitRegulatorData nextUnitRegulator in activeUnitRegulators.Values)
            {
                //if we can auto create this:
                if (nextUnitRegulator.instance.Data.CanAutoCreate
                    && !nextUnitRegulator.instance.HasTargetCount)
                {
                    // To keep this component monitoring the creation of the next units 
                    IsActive = true;

                    if (nextUnitRegulator.spawnTimer.ModifiedDecrease())
                    {
                        nextUnitRegulator.spawnTimer.Reload(nextUnitRegulator.instance.Data.SpawnReload);

                        OnCreateUnitRequestInternal(
                            nextUnitRegulator.instance,
                            nextUnitRegulator.instance.TargetCount - nextUnitRegulator.instance.Count,
                            out _);
                    }
                }
            }
        }

        public bool OnCreateUnitRequest(string unitCode, int requestedAmount, out int createdAmount)
        {
            createdAmount = 0;

            NPCUnitRegulator nextUnitRegulator = GetActiveUnitRegulator(unitCode);
            if (nextUnitRegulator?.Data.CanCreateOnDemand == false)
                return false;

            return OnCreateUnitRequestInternal(nextUnitRegulator, requestedAmount, out createdAmount);
        }

        private bool OnCreateUnitRequestInternal(NPCUnitRegulator instance, int requestedAmount, out int createdAmount)
        {
            createdAmount = 0;

            if (!instance.IsValid()
                || instance.HasReachedMaxAmount
                || requestedAmount <= 0)
                return false;

            // If there are no task launchers assigned to this unit regulator that can create instances of the units...
            if (instance.CreatorsCount == 0)
                // FUTURE FEATURE: Allow NPC faction to scan its available units/buildings to create one that can produce this unit type
                return false;

            createdAmount = requestedAmount;
            instance.Create(ref requestedAmount);

            createdAmount -= requestedAmount;

            return true;
        }
        #endregion

#if UNITY_EDITOR

        [Header("Logs")]
        [SerializeField, ReadOnly, Space()]
        private NPCActiveFactionEntityRegulatorLogData[] activeUnitRegulatorLogs = new NPCActiveFactionEntityRegulatorLogData[0];

        protected override void UpdateLogStats()
        {
            activeUnitRegulatorLogs = activeUnitRegulators.Values
                .Select(regulator => new NPCActiveFactionEntityRegulatorLogData(
                    regulator.instance,
                    spawnTimer: regulator.spawnTimer.CurrValue,
                    creators: regulator.instance.Creators.Select(creator => $"{creator.Entity.Key}: {creator.Entity.Code}").ToArray()
                    ))
                .ToArray();
        }
#endif
    }
}
