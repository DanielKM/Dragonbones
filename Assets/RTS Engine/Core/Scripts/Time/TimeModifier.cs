using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Determinism
{
    public class TimeModifier : MonoBehaviour, ITimeModifier
    {
        #region Attributes
        [SerializeField, Tooltip("The default modifier value (used when there is no IGameBuilder instance available in the map scene that overwrites the default value)")]
        private float defaultModifier = 1.0f;

        public static float CurrentModifier { private set; get; } = 1.0f;
        public static float ApplyModifier(float input) => input * CurrentModifier;

        private List<KeyValuePair<GlobalTimeModifiedTimer, Action>> globalTimers;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<ITimeModifier, EventArgs> ModifierUpdated;

        private void RaiseModifierUpdate()
        {
            var handler = ModifierUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            globalTimers = new List<KeyValuePair<GlobalTimeModifiedTimer, Action>>();
        }

        private void OnDestroy()
        {
            if (!ModifierUpdated.IsValid() || ModifierUpdated.GetInvocationList().IsValid())
                return;

            // Remove all subscribers manually in case individual subscribers haven't.
            foreach (Delegate subscriber in ModifierUpdated.GetInvocationList())
                ModifierUpdated -= subscriber as CustomEventHandler<ITimeModifier, EventArgs>;
        }
        #endregion

        #region Updating Time Modifier
        public ErrorMessage SetModifier(float newModifier, bool playerCommand)
        {
            return inputMgr.SendInput(new CommandInput
            {
                sourceMode = (byte)InputMode.master,
                targetMode = (byte)InputMode.setTimeModifier,

                floatValue = newModifier,
                playerCommand = playerCommand
            });
        }

        public ErrorMessage SetModifierLocal(float newModifier, bool playerCommand)
        {
            logger.RequireTrue(newModifier >= 0.0f,
              $"[{GetType().Name}] Time Modifier must be => 0.0f!");
            
            CurrentModifier = Mathf.Max(0.0f, newModifier);

            switch(gameMgr.State)
            {
                case GameStateType.running:
                    if (CurrentModifier == 0.0f)
                        gameMgr.SetState(GameStateType.frozen);
                    break;

                case GameStateType.frozen:
                    if (CurrentModifier > 0.0f)
                        gameMgr.SetState(GameStateType.running);
                    break;
            }

            RaiseModifierUpdate();

            return ErrorMessage.none;
        }

        public void ResetModifier(bool playerCommand)
        {
            SetModifier(gameMgr.CurrBuilder.IsValid()
                ? gameMgr.CurrBuilder.Data.timeModifier
                : defaultModifier,
                playerCommand);
        }
        #endregion

        #region Handling Global Timers
        private void Update()
        {
            if (globalTimers.Count == 0)
                return;

            foreach (KeyValuePair<GlobalTimeModifiedTimer, Action> timer in globalTimers.ToList())
                if(timer.Key.ModifiedDecrease())
                    RemoveTimer(timer);
        }

        public void AddTimer(GlobalTimeModifiedTimer timer, Action removalCallback)
        {
            globalTimers.Add(new KeyValuePair<GlobalTimeModifiedTimer, Action>(timer, removalCallback));
        }

        public bool RemoveTimer(GlobalTimeModifiedTimer timer)
        {
            KeyValuePair<GlobalTimeModifiedTimer, Action> timerSlot = globalTimers
                .FirstOrDefault(elem => elem.Key == timer);

            if (timerSlot.Key.IsValid())
                return RemoveTimer(timerSlot);

            return false;
        }

        public bool RemoveTimer(KeyValuePair<GlobalTimeModifiedTimer, Action> timer)
        {
            bool removed = globalTimers.Remove(timer);

            if (timer.Value.IsValid())
                timer.Value();

            return removed;
        }
        #endregion
    }
}
