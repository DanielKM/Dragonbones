
using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.Attack
{
    public struct LaunchAttackData<T>
    {
        public T source;

        public IFactionEntity targetEntity;
        public bool allowTerrainAttack;

        public Vector3 targetPosition;

        public bool playerCommand;
    }
}