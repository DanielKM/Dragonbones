using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public interface IEntityWorkerManager : IAddableUnit
    {
        int Amount { get; }
        int MaxAmount { get; }
        bool HasMaxAmount { get; }

        IEnumerable<IUnit> Workers { get; }

        Vector3 GetOccupiedPosition(IUnit worker, out bool isStaticPosition);
        void Remove(IUnit worker);

        event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerAdded;
        event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerRemoved;
    }
}
