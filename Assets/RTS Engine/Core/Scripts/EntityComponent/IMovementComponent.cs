using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Movement;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Terrain;

namespace RTSEngine.EntityComponent
{
    public interface IMovementComponent : IEntityTargetComponent
    {
        bool DestinationReached { get; }
        Vector3 Destination { get; }
        TargetData<IEntity> Target { get; }

        IEnumerable<TerrainAreaType> TerrainAreas { get; }

        MovementFormationSelector Formation { get; }

        IMovementController Controller { get; }

        IMovementTargetPositionMarker TargetPositionMarker { get; }

        event CustomEventHandler<IMovementComponent, MovementEventArgs> MovementStart;
        event CustomEventHandler<IMovementComponent, EventArgs> MovementStop;

        ErrorMessage SetTarget(TargetData<IEntity> newTarget, float stoppingDistance, MovementSource source);
        ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, float stoppingDistance, MovementSource source);

        void OnPathFailure();
        void OnPathPrepared();

        ErrorMessage OnPathDestination(TargetData<IEntity> newTarget, MovementSource source);

        void UpdateRotationTarget (IEntity rotationTarget, Vector3 rotationPosition);

        bool IsPositionReached(Vector3 position);
    }
}
