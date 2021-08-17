using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Selection;

namespace RTSEngine.UI
{
    public class MultipleSelectionTaskUI : BaseTaskUI<MultipleSelectionTaskUIAttributes>
    {
        #region Attributes
        protected override Sprite Icon => Attributes.selectedEntities.First().Icon;

        protected override Color IconColor => Color.white;

        protected override bool IsTooltipEnabled => Attributes.data.tooltipEnabled;

        protected override string TooltipDescription => Attributes.data.description;

        // Amount of selected entities represented by this multiple selection task.
        private int count = 0;

        [SerializeField, Tooltip("To display the progress of the pending task.")]
        private ProgressBarUI progressBar = new ProgressBarUI();

        [SerializeField, Tooltip("UI Text to display the amount of the multiple selected entities.")]
        private Text label = null;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            if (!logger.RequireValid(image,
                $"[{GetType().Name}] The 'Label' field must be assigned!"))
                return;

            progressBar.Init(gameMgr);
        }
        #endregion

        #region Disabling Task UI
        protected override void OnDisabled()
        {
            if (count == 1)
                Attributes.selectedEntities.First().Health.EntityHealthUpdated -= HandleSelectedEntityHealthUpdated;

            progressBar.Toggle(false);
            label.enabled = false;

            count = 0;
        }
        #endregion

        #region Handling Event: Selected Entity Health Updated
        private void HandleSelectedEntityHealthUpdated(IEntity entity, HealthUpdateEventArgs e)
        {
            progressBar.Update(entity.Health.CurrHealth / (float)entity.Health.MaxHealth);
        }
        #endregion

        #region Handling Attributes Reload
        protected override void OnReload()
        {
            count = Attributes.selectedEntities.Count();

            // Only display health for individual selection tasks.
            if (count == 1)
            {
                progressBar.Toggle(true);

                Attributes.selectedEntities.First().Health.EntityHealthUpdated += HandleSelectedEntityHealthUpdated;
                // Call to set the initial health bar value:
                HandleSelectedEntityHealthUpdated(Attributes.selectedEntities.First(), null);

                label.enabled = false;
            }
            else
            {
                progressBar.Toggle(false);

                label.enabled = true;
                // Only if this a multiple selection task for multiple entities, then show their amount
                label.text = count.ToString();
            }

        }
        #endregion

        #region Handling Task UI Interaction
        protected override void OnClick()
        {
            // If the player is holding the multiple selection key then deselect the clicked entity
            if (mouseSelector.MultipleSelectionKeyDown) 
                selectionMgr.Remove(Attributes.selectedEntities);
            else 
            {
                if (count == 1)
                    selectionMgr.Add(
                        Attributes.selectedEntities.First(),
                        SelectionType.single); 
                else
                    selectionMgr.Add(Attributes.selectedEntities);
            }

            HideTaskTooltip();
        }
        #endregion
    }
}
