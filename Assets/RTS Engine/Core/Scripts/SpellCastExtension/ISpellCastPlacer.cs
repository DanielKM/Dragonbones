using UnityEngine;
using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Terrain;

namespace RTSEngine.SpellCastExtension
{
    public interface ISpellCastPlacer : IMonoBehaviour
    {
        ISpell Spell { get; }

        IEnumerable<TerrainAreaType> PlacableTerrainAreas { get; }

        ISpellRange PlacementCenter { get; }

        bool CanPlace { get; }
        bool CanPlaceOutsideRange { get; }
        bool Placed { get; }

        void OnPlacementStart(SpellCastPlacement spellCastPlacement);
        void OnPositionUpdate(Vector3 newSpellCasterLocation, Vector3 newSpellCastLocation);
    }
}
