using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.UnitExtension
{
    public interface IAddableUnit : IMonoBehaviour
    {
        string Code { get; }

        Vector3 GetAddablePosition(IUnit unit);

        ErrorMessage CanMove(IUnit unit, AddableUnitData data = default);

        ErrorMessage Move(IUnit unit, AddableUnitData data);

        ErrorMessage CanAdd(IUnit unit, AddableUnitData data = default);

        ErrorMessage Add (IUnit unit, AddableUnitData data);
    }
}
