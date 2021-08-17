using Mirror;
using RTSEngine.Lobby;
using RTSEngine.Multiplayer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace RTSEngine.Multiplayer.Mirror.Lobby
{
    public class MultiplayerLobbyUIManager : LobbyUIManagerBase
    {
        #region Attributes
        protected IMultiplayerManager multiplayerMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            this.multiplayerMgr = NetworkManager.singleton as IMultiplayerManager;

            if (!logger.RequireValid(multiplayerMgr,
              $"[{GetType().Name}] A component that implements the '{typeof(IMultiplayerManager).Name}' interface can not be found!"))
                return; 

            // Server is not allowed to update the UI, only the host can.
            if(this.multiplayerMgr.Role == MultiplayerRole.server)
                SetInteractable(false);
        }

        protected override void OnDestroyed()
        {
        }
        #endregion
    }
}
