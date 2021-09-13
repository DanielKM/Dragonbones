using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.SpellCastExtension
{
    public interface ISpellRange : IMonoBehaviour
    {
        ISpell Spell { get; }
        bool IsActive { get; }

        int SortingOrder { get; }
        float Size { get; }
        float Surface { get; }

        IEnumerable<ISpell> SpellsInRange { get; }
        IEnumerable<IResource> ResourcesInRange { get; }
        IEnumerable<IEntity> EntitiesInRange { get; }

        void Init(IGameManager gameMgr, ISpell spell);
        void Disable();

        bool IsInRange(Vector3 testPosition);

        bool AllowSpellInBorder(ISpell spell);

        void RegisterSpell(ISpell newSpell);
        void UnegisterSpell(ISpell oldSpell);
    }
}
