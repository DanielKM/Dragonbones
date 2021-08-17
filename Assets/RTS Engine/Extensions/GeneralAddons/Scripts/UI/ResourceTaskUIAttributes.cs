using RTSEngine.ResourceExtension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    public struct ResourceTaskUIAttributes : ITaskUIAttributes
    {
        public IFactionResourceHandler resourceHandler; 

        public bool tooltipEnabled;
        public string tooltipText;

        public Color maxCapacityColor;
    }
}
