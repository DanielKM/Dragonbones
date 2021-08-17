using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Attack;
using RTSEngine.Entities;
using RTSEngine.Faction;
using RTSEngine.ResourceExtension;
using RTSEngine.Determinism;
using RTSEngine.Search;
using RTSEngine.NPC.Event;

namespace RTSEngine.NPC.Attack
{

    /// <summary>
    /// Responsible for picking a target faction and launching attacks for a NPC faction
    /// </summary>
    public class NPCAttackManager : NPCComponentBase, INPCAttackManager
    {
        #region Attributes
        [Header("Picking Target")]
        [SerializeField, Tooltip("Can the NPC faction attack other factions? Disable to deactivate this component's behavior.")]
        private bool canAttack = true;

        [SerializeField, Tooltip("Resource types whose values determine the criteria of evaluating a potential target faction's strength.")]
        private ResourceTypeInfo[] attackResources = new ResourceTypeInfo[0];

        [SerializeField, Tooltip("What's the NPC faction's strategy when picking a target faction?")]
        private NPCAttackTargetPickerType targetFactionType = NPCAttackTargetPickerType.random;

        [SerializeField, Tooltip("Delay time before picking a target faction?")]
        private FloatRange setTargetFactionDelay = new FloatRange(10, 15);
        private TimeModifiedTimer setTargetFactionTimer;

        // Faction slot of the current target faction
        protected IFactionSlot targetFactionSlot { private set; get; }

        [Header("Launching Attack")]
        [SerializeField, Tooltip("How often does the NPC faction consider whether to launch an attack or not?")]
        private FloatRange launchAttackReloadRange = new FloatRange(10.0f, 15.0f);
        private TimeModifiedTimer launchAttackTimer;

        // Is the NPC faction currently engaging in an attack towards its current target faction?
        public bool IsAttacking { private set; get; }

        [SerializeField, Tooltip("Minimum amount of resource types required to launch an attack.")]
        private ResourceInputRange[] launchAttackResources = new ResourceInputRange[0];

        [Header("Handling Attack")]
        [SerializeField, Tooltip("How often does the NPC faction pick the next target unit/building to attack while engaging in an attack towards enemy faction?"),]
        private FloatRange attackOrderReloadRange = new FloatRange(3.0f, 7.0f);
        private TimeModifiedTimer attackOrderTimer;

        // Last position where a command was made to attack as part of an active attack engagement.
        protected Vector3 lastAttackPos { private set; get; }

        [SerializeField, Tooltip("Define the buildings/units that the NPC faction will prioritize targeting during an active attack.")]
        private AttackTargetPicker targetPicker = new AttackTargetPicker();

        [SerializeField, Tooltip("Only target enemy faction entities that are defined in the above 'Target Picker'?")]
        private bool targetPickerOnly = false;

        public enum NPCAttackFactionEntityPreference { random, units, buildings }

        [SerializeField, Tooltip("NPC faction preferences when picking the next faction entity target using this component.")]
        private NPCAttackFactionEntityPreference targetPreference = NPCAttackFactionEntityPreference.random;

        // The current faction entity that this faction is attempting to destroy, as part of its active attack.
        protected IFactionEntity currentTargetEntity { private set; get; }

        [SerializeField, Tooltip("If the faction has one of the following resource types under the specified amount, the faction will stop its active attack and retrieve its attacking army of units back to its territory.")]
        private ResourceInputRange[] cancelAttackResources = new ResourceInputRange[0];

        // NPC Components
        protected INPCDefenseManager npcDefenseMgr { private set; get; }
        protected INPCEventPublisher npcEvent { private set; get; } 

        // Game services
        protected IResourceManager resourceMgr { private set; get; }
        protected IAttackManager attackMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();

            this.npcDefenseMgr = npcMgr.GetNPCComponent<INPCDefenseManager>();
            this.npcEvent = npcMgr.GetNPCComponent<INPCEventPublisher>();

            // Initial state
            IsActive = canAttack;
            IsAttacking = false;
            targetFactionSlot = null;

            setTargetFactionTimer = new TimeModifiedTimer(setTargetFactionDelay);
            launchAttackTimer = new TimeModifiedTimer(launchAttackReloadRange);
        }

        protected override void OnPostInit()
        {
            factionSlot.FactionSlotStateUpdated += HandleFactionSlotStateUpdated;
        }

        protected override void OnDestroyed()
        {
            factionSlot.FactionSlotStateUpdated -= HandleFactionSlotStateUpdated;
        }
        #endregion

        #region Handling Events: Faction Slot State Updated
        private void HandleFactionSlotStateUpdated(IFactionSlot sender, EventArgs args)
        {
            // When the NPC faction is eliminated, cancel the current attack
            if (factionSlot.State == FactionSlotState.eliminated)
                CancelAttack();
        }
        #endregion

        #region Updating State
        protected override void OnActiveUpdate()
        {
            if (gameMgr.InPeaceTime)
                return;

            if (!targetFactionSlot.IsValid())
                OnTargetFactionSearch();
            else
            {
                if (IsAttacking)
                    OnActiveAttackUpdate();
                else
                    OnInactiveAttackUpdate();
            }
        }
        #endregion

