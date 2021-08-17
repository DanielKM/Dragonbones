using UnityEngine;
using UnityEngine.Events;

using RTSEngine.Determinism;
using RTSEngine.Game;
using RTSEngine.Event;
using RTSEngine.Audio;

namespace RTSEngine.Effect
{
    public class EffectObject : MonoBehaviour, IEffectObject
    {
        #region Class Attributes
        /// <summary>
        /// The current state of the effect object.
        /// </summary>
        public EffectObjectState State { private set; get; }

        [SerializeField, Tooltip("Assign a unique code for each effect object type.")]
        private string code = "unique_effect_object"; 
        public string Code => code;

        [SerializeField, Tooltip("Enable to control the life time of the effect object."), Space()]
        private bool enableLifeTime = true; 

        [SerializeField, Tooltip("If the life time is enabled then this represents the time (in seconds) during which the effect object will be shown.")]
        private float defaultLifeTime = 3.0f; 

        // When > 0, the disable events will be invoked and then timer with this length will start and then the effect object will be hidden
        [SerializeField, Tooltip("When the effect object is disabled, this is how long (in seconds) it will take for the object to disappear.")]
        private float disableTime = 0.0f;

        // Handles life and disable timers
        private TimeModifiedTimer timer;

        [SerializeField, Tooltip("Offsets the spawn position of the effect object when it is enabled."), Space()]
        private Vector3 spawnPositionOffset = Vector3.zero;

        [SerializeField, Tooltip("Invoked when the effect object is enabled."), Space()]
        private UnityEvent enableEvent = null;
        [SerializeField, Tooltip("Invoked when the effect object is disabled.")]
        private UnityEvent disableEvent = null;

        public AudioSource AudioSourceComponent { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }

        // Other components
        protected IGameManager gameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the EffectObj instance when it is first created.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the currently active game.</param>
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>(); 

            AudioSourceComponent = GetComponent<AudioSource>();

            globalEvent.RaiseEffectObjectCreatedGlobal(this);

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            effectObjPool.Despawn(this, destroyed: true);

            globalEvent.RaiseEffectObjectDestroyedGlobal(this);
        }
        #endregion

        #region Updating State (Enabling/Disabling)
        public void Activate(bool enableLifeTime, bool useDefaultLifeTime = true, float customDuration = 0.0f)
        {
            if (State != EffectObjectState.inactive)
                return;

            State = EffectObjectState.running;

            this.enableLifeTime = enableLifeTime;
            if (this.enableLifeTime)
                timer = new TimeModifiedTimer(useDefaultLifeTime ? defaultLifeTime : customDuration);

            transform.position += spawnPositionOffset; //set spawn position offset.

            gameObject.SetActive(true);

            enableEvent.Invoke(); //invoke the event

            OnActivated();
        }

        protected virtual void OnActivated() { }

        private void Update()
        {
            if (State == EffectObjectState.inactive 
                || !enableLifeTime)
                return;

            OnActiveUpdate();

            timer.ModifiedDecrease();
            if (timer.CurrValue > 0.0f)
                return;

            switch(State)
            {
                case EffectObjectState.running:
                    Deactivate();
                    break;

                case EffectObjectState.disabling:
                    DeactivateFinalize();
                    break;
            }
        }

        protected virtual void OnActiveUpdate() { }

        public void Deactivate(bool useDisableTime = true)
        {
            if (disableTime <= 0.0f || !useDisableTime)
            {
                DeactivateFinalize();
                return;
            }

            // If the effect object is already being disabled with the timer then wait for the timer
            if (State != EffectObjectState.running)
                return;

            disableEvent.Invoke();

            State = EffectObjectState.disabling;
            timer = new TimeModifiedTimer(disableTime);
        }
        
        protected virtual void OnDeactivated() { }

        private void DeactivateFinalize ()
        {
            if (State == EffectObjectState.inactive)
                return;

            State = EffectObjectState.inactive;

            //Make sure it has no parent object anymore.
            transform.SetParent(null, true); 
            gameObject.SetActive(false);

            effectObjPool.Despawn(this);

            OnDeactivated();
        }
        #endregion
    }
}