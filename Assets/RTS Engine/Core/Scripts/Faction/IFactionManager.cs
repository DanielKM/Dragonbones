using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using System.Collections.Generic;

namespace RTSEngine.Faction
{
    public interface IFactionManager
    {
        int FactionID { get; }
        IFactionSlot Slot { get; }

        IEnumerable<IFactionEntity> FactionEntities { get; }

        IEnumerable<IFactionEntity> MainEntities { get; }

        IEnumerable<IUnit> Units { get; }
        IEnumerable<IUnit> GetAttackUnits(float range = 1);

        IEnumerable<IBuilding> Buildings { get; }
        IEnumerable<IBuilding> BuildingCenters { get; }

        event CustomEventHandler<IFactionManager, EntityEventArgs<IFactionEntity>> OwnFactionEntityAdded;
        event CustomEventHandler<IFactionManager, EntityEventArgs<IFactionEntity>> OwnFactionEntityRemoved;

        void Init(IGameManager gameMgr, IFactionSlot slot);

        bool AssignLimits(IEnumerable<FactionEntityAmountLimit> newLimits);
        bool HasReachedLimit(IEntity entity);
        bool HasReachedLimit(string code, string category);
        bool HasReachedLimit(string code, IEnumerable<string> category);
    }
}
