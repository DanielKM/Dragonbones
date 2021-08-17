using System.Collections.Generic;

using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;

namespace RTSEngine.NPC.BuildingExtension
{
    public interface INPCBuildingPlacer : INPCComponent
    {
        bool OnBuildingPlacementRequest(BuildingCreationTask creationTask, IBuilding buildingCenter, IEnumerable<BuildingPlaceAroundData> placeAroundDataSet, bool canRotate);
    }
}