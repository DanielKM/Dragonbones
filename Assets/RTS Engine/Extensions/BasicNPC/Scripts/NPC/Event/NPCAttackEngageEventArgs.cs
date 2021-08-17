using System;

using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.NPC.Event
{
    public class NPCAttackEngageEventArgs : EventArgs
    {
        public IFactionEntity Target { private set; get; }
        public Vector3 TargetPosition { private set; get; }

        public NPCAttackEngageEventArgs(IFactionEntity target, Vector3 targetPosition)
        {
            Target = target;
            TargetPosition = targetPosition;
        }
    }
}
