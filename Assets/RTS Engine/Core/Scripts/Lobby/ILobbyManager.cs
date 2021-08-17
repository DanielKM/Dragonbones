using System;
using System.Collections.Generic;

using RTSEngine.Event;
using RTSEngine.Service;
using RTSEngine.Lobby.Service;
using RTSEngine.Lobby.Utilities;
using RTSEngine.Lobby.UI;

namespace RTSEngine.Lobby
{
    public interface ILobbyManager : IMonoBehaviour, IServicePublisher<ILobbyService>
    {
        string GameCode { get; }

        IEnumerable<ILobbyFactionSlot> FactionSlots { get; }
        int FactionSlotCount { get; }
        ILobbyFactionSlot GetFactionSlot(int factionSlotID);
        ILobbyFactionSlot LocalFactionSlot { get; }

        IEnumerable<LobbyMapData> Maps { get; }
        LobbyMapData CurrentMap { get; }
        LobbyMapData GetMap(int mapID);

        ColorSelector FactionColorSelector { get; }

        DefeatConditionDropdownSelector DefeatConditionSelector { get; }
        TimeModifierDropdownSelector TimeModifierSelector { get; }
        ResourceInputDropdownSelector InitialResourcesSelector { get; }

        LobbyGameData CurrentLobbyGameData { get; }

        bool IsStartingLobby { get; }

        event CustomEventHandler<ILobbyFactionSlot, EventArgs> FactionSlotAdded;
        event CustomEventHandler<ILobbyFactionSlot, EventArgs> FactionSlotRemoved;

        event CustomEventHandler<LobbyGameData, EventArgs> LobbyGameDataUpdated;

        bool IsLobbyGameDataMaster();
        void UpdateLobbyGameDataComplete(LobbyGameData lobbyGameData);
        void UpdateLobbyGameDataRequest(LobbyGameData lobbyGameData);

        void AddFactionSlot(ILobbyFactionSlot newSlot);
        void RemoveFactionSlotRequest(int slotID);
        bool CanRemoveFactionSlot(ILobbyFactionSlot slot);
        void RemoveFactionSlotComplete(ILobbyFactionSlot slot);

        void LeaveLobby();
        void StartLobby();
        bool StartLobbyInterrupt();
    }
}
