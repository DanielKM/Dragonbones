using System;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.SpellCastExtension;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Health;

namespace RTSEngine.Entities
{
    public class Spell : FactionEntity, ISpell
    {
        #region Class Attributes
        public sealed override EntityType Type => EntityType.spell;

        public bool IsPlacementInstance { private set; get; } = false;
        public override bool IsDummy => IsPlacementInstance;

        public bool IsCast { private set; get; }
        public sealed override bool CanLaunchTask => base.CanLaunchTask && IsCast;

        public ISpellCastPlacer PlacerComponent { private set; get; }

        public new ISpellHealth Health { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<ISpell, EventArgs> SpellCastComplete;
        private void RaiseSpellCast()
        {
            var handler = SpellCastComplete;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, InitSpellParameters initParams)
        {
            IsCast = false;

            base.Init(gameMgr, initParams);

            if(!logger.RequireValid(Model,
                $"[{GetType().Name} - {Code}] The field 'Model' must be assigned!"))
                return;

            if (!IsPlacementInstance)
                Place(initParams.playerCommand);
        }

        protected override void FetchComponents()
        {
            PlacerComponent = transform.GetComponentInChildren<ISpellCastPlacer>();
            if (!logger.RequireValid(PlacerComponent,
                $"[{GetType().Name} - {Code}] Spell object must have a component that extends {typeof(ISpellCastPlacer).Name} interface attached to it!"))
                return;

            Health = transform.GetComponentInChildren<ISpellHealth>();

            base.FetchComponents();
        }

        protected override void SubToEvents()
        {
            base.SubToEvents();

            //subscribe to events
            Health.EntityHealthUpdated += HandleSpellHealthUpdated;
        }

        public void InitPlacementInstance (IGameManager gameMgr, InitSpellParameters initParams)
        {
            IsPlacementInstance = true;

            Init(gameMgr, initParams);

            if(this.IsLocalPlayerFaction() && SelectionMarker.IsValid())
                SelectionMarker.Enable(Color.green);
        }

        protected sealed override void Disable(bool isUpgrade, bool isFactionUpdate)
        {
            base.Disable(isUpgrade, isFactionUpdate);

            // if(!isFactionUpdate)
                Health.EntityHealthUpdated -= HandleSpellHealthUpdated; //just in case the spell is destroyed before it is fully constructed.

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Handling Events: Spell Health
        private void HandleSpellHealthUpdated(IEntity sender, HealthUpdateEventArgs e)
        {
            if(Health.HasMaxHealth)
            {
                CompleteConstruction();

                Health.EntityHealthUpdated -= HandleSpellHealthUpdated;
            }
        }
        #endregion

        #region Updating Spell State: Placed, ConstructionComplete
        private void Place(bool playerCommand)
        {
            // Hide the selection marker since it was used to display whether the spell can be placed or not.
            SelectionMarker?.Disable(); 

            CompleteInit();
            globalEvent.RaiseSpellPlacedGlobal(this);

            if (IsFree) //free builidng? job is done here
                return;

            if (!RTSHelper.IsLocalPlayerFaction(this))
                return;

            foreach (IUnit unit in selectionMgr.GetEntitiesList(EntityType.unit, exclusiveType: false, localPlayerFaction: true))
                unit.SetTargetFirst(this, playerCommand);
        }

        private void CompleteConstruction()
        {
            if (IsCast)
                return;

            IsCast = true;

            Model.SetActive(true);

            if (IsFree)
                return;

            resourceMgr.UpdateResource(FactionID, InitResources, add:true);

            RaiseSpellCast();
            globalEvent.RaiseSpellBuiltGlobal(this);
            OnConstructionComplete();
        }

        protected virtual void OnConstructionComplete() { }
        #endregion
    }
}
