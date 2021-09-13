using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.SpellCastExtension
{
    public interface ISpellCastManager : IPreRunGameService
    {
        IEnumerable<ISpellRange> AllSpellRanges { get; }
        int LastBorderSortingOrder { get; }

        Color FreeSpellColor { get; }
        IEnumerable<ISpell> FreeSpells { get; }

        ErrorMessage CreatePlacedSpell(ISpell spellPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitSpellParameters initParams);
        ISpell CreatePlacedSpellLocal(ISpell spellPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitSpellParameters initParams);
    }
}