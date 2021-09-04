using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using System;

namespace RTSEngine.Search
{
    public interface IGridSearchHandler : IPreRunGameService, IMonoBehaviour
    {
        int CellSize { get; }

        IEnumerable<SearchCell> FindNeighborCells(Int2D sourcePosition);
        ErrorMessage TryGetSearchCell(Vector3 position, out SearchCell cell);

        ErrorMessage Search<T>(Vector3 sourcePosition, FloatRange radius, RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out T potentialTarget, bool findClosest = true) where T : IEntity;
        ErrorMessage Search<T>(Vector3 sourcePosition, float radius, RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out T potentialTarget, bool findClosest = true) where T : IEntity;
        ErrorMessage Search<T>(Vector3 sourcePosition, float radius, int amount, RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out IEnumerable<T> potentialTargets, bool findClosest = true) where T : IEntity;

        ErrorMessage TryAddSearchObstacle(ISearchObstacle newObstacle);

        ErrorMessage IsPositionReserved(Vector3 testPosition, float radius, IEnumerable<TerrainAreaType> terrainAreas, bool playerCommand);

        ErrorMessage SearchRect<T>(Rect rect, Func<T, bool> filter, out List<T> resultList) where T : IEntity;
    }
}