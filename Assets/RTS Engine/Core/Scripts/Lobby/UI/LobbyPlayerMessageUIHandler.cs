using RTSEngine.Lobby.Audio;
using RTSEngine.Lobby.Logging;
using RTSEngine.UI;

namespace RTSEngine.Lobby.UI
{
    public class LobbyPlayerMessageUIHandler : PlayerMessageUIHandlerBase, ILobbyPlayerMessageUIHandler
    {
        #region Initializing/Terminating
        public void Init(ILobbyManager lobbyMgr)
        {
            InitBase(logger: lobbyMgr.GetService<ILobbyLoggingService>(),
                audioMgr: lobbyMgr.GetService<ILobbyAudioManager>());
        }
        #endregion
    }
}
