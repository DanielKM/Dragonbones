using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Determinism;
using RTSEngine.Logging;

namespace RTSEngine.ResourceExtension
{
    public class ResourceManager : MonoBehaviour, IResourceManager
    {
        #region Attributes
        [SerializeField, EnforceType(sameScene: true), Tooltip("All pre-placed resources must be placed as a children of this transform so that they are fetched when the game starts.")]
        private Transform resourcesParent = null;

        [SerializeField, Tooltip("Define resources that can be used inside this map.")]
        private ResourceTypeInfo[] mapResourceTypes = new ResourceTypeInfo[0];

        // Key: faction ID
        // Value: FactionSlotResourceManager instance that handles resources for faction slot of ID = key
        public IReadOnlyDictionary<int, IFactionSlotResourceManager> FactionResources { private set; get; } = null;

        [SerializeField, Tooltip("Can resource collectors gather resources and add them automatically to the faction or do they need to be dropped off before they are added to the faction?")]
        private bool autoCollect = true;
        public bool CanAutoCollect => autoCollect;

        private List<IResource> allResources = new List<IResource>();
        public IEnumerable<IResource> AllResources => allResources;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            if (!logger.RequireValid(mapResourceTypes,
              $"[{GetType().Name}] Some of the defined entries in the 'Map Resources' field are not valid!"))
                return; 

            this.gameMgr.GameBuilt += HandleGameBuilt;
            this.gameMgr.GameStartRunning += HandleGameStartRunning;

            globalEvent.ResourceInitiatedGlobal += HandleResourceInitiatedGlobal;
            globalEvent.ResourceDeadGlobal += HandleResourceDeadGlobal;
        }

        private void OnDestroy()
        {
            gameMgr.GameBuilt -= HandleGameBuilt;
            gameMgr.GameStartRunning -= HandleGameStartRunning;

            globalEvent.ResourceInitiatedGlobal -= HandleResourceInitiatedGlobal;
            globalEvent.ResourceDeadGlobal -= HandleResourceDeadGlobal;
        }

        private void HandleGameBuilt(IGameManager source, EventArgs args)
        {
            IReadOnlyDictionary<ResourceTypeInfo, ResourceTypeValue> initialResources = 
                (gameMgr.CurrBuilder.IsValid() 
                    ? gameMgr.CurrBuilder.Data.initialResources 
                    : Enumerable.Empty<ResourceTypeInput>()
                )
                .ToDictionary(input => input.type, input => input.value);

            // Initialize faction resources
            FactionResources = gameMgr.FactionSlots
                .ToDictionary(factionSlot => factionSlot.ID,
                factionSlot => new FactionSlotResourceManager(
                    factionSlot,
                    gameMgr,
                    1.0f,
                    mapResourceTypes,
                    initialResources) as IFactionSlotResourceManager);
        }

        private void HandleGameStartRunning(IGameManager source, EventArgs args)
        {
            allResources = new List<IResource>();
            foreach (IResource resource in resourcesParent.GetComponentsInChildren<IResource>(true))
                // When a resource is successfully initiated, it will trigger an event that will add it to the allResources list.
                resource.Init(
                    gameMgr,
                    new InitResourceParameters
                    {
                        free = true,
                        factionID = -1,

                        setInitialHealth = false,
                    });
        }
        #endregion

        #region Handling Events: Monitoring Resources
        private void HandleResourceInitiatedGlobal(IResource resource, EventArgs e)
        {
           allResources.Add(resource);
        }

        private void HandleResourceDeadGlobal(IResource resource, DeadEventArgs e)
        {
            allResources.Remove(resource);
        }
        #endregion

        #region Update Resource Amount/Capacity
        /// <summary>
        /// Adds/removes an amount of a faction's resource.
        /// </summary>
        /// <param name="factionID">ID of the faction whose resources will be updated.</param>
        /// <param name="resourceInputArray">Array where each element defines a resource type and the amount to add/remove.</param>
        /// <param name="add">Adds the resources when true, otherwise removes the resources from the faction.</param>
        public void UpdateResource(int factionID, IEnumerable<ResourceInput> resourceInputArray, bool add)
        {
            foreach (ResourceInput resourceInput in resourceInputArray)
                UpdateResource(factionID, resourceInput, add);
        }

