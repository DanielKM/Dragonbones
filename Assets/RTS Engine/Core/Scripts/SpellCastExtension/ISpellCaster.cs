using RTSEngine.Entities;
using System.Collections.Generic;

namespace RTSEngine.EntityComponent
{
    public interface ISpellCaster : IEntityTargetComponent
    {
        TargetData<ISpell> Target { get; }

        bool InProgress { get; }

        IEnumerable<SpellCastTask> CreationTasks { get; }
        IEnumerable<SpellCastTask> UpgradeTargetCreationTasks { get; }
    }
}
