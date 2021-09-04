using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Terrain;

namespace RTSEngine.SpellCastExtension
{
    public interface ISpellCastPlacer : IMonoBehaviour
    {
        ISpell Spell { get; }

        IEnumerable<TerrainAreaType> PlacableTerrainAreas { get; }

        bool CanPlace { get; }
        bool CanPlaceOutsideBorder { get; }
        bool Placed { get; }

        void OnPlacementStart();
        void OnPositionUpdate();
    }
}
