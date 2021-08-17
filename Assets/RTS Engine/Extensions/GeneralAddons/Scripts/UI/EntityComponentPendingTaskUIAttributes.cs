using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    public struct EntityComponentPendingTaskUIAttributes : ITaskUIAttributes
    {
        public EntityComponentTaskUIData data;

        public EntityComponentPendingTaskUIData pendingData;

        public bool locked;
        public EntityComponentLockedTaskUIData lockedData;
    }
}
