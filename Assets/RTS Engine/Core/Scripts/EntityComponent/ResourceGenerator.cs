using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using RTSEngine.UI;
using RTSEngine.Entities;
using RTSEngine.ResourceExtension;
using RTSEngine.Determinism;
using RTSEngine.Game;
using RTSEngine.Event;
using RTSEngine.Audio;
using System.Linq;
using RTSEngine.Utilities;

namespace RTSEngine.EntityComponent
{
    /// <summary>
    /// Allows a FactionEntity instance (can be a unit or a building) to generate resources.
    /// </summary>
    public class ResourceGenerator : MonoBehaviour, IEntityComponent, IEntityPostInitializable
    {
        #region Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        /*
         * Action types and their parameters:
         * generateResources: no parameters.
         * collectResources: no parameters.
         * */
        public enum ActionType : byte { generateResources, collectResources }

        public bool IsInitialized { private set; get; } = false;

        [SerializeField, Tooltip("Code that defines the entity component, unique within the entity.")]
        private string code = "comp_code";
        /// <summary>
        /// Code that defines the entity component, unique only within the entity.
        /// </summary>
        public string Code => code;

        //the FactionEntity instance to which this component is attached to.
        private IFactionEntity factionEntity = null;
        /// <summary>
        /// IEntity instance that this component is attached to.
        /// </summary>
        public IEntity Entity => factionEntity;

        [SerializeField, Tooltip("Is the component active by default?")]
        private bool isActive = true; //is this generator active or not?
        /// <summary>
        /// Is the task component currently active?
        /// </summary>
        public bool IsActive => isActive;

        [SerializeField, Tooltip("Duration (in seconds) required to generate resources.")]
        private float period = 1.0f;
        private TimeModifiedTimer timer;

        [SerializeField, Tooltip("Resources to generate every period."), Space(10)]
        private ResourceInput[] resources = new ResourceInput[0];

        // Holds the amount of the currently generated resources.
        private ModifiableResourceTypeValue[] generatedResources = new ModifiableResourceTypeValue[0];

        [SerializeField, Tooltip("Required resources to generate the above resources during each period.")]
        private ResourceInput[] requiredResources = new ResourceInput[0];

        // If a resource type is inclued in "resources" but not here then it will be assumed that it does not require to reach a threshold.
        [SerializeField, Tooltip("Threshold of generated resources required to achieve so that the resources are collectable by the player.")]
        private ResourceInput[] collectionThreshold = new ResourceInput[0];

        // For direct access to the resources threshold.
        private Dictionary<string, ResourceTypeValue> collectionThresholdDic = new Dictionary<string, ResourceTypeValue>();

        // Have the generated resources hit the target threshold?
        private bool isThresholdMet = false;
        [SerializeField, Tooltip("Stop generating resources when the target threshold is met?")]
        private bool stopGeneratingOnThresholdMet = false;

        [SerializeField, Tooltip("Automatically add resources to the player's faction when the threshold is met?")]
        private bool autoCollect = false;

        [SerializeField, Tooltip("What audio clip to play when the player collects resources produced by this generator?"), Space(10)]
        private AudioClipFetcher collectionAudio = new AudioClipFetcher();

        [SerializeField, Tooltip("Information used to display the resource collection task in case it is manaully collected by the player.")]
        private EntityComponentTaskUIAsset collectionTaskUI = null;

        // Game services
        protected IResourceManager resourceMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; } 

        [SerializeField, Tooltip("Event triggered when the resource generator hits their collection threshold."), Space(10)]
        private UnityEvent onThresholdMet = new UnityEvent();

        [SerializeField, Tooltip("Event triggered when the resource generator's generated resources are collected.")]
        private UnityEvent onCollected = new UnityEvent();
        #endregion

        #region Initialization/Termination
        /// <summary>
        /// Initializer method required for each entity component that gets called by the Entity instance that the component is attached to.
        /// </summary>
        /// <param name="gameMgr">Active instance of the GameManager component.</param>
        /// <param name="entity">Entity instance that the component is attached to.</param>
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>(); 

            this.factionEntity = entity as IFactionEntity;

            // Assign an empty list of the generated resources
            generatedResources = resources
                .Select(resource => new ModifiableResourceTypeValue())
                .ToArray();

            collectionThresholdDic.Clear();
            // Populate the collection threshold dictionary for easier direct access later when collecting resources
            foreach (ResourceInput ri in collectionThreshold)
                collectionThresholdDic.Add(ri.type.Key, ri.value);

            // Initial settings
            timer = new TimeModifiedTimer(period);
            isThresholdMet = false;

