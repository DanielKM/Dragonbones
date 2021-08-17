using System;
using System.Collections.Generic;

using RTSEngine.Determinism;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Service;

namespace RTSEngine.Game
{
    public interface IGameManager : IMonoBehaviour, IServicePublisher<IPreRunGameService>, IServicePublisher<IPostRunGameService>
    {
        IGameBuilder CurrBuilder { get; }

        GameStateType State { get; }

        DefeatConditionType DefeatCondition { get; }

        TimeModifiedTimer PeaceTimer { get; }
        bool InPeaceTime { get; }

        IEnumerable<IFactionSlot> FactionSlots { get; }
        IFactionSlot GetFactionSlot(int ID);
        int FactionCount { get; }
        int ActiveFactionCount { get; }

        IFactionSlot LocalFactionSlot { get; }
        int LocalFactionSlotID { get; }

        event CustomEventHandler<IGameManager, EventArgs> GameServicesInitialized;
        event CustomEventHandler<IGameManager, EventArgs> GameBuilt;
        event CustomEventHandler<IGameManager, EventArgs> GameStartRunning;

        ErrorMessage OnFactionDefeated(int factionID);
        ErrorMessage OnFactionDefeatedLocal(int factionID);

        void SetState(GameStateType newState);
    }
}
