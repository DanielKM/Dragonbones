using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IUnitCreator : IPendingTaskEntityComponent
    {
        Vector3 SpawnPosition { get; }

        int FindTaskIndex (string unitCode);
    }
}
