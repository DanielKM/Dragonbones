using RTSEngine.Audio;
using RTSEngine.Lobby.Logging;

namespace RTSEngine.Lobby.Audio
{
    public class LobbyAudioManager : AudioManagerBase, ILobbyAudioManager
    {
        #region Attributes
        protected ILobbyLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(ILobbyManager lobbyMgr)
        {
            this.logger = lobbyMgr.GetService<ILobbyLoggingService>();
        }
        #endregion
    }
}
