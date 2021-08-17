using System.Collections.Generic;

using UnityEngine;

using RTSEngine.UI;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Effect;
using RTSEngine.Determinism;
using RTSEngine.Game;
using RTSEngine.Audio;
using RTSEngine.Logging;
using RTSEngine.Task;
using RTSEngine.Movement;
using RTSEngine.Selection;
using System;
using RTSEngine.Utilities;

namespace RTSEngine.EntityComponent
{
    public abstract class FactionEntityTargetComponent<T> : MonoBehaviour, IEntityPostInitializable, IEntityTargetComponent where T : IEntity
    {
        #region Class Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        public bool IsInitialized { private set; get; } = false;

        [SerializeField, Tooltip("Code that defines this component, uniquely within the entity.")]
        private string code = "comp_code";
        /// <summary>
        /// Code that defines the entity component, uniquely only within the entity.
        /// </summary>
        public string Code => code;

        protected IFactionEntity factionEntity { private set; get; }
        public IEntity Entity => factionEntity;

        [SerializeField, Tooltip("Is the component enabled by default?")]
        private bool isActive = true;
        public bool IsActive => isActive;

        [SerializeField, Tooltip("The active component with the lowest value will be considered for the right mouse click target set.")]
        private int priority = 0;
        public int Priority => priority;

        [SerializeField, Tooltip("Enable to require the entity where this component is attached to be idle when this component has an active target.")]
        private bool requireIdleEntity = true;
        public bool RequireIdleEntity => requireIdleEntity;
        public abstract bool IsIdle { get; }

        /// <summary>
        /// The instance that is being actively targetted.
        /// </summary>
        public TargetData<T> Target { get; protected set; }

        public virtual bool HasTarget => Target.instance.IsValid();


        [SerializeField, Tooltip("Set the settings for allowing the entity to launch this component automatically.")]
        private TargetEntityFinderData targetFinderData = new TargetEntityFinderData { enabled = true, idleOnly = true, range = 10.0f, reloadTime = 5.0f };
        protected TargetEntityFinderData TargetFinderData => targetFinderData;
        protected TargetEntityFinder<T> TargetFinder { private set; get; } = null;

        [SerializeField, Tooltip("Defines information used to display a task to set the target of this component in the task panel, when the faction entity is selected.")]
        private EntityComponentTaskUIAsset setTargetTaskUI = null;
        public EntityComponentTaskUIAsset SetTargetTaskUI => setTargetTaskUI;

        [SerializeField, Tooltip("What audio clip to play when the faction entity is ordered to perform the task of this component?")]
        private AudioClipFetcher orderAudio = new AudioClipFetcher();
        public AudioClip OrderAudio => orderAudio.Fetch();

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; } 
        protected ITaskManager taskMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit (IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.factionEntity = entity as IFactionEntity;

            if (!logger.RequireTrue(!IsInitialized,
              $"[{GetType().Name} - {factionEntity.Code}] Component already initialized! It is not supposed to be initialized again! Please retrace and report!"))
                return; 

            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.mouseSelector = gameMgr.GetService<IMouseSelector>(); 
            this.taskMgr = gameMgr.GetService<ITaskManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();

            TargetFinder = new TargetEntityFinder<T>(gameMgr, source: this, center: factionEntity.transform, data: targetFinderData);

            OnInit();

            factionEntity.FactionUpdateComplete += HandleFactionEntityFactionUpdateComplete;

            IsInitialized = true;
        }

        protected virtual void OnInit() { }

        public void Disable()
        {
            Stop();
            if(TargetFinder.IsValid())
                TargetFinder.Enabled = false;

            factionEntity.FactionUpdateComplete -= HandleFactionEntityFactionUpdateComplete;

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Handling Faction Update Complete Event
        private void HandleFactionEntityFactionUpdateComplete(IEntity sender, FactionUpdateArgs args)
        {
            Stop();
        }
        #endregion

        #region Handling Component Upgrade
        public void HandleComponentUpgrade (IEntityComponent sourceEntityComponent)
        {
            FactionEntityTargetComponent<T> sourceFactionEntityTargetComponent = sourceEntityComponent as FactionEntityTargetComponent<T>;
            if (!sourceFactionEntityTargetComponent.IsValid())
                return;

            if (sourceFactionEntityTargetComponent.HasTarget)
            {
                TargetData<T> lastTarget = sourceFactionEntityTargetComponent.Target;
                sourceFactionEntityTargetComponent.Disable();

                SetTarget(lastTarget, false);
            }

            OnComponentUpgraded(sourceFactionEntityTargetComponent);
        }

        protected virtual void OnComponentUpgraded(FactionEntityTargetComponent<T> sourceFactionEntityTargetComponent) { }
        #endregion

        #region Activating/Deactivating Component
        public ErrorMessage SetActive(bool active, bool playerCommand) => RTSHelper.SetEntityComponentActive(this, active, playerCommand);

        public ErrorMessage SetActiveLocal(bool active, bool playerCommand)
        {
            isActive = active;

            if(TargetFinder.IsValid())
                TargetFinder.Enabled = isActive;

            if (!isActive)
                Stop();

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);

            OnActiveStatusUpdated();

            return ErrorMessage.none;
        }

