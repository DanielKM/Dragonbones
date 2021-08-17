using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.ResourceExtension;
using RTSEngine.Animation;
using RTSEngine.Logging;
using RTSEngine.UnitExtension;
using System;

namespace RTSEngine.EntityComponent
{
    public class DropOffSource : FactionEntityTargetComponent<IFactionEntity>, IDropOffSource
    {
        #region Attributes
        /*
         * Action types and their parameters:
         * startDropoff: no parameters.
         * */
        public enum ActionType : byte { startDropoff }

        protected IUnit unit;

        // Always set 'HasTarget' to false because we do not want this component to affect the idle status of the unit.
        // If at least one IEntityTargetComponent of an IEntity has this property set to true then the unit is considered in idle status.
        // We still want to classify the unit as idle even in the case it has a drop off point assigned.
        public override bool IsIdle => true;

        [SerializeField, Tooltip("Define the faction entities that can be used as drop off points.")]
        private FactionEntityTargetPicker targetPicker = new FactionEntityTargetPicker();

        [SerializeField, Tooltip("Only allow the unit to drop off resources at points within a certain distance from the resource?")]
        private bool maxDropOffDistanceEnabled = false;
        [SerializeField, Tooltip("If the above option is enabled then this is the max drop off point distance")]
        private float maxDropOffDistance = 10.0f;

        // Holds the collected resources amount defined by each resource type.
        private IDictionary<ResourceTypeInfo, int> collectedResources;
        public IReadOnlyDictionary<ResourceTypeInfo, int> CollectedResources => collectedResources as IReadOnlyDictionary<ResourceTypeInfo, int>;

        [SerializeField, Tooltip("What types of resources can be dropped off?")]
        private CollectableResourceData[] dropOffResources = new CollectableResourceData[0];

        private IReadOnlyDictionary<ResourceTypeInfo, CollectableResourceData> dropOffResourcesDic = null;

        private GameObject dropOffObject;

        [SerializeField, Tooltip("The total maximum capacity of all resources that the collector can hold before they have to drop it off.")]
        private int totalMaxCapacity = 10;

        public DropOffState State { private set; get; }

        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Events
        public event CustomEventHandler<IDropOffSource, EventArgs> DropOffStateUpdated;

        private void RaiseDropOffStateUpdated(DropOffState newState)
        {
            State = newState;

            var handler = DropOffStateUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();

            this.unit = Entity as IUnit;

            if (!logger.RequireValid(unit.CollectorComponent,
                $"[{GetType().Name} - {Entity.Code}] A component that extends {typeof(IResourceCollector).Name} interface must be attached to same object!"))
                return;

            ResetCollectedResources();

            // Just used to have constant access time to drop off data rather than having to go through the list each time
            dropOffResourcesDic = dropOffResources.ToDictionary(dr => dr.type, dr => dr);

            RaiseDropOffStateUpdated(DropOffState.inactive);
        }

        private void ResetCollectedResources()
        {
            collectedResources = resourceMgr.FactionResources[unit.IsFree ? 0 : unit.FactionID].ResourceHandlers.Values
                .ToDictionary(resourceHandler => resourceHandler.Type, mr => 0);
        }
        #endregion

        #region Handling Component Upgrade
        protected override void OnComponentUpgraded(FactionEntityTargetComponent<IFactionEntity> sourceFactionEntityTargetComponent)
        {
            foreach (KeyValuePair<ResourceTypeInfo, int> collectedResourcePair in (sourceFactionEntityTargetComponent as IDropOffSource).CollectedResources)
                UpdateCollectedResources(collectedResourcePair.Key, collectedResourcePair.Value);

            AttemptStartDropOff();
        }
        #endregion

        #region Searching/Updating Target
        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            return !maxDropOffDistanceEnabled
                || !unit.CollectorComponent.Target.instance.IsValid()
                || Vector3.Distance(unit.CollectorComponent.Target.instance.transform.position, target.instance.transform.position) 
                    <= maxDropOffDistance + target.instance.Radius + unit.CollectorComponent.Target.instance.Radius;
        }

