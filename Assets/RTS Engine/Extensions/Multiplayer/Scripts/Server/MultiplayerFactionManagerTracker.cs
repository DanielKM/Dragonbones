using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Multiplayer.Logging;
using RTSEngine.Multiplayer.Utilities;
using RTSEngine.Logging;

namespace RTSEngine.Multiplayer.Server
{
    public class MultiplayerFactionManagerTracker : IMultiplayerFactionManagerTracker
    {
        #region Attributes
        public bool IsActive { private set; get; } = false;
        public int InactiveTurns { private set; get; } = 0;

        public MultiplayerFactionManagerTrackerData Data { get; private set; }

        public int CurrTurn { private set; get; }

        private float[] rttLog;
        public IEnumerable<float> RTTLog => rttLog;

        private List<MultiplayerInputWrapper>[] inputLog = null;

        // Services
        protected IMultiplayerLoggingService logger { private set; get; }

        // Other components
        protected IMultiplayerManager multiplayerMgr { private set; get; }
        protected IMultiplayerServerGameManager serverGameMgr { private set; get; }
        //protected IMultiplay
        #endregion

        #region Initializing/Terminating
        public void Init(IMultiplayerManager multiplayerMgr, MultiplayerFactionManagerTrackerData data)
        {
            this.multiplayerMgr = multiplayerMgr;
            this.serverGameMgr = multiplayerMgr.ServerGameMgr;
            this.logger = multiplayerMgr.GetService<IMultiplayerLoggingService>();

            this.Data = data;

            rttLog = new float[data.logSize];

            inputLog = new List<MultiplayerInputWrapper>[data.logSize];
            for (int i = 0; i < data.logSize; i++)
                inputLog[i] = new List<MultiplayerInputWrapper>();

            CurrTurn = 0;

            IsActive = true;
            InactiveTurns = 0;
        }

        public void Disable()
        {
            IsActive = false;
        }
        #endregion

        #region Adding/Relaying Input
        public bool AddInput(IEnumerable<MultiplayerInputWrapper> newInputs, int turnID)
        {
            int logIndex = turnID % Data.logSize;

            inputLog[logIndex].AddRange(newInputs);

            int excessAmount = inputLog[logIndex].Count - Data.maxInputCount;
            if (!logger.RequireTrue(excessAmount <= 0,
              $"[{GetType().Name} - Faction ID: {Data.factionID}] Input log list of index {logIndex} has reached its maximum size: {Data.maxInputCount}! Excess Received input ({excessAmount} inputs) is discarded!"))
            {
                inputLog[logIndex].RemoveRange(Data.maxInputCount, excessAmount);
                return false;
            }

            return true;
        }

        public IEnumerable<MultiplayerInputWrapper> GetRelayInput (int turnID)
        {
            int logIndex = turnID % Data.logSize;

            return inputLog[logIndex];
        }

        public void OnRelayedInputReceived (int turnID, float lastRTT)
        {
            // Issue that leads to disconnection occurs here
            if (!logger.RequireTrue(turnID == CurrTurn,
                $"[{GetType().Name} - Server Turn: {serverGameMgr.ServerTurn} - Faction ID: {Data.factionID}] Expected input 'turnID' to be {CurrTurn} but got {turnID} instead.!",
                type: LoggingType.warning))
                return;

            CurrTurn++;
            if (!IsActive)
                InactiveTurns++;

            int logIndex = turnID % Data.logSize;

            // Prepare next inputLog slot
            inputLog[logIndex].Clear();

            rttLog[logIndex] = lastRTT;
        }
        #endregion
    }
}
