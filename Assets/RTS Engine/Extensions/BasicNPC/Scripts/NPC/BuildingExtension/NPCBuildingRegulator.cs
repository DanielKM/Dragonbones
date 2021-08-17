using System;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.NPC.BuildingExtension
{
    /// <summary>
    /// Regulates the creation of a building type for a NPC faction.
    /// </summary>
    public class NPCBuildingRegulator : NPCRegulator<IBuilding>
    {
        #region Attributes 
        public NPCBuildingRegulatorData Data { private set; get; } 

        // The building center in which the building prefab is regulated at.
        private IBuilding buildingCenter;
        #endregion

        #region Initializing/Terminating
        public NPCBuildingRegulator (NPCBuildingRegulatorData data,
                                     IBuilding prefab,
                                     IGameManager gameMgr,
                                     INPCManager npcMgr,
                                     IBuilding buildingCenter)
            : base(data, prefab, gameMgr, npcMgr)
        {
            this.Data = data;

            this.buildingCenter = buildingCenter;

            // Add the existing buildings that can be regulated by this component
            foreach (IBuilding nextBuilding in this.factionMgr.Buildings)
                AddExisting(nextBuilding);

            globalEvent.BuildingPlacementStartGlobal += HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementStopGlobal += HandleBuildingPlacementStopGlobal;

            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;
        }

        protected override void OnDisabled()
        {
            globalEvent.BuildingPlacementStartGlobal -= HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementStopGlobal -= HandleBuildingPlacementStopGlobal;

            globalEvent.BuildingPlacedGlobal -= HandleBuildingPlacedGlobal;
        }
        #endregion

        #region Handling Events: Building Placement Start, Stop & Complete
        private void HandleBuildingPlacementStartGlobal(IBuilding building, EventArgs e) => AddPending(building);
        private void HandleBuildingPlacementStopGlobal(IBuilding building, EventArgs e) =>RemovePending(building);

        private void HandleBuildingPlacedGlobal(IBuilding building, EventArgs e) => AddNewlyCreated(building);
        #endregion

        #region Building Regulation Helper Methods
        // For buildings, pending instances are placement instances which are instantiated as faction entities (faction IDs and building centers are set), therefore we do an additional check for the faction
        public override bool CanPendingInstanceBeRegulated(IBuilding building) => base.CanPendingInstanceBeRegulated(building) && factionMgr.IsSameFaction(building) && CanBuildingBeRegulated(building);
        public override bool CanInstanceBeRegulated(IBuilding building) => base.CanInstanceBeRegulated(building) && CanBuildingBeRegulated(building);

        private bool CanBuildingBeRegulated(IBuilding building)
        {
            return !Data.RegulatePerBuildingCenter
                    || (building.PlacerComponent.CanPlaceOutsideBorder && !building.CurrentCenter.IsValid())
                    || (building.CurrentCenter.IsValid() && buildingCenter == building.CurrentCenter.Building);
        }
        #endregion
    }
}
