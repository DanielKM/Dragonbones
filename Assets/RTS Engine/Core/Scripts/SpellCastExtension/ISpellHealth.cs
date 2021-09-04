using RTSEngine.Entities;

namespace RTSEngine.Health
{
    public interface ISpellHealth : IFactionEntityHealth
    {
        ISpell Spell { get; }
    }
}
