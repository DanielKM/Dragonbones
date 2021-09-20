using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Upgrades;
using RTSEngine.Event;
using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(IEntity))]
    public class UpgradeLauncher : PendingTaskEntityComponentBase, IUpgradeLauncher, IEntityPostInitializable
    {
        #region Class Attributes
        [SerializeField, Tooltip("Task input for EntityUpgrade or EntityComponentUpgrade upgrades that can be launched using this component.")]
        private List<UpgradeTask> upgradeTasks = new List<UpgradeTask>();
        public override IReadOnlyList<IEntityComponentTaskInput> Tasks => upgradeTasks;

        [SerializeField, Tooltip("List of entity upgrade tasks that can be launched through this component after their entity upgrades are unlocked.")]
        private UpgradeTask[] entityTargetUpgradeTasks = new UpgradeTask[0];

        [SerializeField, Tooltip("List of entity component upgrade tasks that can be launched through this component after their entity component upgrades are unlocked.")]
        private UpgradeTask[] entityComponentTargetUpgradeTasks = new UpgradeTask[0];

        // Game services
        protected IEntityUpgradeManager entityUpgradeMgr { private set; get; }
        protected IEntityComponentUpgradeManager entityCompUpgradeMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            if (!logger.RequireTrue(upgradeTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Tasks' array have the 'Prefab Object' field unassigned!")

                || !logger.RequireTrue(entityTargetUpgradeTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Target Upgrade Tasks' array have the 'Prefab Object' field unassigned!"))
                return;

            this.entityUpgradeMgr = gameMgr.GetService<IEntityUpgradeManager>();
            this.entityCompUpgradeMgr = gameMgr.GetService<IEntityComponentUpgradeManager>();

            if (!Entity.IsFree)
            {
                // Divide upgrade tasks into entity upgrades (group with key = true) and entity component upgrades (group with key = false)
                var upgradeTaskGroups = upgradeTasks
                    .GroupBy(task => task.Prefab is EntityUpgrade);

                var entityUpgradeTasks = upgradeTaskGroups
                    .Where(group => group.Key == true)
                    .SelectMany(group => group);

                // Check for already launched entity upgrades:
                if (entityUpgradeMgr.TryGet(Entity.FactionID, out UpgradeElement<IEntity>[] upgradedEntityElements))
                {
                    entityUpgradeTasks = entityUpgradeTasks
                        .Where(task => !upgradedEntityElements
                            .Select(upgradeElement => upgradeElement.sourceCode)
                            .Contains(task.Prefab.SourceCode))
                        .Concat(entityTargetUpgradeTasks
                            .Where(upgradedTask => upgradedTask.Prefab.SourceEntity.IsValid() && upgradedEntityElements
                                .Select(upgradeElement => upgradeElement.target)
                                .Contains(upgradedTask.Prefab.SourceEntity))
                        )
                        .ToArray();
                }

                // Get the EntityComponentUpgrade tasks
                var entityComponentUpgradeTasks = upgradeTaskGroups
                    .Where(group => group.Key == false)
                    .SelectMany(group => group)
                    .ToList();

                // Check for already launched entity component upgrades
                // Go through each EntityComponentUpgrade in the above group
                foreach (EntityComponentUpgrade componentUpgrade in entityComponentUpgradeTasks
                    .Where(task => (task.Prefab is EntityComponentUpgrade))
                    .Select(task => task.Prefab as EntityComponentUpgrade)
                    .ToArray())
                {
                    // For each instance, check if the upgrades have been already launched
                    if (entityCompUpgradeMgr.TryGet(componentUpgrade.SourceEntity, Entity.FactionID, out List<UpgradeElement<IEntityComponent>> upgradedComponentElements))
                    {
                        // Remove all launched upgrades
                        entityComponentUpgradeTasks.RemoveAll(task => upgradedComponentElements
                            .Select(upgradeElement => upgradeElement.sourceCode)
                            .Contains(task.Prefab.SourceCode));
                    }
                }

                // Rejoin EntityUpgrade and EntityUpgradeComponent tasks.
                upgradeTasks = entityUpgradeTasks
                    .Concat(entityComponentUpgradeTasks)
                    .ToList();

                // Initialize upgrade tasks
                foreach (var task in upgradeTasks)
                    task.Init(this, gameMgr);
            }

            globalEvent.EntityUpgradedGlobal += HandleEntityUpgradedGlobal;
            globalEvent.EntityComponentUpgradedGlobal += HandleEntityComponentUpgradedGlobal;
        }

        protected override void OnDisabled()
        {
            globalEvent.EntityUpgradedGlobal -= HandleEntityUpgradedGlobal;
            globalEvent.EntityComponentUpgradedGlobal -= HandleEntityComponentUpgradedGlobal;
        }
        #endregion

        #region Handling Event: EntityComponentUpgradedGlobal
        private void HandleEntityComponentUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntityComponent> args)
        {
            if (args.FactionID != Entity.FactionID)
                return;

            // If there are pending tasks that use the upgraded entity component then cancel them.
            int upgradeTaskID = upgradeTasks.FindIndex(upgradeTask => (upgradeTask.Prefab is EntityComponentUpgrade) && upgradeTask.Prefab.SourceCode == args.UpgradeElement.sourceCode);
            Entity.PendingTasksHandler.CancelBySourceID(this, upgradeTaskID);

            if (upgradeTaskID.IsValidIndex(upgradeTasks))
                upgradeTasks[upgradeTaskID].Disable();

            upgradeTasks.AddRange(entityComponentTargetUpgradeTasks
                .Where(upgradedTask =>
                {
                    if (upgradedTask.Prefab.SourceCode == args.UpgradeElement.target.Code)
                    {
                        // Initialize the task that will be moved from the upgraded tasks to the actively available ones.
                        upgradedTask.Init(this, gameMgr);
                        return true;
                    }

                    return false;
                }));

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
            globalEvent.RaisePendingTaskEntityComponentUpdated(this);
        }
        #endregion

        #region Handling Event: EntityUpgradedGlobal
        private void HandleEntityUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntity> args)
        {
            if (args.FactionID != Entity.FactionID)
                return;

            // If there are pending tasks that use the upgraded entity then cancel them.
            int upgradeTaskID = upgradeTasks.FindIndex(upgradeTask => (upgradeTask.Prefab is EntityUpgrade) && upgradeTask.Prefab.SourceCode == args.UpgradeElement.sourceCode);
            Entity.PendingTasksHandler.CancelBySourceID(this, upgradeTaskID);

            if (upgradeTaskID.IsValidIndex(upgradeTasks))
                upgradeTasks[upgradeTaskID].Disable();

            upgradeTasks.AddRange(entityTargetUpgradeTasks
                .Where(upgradedTask =>
                {
                    if (upgradedTask.Prefab.SourceCode == args.UpgradeElement.target.Code)
                    {
                        // Initialize the task that will be moved from the upgraded tasks to the actively available ones.
                        upgradedTask.Init(this, gameMgr);
                        return true;
                    }

                    return false;
                }));

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
            globalEvent.RaisePendingTaskEntityComponentUpdated(this);
        }
        #endregion

        #region Handling Upgrade Action
        protected override ErrorMessage CompleteTaskActionLocal(int upgradeTaskID, bool playerCommand)
        {
            upgradeTasks[upgradeTaskID].Prefab.LaunchLocal(gameMgr, factionEntity.FactionID);

            return ErrorMessage.none;
        }
        #endregion

        #region Task UI
        protected override string GetTooltipText(IEntityComponentTaskInput taskInput)
        {
            UpgradeTask nextTask = taskInput as UpgradeTask;

            textDisplayer.UpgradeTaskToString(
                nextTask,
                out string tooltipText);

            return tooltipText;
        }
        #endregion
    }
}
