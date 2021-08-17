using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.NPC.Event;

namespace RTSEngine.NPC
{
    public interface INPCRegulator
    {
        int Count { get; }
        int CurrPendingAmount { get; }
        bool HasReachedMaxAmount { get; }
        bool HasReachedMinAmount { get; }
        bool HasTargetCount { get; }
        int MaxPendingAmount { get; }
        int MaxTargetAmount { get; }
        int MinTargetAmount { get; }
        int TargetCount { get; }
        IFactionEntity Prefab { get; }

        void UpdateTargetCount(int newTargetCount);
    }
}