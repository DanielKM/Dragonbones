using RTSEngine.Entities;
using System.Collections.Generic;

namespace RTSEngine.EntityComponent
{
    public interface IBuilder : IEntityTargetComponent
    {
        TargetData<IBuilding> Target { get; }

        bool InProgress { get; }

        IEnumerable<BuildingCreationTask> CreationTasks { get; }
        IEnumerable<BuildingCreationTask> UpgradeTargetCreationTasks { get; }
    }
}
