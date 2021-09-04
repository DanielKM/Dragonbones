using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Utilities;

namespace RTSEngine.Search
{
    public class GridSearchHandler : MonoBehaviour, IGridSearchHandler
    {
        #region Attributes
        [SerializeField, Tooltip("Defines the lower-left corner of the search grid where search cells will be generated.")]
        private Int2D lowerLeftCorner = new Int2D { x = 0, y = 0 };
        [SerializeField, Tooltip("Defines the upper-right corner of the search grid where search cells will be generated.")]
        private Int2D upperRightCorner = new Int2D { x = 100, y = 100 };

        [SerializeField, Tooltip("The size of each individual cell."), Min(1)]
        private int cellSize = 10;
        /// <summary>
        /// Gets the fixed size of each cell in the grid.
        /// </summary>
        public int CellSize => cellSize;

        //holds all generated cells according to their positions.
        private Dictionary<Int2D, SearchCell> gridDict = new Dictionary<Int2D, SearchCell>();

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            //subscribe to following events:
            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;

            GenerateCells(); //generate the grid search cells
        }

        private void OnDestroy()
        {
            //unsubscribe from following events:
            globalEvent.EntityInitiatedGlobal -= HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal -= HandleEntityDeadGlobal;
        }
        #endregion

        #region Handling Events
        private void HandleEntityInitiatedGlobal(IEntity entity, EventArgs args)
        {
            if (TryGetSearchCell(entity.transform.position, out SearchCell cell) == ErrorMessage.none) 
                cell.Add(entity); 
        }

        private void HandleEntityDeadGlobal(IEntity entity, DeadEventArgs args)
        {
            if (TryGetSearchCell(entity.transform.position, out SearchCell cell) == ErrorMessage.none)
                cell.Remove(entity);
        }
        #endregion

        #region Generating/Finding Cells
        private void GenerateCells ()
        {
            if (!logger.RequireTrue(cellSize > 0,
              $"[{GetType().Name}] The search grid cell size must be >= 0."))
                return;

            gridDict = new Dictionary<Int2D, SearchCell>();

            // According to the start and end position coordinates, create the required search cells
            for(int x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    // Each search cell instance is added to the dictionary after it is created for easier direct access using coordinates in the future.
                    Int2D nextPosition = new Int2D
                    {
                        x = x,
                        y = y
                    };

                    gridDict.Add(nextPosition, new SearchCell());
                }

            // Go through all generated cells, init them and assign their neighbors
            foreach (Int2D position in gridDict.Keys) 
                gridDict[position].Init(gameMgr, position, FindNeighborCells(position));
        }

        public IEnumerable<SearchCell> FindNeighborCells (Int2D sourcePosition)
        {
            // To store the found neighbor cells
            List<SearchCell> neighbors = new List<SearchCell>(); 

            // Maximum amount of potential neighboring cells
            int maxNeighborAmount = 8;
            Int2D nextPosition = new Int2D();

            while(maxNeighborAmount > 0)
            {
                switch(maxNeighborAmount)
                {
                    case 1: //right
                        nextPosition = new Int2D { x = sourcePosition.x + cellSize, y = sourcePosition.y };
                        break;
                    case 2: //left
                        nextPosition = new Int2D { x = sourcePosition.x - cellSize, y = sourcePosition.y };
                        break;
                    case 3: //up
                        nextPosition = new Int2D { x = sourcePosition.x, y = sourcePosition.y + cellSize };
                        break;
                    case 4: //down
                        nextPosition = new Int2D { x = sourcePosition.x, y = sourcePosition.y - cellSize };
                        break;

                    case 5: //upper-right
                        nextPosition = new Int2D { x = sourcePosition.x + cellSize, y = sourcePosition.y + cellSize };
                        break;
                    case 6: //upper-left
                        nextPosition = new Int2D { x = sourcePosition.x - cellSize, y = sourcePosition.y + cellSize };
                        break;
                    case 7: //lower-right
                        nextPosition = new Int2D { x = sourcePosition.x + cellSize, y = sourcePosition.y - cellSize };
                        break;
                    case 8: //lower-left
                        nextPosition = new Int2D { x = sourcePosition.x - cellSize, y = sourcePosition.y - cellSize };
                        break;
                }

                if (gridDict.TryGetValue(nextPosition, out SearchCell neighborCell))
                    neighbors.Add(neighborCell);

                maxNeighborAmount--;
            }

            return neighbors;
        }

