using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.UI;
using System.Collections.Generic;

namespace RTSEngine.Task
{
    public interface IPendingTasksHandler
    {
        IEntity Entity { get; }

        IEnumerable<PendingTask> Queue { get; }
        int QueueCount { get; }
        float QueueTimerValue { get; }

        event CustomEventHandler<IPendingTasksHandler, PendingTaskEventArgs> PendingTaskStateUpdated;

        bool Add(PendingTask newPendingTask);

        void CancelByQueueID(int pendingTaskIndex);
        void CancelBySourceID(IPendingTaskEntityComponent sourceComponent, int sourceID);
        void CancelAll();

        void CompleteCurrent();

        bool OnPendingTaskUIRequest(out IEnumerable<EntityComponentPendingTaskUIAttributes> taskUIAttributes);
    }
}