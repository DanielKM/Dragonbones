using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Effect;

namespace RTSEngine.Attack
{
    public interface IAttackManager : IPreRunGameService
    {
        IEffectObject TerrainAttackTargetEffect { get; }

        bool CanMoveAttack { get; }
        IEffectObject MoveAttackTargetEffect { get; }

        bool CanLaunchTerrainAttack<T>(LaunchAttackData<T> data);

        ErrorMessage LaunchAttack(LaunchAttackData<IEntity> data);
        ErrorMessage LaunchAttackLocal(LaunchAttackData<IEntity> data);

        ErrorMessage LaunchAttack(LaunchAttackData<IEnumerable<IEntity>> data);
        ErrorMessage LaunchAttackLocal(LaunchAttackData<IEnumerable<IEntity>> data);

        bool TryGetAttackPosition(IEntity attacker, IFactionEntity target, Vector3 targetPosition, bool playerCommand, out Vector3 attackPosition);
    }
}