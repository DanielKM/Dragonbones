using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.NPC.Event;

namespace RTSEngine.NPC
{
    public abstract class NPCRegulator<T> : INPCRegulator where T : IFactionEntity
    {
        #region Attributes 
        public IFactionEntity Prefab { private set; get; }

        public int Count { private set; get; }
        /// <summary>
        /// Amount of the regulated faction entity type that the NPC faction aims to reach.
        /// </summary>
        public int TargetCount { private set; get; }
        public bool HasTargetCount => Count >= TargetCount;

        // Inferred from the provided regulator data
        public int MinTargetAmount { private set; get; }
        public int MaxTargetAmount { private set; get; }

        public bool HasReachedMaxAmount => Count >= MaxTargetAmount || factionMgr.HasReachedLimit(Prefab.Code, Prefab.Category) || CurrPendingAmount >= MaxPendingAmount;
        public bool HasReachedMinAmount => Count >= MinTargetAmount;

        // Current spawned instances of the regulated prefabs
        protected List<T> instances = new List<T>();
        public IEnumerable<T> Instances => instances.ToList();
        public IEnumerable<T> InstancesIdleOnly => instances.Where(factionEntity => factionEntity.IsIdle);
        public IEnumerable<T> InstancesIdleFirst => instances.OrderByDescending(factionEntity => factionEntity.IsIdle);

        // Inferred from the provided regulator data
        public int MaxPendingAmount { private set; get; }
        public int CurrPendingAmount { private set; get; }

        protected IFactionManager factionMgr { private set; get; }

        protected INPCManager npcMgr { private set; get; }

        protected IGameManager gameMgr { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<NPCRegulator<T>, NPCRegulatorUpdateEventArgs> AmountUpdated;

        private void RaiseAmountUpdated(int count, int pendingAmount)
        {
            Count += count;
            CurrPendingAmount += pendingAmount;

            NPCRegulatorUpdateEventArgs args = new NPCRegulatorUpdateEventArgs(count, pendingAmount);

            var handler = AmountUpdated;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public NPCRegulator(NPCRegulatorData data, T prefab, IGameManager gameMgr, INPCManager npcMgr)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();

            this.npcMgr = npcMgr;
            this.factionMgr = npcMgr.FactionMgr;

            this.Prefab = prefab;

            MaxTargetAmount = data.MaxAmount;
            MinTargetAmount = data.MinAmount;
            MaxPendingAmount = data.MaxPendingAmount;

            Count = 0;
            TargetCount = MinTargetAmount;

            globalEvent.EntityFactionUpdateCompleteGlobal += HandleEntityFactionUpdateCompleteGlobal;
        }

        public void Disable()
        {
            globalEvent.EntityFactionUpdateCompleteGlobal -= HandleEntityFactionUpdateCompleteGlobal;

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Handling Events: Faction Entity Death/Update
        private void HandleFactionEntityDead(IEntity factionEntity, DeadEventArgs e)
        {
            RemoveExisting((T)factionEntity);
        }

        private void HandleEntityFactionUpdateCompleteGlobal(IEntity updatedInstance, FactionUpdateArgs args)
        {
            if (!(updatedInstance is T))
                return;

            T factionEntity = (T)updatedInstance;

            // Remove the old instance and add the upgrade target one
            RemoveExisting(factionEntity);
            AddNewlyCreated(factionEntity);
        }
        #endregion

        #region Adding/Removing Instances
        protected void AddExisting(T factionEntity)
        {
            if (!CanInstanceBeRegulated(factionEntity))
                return;

            instances.Add(factionEntity);
            RaiseAmountUpdated(count: 1, pendingAmount: 0);

            factionEntity.Health.EntityDead += HandleFactionEntityDead;
        }

        protected void AddNewlyCreated(T factionEntity)
        {
            if (!CanInstanceBeRegulated(factionEntity))
                return;

            instances.Add(factionEntity);
            RaiseAmountUpdated(count: 0, pendingAmount: -1);

            factionEntity.Health.EntityDead += HandleFactionEntityDead;
        }

        protected void AddPending(T pendingFactionEntity)
        {
            if (!CanPendingInstanceBeRegulated(pendingFactionEntity))
                return;

            RaiseAmountUpdated(count: 1, pendingAmount: 1);
        }

        protected void RemoveExisting(T factionEntity)
        {
            if (!CanInstanceBeRegulated(factionEntity)
                || !instances.Remove(factionEntity))
                return;

            factionEntity.Health.EntityDead -= HandleFactionEntityDead;

            RaiseAmountUpdated(count: -1, pendingAmount: 0);
        }

        protected void RemovePending(T pendingFactionEntity)
        {
            if (!CanPendingInstanceBeRegulated(pendingFactionEntity))
                return;

            RaiseAmountUpdated(count: -1, pendingAmount: -1);
        }
        #endregion

        #region Handling Target Count
        public void UpdateTargetCount(int newTargetCount)
        {
            TargetCount = Mathf.Clamp(newTargetCount, MinTargetAmount, MaxTargetAmount);

            RaiseAmountUpdated(count: 0, pendingAmount: 0);
        }
        #endregion

        #region Regulation Helper Methods
        public virtual bool CanPendingInstanceBeRegulated(T factionEntity)
        {
            return factionEntity.IsValid()
                && factionEntity.Code == Prefab.Code;
        }

        public virtual bool CanInstanceBeRegulated(T factionEntity)
        {
            return factionEntity.IsValid()
                && factionMgr.IsSameFaction(factionEntity)
                && factionEntity.Code == Prefab.Code;
        }
        #endregion
    }
}