        public ErrorMessage TryGetSearchCell (Vector3 position, out SearchCell cell)
        {
            // Find the coordinates of the potential search cell where the input position is in
            Int2D nextPosition = new Int2D
            {
                x = ( ((int)position.x - lowerLeftCorner.x) / cellSize) * cellSize + lowerLeftCorner.x,
                y = ( ((int)position.z - lowerLeftCorner.y) / cellSize) * cellSize + lowerLeftCorner.y
            };

            if(gridDict.TryGetValue(nextPosition, out cell)) 
                return ErrorMessage.none;

            logger.Log(
                $"[{GetType().Name}] No search cell has been defined to contain position: {position}!",
                source: this,
                type: LoggingType.warning);
            return ErrorMessage.searchCellNotFound;
        }
        #endregion

        #region Handling Search
        public ErrorMessage SearchRect<T>(Rect rect, Func<T, bool> filter, out List<T> resultList) where T : IEntity
        {
            resultList = new List<T>();
            ErrorMessage errorMessage;

            for(float x = rect.x; x < rect.x + rect.width + cellSize; x += cellSize)
                for(float y = rect.y; y < rect.y + rect.height + cellSize; y += cellSize)
                {
                    if((errorMessage = TryGetSearchCell(new Vector3(x, 0, y), out SearchCell nextCell)) != ErrorMessage.none)
                        return errorMessage;

                    foreach (IEntity entity in nextCell.Entities)
                    {
                        if (!entity.IsValid() 
                            || !entity.IsSearchable
                            || !(entity is T))
                            continue;

                        if(rect.Contains(new Vector2(entity.transform.position.x, entity.transform.position.z))
                            && filter((T)entity))
                            resultList.Add((T)entity);
                    }
                }

            return ErrorMessage.none;
        }

        /// <summary>
        /// Searches for the closest potential target that extends type Entity and satisfies a set of conditions.
        /// </summary>
        /// <typeparam name="T">Type of the potential target that extends the Entity type.</typeparam>
        /// <param name="sourcePosition">Vector3 position that represents where the search will start from.</param>
        /// <param name="radius">The radius of the search.</param>
        /// <param name="IsTargetValid">Delegate that takes an instance of the searched type and returns a RTSEngine.ErrorMessage which allows to define the search conditions.</param>
        /// <param name="target">Potential search target instnce in case one is found, otherwise null.</param>
        /// <returns>ErrorMessage.none if the search was completed error-free, otherwise failure'S error code.</returns>
        public ErrorMessage Search<T>(Vector3 sourcePosition, FloatRange radius, RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out T potentialTarget, bool findClosest = true) where T : IEntity
        {
            ErrorMessage errorMessage = Find(
                sourcePosition,
                radius,
                1,
                IsTargetValid,
                findClosest,
                playerCommand,
                out IEnumerable<T> targets);

            potentialTarget = targets.FirstOrDefault();

            return errorMessage;
        }

        public ErrorMessage Search<T>(
            Vector3 sourcePosition, float radius, RTSHelper.IsTargetValidDelegate IsTargetValid,
            bool playerCommand, out T potentialTarget, bool findClosest = true) where T : IEntity
            => Search(sourcePosition,
                new FloatRange(0.0f, radius),
                IsTargetValid,
                playerCommand,
                out potentialTarget,
                findClosest);


        public ErrorMessage Search<T>(Vector3 sourcePosition, float radius, int amount,
            RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out IEnumerable<T> potentialTargets, bool findClosest = true) where T : IEntity
        {
            return Find(
                sourcePosition,
                new FloatRange(0.0f, radius),
                amount,
                IsTargetValid,
                findClosest,
                playerCommand,
                out potentialTargets);
        }

