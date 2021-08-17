using RTSEngine.Task;
using System;

namespace RTSEngine.Event
{
    public class PendingTaskEventArgs : EventArgs
    {
        public PendingTask Data { private set; get; }
        public int pendingQueueID { private set; get; }
        public PendingTaskState State { private set; get; }

        public PendingTaskEventArgs(PendingTask data, PendingTaskState state, int pendingQueueID = -1)
        {
            this.Data = data;
            this.pendingQueueID = pendingQueueID;
            this.State = state;
        }
    }
}
