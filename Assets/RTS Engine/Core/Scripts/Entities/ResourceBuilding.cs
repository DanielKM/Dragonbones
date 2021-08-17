using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.ResourceExtension;
using RTSEngine.Event;
using RTSEngine.Audio;

namespace RTSEngine.Entities
{
    public class ResourceBuilding : Building, IResourceBuilding
    {
        [SerializeField, Tooltip("Resource type that is represented by this object.")]
        private ResourceTypeInfo resourceType = null;
        public ResourceTypeInfo ResourceType => resourceType;

        public bool CanCollect => IsBuilt;

        public bool CanCollectOutsideBorder => PlacerComponent.CanPlaceOutsideBorder;

        [SerializeField, Tooltip("Audio clip played when a unit is actively collecting this resource.")]
        private AudioClipFetcher collectionAudio = new AudioClipFetcher();
        public AudioClipFetcher CollectionAudio => collectionAudio;

        // Using the new keyword allows to set the resource health component for this class however base.Health remains of type IBuildingHealth
        public new IResourceHealth Health { private set; get; } 
        public new IResourceWorkerManager WorkerMgr { private set; get; }

        // The resource part of this entity gets initialized only when the building is constructed for the first time.
        public void Init(IGameManager gameMgr, InitResourceParameters initParams)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnConstructionComplete()
        {
            if (!logger.RequireValid(resourceType,
              $"[{GetType().Name}] The 'Resource Type' field must be assigned!"))
                return;

            // Subscribe to the resource health (IResourceHelath) death event to know when the resource is destroyed so that a destruction of the whole building can be triggered.
            Health.EntityDead += HandleResourceDead;

            globalEvent.RaiseResourceInitiatedGlobal(this);
        }

        protected override void OnDisabled()
        {
            // This is called post Destroy call on the IBuildingHealth component
            // When the building is destroyed, make sure to destroy the resource health as well.
            Health.DestroyLocal(false, null);
        }

        private void HandleResourceDead(IEntity entity, DeadEventArgs args)
        {
            base.Health.DestroyLocal(args.IsUpgrade, args.Source);

            Health.EntityDead -= HandleResourceDead;
        }

        protected override void FetchComponents()
        {
            // Unlike the simple Resource entity, we will not set the resource health component as the main health component, it rather the one connected to the building.
            Health = gameObject.GetComponentInChildren<IResourceHealth>();

            WorkerMgr = transform.GetComponentInChildren<IResourceWorkerManager>();

            base.FetchComponents();

            if (!logger.RequireValid(WorkerMgr,
                $"[{GetType().Name} - {Code}] Resource object must have a component that extends {typeof(IEntityWorkerManager).Name} interface attached to it!"))
                return;
        }
    }
}
