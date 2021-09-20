using System.Collections.Generic;

using RTSEngine.EntityComponent;
using RTSEngine.Health;
using RTSEngine.ResourceExtension;
using RTSEngine.Faction;
using RTSEngine.Event;

namespace RTSEngine.Entities
{
    public interface IFactionEntity : IEntity
    {
        IFactionManager FactionMgr { get; }

        bool IsMainEntity { get; }
        bool IsFactionLocked { get; }

        IEnumerable<ResourceInput> InitResources { get; }
        IEnumerable<ResourceInput> DisableResources { get; }

        new IFactionEntityHealth Health { get; }

        IRallypoint Rallypoint { get; }
        IDropOffTarget DropOffTarget { get; }
        IUnitCarrier UnitCarrier { get; }

    }
}
