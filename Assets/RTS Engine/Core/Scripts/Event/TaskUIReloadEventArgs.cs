using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Event
{
    public class TaskUIReloadEventArgs : EventArgs
    {
        //when true, this triggers all drawn tasks to be hidden then redrawn again.
        public bool ReloadAll { private set; get; }

        public TaskUIReloadEventArgs()
        {
            this.ReloadAll = false;
        }

        public TaskUIReloadEventArgs(bool reloadAll)
        {
            this.ReloadAll = reloadAll;
        }
    }
}
