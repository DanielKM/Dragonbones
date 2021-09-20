using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Logging;
using RTSEngine.Multiplayer.Game;
using RTSEngine.Multiplayer.Logging;
using RTSEngine.Multiplayer.Utilities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Multiplayer.Event;

namespace RTSEngine.Multiplayer.Server
{
    public partial class MultiplayerServerGameManager : MonoBehaviour, IMultiplayerServerGameManager
    {
        #region Attributes
        public ServerGameState CurrState { private set; get; } = ServerGameState.initial;

        // Client validation
        private int validatedAmount = -1;

        /// <summary>
        /// Represents the turn step that the server is attempting to sync to all its clients.
        /// All turns smaller than this value have been successfully synced to all clients in the game.
        /// </summary>
        public int ServerTurn { private set; get; } = -1;

        /// <summary>
        /// This is only considered when the instance running this server is a headless server.
        /// This allows for the headless server to have a synced game with all of its clients.
        /// </summary>
        public int LocalServerTurn { private set; get; } = -1;

        /// <summary>
        /// Each added input coming from a client gets a unique identifier number represented by the value of this property.
        /// This property is incremented with each added input.
        /// </summary>
        public int NextInputID { private set; get; } = -1;

        [SerializeField, Tooltip("Maximum amount of input and RTT records that can be stored in the server tracker's cache per client.")]
        private int logSize = 10;

        [SerializeField, Tooltip("Maximum amount of input that are stored in the server tracker's log per client, per server turn.")]
        private int maxInputCount = 10;

        private Dictionary<int, IMultiplayerFactionManagerTracker> multiFactionMgrTrackers = null;

        public int TotalFactionCount => multiplayerMgr.CurrentGameMgr.FactionCount;
        public int ActiveFactionCount => multiplayerMgr.CurrentGameMgr.ActiveFactionCount;

        public bool AwaitingReceipt { get; private set; }

        [SerializeField, Tooltip("When the simulation is paused, every client who has not caught up to the server's turn will be kicked. It is recommneded that this value is higher than a couple lockstep turns.")]
        private float clientTimeoutReleaseTime = 5.0f;
        private float clientTimeoutReleaseTimer;

        [SerializeField, Tooltip("Handles lockstep turns and updating timer based on clients response times.")]
        private TurnHandler turnHandler = new TurnHandler();

        // Holds faction IDs of clients that timed out during the game and for which faction slots will be marked as defeated when the simulation resumes
        private List<int> disconnectedFactionIDs; 

        // Game Services
        protected IInputManager inputMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        // Multiplayer Services
        protected IMultiplayerLoggingService logger { private set; get; }

        protected IMultiplayerManager multiplayerMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IMultiplayerManager multiplayerMgr)
        {
            this.multiplayerMgr = multiplayerMgr;

            // Get multiplayer/game services
            this.logger = multiplayerMgr.GetService<IMultiplayerLoggingService>();
            this.inputMgr = multiplayerMgr.CurrentGameMgr.GetService<IInputManager>();
            this.globalEvent = multiplayerMgr.CurrentGameMgr.GetService<IGlobalEventPublisher>();

            ServerTurn = -1;

            SetState(ServerGameState.awaitingValidation);

            multiFactionMgrTrackers = new Dictionary<int, IMultiplayerFactionManagerTracker>();

            disconnectedFactionIDs = new List<int>();

            multiplayerMgr.MultiplayerFactionManagerValidated += HandleMultiplayerFactionManagerValidated;

            globalEvent.GameStateUpdatedGlobal += HandleGameStateUpdatedGlobal;
        }

        private void OnDestroy()
        {
            multiplayerMgr.MultiplayerFactionManagerValidated -= HandleMultiplayerFactionManagerValidated;

            globalEvent.GameStateUpdatedGlobal -= HandleGameStateUpdatedGlobal;

            turnHandler.Disable();
        }
        #endregion

