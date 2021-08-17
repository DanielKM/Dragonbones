using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Movement;
using RTSEngine.Attack;
using RTSEngine.Determinism;
using RTSEngine.EntityComponent;
using RTSEngine.Search;

namespace RTSEngine.NPC.Attack
{
    /// <summary>
    /// Responsible for defending a NPC faction's territory.
    /// </summary>
    public class NPCDefenseManager : NPCComponentBase, INPCDefenseManager
    {
        #region Attributes 
        [Header("Territory Defense")]
        [SerializeField, Tooltip("Enable to allow the NPC faction to defend its territory when a unit or a building that it owns is under attack inside its territory. The defenese is simply having the NPC faction's attack units aware of enemy entities inside its territory with the goal of eliminating them.")]
        private bool canDefendTerritory = true;

        [SerializeField, Tooltip("Cancel an active attack if the NPC faction's territory is under attack?")]
        private bool cancelAttackOnTerritoryDefense = true;

        [SerializeField, Tooltip("How often does the NPC faction decide whether it has to keep defending its territory or stop?")]
        private FloatRange cancelTerritoryDefenseReloadRange = new FloatRange(3.0f, 7.0f);
        private TimeModifiedTimer cancelTerritoryDefenseTimer;

        /// <summary>
        /// Is the NPC faction currently defending the territory of a building center?
        /// </summary>
        public bool IsDefending { private set; get; }

        /// <summary>
        /// The last building center (one with a Border component) whose territory is being defened
        /// </summary>
        public IBuilding LastDefenseCenter { private set; get; }

        [Header("Unit Support")]
        [SerializeField, Tooltip("Enable to allow a NPC unit to ask for support from units in its range when it is attacked.")]
        private bool unitSupportEnabled = true;
        [SerializeField, Tooltip("If unit support (above field) is enabled, then this is the range in which units can be called for support.")]
        private FloatRange unitSupportRange = new FloatRange(5, 10);

        // NPC Components
        private INPCAttackManager npcAttackMgr;

        // Game services
        protected IAttackManager attackMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();

            this.npcAttackMgr = npcMgr.GetNPCComponent<INPCAttackManager>();

            // Initial state
            cancelTerritoryDefenseTimer = new TimeModifiedTimer();
            IsDefending = false;
            LastDefenseCenter = null;
        }

        protected override void OnPostInit()
        {
            IsActive = canDefendTerritory;

            globalEvent.FactionEntityHealthUpdatedGlobal += HandleFactionEntityHealthUpdated;
        }

        protected override void OnDestroyed()
        {
            globalEvent.FactionEntityHealthUpdatedGlobal -= HandleFactionEntityHealthUpdated;
        }
        #endregion

        #region Event Callbacks: Faction Entity Health Updated
        private void HandleFactionEntityHealthUpdated(IFactionEntity factionEntity, HealthUpdateEventArgs args)
        {
            // Only consider faction entities owned by the NPC faction who has been damaged by an enemy faction entity
            if (!factionMgr.IsSameFaction(factionEntity)
                || args.Value >= 0.0f
                || !args.Source.IsValid()
                || args.Source.IsFriendlyFaction(factionSlot))
                return;

            OnUnitSupportRequest(factionEntity.transform.position, args.Source as IFactionEntity);

            foreach (IBuilding nextBuildingCenter in factionMgr.BuildingCenters)
                if (nextBuildingCenter.BorderComponent.IsInBorder(factionEntity.transform.position))
                {
                    LaunchDefense(nextBuildingCenter, forceUpdateDefenseCenter: false);
                    break;
                }
        }
        #endregion

        #region Handling Territory Defense 
        protected override void OnActiveUpdate()
        {
            if (!IsDefending)
                return;

            if (cancelTerritoryDefenseTimer.ModifiedDecrease())
                CancelDefense();
        }

        public void LaunchDefense(Vector3 defensePosition, bool forceUpdateDefenseCenter)
            => LaunchDefense(RTSHelper.GetClosestEntity(defensePosition, factionMgr.BuildingCenters), forceUpdateDefenseCenter);

        // "forceUpdateDefenseCenter", when false, only units who do not have an active attack target will have their defense center forced
        public void LaunchDefense(IBuilding nextDefenseCenter, bool forceUpdateDefenseCenter)
        {
            if (!canDefendTerritory
                || !nextDefenseCenter.IsValid()
                || !nextDefenseCenter.BorderComponent.IsValid())
                return;

            IsDefending = true;

            // Keep reloading the cancel defense timer until no calls to launch a defense happen
            cancelTerritoryDefenseTimer.Reload(cancelTerritoryDefenseReloadRange);

            if (cancelAttackOnTerritoryDefense && npcAttackMgr.IsAttacking)
                npcAttackMgr.CancelAttack();
        }

        public void CancelDefense()
        {
            IsDefending = false;

            LastDefenseCenter = null;
        }
        #endregion

        #region Handling Unit Support 
        public bool OnUnitSupportRequest(Vector3 supportPosition, IFactionEntity target)
        {
            if (!unitSupportEnabled
                || !target.IsValid()
                || target.Health.IsDead)
                return false;

            gridSearch.Search(
                supportPosition,
                unitSupportRange.RandomValue,
                amount: -1, // negative value gets all potential units
                IsValidUnitSupport,
                playerCommand: false,
                out IEnumerable<IUnit> supportUnits);

            if (supportUnits.Any())
                attackMgr.LaunchAttack(new LaunchAttackData<IEnumerable<IEntity>>
                {
                    source = supportUnits,
                    targetEntity = target,
                    targetPosition = target.transform.position,
                    playerCommand = false
                });

            return true;
        }

        private ErrorMessage IsValidUnitSupport(TargetData<IEntity> entity, bool playerCommand)
        {
            if (!entity.instance.IsValid()
                || entity.instance.Type != EntityType.unit
                || !factionMgr.IsSameFaction(entity.instance)
                || !entity.instance.CanAttack)
                return ErrorMessage.invalid;
            // Make sure that the unit to test has a target that can not attack back so that it can switch to support
            else if (entity.instance.AttackComponent.HasTarget && entity.instance.AttackComponent.Target.instance.CanAttack)
                return ErrorMessage.attackTargetNoChange;

            return ErrorMessage.none;
        }
        #endregion

#if UNITY_EDITOR
        [System.Serializable]
        private struct NPCDefenseLogData 
        {
            public bool isDefending;

            public GameObject lastDefenseCenter;
        }

        [Header("Logs")]
        [SerializeField, ReadOnly, Space()]
        private NPCDefenseLogData defenseLogs = new NPCDefenseLogData();

        protected override void UpdateLogStats()
        {
            defenseLogs = new NPCDefenseLogData
            {
                isDefending = IsDefending,

                lastDefenseCenter = LastDefenseCenter.IsValid() ? LastDefenseCenter.gameObject :null,
            };
        }
#endif

    }
}
