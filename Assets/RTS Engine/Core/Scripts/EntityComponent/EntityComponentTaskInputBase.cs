using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.ResourceExtension;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI;
using UnityEngine.Serialization;

namespace RTSEngine.EntityComponent
{
    public abstract class EntityComponentTaskInputBase<T> : IEntityComponentTaskInput
    {
        [HideInInspector]
        public string taskTitle = "Prefab Missing";

        public bool IsInitialized { private set; get; } = false;
        public bool IsEnabled { private set; get; } = false;

        [Space(), SerializeField, Tooltip("Prefab that represents the task.")]
        private GameObject prefabObject = null;
        public GameObject PrefabObject => prefabObject;
        public T Prefab => prefabObject.GetComponent<T>();

        [SerializeField, Tooltip("Defines the data used to display the task."), FormerlySerializedAs("asset")]
        private EntityComponentTaskUIAsset taskUI = null;
        public EntityComponentTaskUIData Data => taskUI.Data;

        [Space(), SerializeField, Tooltip("Resources required to launch the task.")]
        protected ResourceInput[] requiredResources = new ResourceInput[0];
        public IEnumerable<ResourceInput> RequiredResources => requiredResources.ToList();

        [Space(), SerializeField, Tooltip("Input the faction units/buildings required to create this faction entity.")]
        protected FactionEntityRequirement[] factionEntityRequirements = new FactionEntityRequirement[0];
        public IEnumerable<FactionEntityRequirement> FactionEntityRequirements => factionEntityRequirements.ToList();

        [Space(), SerializeField, Tooltip("How would the task icon look in the task panel in case requirements are not met?")]
        private EntityComponentLockedTaskUIData missingRequirementData = new EntityComponentLockedTaskUIData { color = new Color (255, 76, 76, 1.0f), icon = null };
        public EntityComponentLockedTaskUIData MissingRequirementData => missingRequirementData;

        /// <summary>
        /// Amounts of times the task has been launched.
        /// </summary>
        public int LaunchTimes { private set; get; }
        /// <summary>
        /// Current pending amount of active instances of the task.
        /// </summary>
        public int PendingAmount { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }

        public IEntityComponent EntityComponent { private set; get; }
        public IEntity Entity => EntityComponent.Entity;

        public void Init(IEntityComponent entityComponent, IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.logger = this.gameMgr.GetService<IGameLoggingService>();

            if (!this.logger.RequireTrue(!IsInitialized,
                $"[{GetType().Name}] Input task has been already initialized. Unable to initialize again."))
                return;

            this.resourceMgr = this.gameMgr.GetService<IResourceManager>();

            this.EntityComponent = entityComponent;

            if (!this.logger.RequireValid(this.Entity,
                $"[{GetType().Name}] Input task must be initialized with a valid instance of '{typeof(IEntity).Name}' as the source.")
                || !logger.RequireValid(this.Prefab,
                $"[{GetType().Name}] The 'Prefab Object' field must be assigned to a prefab that has a component of type '{typeof(T).Name}' attached to it.")
                || !this.logger.RequireValid(taskUI,
                $"[{GetType().Name} - Entity: {this.Entity.Code} - Faction ID: {this.Entity.FactionID}] Input tasks must have the 'Asset' field assigned!"))
                return;

            IsInitialized = true;
            IsEnabled = true;

            LaunchTimes = 0;

            OnInit();
        }

        protected virtual void OnInit() { }

        public void Disable()
        {
            IsEnabled = false;

            OnDisabled();
        }

        protected virtual void OnDisabled() { }

        //are all conditions for launching the task satisfied?
        public virtual ErrorMessage CanComplete()
        {
            if (!IsInitialized)
                return ErrorMessage.inactive;
            else if (!IsEnabled)
                return ErrorMessage.disabled;
            else if (!RTSHelper.TestFactionEntityRequirements(factionEntityRequirements, gameMgr.GetFactionSlot(Entity.FactionID).FactionMgr))
                return ErrorMessage.taskMissingFactionEntityRequirements;
            else if (!resourceMgr.HasResources(requiredResources, Entity.FactionID))
                return ErrorMessage.taskMissingResourceRequirements;

            return ErrorMessage.none;
        }

        //what happens regarding the task requirements when an instance of this task is launched?
        public virtual void OnComplete()
        {
            if (!logger.RequireTrue(IsInitialized,
                $"[{GetType().Name}] Component must be initialized before it can be used!"))
                return;

            resourceMgr.UpdateResource(Entity.FactionID, requiredResources, add: false);
            PendingAmount--;
        }

        public virtual ErrorMessage CanStart() => CanComplete();

        public virtual void OnStart()
        {
            LaunchTimes++;
            PendingAmount++;
        }

        public virtual void OnCancel() 
        {
            LaunchTimes--;
            PendingAmount--;
        }
    }

    public abstract class FactionEntityCreationTask<T> : EntityComponentTaskInputBase<T> where T : IFactionEntity
    {
        public override ErrorMessage CanComplete()
        {
            ErrorMessage errorMessage;
            if ((errorMessage = base.CanComplete()) != ErrorMessage.none)
                return errorMessage;
            else if(gameMgr.GetFactionSlot(Entity.FactionID).FactionMgr.HasReachedLimit(Prefab.Code, Prefab.Category))
                return ErrorMessage.factionLimitReached;

            return ErrorMessage.none;
        }
    }
}
