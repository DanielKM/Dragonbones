using System.Linq;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

using UnityEngine;

namespace RTSEngine.Faction
{
    public class FactionDefeatHandler : MonoBehaviour, IPostRunGameService
    {
        public enum FactionDefeatResponseType { none = 0, custom = 1, destroyList = 2}
        [SerializeField]
        private FactionDefeatResponseType factionDefeatResponse = FactionDefeatResponseType.destroyList;

        [SerializeField]
        private FactionEntityTargetPicker destroyList = new FactionEntityTargetPicker();

        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            globalEvent.FactionSlotDefeatedGlobal += HandleFactionSlotDefeatedGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.FactionSlotDefeatedGlobal -= HandleFactionSlotDefeatedGlobal;
        }

        private void HandleFactionSlotDefeatedGlobal(IFactionSlot factionSlot, DefeatConditionEventArgs args)
        {
            switch(factionDefeatResponse)
            {
                case FactionDefeatResponseType.none:
                    return;

                case FactionDefeatResponseType.custom:
                    OnCustomDefeatResponse(factionSlot, args);
                    break;

                case FactionDefeatResponseType.destroyList:
                    foreach (IFactionEntity entity in factionSlot.FactionMgr.FactionEntities.ToList())
                        if(destroyList.IsValidTarget(entity))
                            entity.Health.DestroyLocal(false, null);
                    break;
            }

        }

        protected virtual void OnCustomDefeatResponse(IFactionSlot factionSlot, DefeatConditionEventArgs args) { }
    }
}