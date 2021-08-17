using RTSEngine.EntityComponent;
using RTSEngine.Task;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    public struct EntityComponentPendingTaskUIData
    {
        public int queueIndex;
        public IPendingTasksHandler handler;
    }
}