        // A negative integer in the "amount" parameter -> find all entities that satisfy the search conditions
        private ErrorMessage Find<T> (Vector3 sourcePosition, FloatRange radius, int amount,
            RTSHelper.IsTargetValidDelegate IsTargetValid, bool findClosest, bool playerCommand,
            out IEnumerable<T> targets) where T : IEntity
        {
            targets = Enumerable.Empty<T>();

            ErrorMessage errorMessage;
            // Only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(sourcePosition, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            // What cells are we searching next? the source cell and its direct neighbors.
            List<SearchCell> nextCells = new List<SearchCell>(sourceCell.Neighbors) { sourceCell };
            // What cells have been already searched or are marked to be searched.
            List<SearchCell> searchedCells = new List<SearchCell>(nextCells); 

            // The size of the covered surface in terms of cell size
            int coveredSurface = 0; 

            // Using a sorted list allows to sort potential targets depending on their distance from the search source position
            SortedList<float, List<T>> sortedTargets = new SortedList<float, List<T>>();

            // As long as there cells to search
            while(nextCells.Count > 0)
            {
                FloatRange radiusSqr = new FloatRange(radius.min * radius.min, radius.max * radius.max);

                float nextDistance = 0.0f;

                // Holds the neighbor cells of the current cells to search so they would be searched in the next round.
                List<SearchCell> neighborCells = new List<SearchCell>(); 

                // Go through all the cells that are next to search
                foreach(SearchCell cell in nextCells)
                {
                    foreach(IEntity entity in cell.Entities.ToArray())
                    {
                        if (!entity.IsValid() 
                            || !entity.IsSearchable
                            || !(entity is T))
                            continue;

                        nextDistance = (entity.transform.position - sourcePosition).sqrMagnitude;

                        if((nextDistance >= radiusSqr.min && nextDistance <= radiusSqr.max)
                            && IsTargetValid(RTSHelper.ToTargetData(entity), playerCommand) == ErrorMessage.none) 
                        {
                            if (sortedTargets.ContainsKey(nextDistance))
                                sortedTargets[nextDistance].Add((T)entity);
                            else
                                sortedTargets.Add(nextDistance, new List<T> { (T)entity });
                            amount--;

                            if (!findClosest && amount <= 0)
                                return ErrorMessage.none;
                        }
                    }

                    // Go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    foreach (SearchCell neighborCell in cell.Neighbors)
                        if (!searchedCells.Contains(neighborCell))
                        {
                            neighborCells.Add(neighborCell);
                            searchedCells.Add(neighborCell);
                        }
                }

                targets = sortedTargets.Values.SelectMany(nextTargets => nextTargets);

                // After going through all the current cells to search
                // See if we have a potential target or if we have already found all of our required targets
                if (amount <= 0) 
                    return ErrorMessage.none; 
                else 
                {
                    // No potential target found? Increase the search surface
                    coveredSurface += cellSize; 

                    // As long as the covered search surface has not got beyond the allowed search radius
                    if (coveredSurface < radius.max)
                        // Every search round, we go one cell size (or search cell) further.
                        // The next cells to search are now the yet-unsearched neighbor cells
                        nextCells = neighborCells;
                    else //we have already gone through the allowed search radius
                        break;
                }
            }

            return ErrorMessage.searchTargetNotFound;
        }

        public ErrorMessage TryAddSearchObstacle (ISearchObstacle newObstacle)
        {
            ErrorMessage errorMessage;
            // Only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(newObstacle.Center, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            Vector2 circleCenter = new Vector2(newObstacle.Center.x, newObstacle.Center.z);

            // What cells are we searching next? the source cell and its direct neighbors.
            List<SearchCell> nextCells = new List<SearchCell>(sourceCell.Neighbors) { sourceCell };
            // What cells have been already searched or are marked to be searched.
            List<SearchCell> searchedCells = new List<SearchCell>(nextCells); 

            // The size of the covered surface in terms of cell size
            int coveredSurface = 0; 

            // As long as there cells to search
            while(nextCells.Count > 0)
            {
                float radiusSqr = newObstacle.Size * newObstacle.Size;

                // Holds the neighbor cells of the current cells to search so they would be searched in the next round.
                List<SearchCell> neighborCells = new List<SearchCell>();

                // Go through all the cells that are next to search
                foreach (SearchCell cell in nextCells)
                {
                    // Temporary variables to set edges for testing
                    float testX = cell.Position.x + (circleCenter.x <= cell.Position.x ? 0.0f : CellSize);
                    float testY = cell.Position.y + (circleCenter.y <= cell.Position.y ? 0.0f : CellSize);

                    // Get distance from closest edges
                    float distX = circleCenter.x - testX;
                    float distY = circleCenter.y - testY;
                    float nextDistance = (distX * distX) + (distY * distY);

                    // If the distance is less than the radius, the obstacle would be present in the current search cell.
                    if (nextDistance <= radiusSqr)
                        cell.AddObstacle(newObstacle);

                    // Go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    foreach (SearchCell neighborCell in cell.Neighbors)
                        if (!searchedCells.Contains(neighborCell))
                        {
                            neighborCells.Add(neighborCell);
                            searchedCells.Add(neighborCell);
                        }
                }

                // No potential target found? Increase the search surface
                coveredSurface += cellSize; 

                // As long as the covered search surface has not got beyond the allowed search radius
                if (coveredSurface < newObstacle.Size)
                    // Every search round, we go one cell size (or search cell) further.
                    // The next cells to search are now the yet-unsearched neighbor cells
                    nextCells = neighborCells;
                else //we have already gone through the allowed search radius
                    break;
            }

            return ErrorMessage.none;
        }

