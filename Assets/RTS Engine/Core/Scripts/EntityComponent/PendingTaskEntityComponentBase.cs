using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Task;
using RTSEngine.UI;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Utilities;

namespace RTSEngine.EntityComponent
{
    public abstract class PendingTaskEntityComponentBase : MonoBehaviour, IPendingTaskEntityComponent
    {
        #region Class Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        /*
         * Action types and their parameters:
         * launch: target.position.x => task ID/index of the task to launch.
         * complete: target.position.x => task ID/index of the task to complete.
         * */
        public enum ActionType : byte { launch, complete }

        [SerializeField, Tooltip("Code to identify this component, unique within the entity")]
        private string code = "new_component_code";
        public string Code => code;

        protected IFactionEntity factionEntity { private set; get; }
        public IEntity Entity => factionEntity;

        [SerializeField, Tooltip("Is the component enabled by default?")]
        private bool isActive = true;
        public bool IsActive => isActive;

        // Used by the child class to specify the tasks array (since the type of the task might be different depending on the child class):
        public abstract IReadOnlyList<IEntityComponentTaskInput> Tasks { get; }

        // Game services
        protected IGameManager gameMgr { private set; get; } 
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGameUITextDisplayManager textDisplayer { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IPendingTaskEntityComponent, PendingTaskEventArgs> PendingTaskAction;
        private void RaisePendingTaskAction(PendingTaskEventArgs args)
        {
            var handler = PendingTaskAction;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.textDisplayer = gameMgr.GetService<IGameUITextDisplayManager>(); 

            this.factionEntity = entity as IFactionEntity;

            if (!logger.RequireValid(entity.PendingTasksHandler,
                $"[{GetType().Name} - {entity.Code}] This component requires a component that implements '{typeof(IPendingTasksHandler).Name}' interface to be attached to the source entity!",
                source: entity))
                return;

            OnInit();

            globalEvent.RaisePendingTaskEntityComponentAdded(this);
        }

        protected virtual void OnInit() { }

        public void Disable()
        {
            OnDisabled();

            globalEvent.RaisePendingTaskEntityComponentRemoved(this);
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Handling Component Upgrade
        public virtual void HandleComponentUpgrade(IEntityComponent sourceEntityComponent) { }
        #endregion

        #region Activating/Deactivating Component
        public ErrorMessage SetActive(bool active, bool playerCommand) => RTSHelper.SetEntityComponentActive(this, active, playerCommand);

        public ErrorMessage SetActiveLocal(bool active, bool playerCommand)
        {
            isActive = active;

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Actions
        public ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            return RTSHelper.LaunchEntityComponentAction(this, actionID, target, playerCommand);
        }

        public ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            if (!Entity.CanLaunchTask)
                return ErrorMessage.taskSourceCanNotLaunch;

            switch ((ActionType)actionID)
            {
                case ActionType.launch:
                    return LaunchTaskActionLocal((int)target.position.x, playerCommand);
                case ActionType.complete:
                    return CompleteTaskActionLocal((int)target.position.x, playerCommand);
                default:
                    return ErrorMessage.undefined;
            }
        }
        #endregion

        #region Handling Launch Task Action
        public ErrorMessage LaunchTaskAction (int taskID, bool playerCommand)
        {
            if (!RTSHelper.HasAuthority(Entity))
                return ErrorMessage.noAuthority;
            else if (taskID < 0 || taskID >= Tasks.Count)
                return ErrorMessage.invalid;

            ErrorMessage errorMessage = Tasks[taskID].CanStart();
            return errorMessage != ErrorMessage.none
                ? errorMessage
                : LaunchAction(
                    (byte)ActionType.launch,
                    new TargetData<IEntity>
                    {
                        position = new Vector3(taskID, 0.0f, 0.0f)
                    },
                    playerCommand);
        }

        private ErrorMessage LaunchTaskActionLocal(int taskID, bool playerCommand)
        {
            var task = Tasks[taskID];
            PendingTask newPendingTask = new PendingTask
            {
                sourceComponent = this,
                sourceID = taskID,

                playerCommand = playerCommand,

                sourceTaskInput = task
            };
            Entity.PendingTasksHandler.Add(newPendingTask);

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            RaisePendingTaskAction(new PendingTaskEventArgs(data: newPendingTask, state: PendingTaskState.added));

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Complete Action
        private ErrorMessage CompleteTaskAction(int taskID, bool playerCommand)
        {
            if (!RTSHelper.HasAuthority(Entity))
                return ErrorMessage.noAuthority;
            else if (taskID < 0 || taskID >= Tasks.Count)
                return ErrorMessage.invalid;

            return LaunchAction(
                (byte)ActionType.complete,
                new TargetData<IEntity>
                {
                    position = new Vector3(taskID, 0.0f, 0.0f)
                },
                playerCommand);
        }

        protected abstract ErrorMessage CompleteTaskActionLocal(int taskID, bool playerCommand);
        #endregion

        #region Task UI
        public virtual bool OnTaskUIRequest(out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            disabledTaskCodes = Enumerable.Empty<string>();

            if (!Entity.CanLaunchTask
                || !IsActive
                || !RTSHelper.IsLocalPlayerFaction(Entity))
                return false;

            //for upgrade tasks, we show tasks that do not have required conditions to launch but make them locked.
            taskUIAttributes = Tasks
                .Where(task => task.IsEnabled)
                .Select(task => new EntityComponentTaskUIAttributes
                {
                    data = task.Data,

                    locked = task.CanStart() != ErrorMessage.none,
                    lockedData = task.MissingRequirementData,

                    tooltipText = GetTooltipText(task)
                });

            return true;
        }

        protected abstract string GetTooltipText(IEntityComponentTaskInput taskInput);

        public bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            return LaunchTaskAction(
                RTSHelper.FindIndex(Tasks, nextTask => nextTask.Data.code == taskAttributes.data.code),
                true) == ErrorMessage.none;
        }
        #endregion

        #region Handling Pending Task
        public void OnPendingTaskCompleted(PendingTask pendingTask)
        {
            CompleteTaskAction(pendingTask.sourceID, pendingTask.playerCommand);

            RaisePendingTaskAction(new PendingTaskEventArgs(data: pendingTask, state: PendingTaskState.completed));
        }

        public void OnPendingTaskCancelled(PendingTask pendingTask)
        {
            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            RaisePendingTaskAction(new PendingTaskEventArgs(data: pendingTask, state: PendingTaskState.cancelled));
        }
        #endregion
    }
}
