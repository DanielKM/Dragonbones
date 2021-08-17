
using RTSEngine.Entities;

namespace RTSEngine.Health
{
    public interface IUnitHealth : IFactionEntityHealth
    {
        IUnit Unit { get; }
    }
}