        #region Handling Event: GameStateUpdated
        private void HandleGameStateUpdatedGlobal(IGameManager gameMgr, EventArgs args)
        {
            // When the game is running, say after it being paused due to a client timing out
            // We need to look over at the faction IDs of the clients that timed out in order to send a faction defeat command to their faction slots
            // We can not do this directly because as the client times out, the game simulation is paused and we are unable to send commands
            if(gameMgr.State == GameStateType.running)
            {
                foreach(int factionID in disconnectedFactionIDs)
                    gameMgr.OnFactionDefeated(factionID);

                disconnectedFactionIDs.Clear();
            }
        }
        #endregion

        #region Handling State Update
        private void SetState(ServerGameState newState)
        {
            switch(CurrState)
            {
                case ServerGameState.initial:

                    if (!logger.RequireTrue(newState == ServerGameState.awaitingValidation,
                      $"[{GetType().Name} - Server Turn: {ServerTurn}] It is not allowed to move the server's state from {CurrState} to {newState}!"))
                        return; 

                    validatedAmount = 0;

                    logger.Log($"[{GetType().Name} - Server Turn: {ServerTurn}] Started listening to validate that the game started for each client");

                    break;

                case ServerGameState.awaitingValidation:

                    if (!logger.RequireTrue(newState == ServerGameState.simRunning,
                      $"[{GetType().Name} - Server Turn: {ServerTurn}] It is not allowed to move the server's state from {CurrState} to {newState}!"))
                        return;

                    else if (!logger.RequireTrue(validatedAmount == TotalFactionCount,
                      $"[{GetType().Name} - Server Turn: {ServerTurn}] Can not start the simulation while not all factions have been validated to start the game!"))
                        return;

                    // Initial server turn:
                    ServerTurn = 0;
                    LocalServerTurn = 0;

                    // Initial input ID (set to -1 since this is incremented before the input is assigned an identifier).
                    NextInputID = -1;

                    AwaitingReceipt = false;

                    turnHandler.Init(multiplayerMgr, OnTurnComplete);
                    // Set the initial turn time based on the initial RTT collected when the mutliplayer faction manager was validated
                    turnHandler.UpdateTurnTime(
                        multiFactionMgrTrackers.Values
                        .Select(tracker => new float[] { tracker.Data.initialRTT })
                        .ToArray()
                    );

                    logger.Log($"[{GetType().Name} - Server Turn: {ServerTurn}] Lockstep turn handler has been initialized and the simulation has now started running.");

                    break;

                case ServerGameState.simRunning:

                    if (!logger.RequireTrue(newState == ServerGameState.simPaused,
                      $"[{GetType().Name} - Server Turn: {ServerTurn}] It is not allowed to move the server's state from {CurrState} to {newState}!"))
                        return;

                    // Pausing the simulation
                    foreach (IMultiplayerFactionManager multiFactionMgr in multiplayerMgr.MultiplayerFactionMgrs)
                        multiFactionMgr.PauseSimulation(true);

                    clientTimeoutReleaseTimer = clientTimeoutReleaseTime;

                    logger.Log(
                        $"[{GetType().Name} - Server Turn: {ServerTurn}] Simulation paused due to non receipt of relayed input confirmations from clients.",
                        source: this,
                        type: LoggingType.warning);

                    break;

                case ServerGameState.simPaused:

                    if (!logger.RequireTrue(newState == ServerGameState.simRunning,
                      $"[{GetType().Name} - Server Turn: {ServerTurn}] It is not allowed to move the server's state from {CurrState} to {newState}!"))
                        return;

                    // Resuming the simulation
                    foreach (IMultiplayerFactionManager multiFactionMgr in multiplayerMgr.MultiplayerFactionMgrs)
                        multiFactionMgr.PauseSimulation(false);

                    break;

            }

            CurrState = newState;
        }

