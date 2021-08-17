using System.Collections.Generic;

using RTSEngine.Entities;

namespace RTSEngine.NPC.BuildingExtension
{
    public interface INPCBuildingConstructor : INPCComponent
    {
        IReadOnlyDictionary<string, string[]> BuilderPlacableBuildingMapper { get; }

        int GetTargetBuildersAmount(IBuilding building);

        bool IsBuildingUnderConstruction(IBuilding building);

        void OnBuildingConstructionRequest(IBuilding building, int targetBuildersAmount, out int assignedBuilders, bool forceSwitch = false);
    }
}