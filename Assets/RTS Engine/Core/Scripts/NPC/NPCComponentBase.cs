using System;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.NPC
{
    public abstract class NPCComponentBase : MonoBehaviour, INPCComponent
    {
        #region Attributes 
        protected INPCManager npcMgr { private set; get; }
        protected IFactionManager factionMgr { private set; get; }
        protected IFactionSlot factionSlot { private set; get; }
        protected IGameManager gameMgr { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }

        [SerializeField, ReadOnly, Tooltip("Active status of the NPC component.")]
        private bool isActive = false;
        public bool IsActive { 
            protected set 
            {
                isActive = value;

                if (isActive)
                    OnActivtated();
                else
                    OnDeactivated();
            }
            get => isActive;
        }

#if UNITY_EDITOR
        [SerializeField, Tooltip("Enable to allow to update logs on the inspector of the NPC component. Functional only in the editor.")]
        private bool debugEnabled = false;
        protected bool DebugEnabled => debugEnabled;
#endif

        public virtual bool IsSingleInstance => true;
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, INPCManager npcMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            this.npcMgr = npcMgr; 
            this.factionMgr = npcMgr.FactionMgr;
            this.factionSlot = factionMgr.Slot;

            OnPreInit();

            this.npcMgr.InitComplete += HandleNPCFactionInitComplete;
        }

        /// <summary>
        /// Called when the INPCManager instance first initializes the INPCComponent instance.
        /// </summary>
        protected virtual void OnPreInit() { }

        private void HandleNPCFactionInitComplete(INPCManager npcManager, EventArgs args)
        {
            OnPostInit();

            this.npcMgr.InitComplete -= HandleNPCFactionInitComplete;
        }

        /// <summary>
        /// Called after all INPCComponent instances have been cached and initialized by the INPCManager instance.
        /// </summary>
        protected virtual void OnPostInit() { }

        private void OnDestroy()
        {
            this.npcMgr.InitComplete -= HandleNPCFactionInitComplete;

            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Activating/Deactivating:
        protected virtual void OnActivtated() { }

        protected virtual void OnDeactivated() { }
        #endregion

        #region Updating Component
        private void Update()
        {
#if UNITY_EDITOR
            if(DebugEnabled)
                UpdateLogStats();
#endif

            if (!IsActive)
                return;

            OnActiveUpdate();
        }

#if UNITY_EDITOR
        protected virtual void UpdateLogStats()
        {
        }
#endif

        protected virtual void OnActiveUpdate () { }
        #endregion
    }
}
