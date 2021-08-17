using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Attack
{
    public interface IAttackObject : IEffectObject
    {
        AttackObjectLaunchData Data { get; }
        bool InDelay { get; }

        void Launch(AttackObjectLaunchData data);
    }
}
