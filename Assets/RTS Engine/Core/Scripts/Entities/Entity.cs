using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Selection;
using RTSEngine.Health;
using RTSEngine.Event;
using RTSEngine.Animation;
using RTSEngine.Upgrades;
using RTSEngine.Task;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Faction;
using RTSEngine.Determinism;
using RTSEngine.UnitExtension;
using RTSEngine.Utilities;

namespace RTSEngine.Entities
{
    public abstract class Entity : MonoBehaviour, IEntity
    {
        #region Class Attributes

        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        public abstract EntityType Type { get; }

        public bool IsInitialized { private set; get; }

        //multiplayer related:
        public int Key { private set; get; }

        [SerializeField, Tooltip("Name of the entity that will be displayed in UI elements.")]
        private string _name = "entity_name"; 
        public string Name => _name;

        [SerializeField, Tooltip("Unique code for each entity to be used to identify the entity type in the RTS Engine.")]
        private string code = "entity_code"; 
        public string Code => code;
       
        [SerializeField, EntityCategoryInput(isDefiner: true), Tooltip("A category that is used to define a group of entities. You can input multiple categories separated by a ','.")]
        private string category = "entity_category";
        public IEnumerable<string> Category => category.Split(',');

        [SerializeField, TextArea(minLines: 5, maxLines: 5), Tooltip("Description of the entity to be displayed in UI elements.")]
        private string description = "entity_description";
        public string Description => description;

        [SerializeField, Tooltip("Icon of the entity to be displayed in UI elements.")]
        private Sprite icon = null;
        public Sprite Icon => icon;

        [SerializeField, Tooltip("Defines the range that the entity is supposed to occupy on the map. This is represented by the blue sphere gizmo.")]
        private float radius = 2.0f;
        public float Radius { get { return radius; } protected set { radius = value; } }

        [SerializeField, Tooltip("Drag and drop the model of the entity into this field. The model must be a child object of the entity!")]
        private GameObject model = null;
        public GameObject Model => model;

        [SerializeField, Tooltip("Defines the duration that the entity is supposed to persist for")]
        private float duration = 0f;
        public float Duration { get { return duration; } protected set { duration = value; } }

        //double clicking on the unit allows to select all entities of the same type within a certain range
        private float doubleClickTimer;

        public bool IsFree { protected set; get; }
        public int FactionID { protected set; get; }
        public IFactionSlot Slot => gameMgr.GetFactionSlot(FactionID);

        public Color SelectionColor { protected set; get; }

        public AudioSource AudioSourceComponent { private set; get; }

        public IAnimatorController AnimatorController { private set; get; }
        public IEntitySelection Selection { private set; get; }
        public IEntitySelectionMarker SelectionMarker { private set; get; }
        public  IEntityHealth Health { protected set; get; }
        public IEntityWorkerManager WorkerMgr { private set; get; }

        public virtual bool CanLaunchTask => IsInitialized && !Health.IsDead;

        private bool interactable;

        public virtual bool IsDummy => false;

        public bool IsInteractable {
            protected set => interactable = value;
            get => gameObject.activeInHierarchy && interactable && IsInitialized;
        }
        public bool IsSearchable => IsInteractable;

        public bool IsIdle => EntityTargetComponents.Values.All(comp => comp.IsIdle);

        //entity components:
        public IReadOnlyDictionary<string, IEntityComponent> EntityComponents { private set; get; }

        public IPendingTasksHandler PendingTasksHandler { private set; get; }

        public IReadOnlyDictionary<string, IAddableUnit> AddableUnitComponents { private set; get; }

        public IMovementComponent MovementComponent { private set; get; }
        public bool CanMove => MovementComponent != null && MovementComponent.IsActive;

        public IReadOnlyDictionary<string, IEntityTargetComponent> EntityTargetComponents { private set; get; }

        public IEnumerable<IAttackComponent> AttackComponents { private set; get; }
        public IAttackComponent AttackComponent => AttackComponents.Where(comp => comp.IsActive).FirstOrDefault();
        public bool CanAttack => AttackComponent != null && AttackComponent.IsActive;

