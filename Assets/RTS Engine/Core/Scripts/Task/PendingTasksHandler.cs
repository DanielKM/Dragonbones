using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.UI;
using RTSEngine.EntityComponent;
using RTSEngine.Determinism;
using RTSEngine.Logging;

namespace RTSEngine.Event
{
}

namespace RTSEngine.Task
{
    public enum PendingTaskState { none, added, started, cancelled, completed }

    public class PendingTasksHandler : MonoBehaviour, IEntityPreInitializable, IPendingTasksHandler
    {
        #region Attributes
        // When the pending task handler is locked, it means the first task in the queue can not be completed due to missing requirements
        // This allows to avoid refreshing the UI elements every time the requirements are checked and they are still not met.
        private bool isLocked = false;

        public IEntity Entity { private set; get; }

        [SerializeField, Tooltip("Total amount of allowed simultaneous pending tasks at any given time for the entity."), Min(0)]
        private int maxCount = 4;

        private List<PendingTask> queue = new List<PendingTask>();
        public IEnumerable<PendingTask> Queue => queue;
        public int QueueCount => queue.Count;

        // Timer of the first task in the queue, when it is through, the first task in queue is completed and the next one is loaded.
        public TimeModifiedTimer QueueTimer { private set; get; }
        public float QueueTimerValue => QueueTimer.CurrValue;

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IPendingTasksHandler, PendingTaskEventArgs> PendingTaskStateUpdated;
        private void RaisePendingTaskStateUpdated(PendingTaskEventArgs args)
        {
            var handler = PendingTaskStateUpdated;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();

            this.Entity = entity;

            isLocked = false;
            QueueTimer = new TimeModifiedTimer();
        }

        public void Disable()
        {
            CancelAll();
        }
        #endregion

        #region Adding Pending Tasks
        public bool Add(PendingTask newPendingTask)
        {
            if (queue.Count >= maxCount)
                return false;

            queue.Add(newPendingTask);
            newPendingTask.sourceTaskInput.OnStart();

            RaisePendingTaskStateUpdated(new PendingTaskEventArgs(
                data: newPendingTask,
                pendingQueueID: queue.Count - 1,
                state: PendingTaskState.added
            ));
            globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);

            if (queue.Count == 1)
                StartNext();

            return true;
        }
        #endregion

        #region Handling Pending Tasks Progress
        private void StartNext()
        {
            isLocked = false;

            if (queue.Count == 0)
                return;

            QueueTimer.Reload(queue[0].sourceTaskInput.Data.reloadTime);

            RaisePendingTaskStateUpdated(new PendingTaskEventArgs(
                data: queue[0],
                pendingQueueID: 0,
                state: PendingTaskState.started
            ));
        }

        private void Update()
        {
            if (!Entity.CanLaunchTask
                || QueueCount == 0)
                return;

            if (QueueTimer.ModifiedDecrease())
                TryCompleteCurrent();
        }

        private bool TryCompleteCurrent()
        {
            ErrorMessage launchError = queue[0].sourceTaskInput.CanComplete();
            if(launchError == ErrorMessage.none)
            {
                CompleteCurrent();
                return true;
            }

            // Cancel task is it belongs to a NPC faction
            if(RTSHelper.IsNPCFaction(Entity))
                CancelByQueueID(0);

            else if (!isLocked)
            {
                if (Entity.IsLocalPlayerFaction())
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = launchError,

                        source = Entity
                    });

                globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);

                isLocked = true;
            }

            return false;
        }

        public void CompleteCurrent()
        {
            if (queue.Count == 0)
                return;

            PendingTask completedTask = queue[0];
            queue.RemoveAt(0);

            completedTask.sourceTaskInput.OnComplete();
            completedTask.sourceComponent.OnPendingTaskCompleted(completedTask);

            RaisePendingTaskStateUpdated(new PendingTaskEventArgs(
                data: completedTask,
                pendingQueueID: -1,
                state: PendingTaskState.completed
            ));

            StartNext();

            globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);
        }
        #endregion

        #region Cancelling Pending Tasks
        public void CancelAll()
        {
            while (queue.Count > 0)
                CancelByQueueID(0);
        }

        public void CancelByQueueID(int pendingTaskIndex)
        {
            if (pendingTaskIndex < 0 || pendingTaskIndex >= QueueCount)
                return;

            CancelInternal(queue[pendingTaskIndex]);
            queue.RemoveAt(pendingTaskIndex);

            globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);

            if (pendingTaskIndex == 0)
                StartNext();
        }

        public void CancelBySourceID(IPendingTaskEntityComponent sourceComponnet, int sourceID)
        {
            //in case the RemoveAll removes the first pending task in the queue, the queue must start the next one in case there is one.
            PendingTask lastFirst = queue.FirstOrDefault();

            queue.RemoveAll(pendingTask => 
            {
                if (pendingTask.sourceComponent == sourceComponnet && pendingTask.sourceID == sourceID)
                {
                    CancelInternal(pendingTask);
                    return true;
                }
                return false; 
            });

            globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);

            PendingTask newFirst = queue.FirstOrDefault();
            if (!lastFirst.Equals(newFirst))
                StartNext();
        }

        private void CancelInternal(PendingTask task)
        {
            task.sourceTaskInput.OnCancel();
            task.sourceComponent.OnPendingTaskCancelled(task);

            RaisePendingTaskStateUpdated(new PendingTaskEventArgs(
                data: task,
                pendingQueueID: -1,
                state: PendingTaskState.completed
            ));
        }
        #endregion

        #region Handling UI
        public bool OnPendingTaskUIRequest(out IEnumerable<EntityComponentPendingTaskUIAttributes> taskUIAttributes)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentPendingTaskUIAttributes>();

            if (!Entity.CanLaunchTask
                || !RTSHelper.IsLocalPlayerFaction(Entity))
                return false;

            int currIndex = -1;
            int incrementIndex(ref int index) { index++; return index; }

            taskUIAttributes = Queue
                .Where(pendingTask => pendingTask.sourceComponent.IsActive)
                .Select(pendingTask => new EntityComponentPendingTaskUIAttributes
                {
                    data = pendingTask.sourceTaskInput.Data,

                    pendingData = new EntityComponentPendingTaskUIData
                    {
                        queueIndex = incrementIndex(ref currIndex),
                        handler = this
                    },

                    locked = pendingTask.sourceTaskInput.CanComplete() != ErrorMessage.none,
                    lockedData = pendingTask.sourceTaskInput.MissingRequirementData,
                });

            return true;
        }
        #endregion
    }
}
