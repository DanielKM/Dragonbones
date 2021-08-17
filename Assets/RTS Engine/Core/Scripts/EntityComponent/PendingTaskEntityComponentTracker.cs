using System;

using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;

namespace RTSEngine.EntityComponent
{
    public class PendingTaskEntityComponentTracker<T> : IEntityComponentTracker<T> where T : IPendingTaskEntityComponent
    {
        #region Class Attributes
        public List<T> components;
        public IEnumerable<T> Components => components;

        protected readonly IGameManager gameMgr;
        protected readonly IGlobalEventPublisher globalEvent;
        protected readonly IFactionManager factionMgr;
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntityComponentTracker<T>, EntityComponentEventArgs<T>> ComponentAdded;
        public event CustomEventHandler<IEntityComponentTracker<T>, EntityComponentEventArgs<T>> ComponentUpdated;
        public event CustomEventHandler<IEntityComponentTracker<T>, EntityComponentEventArgs<T>> ComponentRemoved;

        private void RaiseComponentAdded (EntityComponentEventArgs<T> e)
        {
            var handler = ComponentAdded;
            handler?.Invoke(this, e);
        }
        private void RaiseComponentUpdated (EntityComponentEventArgs<T> e)
        {
            var handler = ComponentUpdated;
            handler?.Invoke(this, e);
        }
        private void RaiseComponentRemoved (EntityComponentEventArgs<T> e)
        {
            var handler = ComponentRemoved;
            handler?.Invoke(this, e);
        }
        #endregion

        #region Initializing/Terminating
        public PendingTaskEntityComponentTracker(IGameManager gameMgr, IFactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();
            this.factionMgr = factionMgr;

            this.components = new List<T>();

            //inspect the already spawned/created faction entities and see if they have the component that is tracker here
            foreach (IFactionEntity factionEntity in this.factionMgr.FactionEntities)
                foreach(IEntityComponent entityComponent in factionEntity.EntityComponents.Values)
                    if(entityComponent is T)
                        components.Add((T)entityComponent);

            this.globalEvent.PendingTaskEntityComponentAdded += HandlePendingTaskEntityComponentAdded;
            this.globalEvent.PendingTaskEntityComponentUpdated += HandlePendingTaskEntityComponentUpdated;
            this.globalEvent.PendingTaskEntityComponentRemoved += HandlePendingTaskEntityComponentRemoved;
        }

        public void Disable()
        {
            globalEvent.PendingTaskEntityComponentAdded -= HandlePendingTaskEntityComponentAdded;
            globalEvent.PendingTaskEntityComponentUpdated -= HandlePendingTaskEntityComponentUpdated;
            globalEvent.PendingTaskEntityComponentRemoved -= HandlePendingTaskEntityComponentRemoved;

            components.Clear();
        }
        #endregion

        #region Handling Events: IPendingTaskEntityComponent
        private void HandlePendingTaskEntityComponentAdded(IPendingTaskEntityComponent sender, EventArgs e)
        {
            if (!RTSHelper.IsSameFaction(sender.Entity.FactionID, factionMgr.FactionID)
                || !(sender is T))
                return;

            T newComponent = (T)sender;
            components.Add(newComponent);

            RaiseComponentAdded(new EntityComponentEventArgs<T>(newComponent));
        }

        private void HandlePendingTaskEntityComponentUpdated(IPendingTaskEntityComponent sender, EventArgs e)
        {
            if (!(sender is T))
                return;

            RaiseComponentUpdated(new EntityComponentEventArgs<T>((T)sender));
        }

        private void HandlePendingTaskEntityComponentRemoved(IPendingTaskEntityComponent sender, EventArgs e)
        {
            if (!RTSHelper.IsSameFaction(sender.Entity.FactionID, factionMgr.FactionID)
                || !(sender is T))
                return;

            T removeComponent = (T)sender;
            components.Remove(removeComponent);

            RaiseComponentRemoved(new EntityComponentEventArgs<T>(removeComponent));
        }
        #endregion
    }
}
