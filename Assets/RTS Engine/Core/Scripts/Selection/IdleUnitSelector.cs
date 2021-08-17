using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.BuildingExtension;

namespace RTSEngine.Selection
{
    public class IdleUnitSelector : MonoBehaviour, IIdleUnitSelector
    {
        #region Attributes
        [SerializeField, Tooltip("Key code used to select idle units.")]
        private KeyCode key = KeyCode.I;
        [SerializeField, Tooltip("When selecting idle units, only select workers (idle units with a Builder or ResourceCollector component)?")]
        private bool workersOnly = true;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
        }
        #endregion

        #region Handling Idle Unit Selection
        private void Update()
        {
            if (gameMgr.State != GameStateType.running || placementMgr.IsPlacingBuilding)
                return;

            if (Input.GetKeyDown(key))
                SelectIdleUnits();
        }

        public void SelectIdleUnits()
        {
            // Find all idle units
            IEnumerable<IUnit> idleUnits = gameMgr.LocalFactionSlot.FactionMgr.Units
                .Where(unit => unit.IsIdle && workersOnly == (unit.BuilderComponent.IsValid() || unit.CollectorComponent.IsValid()));

            if (idleUnits.Any())
                selectionMgr.Add(idleUnits);
        }
        #endregion
    }
}
