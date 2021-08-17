using System;
using System.Collections.Generic;

using RTSEngine.Determinism;
using RTSEngine.Event;
using RTSEngine.Faction;

namespace RTSEngine.Game
{
    public interface IGameBuilder : IMonoBehaviour
    {
        /// <summary>
        /// Represents the instance of the game that is responsible for running NPC factions and free units/buildings.
        /// In the case of a multiplayer game, this would be the instance where the server is running.
        /// In the case of a singleplayer game, this would be the local player's instance.
        /// </summary>
        bool IsMaster { get; }

        // Determines a valid instance of the IInputAdder can be used to relay inputs
        bool IsInputAdderReady { get; }
        IInputAdder InputAdder { get; }
        void OnInputAdderReady(IInputAdder inputAdder);
        // Raised when the InputAdder is ready to handle and relay inputs
        event CustomEventHandler<IGameBuilder, EventArgs> InputAdderReady;

        GameData Data { get; }

        int FactionSlotCount { get; }

        IEnumerable<FactionSlotData> FactionSlotDataSet { get; }

        void OnGameBuilt(IGameManager gameMgr);
        void OnGameLeave();
    }
}
