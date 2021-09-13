using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.SpellCastExtension
{
    public interface ISpellCastPlacement : IPreRunGameService
    {
        bool IsPlacingSpell { get; }

        float SpellCastPositionYOffset { get; }
        float TerrainMaxDistance { get; }

        bool StartPlacement(SpellCastTask creationTask, SpellCaster spellCaster, SpellCastPlacementOptions options = default);
        bool Stop();
    }
}