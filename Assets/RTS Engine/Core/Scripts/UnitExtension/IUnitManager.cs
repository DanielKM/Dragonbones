using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.UnitExtension
{
    public interface IUnitManager : IPreRunGameService
    {
        IEnumerable<IUnit> AllUnits { get; }
        AnimatorOverrideController DefaultAnimController { get; }

        Color FreeUnitColor { get; }
        IEnumerable<IUnit> FreeUnits { get; }

        ErrorMessage CreateUnit(IUnit unitPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitUnitParameters initParams);
        IUnit CreateUnitLocal(IUnit unitPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitUnitParameters initParams);
    }
}