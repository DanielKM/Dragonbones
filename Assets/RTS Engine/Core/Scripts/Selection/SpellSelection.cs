using System;

using RTSEngine.Entities;

namespace RTSEngine.Selection
{
    public class SpellSelection : EntitySelection
    {
        #region Attributes
        protected ISpell spell { private set; get; }

        protected override bool extraSelectCondition => !spell.IsPlacementInstance;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            spell = Entity as ISpell;

            spell.EntityInitiated += HandleEntityInitiated;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            spell.EntityInitiated -= HandleEntityInitiated;
        }
        #endregion

        #region Handling Event: Entity Initiated
        private void HandleEntityInitiated(IEntity entity, EventArgs args)
        {
            // Can only select spell after they are placed.
            IsActive = true;
        }
        #endregion
    }
}
