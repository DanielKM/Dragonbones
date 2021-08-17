using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public interface IBorder : IMonoBehaviour
    {
        IBuilding Building { get; }
        bool IsActive { get; }

        int SortingOrder { get; }
        float Size { get; }
        float Surface { get; }

        IEnumerable<IBuilding> BuildingsInRange { get; }
        IEnumerable<IResource> ResourcesInRange { get; }
        IEnumerable<IEntity> EntitiesInRange { get; }

        void Init(IGameManager gameMgr, IBuilding building);
        void Disable();

        bool IsInBorder(Vector3 testPosition);

        bool AllowBuildingInBorder(IBuilding building);

        void RegisterBuilding(IBuilding newBuilding);
        void UnegisterBuilding(IBuilding oldBuilding);
    }
}
