using System.Collections.Generic;
using System.Linq;

using RTSEngine.EntityComponent;
using UnityEngine;

namespace RTSEngine.UI
{
    /// <summary>
    /// Tracks the components that implement interfaces which allow to launch the a certain task on the panel (including IEntityComponent, IAwaitingTaskTargetPositionLaunchea certain task on the panel 
    /// </summary>
    public class EntityComponentTaskUITracker : IEntityComponentGroupDisplayer
    {
        public List<IEntityComponent> entityComponents;
        /// <summary>
        /// Components that implement IEntityComponent interface that share the tracked task.
        /// </summary>
        public IEnumerable<IEntityComponent> EntityComponents => entityComponents.Where(comp => comp.IsValid());

        private List<IEntityTargetComponent> entityTargetComponents;
        /// <summary>
        /// Components that implement IEntityTargetComponent interface that share the tracked task.
        /// </summary>
        public IEnumerable<IEntityTargetComponent> EntityTargetComponents => entityTargetComponents.Where(comp => comp.IsValid());

        /// <summary>
        /// TaskUI instance tracked by the EntityComponentTaskUITracker.
        /// </summary>
        public ITaskUI<EntityComponentTaskUIAttributes> Task { private set; get; }

        /// <summary>
        /// Constructor for a new instance of the EntityComponentTaskUITracker class.
        /// </summary>
        /// <param name="component">A component that implements the IEntityComponent interface that activated a new task in the panel.</param>
        /// <param name="taskUI">TaskUI instance of the active task.</param>
        /// <param name="panelCategory">Task panel category ID of the active task.</param>
        public EntityComponentTaskUITracker (ITaskUI<EntityComponentTaskUIAttributes> taskUI)
        {
            ResetComponents();

            Task = taskUI;
        }

        public void ResetComponents()
        {
            entityComponents = new List<IEntityComponent>();
            entityTargetComponents = new List<IEntityTargetComponent>();
        }

        /// <summary>
        /// Refreshes the tracked task.
        /// </summary>
        /// <param name="newAttributes">New attributes to assign to the tracked task.</param>
        public void ReloadTask (EntityComponentTaskUIAttributes attributes, IEntityComponent component)
        {
            if (!entityComponents.Contains(component))
            {
                entityComponents.Add(component);
                entityTargetComponents.Add(component as IEntityTargetComponent);
            }

            Task.Reload(new EntityComponentTaskUIAttributes
            {
                data = attributes.data,

                sourceTracker = this,
                launchOnce = attributes.launchOnce,

                locked = attributes.locked,
                lockedData = attributes.lockedData,

                tooltipText = attributes.tooltipText
            });
        }

        /// <summary>
        /// Disables tracking components and their active task.
        /// </summary>
        public void Disable ()
        {
            entityComponents = null;
            entityTargetComponents = null;

            Task?.Disable();
        }
    }
}
