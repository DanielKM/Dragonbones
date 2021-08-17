using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Audio;
using RTSEngine.ResourceExtension;
using RTSEngine.Selection;
using RTSEngine.Utilities;

namespace RTSEngine.Health
{
    public abstract class EntityHealth : MonoBehaviour, IEntityHealth, IEntityPreInitializable
    {
        #region Class Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        public bool IsInitialized { private set; get; } = false;

        public IEntity Entity { private set; get; }

        public abstract EntityType EntityType { get; }

        [SerializeField, Tooltip("Maximum health points that the entity can have."), Min(1)]
        private int maxHealth = 100;
        public int MaxHealth => maxHealth;

        public int CurrHealth { private set; get; } = 0;
        [SerializeField, Tooltip("Initial health points that the entity starts with."), Min(1)]
        private int initialHealth = 1;

        //This is not accounted for when testing whehter health value can be added or not using the CanAdd method.
        //This simply allows to not update the actual CurrHealth value while at the same time trigger the primitive methods in AddLocal using the input "updateValue"
        //In order to lock health and not allow the whole AddLocal method not to go through, CanDecrease and CanIncrease can be set to false.
        public bool LockHealth { protected set; get; } = false;

        public bool HasMaxHealth => CurrHealth >= MaxHealth;

        public float HealthRatio => (CurrHealth / (float)MaxHealth);

        [SerializeField, Tooltip("Can the health be increased?")]
        private bool canIncrease = true;
        public bool CanIncrease
        {
            get => canIncrease;
            set
            {
                canIncrease = value;
            }
        }

        [SerializeField, Tooltip("Can the health be decreased?")]
        private bool canDecrease = true;
        public bool CanDecrease
        {
            get => canDecrease;
            set
            {
                canDecrease = value;
            }
        }

        [SerializeField, Tooltip("When disabled, no entity with an Attack component can choose this entity as its target.")]
        private bool canBeAttacked = true;
        public bool CanBeAttacked { get => canBeAttacked; set { canBeAttacked = value; } }

        [SerializeField, Tooltip("The height (position on the Y axis) of the hover health bar UI element when it is enabled.")]
        private float hoverHealthBarY = 4.0f; 
        public float HoverHealthBarY => hoverHealthBarY;

        [SerializeField, Tooltip("Triggered on the entity when it loses health."), Space()]
        private EffectObject hitEffect = null; 
        [SerializeField, Tooltip("Played when the entity loses health.")]
        private AudioClipFetcher hitAudio = null;

        public bool IsDead { private set; get; } = false;

        public IEntity TerminatedBy { private set; get; }
        [SerializeField, Tooltip("Destroy the entity object when health reaches zero?")]
        private bool destroyObject = true;
        [SerializeField, Tooltip("If the object is to be destroyed on zero health, this presents how long it takes before the object is destroyed.")]
        private float destroyObjectDelay = 0.0f; 
        [SerializeField, Tooltip("Resources to be awarded to the faction whose entity deals the damage that destroys this.")]
        private ResourceInput[] destroyAward = new ResourceInput[0];

        [SerializeField, Tooltip("What audio clip to play when the entity is destroyed?")]
        private AudioClipFetcher destructionAudio = new AudioClipFetcher();
        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Effect object to spawn when the entity is destroyed.")]
        private GameObject destructionEffect = null;

        protected EntityHealthStateHandler stateHandler;
        [SerializeField, Tooltip("Possible health states that the entity can have.")]
        protected List<EntityHealthState> states = new List<EntityHealthState>();

        // Game services
        protected IInputManager inputMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; } 
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, HealthUpdateEventArgs> EntityHealthUpdated;
        public event CustomEventHandler<IEntity, DeadEventArgs> EntityDead;

        public void RaiseEntityHealthUpdated (HealthUpdateEventArgs e)
        {
            var handler = EntityHealthUpdated;
            handler?.Invoke(Entity, e);
        }
        public void RaiseEntityDead (DeadEventArgs e)
        {
            var handler = EntityDead;
            handler?.Invoke(Entity, e);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.Entity = entity;

            // In case of entity conversion, there is an attempt to re-initialize this component but we do not allow it.
            if (!logger.RequireTrue(!IsInitialized,
              $"[{GetType().Name} - {Entity.Code}] Component already initialized! It is not supposed to be initialized again! Please retrace and report!"))
                return; 

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>(); 

            if (!logger.RequireTrue(maxHealth > 0,
                $"[{GetType().Name} - {entity.Code}] 'Max Health' field value must be > 0!"))
                return;

            IsDead = false;

            stateHandler = new EntityHealthStateHandler();
            stateHandler.Init(gameMgr, this);

            CurrHealth = 0;

            OnInit();

            IsInitialized = true;

            //must bypass the "CanAdd" conditions since the initial health value is enforced.
            //This is also called for all clients in a multiplayer game.
            AddLocal(Mathf.Clamp(initialHealth, 1, MaxHealth), source: null, force: true);
        }

        protected virtual void OnInit() { }

        public void Disable() { }
        #endregion