        //TODO: Requires refactoring so that the Search and this method share internal code.

        /// <summary>
        /// Checks whether a given position is reserved by a unit's target position marker or not.
        /// </summary>
        /// <param name="position">Position to test.</param>
        /// <param name="radius">The free radius required around the position in order to claim the position as not reserved.</param>
        /// <param name="layer">Currently 1 for air units and 0 for ground units. To be changed.</param>
        /// <returns>ErrorMessage.none if the position is not reserved by a target position marker, otherwise either failure's error code in case of failure to check or ErrorMessage.positionReserved in case the position is reserved.</returns>
        public ErrorMessage IsPositionReserved (Vector3 testPosition, float radius, IEnumerable<TerrainAreaType> terrainAreas, bool playerCommand)
        {
            ErrorMessage errorMessage;

            if ((errorMessage = TryGetSearchCell(testPosition, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            // What cells are we searching next? the source cell and its direct neighbors.
            List<SearchCell> nextCells = new List<SearchCell>(sourceCell.Neighbors) { sourceCell };
            // What cells have been already searched or are marked to be searched.
            List<SearchCell> searchedCells = new List<SearchCell>(nextCells); 

            // The size of the covered surface in terms of cell size
            int coveredSurface = 0;

            // Since we're comparing squarred distances we need the squarred value of the radius
            float sqrRadius = radius * radius;

            // As long as there cells to search
            while(nextCells.Count > 0)
            {
                // Holds te neighbor cells of the current cells to search so they would be searched in the next round.
                List<SearchCell> neighborCells = new List<SearchCell>();

                foreach(SearchCell cell in nextCells)
                {
                    if (cell.UnitTargetPositionMarkers
                        .Any(marker => marker.Enabled
                            && RTSHelper.IsTerrainAreaOverlap(marker.TerrainAreaTypes, terrainAreas)
                            && (marker.Position - testPosition).sqrMagnitude <= sqrRadius))
                        return ErrorMessage.mvtPositionMarkerReserved;

                    else if (cell.Obstacles
                        .Any(obstacle => obstacle.IsReserved(testPosition, terrainAreas, playerCommand)))
                        return ErrorMessage.mvtPositionObstacleReserved;

                    // Go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    foreach (SearchCell neighborCell in cell.Neighbors)
                        if (!searchedCells.Contains(neighborCell))
                        {
                            neighborCells.Add(neighborCell);
                            searchedCells.Add(neighborCell);
                        }
                }

                // After going through all the current cells to search
                // Increase the search surface
                coveredSurface += cellSize; 

                // As long as the covered search surface has not got beyond the allowed search radius
                // Every search round, we go one cell size (or search cell) further.
                if (coveredSurface < radius)
                    // The next cells to search are now the yet-unsearched neighbor cells
                    nextCells = neighborCells;
                else
                    // We have already gone through the allowed search radius
                    break;
            }

            // No target position marker is present in the searched range then the position is not reserved.
            return ErrorMessage.none; 
        }
        #endregion

        #region Displaying Cells
#if UNITY_EDITOR
        [Header("Gizmos")]
        public Color gizmoColor = Color.yellow;
        [Min(1.0f)]
        public float gizmoHeight = 1.0f;

        private void OnDrawGizmosSelected()
        {
            if (cellSize <= 0)
                return;

            Gizmos.color = gizmoColor;
            Vector3 size = new Vector3(cellSize, gizmoHeight, cellSize);

            for(int x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    Gizmos.DrawWireCube(new Vector3(x + cellSize/2.0f, 0.0f, y + cellSize/2.0f), size);
                }
        }
#endif
        #endregion
    }
}
