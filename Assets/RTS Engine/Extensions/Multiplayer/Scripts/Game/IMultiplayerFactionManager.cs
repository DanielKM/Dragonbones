using RTSEngine.Determinism;
using RTSEngine.Faction;
using RTSEngine.Multiplayer.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Multiplayer.Game
{
    public interface IMultiplayerFactionManager : IInputAdder
    {
        bool IsInitialized { get; }
        bool IsValidated { get; }

        IFactionSlot GameFactionSlot { get; }

        int CurrTurn { get; }
        int LastInputID { get; }

        bool IsSimPaused { get; }
        double LastRTT { get; }

        void OnAllClientsValidatedServer();

        void RelayInput(IEnumerable<MultiplayerInputWrapper> relayedInputs, int lastInputID, int serverTurn);

        void PauseSimulation(bool enable);
    }
}
