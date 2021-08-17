using System.Collections.Generic;

using RTSEngine.EntityComponent;
using RTSEngine.Health;
using RTSEngine.ResourceExtension;
using RTSEngine.Faction;
using RTSEngine.Event;
#if RTSENGINE_FOW
using FoW;
#endif

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

#if RTSENGINE_FOW
        FogOfWarUnit FoWUnit { get; }
#endif
    }
}
