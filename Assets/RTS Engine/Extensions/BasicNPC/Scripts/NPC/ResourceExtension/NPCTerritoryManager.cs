using System;

using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Terrain;
using RTSEngine.NPC.BuildingExtension;

namespace RTSEngine.NPC.ResourceExtension
{
    public class NPCTerritoryManager : NPCComponentBase, INPCTerritoryManager
    {
        #region Attributes 
        [SerializeField, EnforceType(typeof(IBuilding), prefabOnly: true), Tooltip("Potential list of building center prefabs that a NPC faction can use to expand its territory.")]
        private GameObject[] buildingCenters = new GameObject[0];
        private NPCActiveRegulatorMonitor centerMonitor;

        [SerializeField, Tooltip("Can other NPC components request to expand faction territory?")]
        private bool expandOnDemand = true;

        [SerializeField, Tooltip("Minimum and Maximum target ratio of the map's territory to control. This component will actively attempt to at least control the minimum ratio and will not exceed the maximum ratio.")]
        private FloatRange targetTerritoryRatio = new FloatRange(0.1f, 0.5f);
        private float currentTerritoryRatio = 0;
        private bool HasReachedMaxTerritory => currentTerritoryRatio >= targetTerritoryRatio.max;
        private bool HasReachedMinTerritory => currentTerritoryRatio >= targetTerritoryRatio.min;

        [SerializeField, Tooltip("Delay (in seconds) before the NPC faction considers expanding.")]
        private FloatRange expandDelayRange = new FloatRange(200.0f, 300.0f);

        [SerializeField, Tooltip("How often does the NPC faction check whether to expand or not?")]
        private FloatRange expandReloadRange = new FloatRange(2.0f, 7.0f);
        private TimeModifiedTimer expandTimer;

        // NPC Components
        protected INPCBuildingCreator npcBuildingCreator { private set; get; }

        // Game services
        protected ITerrainManager terrainMgr { private set; get; }
        #endregion

        #region Initializing/Terminating:
        protected override void OnPreInit()
        {
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();

            this.npcBuildingCreator = npcMgr.GetNPCComponent<INPCBuildingCreator>();
            this.npcBuildingCreator.FirstBuildingCenterInitialized += HandleFirstBuildingCenterInitialized;

            currentTerritoryRatio = 0.0f;
            expandTimer = new TimeModifiedTimer(expandDelayRange.RandomValue + expandReloadRange.RandomValue);
        }

        protected override void OnPostInit()
        {
            IsActive = true;

            // Go through the spawned building centers and add their territory size to the faction's territory count
            foreach (IBuilding buildingCenter in factionMgr.BuildingCenters)
                UpdateCurrentTerritory(buildingCenter.BorderComponent.Surface);

            globalEvent.BuildingPlacementStartGlobal += HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementStopGlobal += HandleBuildingPlacementStopGlobal;
            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;
        }

        protected override void OnDestroyed()
        {
            npcBuildingCreator.FirstBuildingCenterInitialized -= HandleFirstBuildingCenterInitialized;

            globalEvent.BuildingPlacementStartGlobal -= HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementStopGlobal -= HandleBuildingPlacementStopGlobal;
            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;

            centerMonitor.Disable();
        }

        private void HandleFirstBuildingCenterInitialized(INPCBuildingCreator npcBuildingCreator, EventArgs args)
        {
            centerMonitor = new NPCActiveRegulatorMonitor(gameMgr, factionMgr);

            foreach (GameObject building in buildingCenters)
            {
                IBorder border = building?.GetComponentInChildren<IBorder>();

                if (!logger.RequireValid(border,
                    $"[{GetType().Name} - {factionMgr.FactionID}] 'Building Centers' field has some unassigned/invalid elements that do not include the '{typeof(IBorder).Name}' component."))
                    return;

                NPCBuildingRegulator nextRegulator;
                if ((nextRegulator = npcBuildingCreator.ActivateBuildingRegulator(
                    building.GetComponent<IBuilding>(),
                    npcBuildingCreator.FirstBuildingCenter)).IsValid())
                    centerMonitor.AddCode(nextRegulator.Prefab.Code);
            }

            if (centerMonitor.Count <= 0)
            {
                logger.LogWarning($"[{GetType().Name} - Faction ID: {factionMgr.FactionID}] No building center regulators have been assigned!");
                IsActive = false;
            }
        }
        #endregion

        #region Handling Events: Border Activated/Disabled
        private void HandleBuildingPlacementStartGlobal(IBuilding building, EventArgs args)
        {
            if (factionMgr.IsSameFaction(building) && building.BorderComponent.IsValid())
                UpdateCurrentTerritory(building.BorderComponent.Surface);
        }

        private void HandleBuildingPlacementStopGlobal(IBuilding building, EventArgs args)
        {
            if (factionMgr.IsSameFaction(building) && building.BorderComponent.IsValid())
                UpdateCurrentTerritory(-building.BorderComponent.Surface);
        }

        private void HandleBorderDisabledGlobal(IBorder border, EventArgs args)
        {
            if (factionMgr.IsSameFaction(border.Building))
                UpdateCurrentTerritory(-border.Surface);
        }
        #endregion

        #region Handling Territory Ratio 
        private void UpdateCurrentTerritory(float value)
        {
            currentTerritoryRatio += (value / terrainMgr.MapSize);

            IsActive = !HasReachedMaxTerritory && !HasReachedMinTerritory;
        }
        #endregion

        #region Handling Expanding Territory
        protected override void OnActiveUpdate()
        {
            if (expandTimer.ModifiedDecrease())
            {
                expandTimer.Reload(expandReloadRange.RandomValue);

                OnExpandRequestInternal();
            }
        }

        public void OnExpandRequest()
        {
            if (!expandOnDemand)
                return;

            OnExpandRequestInternal();
        }

        private void OnExpandRequestInternal()
        {
            npcBuildingCreator.OnCreateBuildingRequest(
                buildingCode: centerMonitor.RandomCode,
                buildingCenter: npcBuildingCreator.FirstBuildingCenter);
        }
        #endregion
    }
}
