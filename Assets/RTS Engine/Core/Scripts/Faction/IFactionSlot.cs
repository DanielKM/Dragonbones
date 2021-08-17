using System;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.NPC;
using RTSEngine.Event;

namespace RTSEngine.Faction
{
    public interface IFactionSlot
    {
        FactionSlotState State { get; }

        FactionSlotData Data { get; }

        int ID { get; }
        IFactionManager FactionMgr { get; }

        Vector3 FactionSpawnPosition { get; }

        INPCManager CurrentNPCMgr { get; }

        event CustomEventHandler<IFactionSlot, EventArgs> FactionSlotStateUpdated;

        void Init(FactionSlotData data, int ID, IGameManager gameMgr);
        void InitDefaultFactionEntities();

        void UpdateState(FactionSlotState newState);
        void UpdateRole(FactionSlotRole newRole);
    }
}
