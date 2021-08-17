using System;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.NPC.BuildingExtension
{
    public interface INPCBuildingCreator : INPCComponent
    {
        bool IsFirstBuildingCenterInitialized { get; }
        IBuilding FirstBuildingCenter { get; }

        event CustomEventHandler<INPCBuildingCreator, EventArgs> FirstBuildingCenterInitialized;

        NPCBuildingRegulator ActivateBuildingRegulator(IBuilding buildingPrefab, IBuilding buildingCenter);

        NPCBuildingRegulator GetBuildingRegulator(string buildingCode, IBuilding buildingCenter);
        NPCBuildingRegulator GetCreatableBuildingRegulatorFirst(string buildingCode, bool creatableOnDemandOnly);

        bool OnCreateBuildingRequest(string buildingCode, IBuilding buildingCenter = null);
    }
}