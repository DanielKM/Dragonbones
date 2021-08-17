using System;
using System.Collections;
using System.Linq;

using UnityEngine;

using RTSEngine.Multiplayer.Logging;

namespace RTSEngine.Multiplayer.Server
{
    [System.Serializable]
    public class TurnHandler
    {
        #region Attributes
        [SerializeField, Tooltip("The allowed duration (in seconds) range that a turn can have. The server is able to adjust the turn time during the game depending on the latency of the clients but it will always keep it inside this range.")]
        private FloatRange turnTimeRange = new FloatRange(0.1f, 0.5f);
        [SerializeField, Tooltip("The initial turn duration (in seconds).")]
        private float defaultTurnTime = 0.2f;
        private float turnTime;

        private Coroutine turnCoroutine;
        private Action onTurnComplete;

        // Services
        protected IMultiplayerLoggingService logger { private set; get; }

        // Other components
        protected IMultiplayerManager multiplayerMgr { private set; get; }
        protected IMultiplayerServerGameManager serverGameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IMultiplayerManager multiplayerMgr, Action onTurnComplete)
        {
            this.multiplayerMgr = multiplayerMgr;
            this.serverGameMgr = multiplayerMgr.ServerGameMgr;

            this.logger = multiplayerMgr.GetService<IMultiplayerLoggingService>();

            if (!logger.RequireTrue(turnCoroutine == null,
              $"[{GetType().Name}] Can not initialize while there's an active turn coroutine running, please call Disable() first."))
                return; 

            this.onTurnComplete = onTurnComplete;

            SetTurnTime();
            turnCoroutine = serverGameMgr.StartCoroutine(UpdateTurn());
        }

        public void Disable()
        {
            serverGameMgr.StopCoroutine(turnCoroutine);
            turnCoroutine = null;
        }
        #endregion

        #region Handling Turn Update
        private IEnumerator UpdateTurn()
        {
            while (true)
            {
                yield return new WaitForSeconds(turnTime);
                onTurnComplete();
            }
        }

        public void UpdateTurnTime(float[][] rttLogs)
        {
            turnTime = turnTimeRange.Clamp(rttLogs
                .Select(clientRTTLog => clientRTTLog.Sum() / clientRTTLog.Length)
                .Sum() / rttLogs.Length);
        }

        private void SetTurnTime()
        {
            turnTime = turnTimeRange.Clamp(defaultTurnTime);
        }
        #endregion
    }
}
