using System.Collections.Generic;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Terrain;
using RTSEngine.Search;

namespace RTSEngine.Movement
{
    public class UnitTargetPositionMarker : IMovementTargetPositionMarker
    {
        #region Attributes
        public SearchCell CurrSearchCell { private set; get; } = null;

        /// <summary>
        /// Gets the current position reserved by the marker.
        /// </summary>
        public Vector3 Position { private set; get; }

        /// <summary>
        /// Gets the radius of the marker's reverse area.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// Gets whether the marker is enabled or not, a marker is only enabled when a unit uses it to reserve its target position.
        /// </summary>
        public bool Enabled { get; private set; } = false;

        /// <summary>
        /// Gets the layer ID of the marker.
        /// </summary>
        public IEnumerable<TerrainAreaType> TerrainAreaTypes { private set; get; }

        // Game services
        protected IGridSearchHandler gridSearchHandler { private set; get; }
        #endregion

        #region Initializing/Terminating
        public UnitTargetPositionMarker(IGameManager gameMgr, IMovementComponent source)
        {
            this.gridSearchHandler = gameMgr.GetService<IGridSearchHandler>();

            this.Radius = source.Controller.Radius;
            this.TerrainAreaTypes = source.TerrainAreas;

            Enabled = false; 
            CurrSearchCell = null;
        }
        #endregion

        #region Activating/Deactivating
        public void AttachToSource ()
        {

        }

        /// <summary>
        /// Enables or disables the marker.
        /// </summary>
        /// <param name="enable">True to enable and false to disable the marker.</param>
        /// <param name="position">New Vector3 position for the marker in case it is enabled.</param>
        public void Toggle(bool enable, Vector3 position = default)
        {
            Enabled = enable;

            if (enable) //in case the marker is to enabled
            {
                this.Position = position;

                if (gridSearchHandler.TryGetSearchCell(position, out SearchCell nextCell) == ErrorMessage.none
                    && CurrSearchCell != nextCell) //assign the new search cell that the marker now belongs to.
                {
                    CurrSearchCell?.Remove(this);

                    CurrSearchCell = nextCell;
                    CurrSearchCell.Add(this);
                }
            }
        }
        #endregion
    }
}
