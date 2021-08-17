using System;
using System.Collections.Generic;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;

namespace RTSEngine.NPC
{
    public interface INPCManager : IMonoBehaviour
    {
        NPCType Type { get; }
        IFactionManager FactionMgr { get; }

        event CustomEventHandler<INPCManager, EventArgs> InitComplete;

        T GetNPCComponent<T>() where T : INPCComponent;

        IEnumerable<T> GetNPCComponentSet<T>() where T : INPCComponent;

        void Init(NPCType npcType, IGameManager gameMgr, IFactionManager factionMgr);
    }
}