            IsInitialized = true;
        }

        public void Disable() { }
        #endregion

        #region Handling Component Upgrade
        public void HandleComponentUpgrade (IEntityComponent sourceEntityComponent)
        {
            ResourceGenerator sourceResourceGenerator = sourceEntityComponent as ResourceGenerator;
            if (!sourceResourceGenerator.IsValid())
                return;

            CollectResourcesAction(playerCommand: false);
        }
        #endregion

        #region Activating/deactivating component
        public ErrorMessage SetActive(bool active, bool playerCommand) => RTSHelper.SetEntityComponentActive(this, active, playerCommand);

        public ErrorMessage SetActiveLocal(bool active, bool playerCommand)
        {
            isActive = active;

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Actions
        public ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand)
            => RTSHelper.LaunchEntityComponentAction(this, actionID, target, playerCommand);

        public ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            switch((ActionType)actionID)
            {
                case ActionType.generateResources:

                    return GeneratePeriodResourcesActionLocal(playerCommand);

                case ActionType.collectResources:

                    return CollectResourcesActionLocal(playerCommand);

                default:
                    return ErrorMessage.undefined;
            }
        }
        #endregion

        #region Resource Generation Action
        /// <summary>
        /// Updates the generated resources.
        /// </summary>
        private void Update()
        {
            // Only allow the master instance to run the timers for generating resources
            if (!IsInitialized
                || !RTSHelper.IsMasterInstance()
                || !factionEntity.CanLaunchTask 
                || !isActive 
                || (stopGeneratingOnThresholdMet && isThresholdMet))
                return;

            // Turn into Action
            // Generating Resources:
            if (timer.ModifiedDecrease())
                GeneratePeriodResourcesAction(playerCommand: false); 
        }

        private ErrorMessage GeneratePeriodResourcesAction(bool playerCommand)
        {
            return LaunchAction(
                (byte)ActionType.generateResources, 
                null,
                playerCommand);
        }

        /// <summary>
        /// Generates the resources for one period.
        /// </summary>
        private ErrorMessage GeneratePeriodResourcesActionLocal(bool playerCommand)
        {
            if (!resourceMgr.HasResources(requiredResources, factionEntity.FactionID))
                return ErrorMessage.taskMissingResourceRequirements;

            // Assume that the target threshold is met:
            isThresholdMet = true;

            for (int i = 0; i < generatedResources.Length; i++)
            {
                generatedResources[i].UpdateValue(resources[i].value);

                // One of the resources haven't met the threshold yet? => threshold not met
                if (isThresholdMet
                    && collectionThresholdDic.TryGetValue(resources[i].type.Key, out ResourceTypeValue thresholdValue)
                    && !generatedResources[i].Has(thresholdValue))
                    isThresholdMet = false;
            }

            // If the threshold is met and we can either autocollect the resources or this is a NPC faction:
            if (isThresholdMet && (autoCollect || factionEntity.IsNPCFaction()))
                CollectResourcesAction(playerCommand: false);

            if (isThresholdMet)
            {
                onThresholdMet.Invoke();

                globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            }
                
            // Consume the required resources per period:
            resourceMgr.UpdateResource(factionEntity.FactionID, requiredResources, add:false);

            timer.Reload();

            return ErrorMessage.none;
        }
        #endregion

        #region Resource Collection Action
        /// <summary>
        /// Collects all the generated resources.
        /// </summary>
        /// <param name="playerCommand">True if the player clicked on the resource collection task, otherwise false.</param>
        private ErrorMessage CollectResourcesAction(bool playerCommand)
        {
            return LaunchAction(
                (byte)ActionType.collectResources, 
                null,
                playerCommand);
        }

        private ErrorMessage CollectResourcesActionLocal(bool playerCommand)
        {
            // We no longer meet the threshold.
            isThresholdMet = false; 

            for (int i = 0; i < generatedResources.Length; i++)
            {
                resourceMgr.UpdateResource(
                    factionEntity.FactionID,
                    new ResourceInput
                    {
                        type = resources[i].type,
                        value = new ResourceTypeValue
                        {
                            amount = generatedResources[i].Amount,
                            capacity = generatedResources[i].Capacity
                        }
                    },
                    add:true);

                //and reset them.
                generatedResources[i].Reset(); 
            }

            onCollected.Invoke();

            if(playerCommand && Entity.IsLocalPlayerFaction())
                audioMgr.PlaySFX(collectionAudio.Fetch(), false);

            return ErrorMessage.none;
        }
        #endregion

        #region Task UI
        /// <summary>
        /// Allows to provide information regarding the resource collection task, if there's one, that is displayed in the task panel when the resource generator is selected.
        /// </summary>
        /// <param name="taskUIAttributes">TaskUIAttributes instance that contains the information required to display the resource collection task.</param>
        /// <param name="disabledTaskCodes">In case the resource generation task is to be disabeld, it will be the single element of this IEnumerable.</param>
        /// <returns>True if there's a resource collection task that requires to be displayed, otherwise false.</returns>
        public bool OnTaskUIRequest(
            out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes)
        {
            return RTSHelper.OnSingleTaskUIRequest(
                this,
                out taskUIAttributes,
                out disabledTaskCodes,
                collectionTaskUI,
                !autoCollect && isThresholdMet);
        }

        /// <summary>
        /// Called when the player clicks on the resource collection task by the TaskUI instance that handles that task.
        /// </summary>
        /// <param name="task">TaskUI instance of the resource collection task. In other more complex components, multiple tasks can be drawn from the same component, this allows to define which task has been clicked.</param>
        public bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            if (collectionTaskUI.IsValid() && taskAttributes.data.code == collectionTaskUI.Key)
            {
                CollectResourcesAction(playerCommand: true);
                return true;
            }

            return false;
        }
        #endregion
    }
}