        /// <summary>
        /// Adds/removes an amount of a faction's resource.
        /// </summary>
        /// <param name="factionID">ID of the faction whose resources will be updated.</param>
        /// <param name="resourceInput">Defines the resource type and amount to add/remove.</param>
        /// <param name="add">Adds the resources when true, otherwise removes the resources from the faction.</param>
        public void UpdateResource(int factionID, ResourceInput resourceInput, bool add)
        {
            if (!IsResourceTypeValidInGame(resourceInput, factionID)
                || resourceInput.nonConsumable)
                return;

            IFactionResourceHandler resourceHandler = FactionResources[factionID].ResourceHandlers[resourceInput.type];

            ResourceTypeValue updateValue = add
                ? resourceInput.value
                : new ResourceTypeValue { amount = -resourceInput.value.amount, capacity = -resourceInput.value.capacity };

            resourceHandler.UpdateAmount(updateValue);
        }

        public bool HasResources(IEnumerable<ResourceInputRange> resourceInputArray, int factionID)
        {
            var fixedValueResourceInput = resourceInputArray
                .Select(input => new ResourceInput
                {
                    type = input.type,
                    nonConsumable = input.nonConsumable,

                    value = new ResourceTypeValue
                    {
                        amount = input.value.amount.RandomValue,
                        capacity = input.value.capacity.RandomValue
                    }
                });

            return HasResources(fixedValueResourceInput, factionID);
        }

        public bool HasResources(IEnumerable<ResourceInput> resourceInputArray, int factionID) =>
            resourceInputArray.All(input => HasResources(input, factionID));

        public bool HasResources(ResourceInput resourceInput, int factionID)
        {
            if (!IsResourceTypeValidInGame(resourceInput, factionID))
                return false;

            var resourceHandler = FactionResources[factionID].ResourceHandlers[resourceInput.type];

            return
                // Make sure the amount value is available
                (resourceHandler.Type.HasCapacity
                    ? (resourceHandler.Capacity - resourceHandler.Amount) >= resourceInput.value.amount
                    : resourceHandler.Amount >= resourceInput.value.amount * FactionResources[factionID].ResourceNeedRatio)
                // Make sure the capacity value is available
                && resourceHandler.Capacity >= resourceInput.value.capacity;
        }
        #endregion

        #region Creating Resources
        public ErrorMessage CreateResource(IResource resourcePrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitResourceParameters initParams)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                isSourcePrefab = true,

                sourceMode = (byte)InputMode.create,
                targetMode = (byte)InputMode.resource,

                sourcePosition = spawnPosition,
                opPosition = spawnRotation.eulerAngles,

                code = JsonUtility.ToJson(initParams),

                playerCommand = false
            },
            source: resourcePrefab,
            target: null);
        }

        public IResource CreateResourceLocal(IResource resourcePrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitResourceParameters initParams)
        {
            IResource newResource = Instantiate(resourcePrefab.gameObject, spawnPosition, spawnRotation).GetComponent<IResource>(); //spawn the new resource

            newResource.Init(gameMgr, initParams); 

            return newResource;
        }
        #endregion

        #region Helper Functions

        public bool IsResourceTypeValidInGame(ResourceInput resourceInput, int factionID)
        {
            return logger.RequireTrue(FactionResources.ContainsKey(factionID),
                $"[{GetType().Name}] Faction ID '{factionID}' does not have a resources manager defined. Are you sure this is a valid faction?")

               && logger.RequireValid(resourceInput.type,
                    $"[{GetType().Name}] Attempting to update faction ID '{factionID}' resources with an invalid resource type!")

               && logger.RequireTrue(FactionResources[factionID].ResourceHandlers.ContainsKey(resourceInput.type),
                    $"[{GetType().Name}] Resource '{resourceInput.type.Key}' is not defined in the 'mapResources' array (not recognized as a usable resource in this map)!");

        }
        #endregion
    }

}