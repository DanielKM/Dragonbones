using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public class BuildingPlaceAroundCondition : MonoBehaviour, IEntityPreInitializable, IBuildingPlacerCondition
    {
        [SerializeField, Tooltip("Place the building only around specific entities?")]
        private BuildingPlaceAroundData data = new BuildingPlaceAroundData { entityType = new CodeCategoryField(), range = new FloatRange(0.0f, 4.0f) };

        private BuildingPlaceAroundHandler handler = null;

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            handler = new BuildingPlaceAroundHandler(gameMgr, entity as IBuilding, data);
        }

        public void Disable()
        {
        }

        public bool CanPlaceBuilding() => handler.IsPlaceAroundValid();
    }
}

