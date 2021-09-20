using RTSEngine.Multiplayer.Utilities;
using System.Collections.Generic;

namespace RTSEngine.Multiplayer.Server
{
    public interface IMultiplayerFactionManagerTracker
    {
        bool IsActive { get; }
        /// <summary>
        /// For how many turns is the tracker inactive? This allows to determine when the server can destroy an inactive tracker.
        /// </summary>
        int InactiveTurns { get; }

        MultiplayerFactionManagerTrackerData Data { get; }

        int CurrTurn { get; }

        IEnumerable<float> RTTLog { get; }

        void Init(IMultiplayerManager multiplayerMgr, MultiplayerFactionManagerTrackerData data);
        void Disable();

        bool AddInput(IEnumerable<MultiplayerInputWrapper> newInput, int turnID);

        IEnumerable<MultiplayerInputWrapper> GetRelayInput(int turnID);

        void OnRelayedInputReceived(int turnID, float lastRTT);
    }
}