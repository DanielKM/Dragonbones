using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using RTSEngine.Game;
using RTSEngine.Logging;
using System.Linq;
using RTSEngine.Utilities;

namespace RTSEngine.Search
{
    public class SearchCell
    {
        #region Attributes
        /// <summary>
        /// Gets the lower-left corner position of the search cell.
        /// </summary>
        public Int2D Position { private set; get; }

        /// <summary>
        /// Gets the set of neighboring cells.
        /// </summary>
        public IEnumerable<SearchCell> Neighbors { get; private set; } = null;

        private List<ISearchObstacle> obstacles = null; 
        public IEnumerable<ISearchObstacle> Obstacles => obstacles.ToList();

        private List<IEntity> entities = null; 
        /// <summary>
        /// Gets the entities that are positioned within the search cell.
        /// </summary>
        public IEnumerable<IEntity> Entities => entities.ToList();

        // Holds the coroutine that periodically checks whether moving entities inside the search cell are still in the cell or have left it
        private IEnumerator entityPositionCheckCoroutine;
        // List of entities in the cell that are actively moving
        private List<IEntity> movingEntities = null;

        //holds all the unit target positions inside the bounds of the search cell.
        private List<IMovementTargetPositionMarker> unitTargetPositionMarkers = null; 
        /// <summary>
        /// Gets the tracked UnitTargetPositionMarker instances inside the search cell.
        /// </summary>
        public IEnumerable<IMovementTargetPositionMarker> UnitTargetPositionMarkers => unitTargetPositionMarkers.ToList();

        // Game services
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, Int2D position, IEnumerable<SearchCell> neighbors)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            this.Position = position;
            this.Neighbors = neighbors;

            entities = new List<IEntity>();
            movingEntities = new List<IEntity>();

            unitTargetPositionMarkers = new List<IMovementTargetPositionMarker>();

            obstacles = new List<ISearchObstacle>();
        }
        #endregion

        #region Handling Events: IMovementComponent
        private void HandleMovementStart (IMovementComponent movementComponent, EventArgs e)
        {
            IEntity entity = movementComponent.Entity;

            if (!movingEntities.Contains(entity)) 
                movingEntities.Add(entity);

            if(entityPositionCheckCoroutine == null) 
            {
                entityPositionCheckCoroutine = EntityPositionCheck(0.1f);
                gridSearch.StartCoroutine(entityPositionCheckCoroutine);
            }
        }

        private void HandleMovementStop (IMovementComponent movementComponent, EventArgs e)
        {
            movingEntities.Remove(movementComponent.Entity);
        }
        #endregion

        #region Adding/Removing Entities
        public void Add(IEntity newEntity)
        {
            entities.Add(newEntity);

            if (newEntity.MovementComponent != null)
            {
                newEntity.MovementComponent.MovementStart += HandleMovementStart;
                newEntity.MovementComponent.MovementStop += HandleMovementStop;

                if (newEntity.MovementComponent.HasTarget) 
                    HandleMovementStart(newEntity.MovementComponent, EventArgs.Empty);
            }
        }

        public void Remove(IEntity entity)
        {
            entities.Remove(entity);

            if (entity.MovementComponent != null)
            {
                movingEntities.Remove(entity);

                entity.MovementComponent.MovementStart -= HandleMovementStart;
                entity.MovementComponent.MovementStop -= HandleMovementStop;

                if (entityPositionCheckCoroutine != null && movingEntities.Count == 0)
                {
                    //stop coroutine as there are no more entities moving inside this cell.
                    gridSearch.StopCoroutine(entityPositionCheckCoroutine);
                    entityPositionCheckCoroutine = null;
                }
            }
        }
        #endregion

        #region Tracking Moving Entities
        /// <summary>
        /// Checks whether moving entities that belong to the search cell have left the cell or not.
        /// </summary>
        /// <param name="waitTime">How often to test whether moving entities are the in cell or not?</param>
        private IEnumerator EntityPositionCheck(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);

                int i = 0;
                while (i < movingEntities.Count)
                {
                    if (!movingEntities[i].IsValid())
                    {
                        movingEntities.RemoveAt(i);
                        continue;
                    }

                    if (!IsIn(movingEntities[i].transform.position))
                    {
                        IEntity nextEntity = movingEntities[i];

                        // Find a new cell for the unit
                        if (!logger.RequireTrue(gridSearch.TryGetSearchCell(nextEntity.transform.position, out SearchCell newCell) == ErrorMessage.none,
                            $"[{GetType().Name}] Unable to find a new search cell for unit of code {nextEntity.Code} at position {nextEntity.transform.position}!"))
                            continue;

                        newCell.Add(nextEntity);

                        Remove(nextEntity); 

                        continue;
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// Check if a Vector3 position is inside the search cell's boundaries.
        /// </summary>
        /// <param name="testPosition">Vector3 position to test.</param>
        /// <returns>True if the input position is inside the search cell's boundaries, otherwise false.</returns>
        public bool IsIn (Vector3 testPosition)
        {
            return testPosition.x >= Position.x && testPosition.x < Position.x + gridSearch.CellSize
                && testPosition.z >= Position.y && testPosition.z < Position.y + gridSearch.CellSize;
        }
        #endregion

        #region Adding/Removing UnitTargetPositionMarker instances
        /// <summary>
        /// Adds a new UnitTargetPositionMarker instance to the tracked lists of unit target position markers inside this search cell.
        /// </summary>
        /// <param name="newMarker">The new UnitTargetPositionMarker instance to add.</param>
        public void Add(IMovementTargetPositionMarker newMarker)
        {
            if (!unitTargetPositionMarkers.Contains(newMarker)) //as long as the new marker hasn't been already added
                unitTargetPositionMarkers.Add(newMarker);
        }

        /// <summary>
        /// Removes a UnitTargetPositionMarker instance from the tracked list of markers inside this search cell.
        /// </summary>
        /// <param name="marker">The UnitTargetPositionMarker instance to remove.</param>
        public void Remove(IMovementTargetPositionMarker marker)
        {
            unitTargetPositionMarkers.Remove(marker);
        }
        #endregion

        #region Adding/Removing Search Obstacles
        public void AddObstacle(ISearchObstacle newObstacle)
        {
            if (obstacles.Contains(newObstacle))
                return;

            obstacles.Add(newObstacle);
            newObstacle.ObstacleRemoved += HandleObstacleRemoved;
        }

        private void HandleObstacleRemoved(ISearchObstacle obstacle, EventArgs args)
        {
            RemoveObstacle(obstacle);
        }

        public bool RemoveObstacle(ISearchObstacle obstacle)
        {
            obstacle.ObstacleRemoved -= HandleObstacleRemoved;

            return obstacles.Remove(obstacle);
        }
        #endregion
    }
}
