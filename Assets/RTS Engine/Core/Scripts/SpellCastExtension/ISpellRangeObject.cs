using RTSEngine.Game;

namespace RTSEngine.SpellCastExtension
{
    public interface ISpellRangeObject : IMonoBehaviour
    {
        void Init(IGameManager gameMgr, ISpellRange spellRange);
    }
}