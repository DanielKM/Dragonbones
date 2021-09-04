using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Faction;

namespace RTSEngine.UI
{
    public class TaskPanelUIHandler : BaseTaskPanelUIHandler<EntityComponentTaskUIAttributes>
    {
        [System.Serializable]
        public struct Category
        {
            [Tooltip("Parent UI object of active tasks in this category.")]
            public GridLayoutGroup parent;
            [Tooltip("Amount of task panel slots to pre-create, in case tasks have pre-assigned slot indexes, this should cover them.")]
            public int preCreatedAmount;
        }
        [SerializeField, Tooltip("Task panel can be broken down into different categories where each category is defined by its index in this array.")]
        private Category[] categories = new Category[0];

        //the index of the tasks array represents the category of the task panel
        //each element tasks[i] of the array is a list that holds all the created tasks inside the panel of index i
        private List<ITaskUI<EntityComponentTaskUIAttributes>>[] tasks = null;

        //if true, the task panel can not be updated.
        private bool isLocked = false;
        private List<string> lastUnusedTaskCodes;

        //Holds the active tasks of the IEntityComponent components organized by their unique codes.
        //key: code of the task in the task panel.
        //value: tracker of the task in the task panel which holds information to the sources that initiatied the task.
        private readonly Dictionary<string, EntityComponentTaskUITracker> componentTasks = new Dictionary<string, EntityComponentTaskUITracker>();

        protected override void OnInit()
        {
            if (!logger.RequireTrue(categories.Length > 0,
                $"[TaskPanelUIHandler] At least one task panel category must be defined through the 'Categories' field!"))
                return;

            //initialize each task panel category with its pre created tasks.
            tasks = new List<ITaskUI<EntityComponentTaskUIAttributes>>[categories.Length];
            for(int categoryID = 0; categoryID < tasks.Length; categoryID++)
            {
                tasks[categoryID] = new List<ITaskUI<EntityComponentTaskUIAttributes>>();

                while (tasks[categoryID].Count < categories[categoryID].preCreatedAmount)
                    Create(tasks[categoryID], categories[categoryID].parent.transform);
            }

            isLocked = false; //by default, the task panel is not locked.

            // Initialize data structs:
            lastUnusedTaskCodes = new List<string>();

            //custom events to update/hide UI elements:
            globalEvent.EntityComponentTaskUIReloadRequestGlobal += HandleEntityComponentTaskUIReloadRequestGlobal;

            globalEvent.BuildingPlacementStartGlobal += HandleBuildingPlacementStartGlobal;

            globalEvent.BuildingPlacementStopGlobal += HandleBuildingPlacementStopOrPlacedGlobal;
            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacementStopOrPlacedGlobal;

            globalEvent.BuildingBuiltGlobal += HandleBuildingBuiltGlobal;

            globalEvent.FactionSlotResourceAmountUpdatedGlobal += HandleFactionSlotResourceAmountUpdatedGlobal;

            globalEvent.EntitySelectedGlobal += HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal += HandleEntitySelectionUpdate;

            Hide();
        }

        public override void Disable()
        {
            globalEvent.EntityComponentTaskUIReloadRequestGlobal -= HandleEntityComponentTaskUIReloadRequestGlobal;

            globalEvent.BuildingPlacementStartGlobal -= HandleBuildingPlacementStartGlobal;

            globalEvent.BuildingPlacementStopGlobal -= HandleBuildingPlacementStopOrPlacedGlobal;
            globalEvent.BuildingPlacedGlobal -= HandleBuildingPlacementStopOrPlacedGlobal;

            globalEvent.BuildingBuiltGlobal += HandleBuildingBuiltGlobal;

            globalEvent.FactionSlotResourceAmountUpdatedGlobal -= HandleFactionSlotResourceAmountUpdatedGlobal;

            globalEvent.EntitySelectedGlobal -= HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal -= HandleEntitySelectionUpdate;
        }

        private void HandleEntityComponentTaskUIReloadRequestGlobal(IEntityComponent component, TaskUIReloadEventArgs e)
        {
            if (isLocked)
                return;

            if (e.ReloadAll)
                Show();
            else
                FetchSingleEntityComponentTasks(component);
        }

