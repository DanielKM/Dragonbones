using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    [System.Serializable]
    public struct TargetEntityFinderData
    {
        [Tooltip("Is it possible to search for potential targets?")]
        public bool enabled;
        [Tooltip("How often does the search get initiated?"), Min(0.0f)]
        public float reloadTime; 
        [Tooltip("How far does the search go?"), Min(0.0f)]
        public float range;
        [Tooltip("Only allow to search for a target if the source entity is in idle state?")]
        public bool idleOnly;
    }
}
