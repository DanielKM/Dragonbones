using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Task
{
    public struct PendingTask
    {
        public IPendingTaskEntityComponent sourceComponent;
        //this is used to identify the pending task in the source component when it is completed.
        public int sourceID;

        public bool playerCommand;

        public IEntityComponentTaskInput sourceTaskInput;
    }
}