        //lock the task panel and hide the tasks when player's building placement starts.
        private void HandleBuildingPlacementStartGlobal(IBuilding building, EventArgs e)
        {
            if(building.IsLocalPlayerFaction())
            {
                globalEvent.RaiseHideTooltipGlobal(this);
                Hide();
                isLocked = true;
            }
        }

        //unlock task panel and re-display the tasks after the building is placed or placement is stopped.
        private void HandleBuildingPlacementStopOrPlacedGlobal(IBuilding building, EventArgs e)
        {
            if(!building.IsValid() || building.IsLocalPlayerFaction())
            {
                isLocked = false;
                Show();
            }
        }

        // reload selection when a building is built
        private void HandleBuildingBuiltGlobal(IBuilding building, EventArgs args)
        {
            if (building.Selection.IsSelected
                && building.IsLocalPlayerFaction())
                Show();
        }

        //when resources change, resource requirements for tasks might be affected, therefore refresh displayed tasks
        private void HandleFactionSlotResourceAmountUpdatedGlobal(IFactionSlot factionSlot, ResourceUpdateEventArgs e)
        {
            if(factionSlot.IsLocalPlayerFaction())
                Show();
        }

        private void HandleEntitySelectionUpdate(IEntity sender, EventArgs e)
        {
            if (selectionMgr.Count > 0)
                Show();
            else
                Hide();
        }

        private ITaskUI<EntityComponentTaskUIAttributes> Add(int categoryID, bool forceSlot = false, int slotIndex = 0)
        {
            if (!logger.RequireTrue(categoryID >= 0 && categoryID < tasks.Length,
                $"[{GetType().Name}] Invalid category ID: {categoryID}"))
                return null;

            // If we want to get a specific task slot from the task panel category.
            if(forceSlot) 
            {
                if (!logger.RequireTrue(slotIndex.IsValidIndex(tasks[categoryID]) /*&& !tasks[categoryID][slotIndex].IsEnabled*/,
                    $"[{GetType().Name}] Requested task slot of index {slotIndex} in task panel category {categoryID} is either invalid or already being used!"))
                    return null;

                return tasks[categoryID][slotIndex];
            }

            //find the first available (disabled) task slot to use next
            foreach (var task in tasks[categoryID])
                if (!task.enabled)
                    return task;

            //no forced slot and no available task slot? create one!
            return Create(tasks[categoryID], categories[categoryID].parent.transform);
        }

        private void Hide()
        {
            for (int categoryID = 0; categoryID < tasks.Length; categoryID++)
                foreach (var task in tasks[categoryID])
                    if (task.enabled)
                        task.Disable();

            componentTasks.Clear();
        }

        private void Show ()
        {
            if (isLocked) //can not update the task panel if it is locked
                return;

            // Hide(); //hide currently active tasks

            lastUnusedTaskCodes.Clear();
            lastUnusedTaskCodes.AddRange(componentTasks.Keys); 

            foreach(var tracker in componentTasks.Values)
                tracker.ResetComponents();

            IDictionary<string, IEnumerable<IEntity>> selectedEntities = selectionMgr.GetEntitiesDictionary(EntityType.all, localPlayerFaction: true);

            foreach(KeyValuePair<string, IEnumerable<IEntity>> selectedEntityType in selectedEntities)
                FetchAllEntityComponentTasks(selectedEntityType.Value);

            foreach (string uncalledTaskCode in lastUnusedTaskCodes)
                DisableEntityComponentTask(uncalledTaskCode, onEmptyOnly: true);

            this.lastUnusedTaskCodes.Clear();
        }

        #region IEntityComponent Task Handling

        // Assumes 'entities' has at least one element and that all IEntity instances are of the same type (code)
        private void FetchAllEntityComponentTasks (IEnumerable<IEntity> entities)
        {
            IEntity refEntity = entities.First();
            foreach (IEntityComponent refEntityComponent in refEntity.EntityComponents.Values)
            {
                if (!refEntityComponent.OnTaskUIRequest(out IEnumerable<EntityComponentTaskUIAttributes> attributes, out IEnumerable<string> disabledTaskCodes))
                {
                    disabledTaskCodes = disabledTaskCodes.Concat(attributes.Select(attr => attr.data.code));
                    attributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
                }

                if (disabledTaskCodes != null)
                    foreach (string code in disabledTaskCodes)
                        DisableEntityComponentTask(code);

                if (attributes != null)
                {
                    IEnumerable<IEntityComponent> nextEntityComponents = entities.Select(entity => entity.EntityComponents[refEntityComponent.Code]);
                    foreach (EntityComponentTaskUIAttributes attr in attributes) //attempt to add them to the task panel
                        AddEntityComponentTask(nextEntityComponents, attr);
                }
            }
        }

