using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Task;
using RTSEngine.Faction;
using System.Linq;

namespace RTSEngine.UI
{
    public class PendingTaskPanelUIHandler : BaseTaskPanelUIHandler<EntityComponentPendingTaskUIAttributes>
    {
        [SerializeField, Tooltip("Parent game object of the active pending tasks.")]
        private GridLayoutGroup panel = null;

        //each created pending task is registered in this list.
        private List<ITaskUI<EntityComponentPendingTaskUIAttributes>> tasks = null;

        [Tooltip("Amount of pending task panel slots to pre-create.")]
        public int preCreatedAmount = 6;

        protected IEntity currSingleSelected { private set; get; }
        private List<ITaskUI<EntityComponentPendingTaskUIAttributes>> currPendingTasks = null;

        protected override void OnInit()
        {
            tasks = new List<ITaskUI<EntityComponentPendingTaskUIAttributes>>();

            currSingleSelected = null;
            currPendingTasks = new List<ITaskUI<EntityComponentPendingTaskUIAttributes>>(); 

            while (tasks.Count < preCreatedAmount)
                Create(tasks, panel.transform);

            globalEvent.EntityComponentPendingTaskUIReloadRequestGlobal += HandleEntityComponentPendingTaskUIReloadRequestGlobal;

            globalEvent.FactionSlotResourceAmountUpdatedGlobal += HandleFactionSlotResourceAmountUpdatedGlobal;

            globalEvent.EntitySelectedGlobal += HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal += HandleEntitySelectionUpdate;

            Hide();
        }

        public override void Disable()
        {
            globalEvent.EntityComponentPendingTaskUIReloadRequestGlobal -= HandleEntityComponentPendingTaskUIReloadRequestGlobal;

            globalEvent.FactionSlotResourceAmountUpdatedGlobal -= HandleFactionSlotResourceAmountUpdatedGlobal;

            globalEvent.EntitySelectedGlobal -= HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal -= HandleEntitySelectionUpdate;
        }

        // When resources change, resource requirements for tasks might be affected, therefore refresh displayed tasks
        private void HandleFactionSlotResourceAmountUpdatedGlobal(IFactionSlot factionSlot, ResourceUpdateEventArgs args)
        {
            if(factionSlot.IsLocalPlayerFaction())
                Show();
        }

        private void HandleEntityComponentPendingTaskUIReloadRequestGlobal(IEntity entity, TaskUIReloadEventArgs args)
        {
            if(entity.IsLocalPlayerFaction() 
                && selectionMgr.IsSelectedOnly(entity, true))
                Show();
        }

        private void HandleEntitySelectionUpdate(IEntity entity, EventArgs args)
        {
            if (!entity.IsLocalPlayerFaction())
                return;

            if (selectionMgr.IsSelectedOnly(entity, true))
                Show();
            else
                Hide();
        }

        private void Hide()
        {
            foreach (var task in tasks)
                if (task.enabled)
                    task.Disable();

            currSingleSelected = null;
            currPendingTasks.Clear(); 
        }

        private ITaskUI<EntityComponentPendingTaskUIAttributes> Add()
        {
            //find the first available (disabled) pending task slot to use next
            foreach (var task in tasks)
                if (!task.enabled)
                    return task;

            //none found? create one!
            return Create(tasks, panel.transform);
        }

        private void Show()
        {
            IEntity nextSingleSelected = selectionMgr.GetSingleSelectedEntity(EntityType.all, true);

            if (nextSingleSelected != currSingleSelected)
                Hide();

            currSingleSelected = nextSingleSelected;
            UpdateEntityComponentPendingTasks(out int updatedAmount);

            while (updatedAmount < currPendingTasks.Count)
                DisableEntityComponentPendingTask(currPendingTasks.Count - 1);
        }

        private void UpdateEntityComponentPendingTasks (out int amount)
        {
            amount = 0;

            if (!currSingleSelected.IsValid() 
                || !currSingleSelected.PendingTasksHandler.IsValid())
                return;

            if (currSingleSelected.PendingTasksHandler.OnPendingTaskUIRequest(out var taskUIAttributes))
            {
                List<EntityComponentPendingTaskUIAttributes> nextPendingTasks = taskUIAttributes.ToList();

                for (int queueIndex = 0; queueIndex < nextPendingTasks.Count; queueIndex++)
                {
                    if (!queueIndex.IsValidIndex(currPendingTasks))
                        currPendingTasks.Add(Add());

                    currPendingTasks[queueIndex].Reload(nextPendingTasks[queueIndex]);
                }

                amount = nextPendingTasks.Count;

            }
        }

        private bool DisableEntityComponentPendingTask (int taskQueueIndex)
        {
            if(taskQueueIndex.IsValidIndex(currPendingTasks))
            {
                currPendingTasks[taskQueueIndex].Disable();

                currPendingTasks.RemoveAt(taskQueueIndex);

                return true;
            }

            return false;
        }
    }
}
