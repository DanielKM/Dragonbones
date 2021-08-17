using RTSEngine.Entities;
using System.Collections.Generic;

namespace RTSEngine.NPC.BuildingExtension
{
    public struct NPCBuildingCenterRegulatorData
    {
        public IBuilding buildingCenter;

        // Key: key of the building where at least one instance is spawned as part of this building center
        // Value: the active regulator instance for that building that regulates the building within this building center
        public Dictionary<string, NPCActiveBuildingRegulatorData> activeBuildingRegulators; 
    }
}