        #region Picking Target Faction
        private void OnTargetFactionSearch()
        {
            if (setTargetFactionTimer.ModifiedDecrease())
            {
                setTargetFactionTimer.Reload(setTargetFactionDelay);

                SetTargetFaction();
            }
        }

        /// <summary>
        /// Pick the target faction from the available active factions.
        /// </summary>
        public void SetTargetFaction()
        {
            // Order the potential target factions via the attack resources assigned for this component
            IFactionSlot[] activeFactions = gameMgr.FactionSlots
                .Where(faction => faction.IsActiveFaction() && faction.FactionMgr != factionMgr)
                .OrderByDescending(faction => attackResources.Sum(resourceType => resourceMgr.FactionResources[faction.ID].ResourceHandlers[resourceType].Amount))
                .ToArray();

            if (!activeFactions.Any())
                return;

            // Cancel current attack (if there is one) and start new one.
            CancelAttack();

            // Depending on the strategy of this component in relation to the attack resources, pick one
            switch (targetFactionType)
            {
                case NPCAttackTargetPickerType.random:
                    targetFactionSlot = activeFactions[UnityEngine.Random.Range(0, activeFactions.Length)];
                    break;

                case NPCAttackTargetPickerType.mostAttackResources:
                    targetFactionSlot = activeFactions[0];
                    break;

                case NPCAttackTargetPickerType.leastAttackResources:
                    targetFactionSlot = activeFactions[activeFactions.Length - 1];
                    break;
            }

            targetFactionSlot.FactionSlotStateUpdated += HandleTargetFactionSlotStateUpdated;
        }

        /// <summary>
        /// Directly assign a target faction slot. 
        /// </summary>
        public bool SetTargetFaction(IFactionSlot newTargetFactionSlot, bool launchAttack)
        {
            if (!newTargetFactionSlot.IsValid() 
                || newTargetFactionSlot == factionMgr
                || !newTargetFactionSlot.IsActiveFaction()
                || newTargetFactionSlot == targetFactionSlot)
                return false;

            // Cancel current attack (if there is one) and start new one.
            CancelAttack();

            targetFactionSlot = newTargetFactionSlot;
            targetFactionSlot.FactionSlotStateUpdated += HandleTargetFactionSlotStateUpdated;

            if (launchAttack)
                LaunchAttack();

            return true;
        }

        private void HandleTargetFactionSlotStateUpdated(IFactionSlot targetFactionSlot, EventArgs args)
        {
            if (targetFactionSlot.State == FactionSlotState.eliminated)
                CancelAttack();
        }
        #endregion

        #region Launching Attack
        private void OnInactiveAttackUpdate()
        {
            // Can not launch a new attack if the NPC faction is already in a defensive mode
            if (npcDefenseMgr.IsDefending)
                return;

            if (launchAttackTimer.ModifiedDecrease())
            {
                launchAttackTimer.Reload(launchAttackReloadRange);

                // Does the NPC faction has enough attacking power to launch attack?
                if (!resourceMgr.HasResources(launchAttackResources, factionMgr.FactionID))
                    return;

                LaunchAttack();
            }
        }

        /// <summary>
        /// Launches an attack on the NPC faction's current target faction.
        /// </summary>
        public bool LaunchAttack()
        {
            if (!targetFactionSlot.IsValid()
                || gameMgr.InPeaceTime)
                return false;

            IsAttacking = true;

            // Start with the faction's spawn position as the last position to search from
            lastAttackPos = gameMgr.GetFactionSlot(factionMgr.FactionID).FactionSpawnPosition;

            // Set initial target faction entity
            currentTargetEntity = null;
            SetTargetEntity(targetFactionSlot.FactionMgr.Buildings.Cast<FactionEntity>(), true);

            // Start the attack order timer to handle attack commands in the active attack engagement
            attackOrderTimer = new TimeModifiedTimer(attackOrderReloadRange);

            return true;
        }
        #endregion

