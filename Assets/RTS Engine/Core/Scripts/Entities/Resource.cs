using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Determinism;
using RTSEngine.Health;
using System;
using RTSEngine.Audio;
using UnityEngine.Serialization;
using RTSEngine.Event;

namespace RTSEngine.Entities
{
    public class Resource : Entity, IResource
    {
        #region Class Attributes
        public sealed override EntityType Type => EntityType.resource;

        [SerializeField, Tooltip("Resource type that is represented by this object.")]
        private ResourceTypeInfo resourceType = null;
        public ResourceTypeInfo ResourceType => resourceType;


        [SerializeField, Tooltip("Can the resource be collected at all?")]
        private bool canCollect = true;
        public bool CanCollect => canCollect;

        [SerializeField, Tooltip("Can the resource be collected outside of faction borders?")]
        private bool canCollectOutsideBorder = false;
        public bool CanCollectOutsideBorder => canCollectOutsideBorder;

        [SerializeField, Tooltip("Audio clip played when a unit is actively collecting this resource.")]
        private AudioClipFetcher collectionAudio = new AudioClipFetcher();
        public AudioClipFetcher CollectionAudio => collectionAudio;

        [SerializeField, Tooltip("Main identifiable color of the resource entity."), FormerlySerializedAs("color")]
        private Color mainColor = Color.green;

        public new IResourceHealth Health { private set; get; }
        public new IResourceWorkerManager WorkerMgr { private set; get; }

        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, InitResourceParameters initParams)
        {
            base.Init(gameMgr, initParams);

            if (!logger.RequireValid(resourceType,
              $"[{GetType().Name}] The 'Resource Type' field must be assigned!"))
                return; 

            SelectionMarker?.Disable();

            CompleteInit();
            globalEvent.RaiseResourceInitiatedGlobal(this);
        }

        protected override void FetchComponents()
        {
            Health = transform.GetComponentInChildren<IResourceHealth>();
            // In the case of a simple resource, the main health component would be the one that handles the resources amount.
            base.Health = Health;

            WorkerMgr = transform.GetComponentInChildren<IResourceWorkerManager>();

            base.FetchComponents();

            if (!logger.RequireValid(WorkerMgr,
                $"[{GetType().Name} - {Code}] Resource object must have a component that extends {typeof(IEntityWorkerManager).Name} interface attached to it!"))
                return;
        }

        protected sealed override void Disable(bool isUpgrade, bool isFactionUpdate)
        {
            base.Disable(isUpgrade, isFactionUpdate);

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Updating Colors
        protected override void UpdateColors()
        {
            SelectionColor = mainColor;
        }
        #endregion

        #region Updating Faction
        public override ErrorMessage SetFaction (IEntity targetFactionEntity, int targetFactionID)
        {
            if (targetFactionID == FactionID)
                return ErrorMessage.factionIsFriendly;

            CommandInput newInput = new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.setFaction,

                intValues = inputMgr.ToIntValues(targetFactionID),
            };

            return inputMgr.SendInput(newInput, source: this, target: targetFactionEntity);
        }

        public override ErrorMessage SetFactionLocal (IEntity source, int targetFactionID)
        {
            FactionID = targetFactionID; //set the new faction ID
            IsFree = FactionID == -1 ? true : false;

            var eventArgs = new FactionUpdateArgs(source, targetFactionID);
            RaiseFactionUpdateComplete(eventArgs);

            return ErrorMessage.none;
        }

        #endregion
    }
}