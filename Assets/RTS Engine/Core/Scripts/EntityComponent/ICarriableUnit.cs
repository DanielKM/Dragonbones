using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public interface ICarriableUnit : IEntityTargetComponent
    {
        IUnitCarrier CurrCarrier { get; }

        AddableUnitData GetAddableData(bool playerCommand);
    }
}