        // Services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; } 
        protected IInputManager inputMgr { private set; get; }
        protected IEntityComponentUpgradeManager entityComponentUpgradeMgr { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, System.EventArgs> EntityInitiated;
        private void RaiseEntityInitiated()
        {
            var handler = EntityInitiated;
            handler?.Invoke(this, System.EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, EventArgs> EntityDoubleClicked;
        private void RaiseEntityDoubleClicked ()
        {
            var handler = EntityDoubleClicked;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, FactionUpdateArgs> FactionUpdateComplete;

        protected void RaiseFactionUpdateComplete(FactionUpdateArgs eventArgs)
        {
            var handler = FactionUpdateComplete;
            handler?.Invoke(this, eventArgs);
        }
        #endregion

        #region Initializing/Terminating
        public virtual void Init(IGameManager gameMgr, InitEntityParameters initParams)
        {
            this.gameMgr = gameMgr;
            this.logger = this.gameMgr.GetService<IGameLoggingService>();

            if(!logger.RequireTrue(!IsInitialized,
                $"[{GetType().Name} - {Code}] Entity has been already initiated!"))
                return;

            this.inputMgr = gameMgr.GetService<IInputManager>();

            // Dummy entities are entities that would not be registered for the faction but are used to fulfil a local service like placement buildings
            if (!IsDummy)
                Key = inputMgr.RegisterEntity(this);

            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();
            this.mouseSelector = this.gameMgr.GetService<IMouseSelector>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>(); 
            this.entityComponentUpgradeMgr = gameMgr.GetService<IEntityComponentUpgradeManager>();

            this.FactionID = initParams.factionID;
            this.IsFree = initParams.free;

            //get the components attached to the entity
            HandleComponentUpgrades();
            FetchComponents();
            SubToEvents();

            //entity parent objects are set to ignore raycasts because selection relies on raycasting selection objects which are typically direct children of the entity objects.
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            UpdateColors();

            InitComponents(initPre: true);

            if(initParams.setInitialHealth)
                //must bypass the "CanAdd" conditions in IEntityHealth since the initial health value is enforced.
                //This is also called for all clients in a multiplayer game.
                Health.AddLocal(initParams.initialHealth - Health.CurrHealth, null);

            //initial settings for the double click
            doubleClickTimer = 0.0f;
        }

        protected void CompleteInit()
        {
            //by default, an entity is interactable.
            IsInteractable = true;
            IsInitialized = true;

            InitComponents(initPost: true);

            RaiseEntityInitiated();
            globalEvent.RaiseEntityInitiatedGlobal(this);

            OnInitComplete();
        }

        protected virtual void OnInitComplete() { }

        protected void InitComponents(bool initPre = false, bool initPost = false)
        {
            if (initPre)
                foreach (IEntityPreInitializable component in transform.GetComponentsInChildren<IEntityPreInitializable>())
                    component.OnEntityPreInit(gameMgr, this);

            if (initPost)
                foreach (IEntityPostInitializable component in transform.GetComponentsInChildren<IEntityPostInitializable>())
                    component.OnEntityPostInit(gameMgr, this);
        }

        protected void DisableComponents()
        {
            foreach (IEntityPreInitializable component in transform.GetComponentsInChildren<IEntityPreInitializable>())
                component.Disable();

            if (!IsInitialized)
                return;

            foreach (IEntityPostInitializable component in transform.GetComponentsInChildren<IEntityPostInitializable>())
                component.Disable();
        }

        private void HandleComponentUpgrades()
        {
            if (IsFree 
                || !entityComponentUpgradeMgr.TryGet(this, FactionID, out List<UpgradeElement<IEntityComponent>> componentUpgrades))
                return;

            foreach(UpgradeElement<IEntityComponent> element in componentUpgrades)
                UpgradeComponent(element);
        }

        //the assumption here is that the targetComponent is attached to an empty prefab game object that includes no additional components!
        //initTime is set to true when this method is called from the initializer method of the Entity, in that case, no need to init the new component/re-fetch components
        //if this method is called from outside the Init() method of this class, then the initTime must be set to false so that components can be refetched and the new component is initialized
        public void UpgradeComponent(UpgradeElement<IEntityComponent> upgradeElement)
        {
            //get the component to be upgraded, destroy it and replace it with the target upgrade component
            //both components must be valid
            if(!upgradeElement.target.IsValid())
                return;

            RTSHelper.TryGetEntityComponentWithCode(this, upgradeElement.sourceCode, out IEntityComponent sourceComponent);

            //since components with their field values can not be added directly, we create a child object of the entity with the upgraded component
            Transform newComponentTransform = Instantiate(upgradeElement.target.gameObject).transform;
            newComponentTransform.SetParent(transform, true);
            newComponentTransform.transform.localPosition = Vector3.zero;

            if (IsInitialized)
            {
                newComponentTransform.GetComponent<IEntityPostInitializable>().OnEntityPostInit(gameMgr, this);
                if(sourceComponent.IsValid())
                    newComponentTransform.GetComponent<IEntityComponent>().HandleComponentUpgrade(sourceComponent);
            }

            //disable old component
            if (sourceComponent.IsValid())
            {
                if(IsInitialized) // Only disable the component if it has been initialized
                    sourceComponent.Disable();

                DestroyImmediate(sourceComponent as UnityEngine.Object);
            }

            // Re-fetch entity components in case the upgrade is called post initialization
            if (IsInitialized)
                FetchComponents();
        }

        protected virtual void SubToEvents()
        {
            //subscribe to events:
            Health.EntityDead += HandleEntityDead;
            Selection.Selected += HandleEntitySelected;
        }

        protected virtual void FetchComponents()
        {
            AnimatorController = transform.GetComponentInChildren<IAnimatorController>();

            Selection = transform.GetComponentInChildren<IEntitySelection>();
            if (!logger.RequireValid(Selection,
                $"[{GetType().Name} - {Code}] A selection component that extends {typeof(IEntitySelection).Name} must be assigned to the 'Selection' field!"))
                return;

            SelectionMarker = GetComponentInChildren<IEntitySelectionMarker>();

            // The Health component is assigned in the childrten of this class before this is called.
            if (!logger.RequireValid(Health,
                $"[{GetType().Name} - {Code}] An entity health component that extends {typeof(IEntityHealth).Name} must be assigned attache to the entity!"))
                return;

            WorkerMgr = transform.GetComponentInChildren<IEntityWorkerManager>();

            //get the audio source component attached to the entity main object:
            AudioSourceComponent = transform.GetComponentInChildren<AudioSource>();

            //finding and initializing entity components.
            IEntityComponent[] entityComponents = transform.GetComponentsInChildren<IEntityComponent>();

            if (!logger.RequireTrue(entityComponents.Select(comp => comp.Code).Distinct().Count() == entityComponents.Length,
                $"[{GetType().Name} - {Code}] All entity components attached to the entity must each have a distinct code to identify it within the entity!"))
                return;

            EntityComponents = entityComponents.ToDictionary(comp => comp.Code);

            PendingTasksHandler = transform.GetComponentInChildren<IPendingTasksHandler>();

            AddableUnitComponents = transform.GetComponentsInChildren<IAddableUnit>().ToDictionary(comp => comp.Code);

            MovementComponent = transform.GetComponentInChildren<IMovementComponent>();

            if (!logger.RequireTrue(transform.GetComponentsInChildren<IMovementComponent>().Length < 2,
                $"[{GetType().Name} - {Code}] Having more than one components that extend {typeof(IMovementComponent).Name} interface attached to the same entity is not allowed!"))
                return;

            EntityTargetComponents = transform.GetComponentsInChildren<IEntityTargetComponent>().OrderBy(comp => comp.Priority).ToDictionary(comp => comp.Code);

            AttackComponents = transform.GetComponentsInChildren<IAttackComponent>().OrderBy(comp => comp.Priority);
        }

        protected virtual void Disable (bool isUpgrade, bool isFactionUpdate)
        {
            SetIdle(null);

            DisableComponents();
        }

        private void OnDestroy()
        {
            if(Health.IsValid())
                Health.EntityDead -= HandleEntityDead;
            if (Selection.IsValid())
                Selection.Selected -= HandleEntitySelected;
        }
        #endregion

        #region Handling Events
        private void HandleEntityDead(IEntity sender, DeadEventArgs e)
        {
            Disable(e.IsUpgrade, false);
        }

        private void HandleEntitySelected(IEntity sender, EntitySelectionEventArgs args)
        {
            SelectionMarker.StopFlash(); //in case the selection texture of the entity was flashing

            if (args.Type != SelectionType.single)
                return;

            if(doubleClickTimer > 0.0f)
            {
                doubleClickTimer = 0.0f;

                //if this is the second click (double click), select all entities of the same type within a certain range
                mouseSelector.SelectEntitisInRange(this, playerCommand: true);

                RaiseEntityDoubleClicked();

                return;
            }

            //if the player doesn't have the multiple selection key down (not looking to select multiple entities one by one)
            if (mouseSelector.MultipleSelectionKeyDown == false)
                doubleClickTimer = 0.5f;
        }
        #endregion

        #region Handling Double Clicks
        protected virtual void Update()
        {
            if (doubleClickTimer > 0)
                doubleClickTimer -= Time.deltaTime;
        }

        public void OnPlayerClick()
        {
        }
        #endregion

        #region Updating IEntityTargetComponent Components State (Except Movement and Attack).
        public ErrorMessage SetTargetFirst (TargetData<IEntity> target, bool playerCommand)
        {
            return inputMgr.SendInput(new CommandInput
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.setComponentTargetFirst,
                targetPosition = target.position,
                opPosition = target.opPosition,

                playerCommand = playerCommand,
            },
            source: this,
            target: target.instance);
        }

        public ErrorMessage SetTargetFirstLocal (TargetData<IEntity> target, bool playerCommand)
        {
            var nextComps = EntityTargetComponents.Values
                .Where(comp => comp.IsActive && comp != MovementComponent && comp != AttackComponent)
                .OrderBy(comp => comp.Priority);

            foreach (IEntityTargetComponent comp in nextComps)
                if (comp.SetTargetLocal(target, playerCommand) == ErrorMessage.none)
                    return ErrorMessage.none;

            return ErrorMessage.failed;
        }

        public void SetIdle(IEntityTargetComponent exception = null, bool includeMovement = true)
        {
            foreach (IEntityTargetComponent comp in EntityTargetComponents.Values)
                if (comp != exception
                    && !comp.IsIdle
                    && (includeMovement || comp != MovementComponent))
                {
                    comp.Stop();
                }
        }
        #endregion

        #region Updating Faction
        public abstract ErrorMessage SetFaction(IEntity source, int targetFactionID);

        public abstract ErrorMessage SetFactionLocal(IEntity source, int targetFactionID);
        #endregion

        #region Updating Entity Colors
        protected abstract void UpdateColors();
        #endregion

        #region Editor Only
#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            //Draw the entity's radius in blue
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
        #endregion
    }
}