        private void Update()
        {
            if (CurrState == ServerGameState.initial
                || CurrState == ServerGameState.awaitingValidation)
                return;

            switch(CurrState)
            {
                case ServerGameState.awaitingValidation:
                    return;

                case ServerGameState.simPaused:

                    if (clientTimeoutReleaseTimer > 0.0f)
                        clientTimeoutReleaseTimer -= Time.deltaTime;
                    else
                    {
                        clientTimeoutReleaseTimer = clientTimeoutReleaseTime;
                        foreach(KeyValuePair<int, IMultiplayerFactionManagerTracker> tracker in multiFactionMgrTrackers)
                        {
                            // Making sure that the tracker is active because we do not want to kick inactive trackers that might have been already kicked.
                            if (tracker.Value.IsActive && tracker.Value.CurrTurn != ServerTurn + 1)
                                KickTimedOutClient(tracker.Key);
                        }

                        TryIncrementServerTurn(ServerTurn);
                    }

                    break;
            }
        }
        #endregion

        #region Handling State: awaitingValidation
        private void HandleMultiplayerFactionManagerValidated(IMultiplayerFactionManager newMultiFactionMgr, MultiplayerFactionEventArgs args)
        {
            if (CurrState != ServerGameState.awaitingValidation)
                return;

            validatedAmount++;

            var nextTracker = new MultiplayerFactionManagerTracker();
            nextTracker.Init(multiplayerMgr,
                new MultiplayerFactionManagerTrackerData
                {
                    multiFactionMgr = newMultiFactionMgr,
                    factionID = newMultiFactionMgr.GameFactionSlot.ID,

                    logSize = logSize,
                    maxInputCount = maxInputCount,

                    initialRTT = args.LastRTT
                });

            multiFactionMgrTrackers.Add(newMultiFactionMgr.GameFactionSlot.ID, nextTracker);

            logger.Log($"[{GetType().Name} - Server Turn: {ServerTurn}] Client of faction slot ID {nextTracker.Data.factionID} has been successfully validated to start the game.");

            if (validatedAmount == TotalFactionCount)
            {
                SetState(ServerGameState.simRunning);

                foreach (IMultiplayerFactionManager multiFactionMgr in multiplayerMgr.MultiplayerFactionMgrs)
                    multiFactionMgr.OnAllClientsValidatedServer();
            }
        }
        #endregion

        #region Handling States: simRunning and simPaused - Adding/Relaying Input
        public void AddInput(IEnumerable<CommandInput> newInputs, int factionID)
        {
            multiFactionMgrTrackers.TryGetValue(factionID, out IMultiplayerFactionManagerTracker nextTracker);

            if (!logger.RequireValid(nextTracker,
              $"[{GetType().Name} - Server Turn: {ServerTurn}] Can not find tracker for client with faction slot ID {factionID}!"))
                return;
            else if (!logger.RequireTrue(nextTracker.IsActive,
                $"[{GetType().Name} - Server Turn: {ServerTurn}] Tracker of client with faction slot ID {factionID} has been disabled!"))
                return;

            IEnumerable<MultiplayerInputWrapper> wrappedInputs = newInputs
                .Select(input =>
                {
                    NextInputID++;

                    return new MultiplayerInputWrapper
                    {
                        ID = NextInputID,
                        input = input
                    };
                });


            // Always add the received input to the next server turn so that they are accumulated and sent all clients.
            nextTracker.AddInput(wrappedInputs, ServerTurn + 1);
        }

