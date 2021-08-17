using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacement : IPreRunGameService
    {
        bool IsPlacingBuilding { get; }

        float BuildingPositionYOffset { get; }
        float TerrainMaxDistance { get; }

        bool StartPlacement(BuildingCreationTask creationTask, BuildingPlacementOptions options = default);
        bool Stop();
    }
}