        private void FetchSingleEntityComponentTasks (IEntityComponent component)
        {
            if (!component.OnTaskUIRequest(out IEnumerable<EntityComponentTaskUIAttributes> attributes, out IEnumerable<string> disabledTaskCodes))
            {
                disabledTaskCodes = disabledTaskCodes.Concat(attributes.Select(attr => attr.data.code));
                attributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            }

            if (disabledTaskCodes != null)
                foreach (string code in disabledTaskCodes)
                    DisableEntityComponentTask(code);

            if (attributes != null)
            {
                IEnumerable<IEntityComponent> nextEntityComponents = Enumerable.Repeat(component, 1);
                foreach (EntityComponentTaskUIAttributes attr in attributes) //attempt to add them to the task panel
                    AddEntityComponentTask(nextEntityComponents, attr);
            }
        }


        // Assumes 'components' are of the same type and that there is at least one element in the enumerable
        public void AddEntityComponentTask (IEnumerable<IEntityComponent> components, EntityComponentTaskUIAttributes attributes)
        {
            if(!attributes.data.enabled)
            {
                DisableEntityComponentTask(attributes.data.code);
                return;
            }

            IEntityComponent refComponent = components.First();

            switch(attributes.data.displayType) //depending on the display type of the task to add, check the fail conditions:
            {
                case EntityComponentTaskUIData.DisplayType.heteroMultipleSelection:

                    if (!refComponent.Entity.Selection.IsSelected)
                    {
                        DisableEntityComponentTask(attributes.data.code);
                        return;
                    }
                    break;

                case EntityComponentTaskUIData.DisplayType.singleSelection:

                    if (!selectionMgr.IsSelectedOnly(refComponent.Entity))
                    {
                        DisableEntityComponentTask(attributes.data.code);
                        return;
                    }
                    break;

                case EntityComponentTaskUIData.DisplayType.homoMultipleSelection:

                    if (!selectionMgr.GetEntitiesList(refComponent.Entity.Type, exclusiveType: true, localPlayerFaction: true).Any())
                    {
                        DisableEntityComponentTask(attributes.data.code);
                        return;
                    }
                    break;
            }

            //at this point, the fail conditions are checked and we are allowed to move on to adding/refreshing the task:

            //see if there's a tracker already for the task:
            if (!componentTasks.TryGetValue(attributes.data.code, out EntityComponentTaskUITracker tracker))
            { 
                //create a new task:
                var newTask = Add(
                    attributes.data.panelCategory,
                    attributes.data.forceSlot,
                    attributes.data.slotIndex);

                //create a new tracker for the task:
                tracker = new EntityComponentTaskUITracker(newTask);

                //add the new tracker to the dictionary:
                componentTasks.Add(attributes.data.code, tracker);
                tracker.ReloadTaskAttributes(attributes);
            }

            tracker.AddTaskComponents(components);

            lastUnusedTaskCodes.Remove(attributes.data.code);
        }

        /// <summary>
        /// Disables an active EntityComponentTaskUITracker instance that tracks a task.
        /// </summary>
        /// <param name="taskCode">Code of the task to be disabled..</param>
        /// <returns>True if a tracker is successfully found and removed, otherwise false.</returns>
        public bool DisableEntityComponentTask (string taskCode, bool onEmptyOnly = false)
        {
            //see if there's an active tracker that tracks the task with the given attributes.
            if (componentTasks.TryGetValue(taskCode, out EntityComponentTaskUITracker tracker))
            {
                if (!onEmptyOnly || (!tracker.EntityComponents.IsValid() || !tracker.EntityComponents.Any()))
                {
                    tracker.Disable();

                    componentTasks.Remove(taskCode);

                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