        public void RelayInput()
        {
            if (AwaitingReceipt && CurrState == ServerGameState.simRunning)
                SetState(ServerGameState.simPaused);

            // Grab all received inputs from all trackers
            List<MultiplayerInputWrapper> relayedInputs = multiFactionMgrTrackers.Values
                .SelectMany(tracker => tracker.GetRelayInput(ServerTurn))
                .OrderBy(wrapper => wrapper.ID)
                .ToList();

            // Handle incrementing the local server turn separately.
            if (LocalServerTurn == ServerTurn)
            {
                if(multiplayerMgr.IsServerOnly)
                    inputMgr.LaunchInput(relayedInputs.Select(inputWrapper => inputWrapper.input));

                LocalServerTurn++;
            }

            foreach (IMultiplayerFactionManager multiFactionMgr in multiplayerMgr.MultiplayerFactionMgrs)
                if (multiFactionMgrTrackers[multiFactionMgr.GameFactionSlot.ID].CurrTurn == ServerTurn)
                {
                    multiFactionMgr.RelayInput(
                        relayedInputs,
                        lastInputID: relayedInputs.LastOrDefault().ID,
                        ServerTurn);
                }

            AwaitingReceipt = true;
        }

        public void OnRelayedInputReceived(int factionID, int turnID, float lastRTT)
        {
            if (!logger.RequireTrue(turnID == ServerTurn,
              $"[{GetType().Name} - Server Turn: {ServerTurn}] Received confirmation of relayed input receipt from client of faction slot ID {factionID} for turn {turnID} while expecting it for current server turn."))
                return; 

            multiFactionMgrTrackers[factionID].OnRelayedInputReceived(turnID, lastRTT);

            TryIncrementServerTurn(turnID);
        }
        #endregion

        #region Handling Lockstep Turn
        public bool TryIncrementServerTurn(int turnID)
        {
            if (multiFactionMgrTrackers.Values.Where(tracker => tracker.IsActive).Any(tracker => tracker.CurrTurn != ServerTurn + 1))
                return false;

            IEnumerable<IMultiplayerFactionManagerTracker> inactiveTrackers = multiFactionMgrTrackers.Values.Where(tracker => !tracker.IsActive);
            // To increment the current turn on inactive trackers that may still have inputs in their log to sync with the rest of the clients.
            foreach (var tracker in inactiveTrackers)
                tracker.OnRelayedInputReceived(turnID, lastRTT: 0.0f);

            AwaitingReceipt = false;
            ServerTurn++;

            // Destroy trackers that have been disabled for a certain amount of turns
            foreach (var destroyTracker in inactiveTrackers.Where(tracker => tracker.InactiveTurns >= logSize).ToList())
            {
                multiFactionMgrTrackers.Remove(destroyTracker.Data.factionID);
                logger.Log(
                    $"[{GetType().Name} - Server Turn: {ServerTurn}] Tracker of client with faction slot ID {destroyTracker.Data.factionID} has been destroyed due to inactivity for {logSize} turns!",
                    source: this,
                    LoggingType.warning);
            }

            if (CurrState == ServerGameState.simPaused)
            {
                // If the simulation was previously paused then we consider changing the lockstep turn duration in order to adhere to clients who might be lagging
                UpdateTurnTimeWithRTTLogs();

                SetState(ServerGameState.simRunning);
            }

            return true;
        }

        public void UpdateTurnTimeWithRTTLogs()
        {
            turnHandler.UpdateTurnTime(multiFactionMgrTrackers.Values
                .Where(tracker => tracker.IsActive)
                .Select(tracker => tracker.RTTLog.ToArray())
                .ToArray());
        }

        private void OnTurnComplete()
        {
            RelayInput();
        }
        #endregion

        #region Kicking Clients
        private void KickTimedOutClient(int factionID)
        {
            multiFactionMgrTrackers[factionID].Disable();
            multiplayerMgr.CurrentLobby.LocalFactionSlot.KickAttempt(factionID);
            disconnectedFactionIDs.Add(factionID);

            logger.Log(
                $"[{GetType().Name} - Server Turn: {ServerTurn} - Client Turn: {multiFactionMgrTrackers[factionID].CurrTurn}] Client of faction slot ID: {factionID} has been requested to leave the server!",
                source: this,
                type: LoggingType.warning);
        }
        #endregion        
    }
}
