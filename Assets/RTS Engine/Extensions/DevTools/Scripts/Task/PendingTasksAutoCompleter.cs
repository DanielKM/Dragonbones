using System;
using System.Collections.Generic;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Task;

namespace RTSEngine.DevTools.Task
{
    public class PendingTasksAutoCompleter : DevToolComponentBase
    {
        private List<IPendingTasksHandler> pendingTaskHandlers = null;

        protected override void OnPreRunInit()
        {
            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;
            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;

            UpdateLabel();

            pendingTaskHandlers = new List<IPendingTasksHandler>();
        }

        private void OnDestroy()
        {
            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;
            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;
        }

        private void HandleEntityInitiatedGlobal(IEntity entity, EventArgs args)
        {
            if (entity.PendingTasksHandler.IsValid())
            {
                pendingTaskHandlers.Add(entity.PendingTasksHandler);
                entity.PendingTasksHandler.PendingTaskStateUpdated += HandlePendingTaskStateUpdated;
            }
        }

        private void HandleEntityDeadGlobal(IEntity entity, DeadEventArgs args)
        {
            if (entity.PendingTasksHandler.IsValid())
            {
                pendingTaskHandlers.Remove(entity.PendingTasksHandler);
                entity.PendingTasksHandler.PendingTaskStateUpdated -= HandlePendingTaskStateUpdated;
            }
        }

        private void HandlePendingTaskStateUpdated(IPendingTasksHandler sender, PendingTaskEventArgs args)
        {
            if (!IsActive
                || !RoleFilter.IsAllowed(sender.Entity.Slot))
                return;

            switch(args.State)
            {
                case PendingTaskState.added:
                    sender.CompleteCurrent();
                    break;
            }
        }

        public override void OnUIInteraction() 
        {
            IsActive = !IsActive;

            if(IsActive)
            {
                foreach (IPendingTasksHandler handler in pendingTaskHandlers)
                {
                    if (RoleFilter.IsAllowed(handler.Entity.Slot))
                        while (handler.QueueCount > 0)
                            handler.CompleteCurrent();
                }
            }

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (Label)
            {
                string colorCode = IsActive ? "green" : "red";
                Label.text = $"<color={colorCode}>Auto-Complete Tasks</color>";
            }
        }
    }
}
