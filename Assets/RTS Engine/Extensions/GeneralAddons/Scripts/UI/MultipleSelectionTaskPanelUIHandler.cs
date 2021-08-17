using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Entities;

namespace RTSEngine.UI
{
    public class MultipleSelectionTaskPanelUIHandler : BaseTaskPanelUIHandler<MultipleSelectionTaskUIAttributes>
    {
        #region Attributes
        [SerializeField, EnforceType(sameScene: true), Tooltip("Parent game object of the active multiple selection tasks.")]
        private GridLayoutGroup panel = null;

        [SerializeField, Tooltip("Multiple selection UI data parameters.")]
        private MultipleSelectionTaskUIData taskData = new MultipleSelectionTaskUIData { description = "Deselect", tooltipEnabled = true };

        [SerializeField, Tooltip("If the multiple selected entities is over this threshold, each type of the selected entities will have one task with the selected amount displayed on the task.")]
        private int entityTypeSelectionTaskThreshold = 10; 

        // Each created multiple selection task is registered in this list.
        private List<ITaskUI<MultipleSelectionTaskUIAttributes>> tasks = null;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            tasks = new List<ITaskUI<MultipleSelectionTaskUIAttributes>>();

            if (!logger.RequireValid(panel,
                $"[{GetType().Name}] The 'Panel' field must be assigned!"))
                return;

            globalEvent.EntitySelectedGlobal += HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal += HandleEntitySelectionUpdate;

            Hide();
        }
        #endregion

        #region Disabling UI Panel
        public override void Disable()
        {
            Hide();

            globalEvent.EntitySelectedGlobal -= HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal -= HandleEntitySelectionUpdate;
        }
        #endregion

        #region Handling Event: Entity Selected/Deselected
        private void HandleEntitySelectionUpdate(IEntity entity, EventArgs e)
        {
            if (selectionMgr.Count > 1)
                Show();
            else
                Hide();
        }
        #endregion

        #region Adding Tasks
        private ITaskUI<MultipleSelectionTaskUIAttributes> Add()
        {
            // Find the first available (disabled) pending task slot to use next
            foreach (var task in tasks)
                if (!task.enabled)
                    return task;

            // None found? create one!
            return Create(tasks, panel.transform);
        }
        #endregion

        #region Hiding/Displaying Panel
        private void Hide()
        {
            foreach (var task in tasks)
                if (task.IsValid() && task.enabled)
                    task.Disable();
        }

        private void Show()
        {
            Hide();

            IEnumerable<IEnumerable<IEntity>> entitySets = null;

            // If the amount of selected units is higher than the maximum allowed multiple selection tasks that represent each entity individually:
            if (selectionMgr.Count >= entityTypeSelectionTaskThreshold)
            {
                // Get the selected units in a form of a dictionary with each selected entity's code as key and the selected entities of each type in a list as the value.
                entitySets = selectionMgr
                    .GetEntitiesDictionary(EntityType.all, localPlayerFaction:false)
                    .Values;
            }
            else
            {
                entitySets = selectionMgr
                    .GetEntitiesList(EntityType.all, true, false)
                    .Select(entity => Enumerable.Repeat(entity, 1));
            }
            
            foreach(IEnumerable<IEntity> set in entitySets)
            {
                var newTask = Add();
                newTask.Reload(new MultipleSelectionTaskUIAttributes
                {
                    data = taskData,
                    selectedEntities = set.ToArray()
                });
            }

            panel.gameObject.SetActive(true);
        }
        #endregion
    }
}
