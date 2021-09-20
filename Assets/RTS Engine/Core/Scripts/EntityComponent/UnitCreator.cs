using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Upgrades;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public class UnitCreator : PendingTaskEntityComponentBase, IUnitCreator, IEntityPostInitializable
    {
        #region Class Attributes
        [SerializeField, Tooltip("List of unit creation tasks that can be launched through this component.")]
        private UnitCreationTask[] creationTasks = new UnitCreationTask[0];
        public override IReadOnlyList<IEntityComponentTaskInput> Tasks => creationTasks;

        [SerializeField, Tooltip("List of unit creation tasks that can be launched through this component after the unit upgrades are unlocked.")]
        private UnitCreationTask[] upgradeTargetCreationTasks = new UnitCreationTask[0];

        [SerializeField, Tooltip("The position at where the created units will spawn.")]
        private Transform spawnTransform = null;
        public Vector3 SpawnPosition => spawnTransform.position;

        // Game services
        protected IEntityUpgradeManager entityUpgradeMgr { private set; get; }
        protected IUnitManager unitMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            if (!logger.RequireValid(spawnTransform,
                $"[{GetType().Name} - {Entity.Code}] Field 'Spawn Transform' must be assigned!")

                || !logger.RequireTrue(creationTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Creation Tasks' array have the 'Prefab Object' field unassigned!")

                || !logger.RequireTrue(upgradeTargetCreationTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Target Creation Tasks' array have the 'Prefab Object' field unassigned!"))
                return;

            this.entityUpgradeMgr = gameMgr.GetService<IEntityUpgradeManager>();
            this.unitMgr = gameMgr.GetService<IUnitManager>(); 

            if (!Entity.IsFree)
            {
                // Check for unit upgrades
                if(entityUpgradeMgr.TryGet (Entity.FactionID, out UpgradeElement<IEntity>[] upgradeElements))
                    creationTasks = creationTasks
                        .Where(task => !upgradeElements
                            .Select(upgradeElement => upgradeElement.sourceCode)
                            .Contains(task.Prefab.Code))
                        .Concat(upgradeTargetCreationTasks
                            .Where(upgradedTask => upgradeElements
                                .Select(upgradeElement => upgradeElement.target)
                                .Contains(upgradedTask.Prefab))
                        )
                        .ToArray();

                // Initialize creation tasks
                foreach (var creationTask in creationTasks)
                    creationTask.Init(this, gameMgr);
            }


            globalEvent.UnitUpgradedGlobal += HandleUnitUpgradedGlobal;
        }

        protected override void OnDisabled()
        {
            globalEvent.UnitUpgradedGlobal -= HandleUnitUpgradedGlobal;
        }
        #endregion

        #region Handling Event: UnitUpgradedGlobal
        private void HandleUnitUpgradedGlobal(IUnit unit, UpgradeEventArgs<IEntity> args)
        {
            if (!Entity.IsSameFaction(args.FactionID))
                return;

            // If there are pending tasks that use the upgraded entity then cancel them.
            int upgradeTaskID = Array.FindIndex(creationTasks, creationTask => creationTask.Prefab.Code == args.UpgradeElement.sourceCode);
            Entity.PendingTasksHandler.CancelBySourceID(this, upgradeTaskID);

            // Remove the unit creation task of the unit that has been upgraded
            creationTasks = creationTasks
                .Where(task => task.Prefab.Code != args.UpgradeElement.sourceCode)
                .Concat(upgradeTargetCreationTasks
                    .Where(upgradedTask =>
                    {
                        if (upgradedTask.Prefab == args.UpgradeElement.target)
                        {
                            // Initialize the task that will be moved from the upgraded tasks to the actively available ones.
                            upgradedTask.Init(this, gameMgr);
                            return true;
                        }
                        return false;
                    })
                )
                .ToArray();

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
            globalEvent.RaisePendingTaskEntityComponentUpdated(this);
        }
        #endregion

        #region Handling UnitCreation Actions
        protected override ErrorMessage CompleteTaskActionLocal(int creationTaskID, bool playerCommand)
        {
            unitMgr.CreateUnit(
                creationTasks[creationTaskID].Prefab,
                spawnTransform.position,
                Quaternion.identity,
                new InitUnitParameters
                {
                    factionID = Entity.FactionID,
                    free = false,

                    setInitialHealth = false,

                    rallypoint = factionEntity.Rallypoint,
                    creatorEntityComponent = this,

                    gotoPosition = spawnTransform.position,
                    
                    playerCommand = playerCommand
                });

            return ErrorMessage.none;

        }
        #endregion

        #region Unit Creator Specific Methods
        // Find the task ID that allows to create the unit in the parameter
        public int FindTaskIndex(string unitCode)
        {
            return Array.FindIndex(creationTasks, task => task.Prefab.Code == unitCode);
        }
        #endregion

        #region Task UI
        protected override string GetTooltipText(IEntityComponentTaskInput taskInput)
        {
            UnitCreationTask nextTask = taskInput as UnitCreationTask;

            textDisplayer.UnitCreationTaskToString(
                nextTask,
                out string tooltipText);

            return tooltipText;
        }
        #endregion
    }
}
        