        #region Updating MaxHealth
        public ErrorMessage SetMax(int value, IEntity source)
        {
            return inputMgr.SendInput(
                new CommandInput
                {
                    sourceMode = (byte)InputMode.health,
                    targetMode = (byte)InputMode.healthSetMax,

                    intValues = inputMgr.ToIntValues((int)EntityType, value)
                },
                Entity,
                source);
        }

        public ErrorMessage SetMaxLocal(int value, IEntity source)
        {
            maxHealth = Mathf.Max(value, 1, value);

            OnHealthUpdated(updateValue: 0, source);

            var args = new HealthUpdateEventArgs(value: 0, source);
            RaiseEntityHealthUpdated(args);
            globalEvent.RaiseEntityHealthUpdatedGlobal(Entity, args);

            return ErrorMessage.none;
        }
        #endregion

        #region Updating Health
        public abstract ErrorMessage CanAdd(int updateValue, IEntity source);

        public ErrorMessage Add(int updateValue, IEntity sourceEntity)
        {
            return inputMgr.SendInput(
                new CommandInput
                {
                    sourceMode = (byte)InputMode.health,
                    targetMode = (byte)InputMode.healthAddCurr,

                    intValues = inputMgr.ToIntValues((int)EntityType, updateValue)
                },
                source: Entity,
                target: sourceEntity);
        }

        public ErrorMessage AddLocal(int updateValue, IEntity source, bool force = false)
        {
            if (!force)
            {
                ErrorMessage errorMessage;
                if ((errorMessage = CanAdd(updateValue, source)) != ErrorMessage.none)
                    return errorMessage;

                else if (!OnPreAddHealth(ref updateValue, source))
                    return ErrorMessage.healthPreAddBlocked;
            }

            if (force || !LockHealth)
            {
                CurrHealth += updateValue;
                CurrHealth = Mathf.Clamp(CurrHealth, 0, MaxHealth);
            }

            stateHandler.Update(updateValue > 0, CurrHealth);

            OnHealthUpdated(updateValue, source);

            if (CurrHealth >= MaxHealth)
                OnMaxHealthReached(updateValue, source);
            else if (CurrHealth <= 0)
            {
                OnZeroHealthReached(updateValue, source);

                Destroy(false, source);
            }

            var e = new HealthUpdateEventArgs(updateValue, source);
            RaiseEntityHealthUpdated(e);
            globalEvent.RaiseEntityHealthUpdatedGlobal(Entity, e);

            if(updateValue < 0)
            {
                // Hit effect and audio
                effectObjPool.Spawn(hitEffect, transform.position, Quaternion.identity, Entity.transform); 
                audioMgr.PlaySFX(Entity.AudioSourceComponent, hitAudio.Fetch(), loop:false);
            }

            return ErrorMessage.none;
        }

        protected virtual bool OnPreAddHealth(ref int updateValue, IEntity source) => true;

        protected virtual void OnMaxHealthReached (int updateValue, IEntity source) { }
        protected virtual void OnZeroHealthReached (int updateValue, IEntity source) { }
        protected virtual void OnHealthUpdated (int updateValue, IEntity source) { }
        #endregion

        #region Destroying Entity
        public virtual ErrorMessage CanDestroy(bool upgrade, IEntity source) => IsDead ? ErrorMessage.dead : ErrorMessage.none;

        public ErrorMessage Destroy(bool upgrade, IEntity source)
        {
            return inputMgr.SendInput(
                    new CommandInput
                    {
                        sourceMode = (byte)InputMode.health,
                        targetMode = (byte)InputMode.healthDestroy,

                        intValues = inputMgr.ToIntValues((int)EntityType, upgrade ? 1 : 0)
                    },
                    Entity,
                    source);
        }

        public ErrorMessage DestroyLocal(bool upgrade, IEntity source)
        {
            ErrorMessage errorMessage;
            if ((errorMessage = CanDestroy(upgrade, source)) != ErrorMessage.none)
                return errorMessage;

            selectionMgr.Remove(Entity);

            IsDead = true;

            TerminatedBy = source;

            CurrHealth = 0;

            if(destroyObject || upgrade)
                //Destroy the faction entity's object:
                Destroy(gameObject, !upgrade ? destroyObjectDelay : 0.0f);

            if (Entity.IsInitialized)
            {
                //If this is no upgrade
                if (!upgrade)
                {
                    IEffectObject nextEffect = null;
                    if (destructionEffect)
                        effectObjPool.Spawn(destructionEffect, transform.position, Quaternion.identity);

                    audioMgr.PlaySFX(nextEffect?.AudioSourceComponent ? nextEffect.AudioSourceComponent : Entity.AudioSourceComponent,
                        destructionAudio.Fetch(), false);
                }

                if (source?.IsFree == false && !RTSHelper.IsSameFaction(source, Entity))
                    resourceMgr.UpdateResource(source.FactionID, destroyAward, add: true);
            }

            OnDestroyed(upgrade, source);

            var e = new DeadEventArgs(upgrade, source);
            RaiseEntityDead(e);
            globalEvent.RaiseEntityDeadGlobal(Entity, e);

            return ErrorMessage.none;
        }

        protected virtual void OnDestroyed(bool upgrade, IEntity source) { }
        #endregion
    }
}
