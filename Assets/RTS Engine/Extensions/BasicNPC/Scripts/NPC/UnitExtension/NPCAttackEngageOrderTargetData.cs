using RTSEngine.Determinism;
using RTSEngine.Entities;
using UnityEngine;

namespace RTSEngine.NPC.UnitExtension
{
    public struct NPCAttackEngageOrderTargetData
    {
        public IFactionEntity target;
        public Vector3 targetPosition;

        public TimeModifiedTimer delayTimer;
    }
}
