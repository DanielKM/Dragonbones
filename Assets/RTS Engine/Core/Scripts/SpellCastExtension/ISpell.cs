using System;

using RTSEngine.SpellCastExtension;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Health;

namespace RTSEngine.Entities
{
    public interface ISpell : IFactionEntity
    {
        bool IsCast { get; }
        bool IsPlacementInstance { get; }

        ISpellCastPlacer PlacerComponent { get; }

        new ISpellHealth Health { get; }

        event CustomEventHandler<ISpell, EventArgs> SpellCastComplete;

        void Init(IGameManager gameMgr, InitSpellParameters initParams);
        void InitPlacementInstance(IGameManager gameMgr, InitSpellParameters initParams);
    }
}
