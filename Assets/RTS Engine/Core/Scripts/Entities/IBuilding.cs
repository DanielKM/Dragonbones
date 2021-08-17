using System;

using RTSEngine.BuildingExtension;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Health;

namespace RTSEngine.Entities
{
    public interface IBuilding : IFactionEntity
    {
        bool IsBuilt { get; }
        bool IsPlacementInstance { get; }

        IBorder CurrentCenter { get; }

        IBorder BorderComponent { get; }
        IBuildingPlacer PlacerComponent { get; }

        new IBuildingHealth Health { get; }
        new IBuildingWorkerManager WorkerMgr { get; }

        event CustomEventHandler<IBuilding, EventArgs> BuildingBuilt;

        void Init(IGameManager gameMgr, InitBuildingParameters initParams);
        void InitPlacementInstance(IGameManager gameMgr, InitBuildingParameters initParams);
    }
}
