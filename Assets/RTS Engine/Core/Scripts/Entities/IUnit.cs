using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Health;

namespace RTSEngine.Entities
{
    public interface IUnit : IFactionEntity
    {
        IRallypoint SpawnRallypoint { get; }
        IEntityComponent CreatorEntityComponent { get; }

        IDropOffSource DropOffSource { get; }
        IResourceCollector CollectorComponent { get; }
        IBuilder BuilderComponent { get; }
        ICarriableUnit CarriableUnit { get; }
        new IUnitHealth Health { get; }

        void Init(IGameManager gameMgr, InitUnitParameters initParams);
    }
}
