using System.Collections.Generic;

namespace RTSEngine.EntityComponent
{
    public interface IEntityComponentGroupDisplayer
    {
        IEnumerable<IEntityComponent> EntityComponents { get; }

        IEnumerable<IEntityTargetComponent> EntityTargetComponents { get; }
    }
}
