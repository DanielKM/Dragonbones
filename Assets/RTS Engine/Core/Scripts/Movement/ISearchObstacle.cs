using RTSEngine.Event;
using RTSEngine.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Search
{
    public interface ISearchObstacle
    {
        Vector3 Center { get; }
        float Size { get; }

        event CustomEventHandler<ISearchObstacle, EventArgs> ObstacleRemoved;

        bool IsReserved(Vector3 testPosition, IEnumerable<TerrainAreaType> testAreaTypes, bool playerCommand);
    }
}