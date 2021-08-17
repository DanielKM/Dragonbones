using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Terrain;

namespace RTSEngine.Search
{
    public class SearchSphereObstacle : MonoBehaviour, ISearchObstacle, IEntityPostInitializable
    {
        #region Attributes
        [SerializeField, Tooltip("Size of the search sphere obstacle.")]
        private float size = 5.0f;
        public float Size => size;

        [SerializeField, Tooltip("Offsets the center of the sphere obstacle.")]
        private Vector3 offset = Vector3.zero;

        public Vector3 Center => transform.position + offset;

        [Space(), SerializeField, Tooltip("Enable to only consider this obstacle in the context of a player command (command initiated directly by the local player).")]
        private bool playerCommandOnly = true;

        [SerializeField, Tooltip("Define the terrain area types to be blocked by this search obstacle. Leave empty for all terrain types.")]
        private TerrainAreaType[] terrainAreas = new TerrainAreaType[0];

        protected IGridSearchHandler gridSearch { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<ISearchObstacle, EventArgs> ObstacleRemoved;

        private void RaiseObstacleRemoved ()
        {
            var handler = ObstacleRemoved;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();

            gridSearch.TryAddSearchObstacle(this);
        }

        public void Disable()
        {
            RaiseObstacleRemoved();
        }
        #endregion

        #region Obstacle Detection
        public bool IsReserved(Vector3 testPosition, IEnumerable<TerrainAreaType> testAreaTypes, bool playerCommand)
            => (!playerCommandOnly || playerCommand)
            && Vector3.Distance(testPosition, Center) <= size
            && terrainAreas.IsTerrainAreaOverlap(testAreaTypes);
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Center, size);
        }
#endif
    }
}
