using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.ResourceExtension;
using RTSEngine.Event;
using RTSEngine.Movement;
using RTSEngine.Health;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public class ResourceCollector : FactionEntityTargetProgressComponent<IResource>, IResourceCollector
    {
        #region Class Attributes
        protected IUnit unit { private set; get; }

        [SerializeField, Tooltip("What types of resources can be collected?")]
        private CollectableResourceData[] collectableResources = new CollectableResourceData[0];
        private IReadOnlyDictionary<ResourceTypeInfo, CollectableResourceData> collectableResourcesDic = null;

        /*[SerializeField, Tooltip("Search for resources with a priority order as defined in the 'Collectable Resources' field.")]
        private bool prioritizeResourceSearch = true;*/

        protected IResourceManager resourceMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnProgressInit()
        {
            base.OnProgressInit();

            this.resourceMgr = gameMgr.GetService<IResourceManager>(); 

            unit = Entity as IUnit;

            if (!logger.RequireTrue(resourceMgr.CanAutoCollect || unit.DropOffSource != null,
                $"[{GetType().Name} - {Entity.Code}] A component that extends {typeof(IDropOffSource).Name} interface must be attached to this resource collector since resources can not be auto collected!"))
                return;

            // Allows for constant access time to collectable resource data rather than having to go through the list each time
            collectableResourcesDic = collectableResources.ToDictionary(cr => cr.type, cr => cr);
        }
        #endregion

        #region Handling Events: Collected Resource
        private void HandleTargetHealthUpdated(IEntity resource, HealthUpdateEventArgs e)
        {
            if (e.Source != unit
                || resource != Target.instance)
                return;

            if (resourceMgr.CanAutoCollect && !unit.IsFree)
            {
                resourceMgr.UpdateResource(
                    unit.FactionID,
                    new ResourceInput
                    {
                        type = Target.instance.ResourceType,
                        value = new ResourceTypeValue 
                        { 
                            amount = -e.Value,
                            capacity = 0
                        }
                    },
                    add: true);
                return;
            }

            unit.DropOffSource.UpdateCollectedResources(Target.instance.ResourceType, -e.Value);

            // Stop the collection audio
            audioMgr.StopSFX(unit.AudioSourceComponent);

            // Hide the source and target effect objects during drop off.
            ToggleSourceTargetEffect(false);

            AttemptDropOff();
        }

        private void HandleEntityDead(IEntity resource, DeadEventArgs e)
        {
            Stop();
            unit.DropOffSource.SendToTarget(false);
        }
        #endregion

        #region Updating Component State
        protected override bool MustStopProgress()
        {
            return Target.instance.Health.IsDead
                || (!Target.instance.CanCollectOutsideBorder && !RTSHelper.IsSameFaction(Target.instance, factionEntity))
                || (InProgress && !IsTargetInRange(transform.position, Target) && (resourceMgr.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive));
        }

        protected override bool CanEnableProgress()
        {
            return IsTargetInRange(transform.position, Target)
                && (resourceMgr.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive || unit.DropOffSource.State == DropOffState.goingBack);
        }

        protected override bool CanProgress() => resourceMgr.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive;

        protected override bool MustDisableProgress() => !resourceMgr.CanAutoCollect && unit.DropOffSource.State != DropOffState.inactive;

        protected override void OnStop(TargetData<IResource> lastTarget, bool wasInProgress)
        {
            inProgressObject = null;

            unit.DropOffSource?.Cancel();

            if (lastTarget.instance.IsValid())
            {
                lastTarget.instance.WorkerMgr.Remove(unit);

                lastTarget.instance.Health.EntityHealthUpdated -= HandleTargetHealthUpdated;
                lastTarget.instance.Health.EntityDead -= HandleEntityDead;
            }
        }
        #endregion

        protected override void OnTargetUpdate()
        {
            if (resourceMgr.CanAutoCollect)
                return;
        }

        private void AttemptDropOff()
        {
            if (!resourceMgr.CanAutoCollect)
            {
                if (unit.DropOffSource.AttemptStartDropOff(force: false, resourceType: Target.instance.ResourceType))
                {
                    DisableProgress();
                    return;
                }
                else if(unit.DropOffSource.State != DropOffState.goingBack)
                    // Cancel drop off if it was pending
                    unit.DropOffSource.Cancel();
            }
        }

        #region Handling Progress
        protected override void OnInProgressEnabled()
        {
            audioMgr.PlaySFX(unit.AudioSourceComponent, Target.instance.CollectionAudio, true);

            //unit is coming back after dropping off resources?
            if (!resourceMgr.CanAutoCollect && unit.DropOffSource.State == DropOffState.goingBack)
                unit.DropOffSource.Cancel();
            else
                globalEvent.RaiseEntityComponentTargetStartGlobal(this, new TargetDataEventArgs(Target));

            unit.MovementComponent.UpdateRotationTarget(Target.instance, Target.instance.transform.position);
        }

        protected override void OnProgress()
        {
            Target.instance.Health.Add(-collectableResourcesDic[Target.instance.ResourceType].amount, unit);
        }
        #endregion

        #region Searching/Updating Target
        public override bool CanSearch => true;

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            Vector3 workerPosition = (target.instance as IResource).WorkerMgr.GetOccupiedPosition(unit, out bool isStaticPosition);

            return isStaticPosition
                ? Vector3.Distance(sourcePosition, workerPosition) <= mvtMgr.StoppingDistance
                : base.IsTargetInRange(sourcePosition, target);
        }

        public bool IsResourceTypeCollectable(ResourceTypeInfo resourceType)
        {
            return collectableResourcesDic != null
                ? collectableResourcesDic.ContainsKey(resourceType)
                : collectableResources.Select(cr => cr.type == resourceType).Any();
        }

        public override ErrorMessage IsTargetValid (TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IResource> potentialTarget = testTarget;

            if (!potentialTarget.instance.IsValid())
                return ErrorMessage.invalid;
            else if (!factionEntity.IsInteractable)
                return ErrorMessage.invalid;
            else if (!potentialTarget.instance.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (!potentialTarget.instance.CanCollect)
                return ErrorMessage.resourceNotCollectable;
            else if (!potentialTarget.instance.CanCollectOutsideBorder && !potentialTarget.instance.IsFriendlyFaction(factionEntity))
                return ErrorMessage.resourceTargetOutsideTerritory;
            else if (!IsResourceTypeCollectable(potentialTarget.instance.ResourceType))
                return ErrorMessage.entityCompTargetPickerUndefined;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.dead;
            // Check if this is not the same resource that is being collected before checking if it has max collectors (in case player is asking the unit to collect the resource it is actively collecting).
            else if (!factionEntity.CanMove && !IsTargetInRange(transform.position, potentialTarget))
                return ErrorMessage.entityCompTargetOutOfRange;

            return potentialTarget.instance.WorkerMgr.CanMove(
                unit,
                new AddableUnitData
                {
                    allowDifferentFaction = true
                });
        }

        protected override void OnTargetPostLocked(bool playerCommand, bool sameTarget)
        {
            // In this component, the in progress object depends on the type of resource that is being collected.
            inProgressObject = collectableResourcesDic[Target.instance.ResourceType].obj;
            progressOverrideController = collectableResourcesDic[Target.instance.ResourceType].animatorOverrideController;

            if(Target.instance.WorkerMgr.Move(
                unit,
                new AddableUnitData
                {
                    allowDifferentFaction = Target.instance.CanCollectOutsideBorder,

                    sourceComponent = this,

                    playerCommand = playerCommand
                }) != ErrorMessage.none)
            {
                unit.DropOffSource?.Cancel();
                Stop();
                return;
            }

            // For the worker component manager, make sure that enough worker positions is available even in the local method.
            // Since they are only updated in the local method, meaning that the SetTarget method would always relay the input in case a lot of consecuive calls are made...
            //... on the same resource from multiple collectors.

            if (sameTarget)
            {
                AttemptDropOff();
                return;
            }

            Target.instance.Health.EntityHealthUpdated += HandleTargetHealthUpdated;
            Target.instance.Health.EntityDead += HandleEntityDead;

            globalEvent.RaiseEntityComponentTargetLockedGlobal(this, new TargetDataEventArgs(Target));

            unit.DropOffSource.UpdateTarget();

            AttemptDropOff();
        }
        #endregion
    }
}
