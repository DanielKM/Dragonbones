using System;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Determinism
{
    [System.Serializable]
    public class GlobalTimeModifiedTimer : TimeModifiedTimer
    {
        [SerializeField, Tooltip("Enable/disable cooldown.")]
        private bool enabled = false;

        [SerializeField, Tooltip("Default timer duration.")]
        private float defaultValue = 2.0f;

        private bool isActive = false;
        public bool IsActive
        {
            set
            {
                if (!enabled)
                    return;

                if (!IsInitialized)
                    RTSHelper.TryGameInit(Init);

                // If we are activating the timer again while it was already active then disable it first to reload it
                if (isActive && value == true)
                    timeModifier.RemoveTimer(this);

                isActive = value;

                if (isActive)
                {
                    // To set the CurrValue.
                    Reload();

                    // To run the timer
                    timeModifier.AddTimer(this, removalCallback);
                }
                else
                    timeModifier.RemoveTimer(this);
            }
            get
            {
                if (!enabled)
                    return false;

                if (!IsInitialized)
                    RTSHelper.TryGameInit(Init);

                return CurrValue > 0.0f;
            }
        }

        public bool IsInitialized { private set; get; } = false;

        // callback called when the timer is removed.
        private Action removalCallback;

        // Game services
        protected ITimeModifier timeModifier { private set; get; } 

        // We want to start the cooldown timer with a CurrValue of 0.0 so the timer is inactive by default
        public GlobalTimeModifiedTimer(bool enabled = false, float defaultValue = 1.0f) : base() 
        {
            this.enabled = enabled;
            this.defaultValue = defaultValue;
        }

        public void Init(IGameManager gameMgr, Action timerRemovedCallback)
        {
            this.removalCallback = timerRemovedCallback;

            this.timeModifier = gameMgr.GetService<ITimeModifier>();
            this.DefaultValue = defaultValue;

            IsInitialized = true;
        }

        public void Init(IGameManager gameMgr) => Init(gameMgr, null);
    }
}
