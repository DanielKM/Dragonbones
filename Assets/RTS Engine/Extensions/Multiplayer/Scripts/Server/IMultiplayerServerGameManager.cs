using System.Collections.Generic;

using RTSEngine.Determinism;

namespace RTSEngine.Multiplayer.Server
{
    public interface IMultiplayerServerGameManager : IMonoBehaviour
    {
        int ServerTurn { get; }

        void Init(IMultiplayerManager multiplayerManager);

        void AddInput(IEnumerable<CommandInput> inputs, int factionID);
        void OnRelayedInputReceived(int factionID, int turnID, float lastRTT);
        void UpdateTurnTimeWithRTTLogs();
    }
}
