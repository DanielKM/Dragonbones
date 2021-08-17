using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Terrain;
using RTSEngine.UnitExtension;
using System.Linq;
using UnityEngine;

namespace RTSEngine.ResourceExtension
{
    public class DropOffTarget : MonoBehaviour, IDropOffTarget, IEntityPostInitializable
    {
        #region Class Attributes
        public IEntity Entity { private set; get; }

        [SerializeField, Tooltip("Code to identify this component, unique within the entity")]
        private string code = "unique_code";
        public string Code => code;

        [SerializeField, Tooltip("Code to identify this component, unique within the entity")]
        private Transform dropOffPosition = null;
        [SerializeField, Tooltip("If populated then this defines the types of terrain areas that the drop off target must use when a resource collector attempts to drop off their resources.")]
        private TerrainAreaType[] forcedTerrainAreas = new TerrainAreaType[0];

        // Game services
        protected ITerrainManager terrainMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.Entity = entity;

            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            if (!logger.RequireValid(dropOffPosition,
              $"[{GetType().Name} - {Entity.Code}] The 'Drop Off Position' field must be assigned!")
                || !logger.RequireTrue(forcedTerrainAreas.Length == 0 || forcedTerrainAreas.All(terrainArea => terrainArea.IsValid()),
              $"[{GetType().Name} - {Entity.Code}] The 'Forced Terrain Areas' field must be either empty or populated with valid elements!"))
                return; 
        }

        public void Disable() { }
        #endregion

        #region IAddableUnit/Dropping Off Resources
        public Vector3 GetAddablePosition(IUnit unit)
        {
            if (forcedTerrainAreas.Length > 0
                && logger.RequireTrue(terrainMgr.GetTerrainAreaPosition(dropOffPosition.position, forcedTerrainAreas, out Vector3 addablePosition),
                    $"[{GetType().Name} - {Entity.Code}] Unable to find a suitable drop off position for the forced terrain areas of the drop off target"))
                return addablePosition;

            return dropOffPosition.position;
        }

        public ErrorMessage CanAdd(IUnit unit, AddableUnitData addableData = default) => ErrorMessage.undefined;

        public ErrorMessage Add(IUnit unit, AddableUnitData addableUnitData)
        {
            if (!unit.IsValid())
                return ErrorMessage.invalid;

            unit.DropOffSource.Unload();

            return ErrorMessage.none;
        }

        public ErrorMessage CanMove(IUnit unit, AddableUnitData addableData = default)
        {
            if (!unit.IsValid())
                return ErrorMessage.invalid;
            else if (!unit.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (unit.Health.IsDead)
                return ErrorMessage.dead;

            else if (!addableData.allowDifferentFaction && !RTSHelper.IsSameFaction(unit, Entity))
                return ErrorMessage.factionMismatch;

            return ErrorMessage.none;
        }

        public ErrorMessage Move(IUnit unit, AddableUnitData addableData)
        {
            Vector3 addablePosition = GetAddablePosition(unit);

            mvtMgr.SetPathDestinationLocal(
                unit,
                addablePosition,
                0.0f,
                null,
                new MovementSource
                {
                    playerCommand = addableData.playerCommand,
                    component = addableData.sourceComponent,
                    targetAddableUnit = this,
                    targetAddableUnitPosition = addablePosition
                });

            return ErrorMessage.none;
        }
        #endregion
    }
}