        protected virtual void OnActiveStatusUpdated() { }
        #endregion

        #region Stopping
        protected virtual bool CanStop() => true;

        public void Stop()
        {
            if (!CanStop() && !HasTarget) //if the component does not have an active target nor it is in progress
                return; //do not proceed

            audioMgr.StopSFX(factionEntity.AudioSourceComponent);

            globalEvent.RaiseEntityComponentTargetStopGlobal(this, new TargetDataEventArgs(Target));

            TargetData<T> lastTarget = Target;
            Target = new TargetData<T> { instance = default, position = Target.position, opPosition = Target.opPosition };

            OnStop(lastTarget);
        }

        protected virtual void OnStop(TargetData<T> lastTarget) { }
        #endregion

        #region Handling Actions
        public virtual ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand) => ErrorMessage.undefined;

        public virtual ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand) => ErrorMessage.undefined;
        #endregion

        #region Searching/Updating Target
        public virtual bool CanSearch => true;

        public abstract ErrorMessage IsTargetValid(TargetData<IEntity> testTarget, bool playerCommand);

        public abstract bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target);

        public virtual ErrorMessage SetTarget (TargetData<IEntity> newTarget, bool playerCommand)
        {
            return inputMgr.SendInput(
                new CommandInput()
                {
                    sourceMode = (byte)InputMode.entity,
                    targetMode = (byte)InputMode.setComponentTarget,

                    targetPosition = newTarget.position,
                    opPosition = newTarget.opPosition,

                    code = Code,
                    playerCommand = playerCommand
                },
                source: factionEntity,
                target: newTarget.instance);
        }

        public virtual ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, bool playerCommand)
        {
            if (!factionEntity.CanLaunchTask) 
                return ErrorMessage.taskSourceCanNotLaunch;

            ErrorMessage errorMsg;
            if ((errorMsg = IsTargetValid(newTarget, playerCommand)) != ErrorMessage.none)
            {
                if (playerCommand && RTSHelper.IsLocalPlayerFaction(factionEntity))
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = Entity,
                        target = newTarget.instance
                    });

                return errorMsg;
            }

            bool sameTarget = newTarget.instance == Target.instance as IEntity && newTarget.instance.IsValid();

            // If this component requires the entity to be idle to run then set the entity to idle before assigning the new target
            if(requireIdleEntity)
                factionEntity.SetIdle(sameTarget ? this : null);

            OnTargetPreLocked(playerCommand, newTarget, sameTarget);

            Target = newTarget;

            if (playerCommand && Target.instance.IsValid() && factionEntity.IsLocalPlayerFaction())
                mouseSelector.FlashSelection(Target.instance, factionEntity.IsFriendlyFaction(Target.instance));

            OnTargetPostLocked(playerCommand, sameTarget);

            return ErrorMessage.none;
        }

        protected virtual void OnTargetPreLocked(bool playerCommand, TargetData<IEntity> newTarget, bool sameTarget) { }

        protected virtual void OnTargetPostLocked (bool playerCommand, bool sameTarget) { }
        #endregion

        #region Task UI
        public virtual bool OnTaskUIRequest(
            out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes)
        {
            return RTSHelper.OnSingleTaskUIRequest(
                this,
                out taskUIAttributes,
                out disabledTaskCodes,
                setTargetTaskUI);
        }

        public virtual bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes) 
        {
            if (SetTargetTaskUI.IsValid() && taskAttributes.data.code == SetTargetTaskUI.Key)
            {
                taskMgr.AwaitingTask.Enable(taskAttributes);
                return true;
            }

            return false;
        }
        #endregion

    }
}
