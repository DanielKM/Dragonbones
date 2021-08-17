using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Terrain;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacer : IMonoBehaviour
    {
        IBuilding Building { get; }

        IEnumerable<TerrainAreaType> PlacableTerrainAreas { get; }

        bool CanPlace { get; }
        bool CanPlaceOutsideBorder { get; }
        bool Placed { get; }

        IBorder PlacementCenter { get; }

        void OnPlacementStart();
        void OnPositionUpdate();

        bool IsBuildingInBorder();
    }
}
