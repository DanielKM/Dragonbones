using RTSEngine.Entities;
using RTSEngine.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Health
{
    public interface IEntityHealth : IMonoBehaviour
    {
        bool IsInitialized { get; }

        IEntity Entity { get; }
        EntityType EntityType { get; }

        int MaxHealth { get; }
        int CurrHealth { get; }
        bool HasMaxHealth { get; }
        float HealthRatio { get; }
        float HoverHealthBarY { get; }

        // WARNING: Make sure that changing these values is locally synced.
        bool CanIncrease { get; set; }
        bool CanDecrease { get; set; }
        bool CanBeAttacked { get; set; }

        bool IsDead { get; }
        IEntity TerminatedBy { get; }

        ErrorMessage CanAdd(int updateValue, IEntity source);
        ErrorMessage Add(int updateValue, IEntity source);
        ErrorMessage AddLocal(int updateValue, IEntity source, bool force = false);

        ErrorMessage CanDestroy(bool upgrade, IEntity source);
        ErrorMessage Destroy(bool upgrade, IEntity source);
        ErrorMessage DestroyLocal(bool upgrade, IEntity source);
        ErrorMessage SetMax(int value, IEntity source);
        ErrorMessage SetMaxLocal(int value, IEntity source);

        event CustomEventHandler<IEntity, HealthUpdateEventArgs> EntityHealthUpdated;
        event CustomEventHandler<IEntity, DeadEventArgs> EntityDead;
    }
}
