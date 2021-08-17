using System;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.UI;
using RTSEngine.Movement;
using RTSEngine.Terrain;

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(IFactionEntity))]
    public class Rallypoint : FactionEntityTargetComponent<IEntity>, IRallypoint
    {
        #region Attributes
        /*
         * Action types and their parameters:
         * send: Target.instance is the target unit to send to the rallypoint.
         * */
        public enum ActionType : byte { send }
        public override bool IsIdle => true;

        // If the entity allows to create unit, they will spawned in this position.
        [SerializeField, Tooltip("Where created units will spawn.")]
        private Transform spawnTransform = null; 
        public Vector3 GetSpawnPosition (LayerMask navMeshLayerMask)
            => new Vector3(spawnTransform.position.x, terrainMgr.SampleHeight(spawnTransform.position, forcedTerrainAreas), spawnTransform.position.z);

        [SerializeField, Tooltip("Initial rallypoint transform. Determines where created units will move to after they spawn.")] 
        private Transform gotoTransform = null;

        [SerializeField, Tooltip("If populated then this defines the types of terrain areas where the rallypoint can be placed at.")]
        private TerrainAreaType[] forcedTerrainAreas = new TerrainAreaType[0];

        [SerializeField, Tooltip("Enable to define constraints on the range of the rallypoint from the source faction entity.")]
        private bool maxDistanceEnabled = false;
        [SerializeField, Tooltip("The maximum allowed distance between the faction entity and the rallypoint."), Min(0.0f)]
        private float maxDistance = 50.0f;

        // Game services
        protected ITerrainManager terrainMgr { private set; get; }

        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();

            if (!logger.RequireValid(spawnTransform,
                  $"[{GetType().Name} - {Entity.Code}] The 'Spawn Transform' field must be assigned!")

                || !logger.RequireValid(gotoTransform,
                  $"[{GetType().Name} - {Entity.Code}] The 'Goto Transform' field must be assigned!")

                || !logger.RequireTrue(forcedTerrainAreas.Length == 0 || forcedTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Entity.Code}] The 'Forced Terrain Areas' field must be either empty or populated with valid elements!"))
                return;

            Vector3 nextGotoPosition = default;
            if (!logger.RequireTrue(terrainMgr.GetTerrainAreaPosition(gotoTransform.position, forcedTerrainAreas, out nextGotoPosition),
                      $"[{GetType().Name} - {Entity.Code}] Unable to update the goto transform position as it is initial position does not comply with the forced terrain areas!"))
                return;

            gotoTransform.position = nextGotoPosition;
            SetGotoTransformActive(false);

            // Set the initial goto position for buildings when they are completely built for the first time (else task will not go through due to building being unable to launch any task).
            if (factionEntity.Type == EntityType.building && !(factionEntity as IBuilding).IsBuilt)
                (factionEntity as IBuilding).BuildingBuilt += HandleBuildingBuilt;
            else
                SetTarget(gotoTransform.position, false);

            this.factionEntity.Selection.Selected += HandleFactionEntitySelectionUpdated;
            this.factionEntity.Selection.Deselected += HandleFactionEntitySelectionUpdated;
        }

        private void HandleBuildingBuilt(IBuilding building, EventArgs args)
        {
            SetTarget(gotoTransform.position, false);

            building.BuildingBuilt -= HandleBuildingBuilt;
        }

        protected override void OnDisabled()
        {
            if (factionEntity.IsValid())
            {
                factionEntity.Selection.Selected -= HandleFactionEntitySelectionUpdated;
                factionEntity.Selection.Deselected -= HandleFactionEntitySelectionUpdated;
            }
        }
        #endregion

        #region Handling Component Upgrade
        protected override void OnComponentUpgraded(FactionEntityTargetComponent<IEntity> sourceFactionEntityTargetComponent)
        {
            gotoTransform.position = sourceFactionEntityTargetComponent.Target.position;
        }
        #endregion

        #region Handling Events: Entity Selection
        private void HandleFactionEntitySelectionUpdated(IEntity entity, EventArgs args)
        {
            if (!entity.IsLocalPlayerFaction()
                || !entity.CanLaunchTask)
                return;

            SetGotoTransformActive(factionEntity.Selection.IsSelected);
        }
        #endregion

        #region Searching/Updating Target
        public override bool CanSearch => false;

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
            => !maxDistanceEnabled || Vector3.Distance(sourcePosition, target.position) <= maxDistance;

        public override ErrorMessage IsTargetValid(TargetData<IEntity> potentialTarget, bool playerCommand)
        {
            if (potentialTarget.instance.IsValid())
            {
                if (!potentialTarget.instance.IsInteractable)
                    return ErrorMessage.uninteractable;
                else if (potentialTarget.instance == Entity)
                    return ErrorMessage.invalid;
            }

            if (maxDistanceEnabled && !IsTargetInRange(factionEntity.transform.position, potentialTarget.position))
                return ErrorMessage.rallypointTargetNotInRange;
            else if (!terrainMgr.GetTerrainAreaPosition(potentialTarget.position, forcedTerrainAreas, out _))
                return ErrorMessage.rallypointTerrainAreaMismatch;

            return ErrorMessage.none;
        }

        protected override void OnTargetPostLocked(bool playerCommand, bool sameTarget)
        {
            // In the case where the rallypoint sends
            if (!Target.instance.IsValid())
                gotoTransform.position = Target.position;

            if (playerCommand && factionEntity.IsLocalPlayerFaction())
            {
                SetGotoTransformActive(!Target.instance.IsValid() && factionEntity.Selection.IsSelected);

                if(Target.instance.IsValid())
                    mouseSelector.FlashSelection(Target.instance, factionEntity.IsFriendlyFaction(Target.instance));
            }
        }

        public void SetGotoTransformActive(bool active)
        {
            gotoTransform.gameObject.SetActive(active);

            OnGotoTransformActiveUpdated(active);
        }

        protected virtual void OnGotoTransformActiveUpdated(bool active) { }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand)
            => RTSHelper.LaunchEntityComponentAction(this, actionID, target, playerCommand);

        public override ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            switch((ActionType)actionID)
            {
                case ActionType.send:
                    return SendActionLocal(target.instance as IUnit, playerCommand);

                default:
                    return ErrorMessage.undefined;
            }
        }
        #endregion

        #region Handling Rallypoint
        public ErrorMessage SendAction (IUnit unit, bool playerCommand)
        {
            return LaunchAction(
                (byte)ActionType.send,
                new TargetData<IEntity> { instance = unit, position = gotoTransform.position },
                playerCommand);
        }

        public ErrorMessage SendActionLocal (IUnit unit, bool playerCommand)
        {
            // If the rallypoint target is moving and it exists the range of this rallypoint then move to the spawn position
            // Until the rallypoint target gets back in range then move to the target instance
            if ((Target.instance.IsValid() && Vector3.Distance(Target.instance.transform.position, Entity.transform.position) <= maxDistance)
                || unit.SetTargetFirstLocal(Target, playerCommand: false) == ErrorMessage.none)
                return ErrorMessage.none;

            return mvtMgr.SetPathDestination(
                    unit,
                    gotoTransform.position,
                    0.0f,
                    null,
                    new MovementSource { playerCommand = false });
        }
        #endregion
    }
}
