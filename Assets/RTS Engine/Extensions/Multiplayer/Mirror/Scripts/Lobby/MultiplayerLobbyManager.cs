using System;
using System.Collections.Generic;

using Mirror;

using RTSEngine.Determinism;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Lobby;
using RTSEngine.Multiplayer.Lobby;
using RTSEngine.Multiplayer.Utilities;

namespace RTSEngine.Multiplayer.Mirror.Lobby
{
    public class MultiplayerLobbyManager : LobbyManagerBase, IMultiplayerLobbyManager
    {
        #region Attributes
        private IMultiplayerManager multiplayerMgr;
        public override bool IsStartingLobby => multiplayerMgr.State == MultiplayerState.startingLobby;
        #endregion

        #region IGameBuilder
        public override bool IsMaster => multiplayerMgr.Role == MultiplayerRole.host || multiplayerMgr.Role == MultiplayerRole.server;

        protected override void OnGameBuiltComplete (IGameManager gameMgr)
        {
            multiplayerMgr.OnGameLoaded(gameMgr);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            multiplayerMgr = NetworkManager.singleton as IMultiplayerManager;

            if (!logger.RequireValid(multiplayerMgr,
              $"[{GetType().Name}] A component that implements the '{typeof(IMultiplayerManager).Name}' interface can not be found!"))
                return; 

            multiplayerMgr.OnLobbyLoaded(this);
        }
        #endregion

        #region Updating Lobby Game Data
        public override bool IsLobbyGameDataMaster()
        {
            return multiplayerMgr.IsValid()
                && multiplayerMgr.Role != MultiplayerRole.server
                && LocalFactionSlot.IsValid()
                && LocalFactionSlot.Role == FactionSlotRole.host;
        }
        #endregion

        #region Adding/Removing Factions Slots
        public override bool CanRemoveFactionSlot(ILobbyFactionSlot slot) => slot.IsValid();

        public override void RemoveFactionSlotRequest(int slotID)
        {
            if (!IsLobbyGameDataMaster())
                return;

            LocalFactionSlot.KickAttempt(slotID);
        }

        protected override void HandleFactionSlotRoleUpdated(ILobbyFactionSlot slot, EventArgs args)
        {
            base.HandleFactionSlotRoleUpdated(slot, args);

            // Assign the local faction slot of the local player.
            // If this is the headless server instance then set it up so that the local slot is the client host one.
            if ((multiplayerMgr.Role == MultiplayerRole.server && slot.Role == FactionSlotRole.host)
                || (slot as NetworkBehaviour).isLocalPlayer)
                LocalFactionSlot = slot;
        }
        #endregion

        #region Starting/Leaving Lobby
        protected override void OnPreLobbyLeave()
        {
            multiplayerMgr.Stop(DisconnectionReason.normal);
        }

        protected override void OnStartLobbyInterrupt()
        {
            multiplayerMgr.InterruptStartLobby();
        }
        #endregion
    }
}
