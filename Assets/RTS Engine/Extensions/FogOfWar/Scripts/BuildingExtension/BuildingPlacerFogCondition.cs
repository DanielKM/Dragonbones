using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.FoW.BuildingExtension
{
    public class BuildingPlacerFogCondition : MonoBehaviour, IBuildingPlacerCondition, IEntityPreInitializable
    {
        private IEntity entity;

        [Range(0, 255), SerializeField, Tooltip("If the fog strength/intensity is smaller than this value in an area then the building can be placed in that area.")]
        private byte minFogStrength = 120;

        protected IFogOfWarRTSManager fowMgr { private set; get; } 

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.entity = entity;
            this.fowMgr = gameMgr.GetService<IFogOfWarRTSManager>();
        }

        public void Disable() { }

        public bool CanPlaceBuilding()
        {
            return !fowMgr.IsInFog(entity.transform.position, minFogStrength);
        }
    }
}
