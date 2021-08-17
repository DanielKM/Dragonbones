using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.ResourceExtension;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IDropOffSource : IEntityTargetComponent
    {
        DropOffState State { get; }
        IReadOnlyDictionary<ResourceTypeInfo, int> CollectedResources { get; }

        event CustomEventHandler<IDropOffSource, EventArgs> DropOffStateUpdated;

        void UpdateCollectedResources(ResourceTypeInfo resourceType, int value);

        void UpdateTarget();
        ErrorMessage SendToTarget(bool playerCommand);

        bool AttemptStartDropOff(bool force = false, ResourceTypeInfo resourceType = null);

        void Unload();
        void Cancel();
    }
}