using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Utilities;
using UnityEngine.UI;

namespace RTSEngine.DevTools
{
    public class DevToolComponentBase : MonoBehaviour, IPreRunGameService, IPostRunGameService
    {
        [SerializeField, Tooltip("Enable to allow buildings to be auto constructed right after placement.")]
        private bool isActive = true;
        public bool IsActive { set => isActive = value; get => isActive; }

        [SerializeField, Tooltip("Pick the faction slot roles allowed to have their pending tasks handled by this dev tool component.")]
        private FactionSlotRoleFilter roleFilter = new FactionSlotRoleFilter { localFactionOnly = true, allowedFactionSlotRoles = new FactionSlotRole[0] };
        protected FactionSlotRoleFilter RoleFilter => roleFilter;

        [SerializeField, Tooltip("Label used to display the activity status of the dev tools component.")]
        private Text label = null;
        protected Text Label => label;

        protected IGameManager gameMgr { private set; get; } 
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            if (gameMgr.State == GameStateType.running)
                OnPostRunInit();
            else
                OnPreRunInit();
        }

        protected virtual void OnPostRunInit() { }
        protected virtual void OnPreRunInit() { }

        public virtual void OnUIInteraction() { }
    }
}
