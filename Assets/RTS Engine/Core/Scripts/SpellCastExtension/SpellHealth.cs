using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Health
{
    public class SpellHealth : FactionEntityHealth, ISpellHealth
    {
        #region Attributes
        public ISpell Spell { private set; get; }
        public override EntityType EntityType => EntityType.spell;


        [SerializeField, Tooltip("Possible health states that the spell can have while it is being constructed.")]
        private List<EntityHealthState> constructionStates = new List<EntityHealthState>();  

        [SerializeField, Tooltip("State to activate when the spell completes construction, a transition state from construction states to regular spell states.")]
        private EntityHealthState constructionCompleteState = new EntityHealthState();
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            Spell = Entity as ISpell;

            // Show the construction state only if this is not the placement instance
            // We also check for whether the spell has been built or not because in case of a faction conversion, components are re-initiated and this would cause the construction states to appear.
            if(!Spell.IsPlacementInstance && !Spell.IsCast) 
                stateHandler.Reset(constructionStates, CurrHealth);
        }
        #endregion

        #region Updating Health
        protected override void OnHealthUpdated(int updateValue, IEntity source)
        {
            base.OnHealthUpdated(updateValue, source);

            globalEvent.RaiseSpellHealthUpdatedGlobal(Spell, new HealthUpdateEventArgs(updateValue, source));
        }

        protected override void OnMaxHealthReached(int updateValue, IEntity source)
        {
            if(!Spell.IsCast)
            {
                stateHandler.Activate(constructionCompleteState);

                stateHandler.Reset(states, CurrHealth);
            }

            base.OnMaxHealthReached(updateValue, source);
        }
        #endregion

        #region Destroying Spell
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseSpellDeadGlobal(Spell, new DeadEventArgs(upgrade, source));
        }
        #endregion
    }
}
