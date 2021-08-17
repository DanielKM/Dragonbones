using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using RTSEngine.Event;
using RTSEngine.UI;
using RTSEngine.Determinism;
using RTSEngine.Faction;
using RTSEngine.Audio;
using RTSEngine.Logging;
using RTSEngine.Service;
using RTSEngine.Utilities;

namespace RTSEngine.Game
{
    public class GameManager : MonoBehaviour, IGameManager
    {
        #region Attributes
        [SerializeField, Tooltip("Scene to load when leaving this map. This is overwritten if the map is loaded through a lobby.")]
        private string prevScene = "MainMenu"; 

        [SerializeField, Tooltip("Default faction defeat condition to determine how to win/lose this map. This is overwritten if the map is loaded through a lobby.")]
        private DefeatConditionType defeatCondition = DefeatConditionType.eliminateMain;
        public DefeatConditionType DefeatCondition => defeatCondition;

        public GameStateType State { private set; get; } = GameStateType.ready;

        [SerializeField, Tooltip("Time (in seconds) after the game starts, during which no faction can attack another.")]
        private float peaceTimeDuration = 60.0f; 
        public TimeModifiedTimer PeaceTimer { private set; get; }
        public bool InPeaceTime => PeaceTimer.CurrValue > 0.0f;

        [SerializeField, Tooltip("Audio clip played when the local player wins the game.")]
        private AudioClipFetcher winGameAudio = null;
        [SerializeField, Tooltip("Audio clip played when the local player loses the game.")]
        private AudioClipFetcher loseGameAudio = null;

        [Header("Faction Slots")]
        [SerializeField, Tooltip("Each element represents a faction slot that can be filled by a player or AI.")]
        private List<FactionSlot> factionSlots = new List<FactionSlot>();
        public IEnumerable<IFactionSlot> FactionSlots => factionSlots.Cast<IFactionSlot>();
        public IFactionSlot GetFactionSlot(int ID) => ID.IsValidIndex(factionSlots) ? factionSlots[ID] : null;
        public int FactionCount => factionSlots.Count;
        public int ActiveFactionCount => factionSlots.Where(slot => slot.State == FactionSlotState.active).Count();
        public IFactionSlot LocalFactionSlot {private set; get;}
        public int LocalFactionSlotID => LocalFactionSlot.IsValid() ? LocalFactionSlot.ID : -1;

        [Space(), SerializeField, Tooltip("Enable to allow randomizing the slot that each player gets.")]
        private bool randomFactionSlots = true;

