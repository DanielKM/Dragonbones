using RTSEngine.Logging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Lobby.Logging
{
    public class LobbyLogger : LoggerBase, ILobbyLoggingService
    {
        protected ILobbyManager lobbyMgr { private set; get; }

        public void Init(ILobbyManager lobbyMgr)
        {
            this.lobbyMgr = lobbyMgr;
        }
    }
}
