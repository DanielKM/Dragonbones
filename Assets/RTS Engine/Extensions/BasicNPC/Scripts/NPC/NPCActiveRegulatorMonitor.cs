using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;

namespace RTSEngine.NPC
{
    /// <summary>
    /// Tracks active instances of NPCUnitRegulator or NPCBuildingRegulaotr.
    /// </summary>
    public class NPCActiveRegulatorMonitor
    {
        #region Attributes
        // Faction entity codes whose NPCRegulator instances are monitored.
        private readonly List<string> codes;

        /// <summary>
        /// Amount of unique faction entity codes monitored by this component.
        /// </summary>
        public int Count => codes.Count;

        /// <summary>
        /// Gets a random faction entity code that is monitored by this component.
        /// </summary>
        public string RandomCode => codes.Count > 0 ? codes[UnityEngine.Random.Range(0, codes.Count)] : "";

        /// <summary>
        /// Gets an IEnumerable instance of all faction entity codes monitored by this component.
        /// </summary>
        public IEnumerable<string> AllCodes => codes;

        protected IGameManager gameMgr { private set; get; }
        protected IFactionManager factionMgr { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public NPCActiveRegulatorMonitor(IGameManager gameMgr, IFactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;
                
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            // Initial state
            codes = new List<string>();

            globalEvent.UnitUpgradedGlobal += HandleFactionEntityUpgradeGlobal;
            globalEvent.BuildingUpgradedGlobal += HandleFactionEntityUpgradeGlobal;
        }

        public void Disable ()
        {
            globalEvent.UnitUpgradedGlobal -= HandleFactionEntityUpgradeGlobal;
            globalEvent.BuildingUpgradedGlobal -= HandleFactionEntityUpgradeGlobal;
        }
        #endregion

        #region Handling Event: Faction Entity Upgrade
        private void HandleFactionEntityUpgradeGlobal(IFactionEntity factionEntity, UpgradeEventArgs<IEntity> args)
        {
            if (!factionMgr.FactionID.IsSameFaction(args.FactionID)
                || !factionEntity.IsValid())
                return;

            ReplaceCode(factionEntity.Code, args.UpgradeElement.target.Code);
        }
        #endregion

        #region Adding/Replacing Tracked Faction Entity Codes
        public void AddCode(string newCode) => ReplaceCode("", newCode);

        public void ReplaceCode(string oldCode, string newCode)
        {
            if (string.IsNullOrEmpty(oldCode) || codes.Contains(oldCode))
            {
                codes.Remove(oldCode);

                if(!codes.Contains(newCode))
                    codes.Add(newCode);
            }
        }
        #endregion
    }
}
