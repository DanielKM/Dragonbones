using UnityEngine;

using RTSEngine.Lobby.Logging;
using RTSEngine.Logging;

namespace RTSEngine.Lobby.Utilities
{
    [System.Serializable]
    public struct ColorSelector
    {
        [Tooltip("In case the below 'Allowed' colors array is not populated, this color will be used.")]
        public Color defaultColor;

        [Tooltip("Colors that a faction is allowed to have.")]
        public Color[] allowed;

        public Color Get(int index) => index.IsValidIndex(allowed) ? allowed[index] : defaultColor;

        public int GetNextIndex(int index) => index.GetNextIndex(allowed);

        public void Init(ILobbyManager lobbyMgr)
        {
            ILoggingService logger = lobbyMgr.GetService<ILobbyLoggingService>();

            logger.RequireTrue(allowed.Length > 0,
              $"[{GetType().Name}] The 'Allowed' colors array has not been populated, the 'Default Color' will be used for all faction slot colors.",
              source: lobbyMgr,
              type: LoggingType.warning);
        }
    }
}
