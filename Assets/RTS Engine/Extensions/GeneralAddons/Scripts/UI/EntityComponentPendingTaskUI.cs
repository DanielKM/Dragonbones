using UnityEngine;

namespace RTSEngine.UI
{
    public class EntityComponentPendingTaskUI : BaseTaskUI<EntityComponentPendingTaskUIAttributes>
    {
        #region Attributes
        protected override Sprite Icon => 
            Attributes.locked && Attributes.lockedData.icon != null 
            ? Attributes.lockedData.icon
            : Attributes.data.icon; 

        protected override Color IconColor => 
            Attributes.locked 
            ? Attributes.lockedData.color 
            : Color.white;

        protected override bool IsTooltipEnabled => Attributes.data.tooltipEnabled;

        protected override string TooltipDescription => Attributes.data.description;

        [SerializeField, Tooltip("To display the progress of the pending task.")]
        private ProgressBarUI progressBar = new ProgressBarUI();
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            progressBar.Init(gameMgr);
        }
        #endregion

        #region Disabling Task UI
        protected override void OnDisabled()
        {
            progressBar.Toggle(false);
        }
        #endregion

        #region Reloading Attributes
        protected override void OnReload()
        {
            progressBar.Toggle(true);

            // Set default size and position of the progress bar:
            progressBar.Update(0.0f);
        }

        private void Update()
        {
            // Only display progress for the first task.
            if(Attributes.pendingData.queueIndex != 0) 
                return;

            // Update the progress bar to show the pending task progress
            progressBar.Update(1.0f - Attributes.pendingData.handler.QueueTimerValue / Attributes.data.reloadTime);
        }
        #endregion

        #region Interacting with Task UI
        protected override void OnClick()
        {
            Attributes.pendingData.handler.CancelByQueueID(Attributes.pendingData.queueIndex);

            if (Attributes.data.hideTooltipOnClick)
                HideTaskTooltip();
        }
        #endregion
    }
}
