using RTSEngine.Entities;

namespace RTSEngine.Health
{
    public interface IBuildingHealth : IFactionEntityHealth
    {
        IBuilding Building { get; }
    }
}