        // Services
        protected IGameLoggingService logger { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected ITimeModifier timeModifier { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; } 

        // Builder that sets up the game for this map.
        public IGameBuilder CurrBuilder { private set; get; }
#endregion

        #region Services Publisher
        private IReadOnlyDictionary<Type, IPreRunGameService> preRunServices = null;
        private IReadOnlyDictionary<Type, IPostRunGameService> postRunServices = null;

        public T GetService<T>() where T : IPreRunGameService
        {
            if (!logger.RequireTrue(preRunServices.ContainsKey(typeof(T)),
                $"[{GetType().Name}] No service of type: '{typeof(T)}' has been registered!"))
                return default;

            if (preRunServices.TryGetValue(typeof(T), out IPreRunGameService value))
                return (T)value;

            return default;
        }

        T IServicePublisher<IPostRunGameService>.GetService<T>()
        {
            if (!logger.RequireTrue(postRunServices.ContainsKey(typeof(T)),
                $"[{GetType().Name}] No service of type: '{typeof(T)}' has been registered!"))
                return default;

            if (postRunServices.TryGetValue(typeof(T), out IPostRunGameService value))
                return (T)value;

            return default;
        }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IGameManager, EventArgs> GameServicesInitialized;
        private void RaiseGameServicesInitialized ()
        {
            var handler = GameServicesInitialized;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public event CustomEventHandler<IGameManager, EventArgs> GameBuilt;
        private void RaiseGameBuilt()
        {
            var handler = GameBuilt;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public event CustomEventHandler<IGameManager, EventArgs> GameStartRunning;
        private void RaiseGameStartRunning()
        {
            var handler = GameStartRunning;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        private void Awake()
        {
            CurrBuilder = DontDestroyOnLoadManager
                .AllDdolObjects
                .Select(obj => obj.GetComponent<IGameBuilder>())
                .SingleOrDefault(obj => obj.IsValid());

            // Services might need the functions in the RTSHelper so it makes sense to have it initialized before the services do.
            // It is a static class and not represented as a service as it does not hold any state expect the current active instance of the IGameManager.
            RTSHelper.Init(this);

            // Get services:
            this.logger = GetComponentInChildren<IGameLoggingService>();
            this.inputMgr = GetComponentInChildren<IInputManager>();
            this.timeModifier = GetComponentInChildren<ITimeModifier>();
            this.globalEvent = GetComponentInChildren<IGlobalEventPublisher>();
            this.audioMgr = GetComponentInChildren<IGameAudioManager>();

            SetState(GameStateType.ready);

            // Register the services when the game starts.
            preRunServices = GetComponentsInChildren<IPreRunGameService>()
                .ToDictionary(service => service.GetType().GetSuperInterfaceType<IPreRunGameService>(), service => service);

            postRunServices = GetComponentsInChildren<IPostRunGameService>()
                .ToDictionary(service => service.GetType().GetSuperInterfaceType<IPostRunGameService>(), service => service);

            // Initialize pre run services.
            foreach (IPreRunGameService service in preRunServices.Values)
                service.Init(this);

            RaiseGameServicesInitialized();

            if(!logger.RequireTrue(Build(),
                $"[{GetType().Name}] Unable to build and start game due to logged errors."))
                return;

            SetLocalPlayerFactionSlot();

            CurrBuilder?.OnGameBuilt(this);
            RaiseGameBuilt();

            PeaceTimer = new TimeModifiedTimer(peaceTimeDuration);

            RaiseGameStartRunning();

            SetState(GameStateType.running);

            // Subscribe to events
            globalEvent.FactionSlotDefeatConditionTriggeredGlobal += HandleFactionSlotDefeatConditionTriggeredGlobal;

            // Initialize post run services.
            foreach (IPostRunGameService service in postRunServices.Values)
                service.Init(this);

            OnInit();
        }

        private bool Build ()
        {
            // No pre defined builder? Use the default game settings!
            if(!CurrBuilder.IsValid())
            {
                RandomizeFactionSlots();

                for (int i = 0; i < factionSlots.Count; i++)
                    factionSlots[i].Init(factionSlots[i].Data, ID:i, this);

                return true;
            }

            RandomizeFactionSlots(CurrBuilder.Data.factionSlotIndexSeed.ToList());

            defeatCondition = CurrBuilder.Data.defeatCondition;

            if(CurrBuilder.FactionSlotCount > FactionCount)
            {
                Debug.LogError($"[GameManager] Game Builder is attempting to initialize {CurrBuilder.FactionSlotCount} slots while there are only {FactionCount} slots available!");
                return false; 
            }

            for (int i = 0; i < CurrBuilder.FactionSlotCount; i++)
            {
                factionSlots[i].Init(CurrBuilder.FactionSlotDataSet.ElementAt(i), ID:i, this);
            }

            // Remove the extra unneeded slots
            while (CurrBuilder.FactionSlotCount < factionSlots.Count)
            {
                factionSlots[factionSlots.Count - 1].InitDestroy();
                factionSlots.RemoveAt(factionSlots.Count - 1);
            }

            return true;
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            globalEvent.FactionSlotDefeatConditionTriggeredGlobal += HandleFactionSlotDefeatConditionTriggeredGlobal;
        }
        #endregion

        #region Setting Local Player Faction
        private bool SetLocalPlayerFactionSlot()
        {
            try
            {
                LocalFactionSlot = FactionSlots.SingleOrDefault(slot => slot.Data.isLocalPlayer);
            }
            catch (InvalidOperationException e)
            {
                logger.LogError($"[{GetType().Name}] Unable to find a single faction slot marked as the local player. Exception: {e.Message}");
            }

            if (!logger.RequireValid(LocalFactionSlot,
                    $"[{GetType().Name}] There is either no local faction slot or there are more than one. The 'LocalFactionSlot' property will be set to null to avoid any unwanted behaviour, please handle controls independently using the faction slots data. If this is intended, ignore this warning.",
                    source: this,
                    type: LoggingType.warning))
            {
                LocalFactionSlot = FactionSlots.FirstOrDefault(slot => slot.Data.isLocalPlayer);
                return false;
            }

            return true;
        }
        #endregion

        #region Handling Peace Time
        void Update()
        {
            if(PeaceTimer.ModifiedDecrease())
                enabled = false;
        }
        #endregion

        #region Handling Faction Slot Randomizing
        private void RandomizeFactionSlots()
        {
            RandomizeFactionSlots(RTSHelper.GenerateRandomIndexList(FactionCount));
        }

        private void RandomizeFactionSlots(List<int> indexSeedList)
        {
            if (!randomFactionSlots
                || !logger.RequireTrue(indexSeedList.Count == factionSlots.Count,
                $"[{GetType().Name}] Unable to randomize faction slots due to an index seed list that does not match with the faction slots count. Faction slots will not be randomized!"))
                return;

            int i = 0;
            while(i < indexSeedList.Count) 
            {
                if (i == indexSeedList[i] || i > indexSeedList[i]) 
                {
                    i++;
                    continue;
                }

                var tempSlot = factionSlots[i];
                factionSlots[i] = factionSlots[indexSeedList[i]];
                factionSlots[indexSeedList[i]] = tempSlot;

                i++;
            }
        }
        #endregion

        #region Handling Defeat Condition / Faction Defeat
        private void HandleFactionSlotDefeatConditionTriggeredGlobal(IFactionSlot slot, DefeatConditionEventArgs args)
        {
            if (args.Type == DefeatCondition)
                OnFactionDefeated(slot.ID);
        }

        public ErrorMessage OnFactionDefeated(int factionID)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.faction,
                targetMode = (byte)InputMode.factionDestroy,

                intValues = inputMgr.ToIntValues(factionID),
            });
        }

        public ErrorMessage OnFactionDefeatedLocal(int factionID)
        {
            if (!factionSlots[factionID].IsActiveFaction())
                return ErrorMessage.inactive;

            factionSlots[factionID].UpdateState(FactionSlotState.eliminated);
            factionSlots[factionID].UpdateRole(FactionSlotRole.client);

            globalEvent.RaiseShowPlayerMessageGlobal(
                this,
                new MessageEventArgs
                (
                    type: MessageType.info,
                    message: $"Faction '{factionSlots[factionID].Data.name}' (ID: {factionID}) has been defeated!"
                ));

            globalEvent.RaiseFactionSlotDefeatedGlobal(factionSlots[factionID], new DefeatConditionEventArgs(type: DefeatCondition));

            if (LocalFactionSlot.IsValid())
            {
                // If this is the local playe rfaction?
                if (factionID == LocalFactionSlot.ID)
                    LooseGame();
                else if (factionSlots.Count(slot => slot.State == FactionSlotState.active) == 1)
                    WinGame();
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Local Player Winning/Losing Game
        public void WinGame()
        {
            audioMgr.PlaySFX(winGameAudio, false);

            SetState(GameStateType.won);
        }

        public void LooseGame()
        {
            audioMgr.PlaySFX(loseGameAudio, false);

            SetState(GameStateType.lost);
        }
        #endregion

        #region Handling Game State
        public void SetState (GameStateType newState)
        {
            State = newState;

            if (State == GameStateType.running)
                timeModifier.ResetModifier(playerCommand: false);
            else if (State == GameStateType.frozen)
                timeModifier.SetModifier(0.0f, playerCommand: false);

            globalEvent.RaiseGameStateUpdatedGlobal();
        }
        #endregion

        #region Leaving Game
        public void LeaveGame()
        {
            if (CurrBuilder.IsValid())
            {
                CurrBuilder.OnGameLeave();
                return;
            }

            SceneManager.LoadScene(prevScene);
        }
        #endregion
    }
}