        #region Picking Faction Entity Target
        /// <summary>
        /// Sets the current faction entity target to the closest from an a set of potential targets. 
        /// </summary>
        public bool SetTargetEntity(IEnumerable<IFactionEntity> factionEntities, bool resetCurrentTarget)
        {
            if (resetCurrentTarget)
                ResetCurrentTarget();

            // Prioritize faction entities that are defined in the target
            IEnumerable<IGrouping<bool, IFactionEntity>> factionEntityGroups = factionEntities
                .GroupBy(factionEntity => targetPicker.IsValidTarget(factionEntity))
                .OrderByDescending(factionEntityGroup => factionEntityGroup.Key == true);

            foreach (IGrouping<bool, IFactionEntity> factionEntityGroup in factionEntityGroups)
            {
                foreach (IFactionEntity nextEntity in factionEntityGroup
                    .OrderBy(factionEntity => Vector3.Distance(factionEntity.transform.position, lastAttackPos)))
                {
                    if (!IsValidTargetFactionEntity(nextEntity))
                        continue;

                    currentTargetEntity = nextEntity;
                    lastAttackPos = currentTargetEntity.transform.position;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the current faction entity target.
        /// </summary>
        public bool SetTargetEntity(FactionEntity nextTarget, bool resetCurrentTarget)
        {
            if (resetCurrentTarget)
                ResetCurrentTarget();

            if (!IsValidTargetFactionEntity(nextTarget))
                return false;

            currentTargetEntity = nextTarget;

            return true;
        }

        public bool IsValidTargetFactionEntity(IFactionEntity potentialTarget)
        {
            return potentialTarget.IsValid()
                && potentialTarget.IsInteractable
                && !potentialTarget.Health.IsDead
                // There must be a target faction and the target entity must belong to it
                && targetFactionSlot.IsValid()
                && targetFactionSlot.IsSameFaction(potentialTarget)
                && (!targetPickerOnly || targetPicker.IsValidTarget(potentialTarget));
        }

        /// <summary>
        /// Resets the currentTarget FactionEntity to null.
        /// </summary>
        public void ResetCurrentTarget()
        {
            currentTargetEntity = null;
        }
        #endregion

        #region Handling Active Attack
        private void OnActiveAttackUpdate()
        {
            if (!attackOrderTimer.ModifiedDecrease())
                return;

            attackOrderTimer.Reload(attackOrderReloadRange);

            // Did the current attack power hit the surrender resource type amount?
            if (!resourceMgr.HasResources(cancelAttackResources, factionMgr.FactionID))
            {
                CancelAttack();
                return;
            }

            IEnumerable<IFactionEntity> primaryTargetSet;
            IEnumerable<IFactionEntity> secondaryTargetSet;

            bool randPickBuildingsPrimary = targetPreference == NPCAttackFactionEntityPreference.random
                && UnityEngine.Random.value > 0.5f;

            if (targetPreference == NPCAttackFactionEntityPreference.buildings
                || randPickBuildingsPrimary)
            {
                primaryTargetSet = targetFactionSlot.FactionMgr.Buildings;
                secondaryTargetSet = targetFactionSlot.FactionMgr.Units;
            }
            else
            {
                secondaryTargetSet = targetFactionSlot.FactionMgr.Buildings;
                primaryTargetSet = targetFactionSlot.FactionMgr.Units;
            }

            // Update current target faction entity
            if (!currentTargetEntity.IsValid()
                && SetTargetEntity(primaryTargetSet, resetCurrentTarget: false) == false)
                SetTargetEntity(secondaryTargetSet, resetCurrentTarget: false);

            EngageCurrentTargetFactionEntity();
        }

        /// <summary>
        /// Stops the NPC faction from attacking and resets its targets.
        /// </summary>
        public void CancelAttack()
        {
            if(IsAttacking)
                npcEvent.RaiseAttackCancelled(this);

            currentTargetEntity = null;

            if(targetFactionSlot.IsValid())
                targetFactionSlot.FactionSlotStateUpdated -= HandleTargetFactionSlotStateUpdated;

            targetFactionSlot = null;

            IsAttacking = false;
        }

        private bool EngageCurrentTargetFactionEntity()
        {
            if (!IsAttacking
                || !targetFactionSlot.IsValid()
                || gameMgr.InPeaceTime)
                return false;

            if (!currentTargetEntity.IsValid()
                || currentTargetEntity.Health.IsDead)
            {
                ResetCurrentTarget();
                return false;
            }

            // If the current target is a building and it is being constructed, then assign an active builder (if it exists) as the current target.
            if (currentTargetEntity.Type == EntityType.building
                && (currentTargetEntity as IBuilding).WorkerMgr.Amount > 0)
            {
                IUnit activeBuilder = (currentTargetEntity as IBuilding)
                    .WorkerMgr.Workers
                    .Where(unit => unit.BuilderComponent.InProgress)
                    .FirstOrDefault();

                if (activeBuilder.IsValid())
                    currentTargetEntity = activeBuilder;
            }

            npcEvent.RaiseAttackEngageOrder(this, new NPCAttackEngageEventArgs(currentTargetEntity, lastAttackPos));

            return true;
        }
        #endregion

#if UNITY_EDITOR
        [System.Serializable]
        private struct NPCAttackLogData 
        {
            public int targetFactionID;

            public bool isAttacking;

            public GameObject currTargetEntity;
            public Vector3 lastAttackPosition;
        }

        [Header("Logs")]
        [SerializeField, ReadOnly, Space()]
        private NPCAttackLogData attackLogs = new NPCAttackLogData();

        protected override void UpdateLogStats()
        {
            attackLogs = new NPCAttackLogData
            {
                targetFactionID = targetFactionSlot.IsValid() ? targetFactionSlot.ID : -1,

                isAttacking = IsAttacking,

                currTargetEntity = currentTargetEntity.IsValid() ? currentTargetEntity.gameObject : null,
                lastAttackPosition = lastAttackPos
            };
        }
#endif
    }
}
