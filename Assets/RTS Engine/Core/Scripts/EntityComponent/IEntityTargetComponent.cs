using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IEntityTargetComponent : IEntityComponent
    {
        int Priority { get; }

        bool HasTarget { get; }
        bool RequireIdleEntity { get; }
        bool IsIdle { get; }

        void Stop();

        bool CanSearch { get; }

        bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target);

        ErrorMessage IsTargetValid(TargetData<IEntity> testTarget, bool playerCommand);
        ErrorMessage SetTarget(TargetData<IEntity> newTarget, bool playerCommand);
        ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, bool playerCommand);

        AudioClip OrderAudio { get; }
    }
}