        public override ErrorMessage IsTargetValid(TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IFactionEntity> potentialTarget = testTarget;

            if (!potentialTarget.instance.IsValid() || !potentialTarget.instance.CanLaunchTask)
                return ErrorMessage.invalid;
            else if (!potentialTarget.instance.IsSameFaction(unit))
                return ErrorMessage.factionMismatch;
            else if (!potentialTarget.instance.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.dead;
            else if (!potentialTarget.instance.DropOffTarget.IsValid())
                return ErrorMessage.dropoffTargetMissing;
            else if (!targetPicker.IsValidTarget(potentialTarget.instance))
                return ErrorMessage.entityCompTargetPickerUndefined;
            else if (!IsTargetInRange(transform.position, potentialTarget))
                return ErrorMessage.entityCompTargetOutOfRange;

            return potentialTarget.instance.DropOffTarget.CanMove(unit);
        }

        protected override void OnTargetPostLocked(bool playerCommand, bool sameTarget)
        {
            // Force unit to drop its resources if this was a direct player command
            if(playerCommand)
                AttemptStartDropOff(force:true);
        }

        // Called when a new drop off point is added to the faction or when the unit starts collecting a new resource
        public void UpdateTarget()
        {
            // Force to get a new drop off point
            Stop();

            TargetFinder.Center = unit.CollectorComponent.Target.instance.IsValid()
                ? unit.CollectorComponent.Target.instance.transform 
                : unit.transform;

            TargetFinder.SearchTarget();
        }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            return RTSHelper.LaunchEntityComponentAction(this, actionID, target, playerCommand);
        }

        public override ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            switch((ActionType)actionID)
            {
                case ActionType.startDropoff:

                    return SendToTargetLocal(playerCommand);

                default:
                    return ErrorMessage.undefined;
            }
        }
        #endregion

        #region Handling Resource Drop Off
        public ErrorMessage SendToTarget(bool playerCommand)
        {
            if(!Target.instance.IsValid())
            { 
                if (playerCommand && unit.IsLocalPlayerFaction())
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = ErrorMessage.dropoffTargetMissing,

                        source = Entity,
                    });

                unit.AnimatorController?.SetState(AnimatorState.idle);

                return ErrorMessage.dropoffTargetMissing;
            }

            return LaunchAction((byte)ActionType.startDropoff, null, playerCommand);
        }

        private ErrorMessage SendToTargetLocal(bool playerCommand)
        {
            RaiseDropOffStateUpdated(DropOffState.active);

            globalEvent.RaiseUnitResourceDropOffStartGlobal(unit, new ResourceEventArgs(unit.CollectorComponent.Target.instance));

            if (unit.CollectorComponent.Target.instance.IsValid())
            {
                CollectableResourceData dropOffData = dropOffResourcesDic[unit.CollectorComponent.Target.instance.ResourceType];

                dropOffObject = dropOffData.obj;
                if (dropOffObject.IsValid())
                    dropOffObject.SetActive(true);

                unit.AnimatorController?.SetOverrideController(dropOffData.animatorOverrideController.Fetch());
            }

            Target.instance.DropOffTarget.Move(
                unit,
                new AddableUnitData
                {
                    sourceComponent = unit.CollectorComponent,
                    playerCommand = playerCommand
                });

            return ErrorMessage.none;
        }

        public void UpdateCollectedResources(ResourceTypeInfo resourceType, int value)
        {
            collectedResources[resourceType] += value;
        }

        // Forcing drop off means that if the collector has at least one resource unit of any type, it will be dropped off.
        public bool AttemptStartDropOff (bool force = false, ResourceTypeInfo resourceType = null)
        {
            int resourceAmount = collectedResources.Values.Sum();
            if (resourceAmount == 0
                || (!force
                    && resourceAmount < totalMaxCapacity 
                    && (resourceType.IsValid() && collectedResources[resourceType] < dropOffResourcesDic[resourceType].amount)) )
                return false;

            RaiseDropOffStateUpdated(DropOffState.ready);

            SendToTarget(false);

            return true;
        }

        public void Unload()
        {
            RaiseDropOffStateUpdated(unit.CollectorComponent.HasTarget ? DropOffState.goingBack : DropOffState.inactive);

            if (dropOffObject.IsValid())
                dropOffObject.SetActive(false);

            // Only units that belong to a faction can update their faction's resources.
            if(!unit.IsFree) 
                foreach (var cr in collectedResources)
                {
                    if (cr.Value == 0)
                        continue;

                    resourceMgr.UpdateResource(
                        unit.FactionID,
                        new ResourceInput
                        {
                            type = cr.Key,
                            value = new ResourceTypeValue 
                            { 
                                amount = cr.Value,
                                capacity = 0
                            }
                        },
                        add: true);
                }

            ResetCollectedResources();

            unit.AnimatorController?.ResetOverrideController();

            globalEvent.RaiseUnitResourceDropOffCompleteGlobal(unit, new ResourceEventArgs(unit.CollectorComponent.Target.instance));

            if (State != DropOffState.goingBack)
                return;

            // Back to collect the last resource
            unit.CollectorComponent.SetTarget(unit.CollectorComponent.Target, false);
        }

        public void Cancel()
        {
            RaiseDropOffStateUpdated(DropOffState.inactive);

            if (dropOffObject.IsValid())
                dropOffObject.SetActive(false);
        }
        #endregion
    }
}
