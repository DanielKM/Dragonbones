using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    [System.Serializable]
    public struct MultipleSelectionTaskUIData
    {
        [Tooltip("Show a description of the task in the tooltip when the mouse hovers over the multiple selection task?")]
        public bool tooltipEnabled;
        [Tooltip("Description of the multiple selection task that will appear in the multiple selection panel.")]
        public string description;
    }
}
