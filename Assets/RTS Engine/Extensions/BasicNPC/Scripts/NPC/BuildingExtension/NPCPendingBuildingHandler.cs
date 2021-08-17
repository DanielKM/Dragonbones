using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Terrain;
using RTSEngine.Game;

namespace RTSEngine.NPC.BuildingExtension
{
    public class NPCPendingBuildingHandler
    {
        public BuildingCreationTask CreationTask { private set; get; }

        public IBuilding Instance { private set; get; }
        public IBuilding BuildingCenter { private set; get; }

        private Vector3 placeAroundPosition;
        public Vector3 PlaceAroundPosition => placeAroundPosition;

        private BuildingPlaceAroundHandler placeAroundHandler;
        public float MaxPlacementRange => placeAroundHandler.CurrData.range.max;
        public bool IsPlaceAroundValid => placeAroundHandler.IsPlaceAroundValid();
        public bool CanRotate { private set; get; }

        // Game services
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; } 

        public NPCPendingBuildingHandler(IGameManager gameMgr, BuildingCreationTask creationTask, IBuilding instance, IBuilding buildingCenter, IEnumerable<BuildingPlaceAroundData> allPlaceAroundData, bool canRotate)
        {
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>(); 

            this.CreationTask = creationTask;

            this.Instance = instance;
            this.BuildingCenter = buildingCenter;

            this.placeAroundHandler = new BuildingPlaceAroundHandler(gameMgr, instance, allPlaceAroundData);
            this.CanRotate = canRotate;
        }

        public bool TrySetNextPlaceAroundData ()
        {
            while(placeAroundHandler.TrySetNextData())
            {
                IEnumerable<IEntity> nextPlaceAroundEntities = BuildingCenter.BorderComponent.EntitiesInRange
                    .Where(entity => placeAroundHandler.CurrData.IsValidType(entity.ToTargetData(), playerCommand: false) == ErrorMessage.none);

                IEntity nextPlaceAroundEntity = nextPlaceAroundEntities
                    .ElementAtOrDefault(Random.Range(0, nextPlaceAroundEntities.Count()));

                if(nextPlaceAroundEntity.IsValid())
                {
                    // Get a suitable position for the new building on the build around position
                    IEnumerable<TerrainAreaType> placableTerrainAreas = CreationTask.Prefab.gameObject.GetComponentInChildren<IBuildingPlacer>().PlacableTerrainAreas;
                    terrainMgr.GetTerrainAreaPosition(nextPlaceAroundEntity.transform.position, placableTerrainAreas, out placeAroundPosition);
                    placeAroundPosition.y += placementMgr.BuildingPositionYOffset;

                    // Offset the building by the place around data minimum distance and the build around entity radius
                    Vector3 nextBuildingPosition = PlaceAroundPosition;
                    nextBuildingPosition.x += nextPlaceAroundEntity.Radius + placeAroundHandler.CurrData.range.min;

                    Instance.transform.position = nextBuildingPosition;

                    // Pick a random starting position for building by randomly rotating it around its build around positio
                    Instance.transform.RotateAround(placeAroundPosition, Vector3.up, Random.Range(0.0f, 360.0f));
                    // Keep initial rotation (because the RotateAround method will change the building's rotation as well which we do not want)
                    Instance.transform.rotation = CreationTask.Prefab.transform.rotation;

                    return true;
                }
            }

            return false;
        }
    }
}
