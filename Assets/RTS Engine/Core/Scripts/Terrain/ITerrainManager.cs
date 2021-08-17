using System.Collections.Generic;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Terrain
{
    public interface ITerrainManager : IPreRunGameService
    {
        float MapSize { get; }
        IEnumerable<TerrainAreaType> Areas { get; }

        float SampleHeight(Vector3 position, IEnumerable<TerrainAreaType> areaTypes);
        float SampleHeight(Vector3 position, IMovementComponent refMvtComp);

        bool IsTerrainArea(GameObject obj);
        bool IsTerrainArea(GameObject obj, string areaKey);
        bool IsTerrainArea(GameObject obj, TerrainAreaType areaType);
        bool IsTerrainArea(GameObject obj, IEnumerable<TerrainAreaType> areaType);

        bool GetTerrainAreaPosition(Vector3 inPosition, string areaKey, out Vector3 outPosition);
        bool GetTerrainAreaPosition(Vector3 inPosition, TerrainAreaType areaType, out Vector3 outPosition);
        bool GetTerrainAreaPosition(Vector3 inPosition, IEnumerable<TerrainAreaType> possibleAreaTypes, out Vector3 outPosition);
        ErrorMessage TryGetCachedHeight(Vector3 position, IEnumerable<TerrainAreaType> areaTypes, out float height);
        bool ScreenPointToTerrainPoint(Vector3 screenPoint, IEnumerable<TerrainAreaType> areaTypes, out Vector3 terrainPoint);
    }
}