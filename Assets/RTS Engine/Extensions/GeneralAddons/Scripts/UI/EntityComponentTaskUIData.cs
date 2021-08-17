using RTSEngine.Task;
using UnityEngine;

/* EntityComponentTaskUI script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.UI
{
    /// <summary>
    /// Holds information regarding the UI elements of a task that belongs to a component that implements IEntityComponent interface and attached to an Entity instance.
    /// </summary>
    [System.Serializable]
    public struct EntityComponentTaskUIData
    {
        /// <summary>
        /// Defines the different display types for the task associated with the EntityComponentTaskUI struct.
        /// singleSelection: Only display task if the entity is the only one selected.
        /// homoMultipleSelection: Only display task if only entities of the same type are selected.
        /// heteroMultipleSelection: Display task as long as the source entity is selected.
        /// </summary>
        public enum DisplayType {singleSelection, homoMultipleSelection, heteroMultipleSelection }

        [Tooltip("Unique code for each task.")]
        public string code;

        [Tooltip("When disabled, the associated task can not be displayed in the task panel.")]
        public bool enabled;

        [Tooltip("Selection conditions to display the associated task.")]
        public DisplayType displayType;

        [Tooltip("The sprite to be used for the task's icon.")]
        public Sprite icon;
        [Tooltip("The category of the UI task panel where the task will be placed at.")]
        public int panelCategory;
        [Tooltip("Enable to force the task to be drawn on a specific slot of the panel category.")]
        public bool forceSlot;
        [Tooltip("Index of the slot to draw the task in."), Min(0)]
        public int slotIndex;

        [Tooltip("Show a description of the task in the tooltip when the mouse hovers over the task?")]
        public bool tooltipEnabled;
        [Tooltip("Description of the task that will appear in the task panel's tooltip.")]
        public string description;
        [Tooltip("Hide tooltip (if it was enabled) when the task is clicked?")]
        public bool hideTooltipOnClick;

        [Tooltip("In case reload time is enabled (pending task), this is its duration. Only valid when the task launcher allows for pending tasks."), Min(0)]
        public float reloadTime;

        [Tooltip("When the task is assigned as the awaiting task, this represents the icon that will be used as the mouse cursor's texture. If not assigned, the main icon is used as the cursor's texture.")]
        public TaskCursorData cursor;
    }
}
