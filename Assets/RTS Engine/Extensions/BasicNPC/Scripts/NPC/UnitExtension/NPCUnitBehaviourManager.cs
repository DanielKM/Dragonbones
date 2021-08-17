using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Attack;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Movement;
using RTSEngine.NPC.Attack;
using RTSEngine.NPC.Event;
using UnityEngine.Assertions;
using RTSEngine.EntityComponent;

namespace RTSEngine.NPC.UnitExtension
{
    public class NPCUnitBehaviourManager : NPCComponentBase, INPCUnitBehaviourManager
    {
        #region Attributes
        // We can have multiple instances of this component
        public override bool IsSingleInstance => false;

        [SerializeField, EnforceType(typeof(IUnit), prefabOnly: true), Tooltip("Prefabs of unit types whose behaviour would managed by this component.")]
        private GameObject[] prefabs = new GameObject[0];
        private NPCActiveRegulatorMonitor regulatorMonitor;

        [Space, SerializeField, Tooltip("Pick parameters that allow to force the NPC faction to update the creation goals (target amount) of the assigned unit types.")]
        private NPCFactionEntityForceCreationData forceCreation = new NPCFactionEntityForceCreationData
        {
            enabled = true,

            targetCountUpdateDelay = new FloatRange(10.0f, 20.0f),
            targetCountUpdatePeriod = new FloatRange(3.0f, 7.0f),

            targetCountUpdateAmount = 1
        };

        /// <summary>
        /// Current behaviour state of the tracked units.
        /// </summary>
        public NPCUnitBehaviourState State { private set; get; }

        [Space, SerializeField, Tooltip("Pick parameters that allow to define the behaviour of the handled unit instances when an attack engagement order is set by the NPC faction.")]
        private NPCAttackEngageOrderUnitBehaviourData attackEngageOrderBehaviour = new NPCAttackEngageOrderUnitBehaviourData
        {
            send = true,
            sendIdleOnly = true,
            sendNoTargetThreatOnly = true,

            sendRatioRange = new FloatRange(0.8f, 0.9f),

            sendDelay = new FloatRange(0.0f, 2.0f),

            attack = true,

            sendBackOnAttackCancel = true
        };
        private bool awaitingAttackEngageOrderResponse = false;
        private NPCAttackEngageOrderTargetData nextAttackEngageOrderTargetData;

        [Space, SerializeField, Tooltip("Pick parameters that allow to define the behaviour of the handled unit instances when the NPC faction announces a territory defense state.")]
        private NPCTerritoryDefenseOrderUnitBehaviourData territoryDefenseOrderBehaviour = new NPCTerritoryDefenseOrderUnitBehaviourData
        {
            defend = true,

            forceChangeDefenseCenter = false,

            sendBackOnDefenseCancel = true
        };

        // Game services
        protected IAttackManager attackMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }

        // NPC Components
        protected INPCUnitCreator npcUnitCreator { private set; get; }
        protected INPCEventPublisher npcEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            this.npcUnitCreator = npcMgr.GetNPCComponent<INPCUnitCreator>();
            this.npcEvent = npcMgr.GetNPCComponent<INPCEventPublisher>();

            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();

            // Initial state
            forceCreation.timer = new TimeModifiedTimer(forceCreation.targetCountUpdateDelay.RandomValue + forceCreation.targetCountUpdatePeriod.RandomValue);
            awaitingAttackEngageOrderResponse = false;

