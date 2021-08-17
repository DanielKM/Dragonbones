using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.EntityComponent;
using RTSEngine.Determinism;
using RTSEngine.NPC.ResourceExtension;
using RTSEngine.NPC.EntityComponent;

namespace RTSEngine.NPC.Upgrades
{
    /// <summary>
    /// Manages upgrade tasks for a NPC faction.
    /// </summary>
    public class NPCUpgradeManager : NPCComponentBase, INPCUpgradeManager
    {
        #region Attributes
        [SerializeField, Tooltip("Allow component to launch upgrade tasks when they are available?")]
        private bool autoUpgrade = true;
        [SerializeField, Tooltip("How often does the NPC faction attempt to launch upgrade tasks?")]
        private FloatRange upgradeReloadRange = new FloatRange(5.0f, 10.0f);
        private TimeModifiedTimer upgradeTimer;

        [SerializeField, Tooltip("Between 0.0 and 1.0, randomizes upgrade decisions where the higher the value, the higher chance to launch an upgrade.")]
        private FloatRange acceptanceRange = new FloatRange(0.5f, 0.8f);

        [SerializeField, Tooltip("Allow other NPC components to launch upgrade tasks?")]
        private bool upgradeOnDemand = true;

        // NPC Components
        private INPCEntityComponentTracker npcTracker;
        protected INPCResourceManager npcResourceMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            this.npcTracker = npcMgr.GetNPCComponent<INPCEntityComponentTracker>();
            this.npcResourceMgr = npcMgr.GetNPCComponent<INPCResourceManager>();

            // Initial state
            upgradeTimer = new TimeModifiedTimer(upgradeReloadRange);
        }

        protected override void OnPostInit()
        {
            IsActive = autoUpgrade;

            npcTracker.UpgradeLauncherTracker.ComponentAdded += HandleUpgradeLauncherAddedOrUpdated;
            npcTracker.UpgradeLauncherTracker.ComponentUpdated += HandleUpgradeLauncherAddedOrUpdated;

            globalEvent.UnitUpgradedGlobal += HandleFactionEntityUpgradedGlobal;
            globalEvent.BuildingUpgradedGlobal += HandleFactionEntityUpgradedGlobal;

            globalEvent.EntityComponentUpgradedGlobal += HandleEntityComponentUpgradedGlobal;
        }

        protected override void OnDestroyed()
        {
            npcTracker.UpgradeLauncherTracker.ComponentAdded -= HandleUpgradeLauncherAddedOrUpdated;
            npcTracker.UpgradeLauncherTracker.ComponentUpdated -= HandleUpgradeLauncherAddedOrUpdated;

            globalEvent.UnitUpgradedGlobal -= HandleFactionEntityUpgradedGlobal;
            globalEvent.BuildingUpgradedGlobal -= HandleFactionEntityUpgradedGlobal;

            globalEvent.EntityComponentUpgradedGlobal -= HandleEntityComponentUpgradedGlobal;
        }
        #endregion

        #region Handling Events: UpgradeLauncherTracker
        private void HandleUpgradeLauncherAddedOrUpdated(IEntityComponentTracker<IUpgradeLauncher> sender, EntityComponentEventArgs<IUpgradeLauncher> e)
        {
            IsActive = autoUpgrade;
        }
        #endregion

        #region Handling Events: Faction Entity Upgrade, Entity Component Upgrade
        // In case a unit, building or entity component that belongs to this NPC faction has been upgraded...
        // This opens the chance to have new upgrade tasks on already existing upgrade launchers and therefore activate this component in such case
        private void HandleEntityComponentUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntityComponent> args)
        {
            if (factionMgr.IsSameFaction(args.FactionID))
                IsActive = autoUpgrade;
        }

        private void HandleFactionEntityUpgradedGlobal(IFactionEntity sender, UpgradeEventArgs<IEntity> args)
        {
            if (factionMgr.IsSameFaction(args.FactionID))
                IsActive = autoUpgrade;
        }
        #endregion

        #region Launching Upgrades
        protected override void OnActiveUpdate()
        {
            if (!upgradeTimer.ModifiedDecrease())
                return;

            upgradeTimer.Reload(upgradeReloadRange);

            // Assume that there are no upgrade tasks left to launch before going through the cached ones
            IsActive = false;

            foreach (IUpgradeLauncher upgradeLauncher in npcTracker.UpgradeLauncherTracker.Components)
            {
                if (upgradeLauncher.Tasks.Count <= 0)
                    continue;

                IsActive = true;

                for (int upgradeTaskID = 0; upgradeTaskID < upgradeLauncher.Tasks.Count; upgradeTaskID++)
                    OnUpgradeLaunchRequestInternal(upgradeLauncher, upgradeTaskID);
            }
        }

        public bool OnUpgradeLaunchRequest(IUpgradeLauncher upgradeLauncher, int upgradeTaskID)
        {
            if (!upgradeOnDemand)
                return false;

            return OnUpgradeLaunchRequestInternal(upgradeLauncher, upgradeTaskID);
        }

        private bool OnUpgradeLaunchRequestInternal(IUpgradeLauncher upgradeLauncher, int upgradeTaskID)
        {
            if (!upgradeLauncher.IsValid()
                || UnityEngine.Random.value > acceptanceRange.RandomValue)
                return false;

            ErrorMessage errorMessage = upgradeLauncher.LaunchTaskAction(upgradeTaskID, false);

            switch (errorMessage)
            {
                case ErrorMessage.taskMissingResourceRequirements:
                    npcResourceMgr.OnIncreaseMissingResourceRequest(upgradeLauncher.Tasks.ElementAt(upgradeTaskID).RequiredResources);
                    break;
            }

            return errorMessage == ErrorMessage.none;
        }
        #endregion
    }
}
