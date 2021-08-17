using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Health
{
    public interface IFactionEntityHealth : IEntityHealth
    {
        IFactionEntity FactionEntity { get; }

        void AddDamageOverTime(DamageOverTimeData dotData, int damage, IEntity source);
    }
}