            State = NPCUnitBehaviourState.idle;
        }

        protected override void OnPostInit()
        {
            ActivateUnitRegulators();

            npcEvent.AttackEngageOrder += HandleAttackEngageOrder;
            npcEvent.AttackCancelled += HandleAttackCancelled;

            npcEvent.TerritoryDefenseOrder += HandleTerritoryDefenseOrder;
            npcEvent.TerritoryDefenseCancelled += HandleTerritoryDefenseCancelled;
        }

        protected override void OnDestroyed()
        {
            npcEvent.AttackEngageOrder -= HandleAttackEngageOrder;
            npcEvent.AttackCancelled -= HandleAttackCancelled;

            npcEvent.TerritoryDefenseOrder -= HandleTerritoryDefenseOrder;
            npcEvent.TerritoryDefenseCancelled -= HandleTerritoryDefenseCancelled;

            regulatorMonitor.Disable();
        }

        private void ActivateUnitRegulators()
        {
            regulatorMonitor = new NPCActiveRegulatorMonitor(gameMgr, factionMgr);

            foreach (GameObject prefab in prefabs)
            {
                IUnit unit = prefab.GetComponent<IUnit>();
                if (!logger.RequireValid(unit,
                    $"[{GetType().Name} - {factionMgr.FactionID}] 'Prefabs' field has some unassigned elements."))
                    return;

                NPCUnitRegulator nextRegulator;
                if ((nextRegulator = npcUnitCreator.ActivateUnitRegulator(unit)).IsValid())
                    regulatorMonitor.AddCode(nextRegulator.Prefab.Code);
            }
        }
        #endregion

        #region Forcing Creation
        protected override void OnActiveUpdate()
        {
            if (forceCreation.enabled)
                OnForceCreationUpdate();

            if (awaitingAttackEngageOrderResponse)
                HandleAttackEngageOrderResponse();
        }

        private void OnForceCreationUpdate()
        {
            if (!forceCreation.timer.ModifiedDecrease())
                return;

            forceCreation.timer.Reload(forceCreation.targetCountUpdatePeriod);

            foreach (string unitCode in regulatorMonitor.AllCodes.ToArray())
            {
                NPCUnitRegulator nextUnitRegulator = npcUnitCreator.GetActiveUnitRegulator(unitCode);

                nextUnitRegulator.UpdateTargetCount(nextUnitRegulator.TargetCount + forceCreation.targetCountUpdateAmount);
            }
        }
        #endregion

        #region Attack Engage Behaviour
        private void HandleAttackEngageOrder(INPCAttackManager sender, NPCAttackEngageEventArgs args)
        {
            if (!attackEngageOrderBehaviour.send)
                return;

            awaitingAttackEngageOrderResponse = true;

            nextAttackEngageOrderTargetData = new NPCAttackEngageOrderTargetData
            {
                target = args.Target,
                targetPosition = args.TargetPosition,

                delayTimer = new TimeModifiedTimer(attackEngageOrderBehaviour.sendDelay),
            };
        }

        private void HandleAttackCancelled(INPCAttackManager sender, EventArgs args)
        {
            awaitingAttackEngageOrderResponse = false;
            nextAttackEngageOrderTargetData = new NPCAttackEngageOrderTargetData();

            if (!attackEngageOrderBehaviour.sendBackOnAttackCancel)
                return;

            SendToSpawnPoints();
        }

        private void HandleAttackEngageOrderResponse()
        {
            if (!nextAttackEngageOrderTargetData.delayTimer.ModifiedDecrease())
                return;

            State = NPCUnitBehaviourState.attacking;

            awaitingAttackEngageOrderResponse = false;

            IEnumerable<IUnit> sendableUnits = regulatorMonitor.AllCodes
                .SelectMany(unitCode =>
                {
                    // For each tracked unit type, we get the regulator and its current available instances (idle or all depending on the behaviour data)
                    List<IUnit> availableUnits = (attackEngageOrderBehaviour.sendIdleOnly
                    ? npcUnitCreator.GetActiveUnitRegulator(unitCode).InstancesIdleOnly
                    : (attackEngageOrderBehaviour.sendNoTargetThreatOnly
                        ? npcUnitCreator.GetActiveUnitRegulator(unitCode).Instances
                            .Where(unit => !unit.CanAttack || !unit.AttackComponent.HasTarget || !unit.AttackComponent.Target.instance.CanAttack)
                        : npcUnitCreator.GetActiveUnitRegulator(unitCode).Instances)).ToList();

                    // Only get a certain range from the availale units, the same list of units will be used until defense mode is enabled or the attack is disabled
                    return availableUnits.GetRange(0, (int)(attackEngageOrderBehaviour.sendRatioRange.RandomValue * availableUnits.Count));
                })
                .Where(unit => unit.IsValid());

            if (!sendableUnits.Any())
                return;

            if (sendableUnits.Contains(null))
                print("ISSUE");

            if (attackEngageOrderBehaviour.attack)
                attackMgr.LaunchAttack(
                    new LaunchAttackData<IEnumerable<IEntity>>
                    {
                        source = sendableUnits,
                        targetEntity = nextAttackEngageOrderTargetData.target,
                        targetPosition = nextAttackEngageOrderTargetData.target.IsValid() ? nextAttackEngageOrderTargetData.target.transform.position : nextAttackEngageOrderTargetData.targetPosition,
                        playerCommand = false
                    });
            else
                mvtMgr.SetPathDestination(
                    sendableUnits,
                    nextAttackEngageOrderTargetData.target.IsValid() ? nextAttackEngageOrderTargetData.target.transform.position : nextAttackEngageOrderTargetData.targetPosition,
                    nextAttackEngageOrderTargetData.target.IsValid() ? nextAttackEngageOrderTargetData.target.Radius : 0.0f,
                    nextAttackEngageOrderTargetData.target,
                    playerCommand: false);
        }
        #endregion

        #region Territory Defense Behaviour
        private void HandleTerritoryDefenseOrder(INPCDefenseManager sender, NPCTerritoryDefenseEngageEventArgs args)
        {
            if (!territoryDefenseOrderBehaviour.defend)
                return;

            State = NPCUnitBehaviourState.defending;

            IEnumerable<IUnit> sendableUnits = regulatorMonitor.AllCodes
                .SelectMany(unitCode => npcUnitCreator.GetActiveUnitRegulator(unitCode).Instances);

            foreach (IUnit unit in sendableUnits)
            {
                if (!unit.CanAttack
                    || (unit.AttackComponent.HasTarget && !territoryDefenseOrderBehaviour.forceChangeDefenseCenter))
                    continue;

                unit.AttackComponent.SearchRangeCenter = args.NextDefenseCenter.BorderComponent;
            }
        }

        private void HandleTerritoryDefenseCancelled(INPCDefenseManager sender, EventArgs args)
        {
            IEnumerable<IUnit> sendableUnits = regulatorMonitor.AllCodes
                .SelectMany(unitCode => npcUnitCreator.GetActiveUnitRegulator(unitCode).Instances);

            foreach (IUnit unit in sendableUnits)
            {
                if (!unit.CanAttack)
                    continue;

                unit.AttackComponent.SearchRangeCenter = null;
            }

            if (!territoryDefenseOrderBehaviour.sendBackOnDefenseCancel)
                return;

            SendToSpawnPoints();
        }
        #endregion

        #region Helper Methods
        private void SendToSpawnPoints()
        {
            State = NPCUnitBehaviourState.idle;

            IBuilding[] buildingRallypoints = factionMgr.Buildings
                .Where(building => building.Rallypoint.IsValid())
                .ToArray();

            // Organize the tracked units via their spawn rallypoints (if they have one)
            IEnumerable<IGrouping<IRallypoint, IUnit>> sendableUnitGroups = regulatorMonitor.AllCodes
                .SelectMany(unitCode => npcUnitCreator.GetActiveUnitRegulator(unitCode).Instances)
                .GroupBy(unit => unit.SpawnRallypoint.IsValid()
                    ? unit.SpawnRallypoint 
                    : (buildingRallypoints.Length > 0 
                        ? buildingRallypoints[UnityEngine.Random.Range(0, buildingRallypoints.Length)].Rallypoint
                        : null));

            // Send tracked units to their spawn rallypoints and if they have none, send them back to the faction spawn position
            foreach (IGrouping<IRallypoint, IUnit> nextUnitGroup in sendableUnitGroups)
                mvtMgr.SetPathDestination(
                    nextUnitGroup.Where(unit => unit.IsValid()),
                    nextUnitGroup.Key.IsValid() ? nextUnitGroup.Key.Entity.transform.position : factionSlot.FactionSpawnPosition,
                    nextUnitGroup.Key.IsValid() ? nextUnitGroup.Key.Entity.Radius : 0.0f,
                    target: null,
                    playerCommand: false);
        }
        #endregion

#if UNITY_EDITOR
        [Header("Logs")]
        [SerializeField, ReadOnly]
        private NPCUnitBehaviourState currStateLog;

        protected override void UpdateLogStats()
        {
            currStateLog = State;
        }
#endif

    }

}
