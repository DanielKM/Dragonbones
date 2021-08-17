using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.UI
{
    [System.Serializable]
    public class ProgressBarUI
    {
        #region Attributes
        [SerializeField, Tooltip("Includes a UI Image component used to display the empty progress bar.")]
        private RectTransform emptyBar = null;
        private Image imageEmpty;

        [SerializeField, Tooltip("Includes a UI Image component used to display the full progress bar.")]
        private RectTransform fullBar = null;
        private Image imageFull;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            imageEmpty = emptyBar.GetComponent<Image>();
            imageFull = fullBar.GetComponent<Image>();

            if(!logger.RequireValid(emptyBar,
                $"[ProgressBarUI] The field 'Empty Bar' field has not been assigned!")
                || !logger.RequireValid(imageEmpty,
                $"[ProgressBarUI] The assigned 'Empty Bar' field does not have a '{typeof(Image).Name}' component attached to it!")
                || !logger.RequireValid(fullBar,
                $"[ProgressBarUI] The field 'Full Bar' field has not been assigned!")
                || !logger.RequireValid(imageFull,
                $"[ProgressBarUI] The assigned 'Full Bar' field does not have a '{typeof(Image).Name}' component attached to it!"))
                return;
        }
        #endregion

        #region Handling Progress Bar
        public void Toggle(bool enable)
        {
            imageEmpty.enabled = enable;
            imageFull.enabled = enable;
        }

        public void Update(float progress)
        {
            progress = Mathf.Clamp(progress, 0.0f, 1.0f);

            fullBar.sizeDelta = new Vector2(
                progress * emptyBar.sizeDelta.x,
                fullBar.sizeDelta.y);

            fullBar.localPosition = new Vector3(
                emptyBar.localPosition.x - (emptyBar.sizeDelta.x - fullBar.sizeDelta.x) / 2.0f,
                emptyBar.localPosition.y,
                emptyBar.localPosition.z);
        }
        #endregion
    }
}
