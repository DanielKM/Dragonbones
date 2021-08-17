using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Event;
using RTSEngine.Logging;

namespace RTSEngine.UI.Utilities
{
    [System.Serializable]
    public class TextMessage : ITextMessage
    {
        #region Attributes
        private IMonoBehaviour source;

        [SerializeField, Tooltip("Parent object of the message UI Text element. This is optional!")]
        private GameObject panel = null;
        [SerializeField, Tooltip("UI Text responsible for displaying the message.")]
        private Text messageDisplay = null;

        [SerializeField, Tooltip("Disable this option to allow the message to be displayed until manually hidden.")]
        private bool useDuration = true;
        [SerializeField, Tooltip("Default duration for which a player message is visible.")]
		private float defaultDuration = 3.0f;

        // Coroutine used to wait for the message duration before disabling the message.
        private IEnumerator hideMessageCoroutine;
        #endregion

        #region Initializing/Terminating
        public void Init(IMonoBehaviour source, ILoggingService logger)
        {
            this.source = source;

            if(!logger.RequireValid(source,
                $"[{GetType().Name}] This class must be initialized by an object that implements interface '{typeof(IMonoBehaviour).Name}'.")
                || !logger.RequireValid(messageDisplay,
                $"[{GetType().Name}] The field 'Message Display' must be assigned!",
                source))
                return;

            Hide();
        }
        #endregion

        #region Displaying Message
        public void Display(string message, MessageType type = MessageType.info) => Display(new MessageEventArgs(type, message));

        public void Display(MessageEventArgs args)
        {
            if(hideMessageCoroutine != null)
                source.StopCoroutine(hideMessageCoroutine);

            if(panel)
                panel.gameObject.SetActive(true);

            messageDisplay.gameObject.SetActive(true);
			messageDisplay.text = args.Message;

            if (useDuration)
            {
                hideMessageCoroutine = Hide(args.CustomDurationEnabled ? args.CustomDuration : defaultDuration);
                source.StartCoroutine(hideMessageCoroutine);
            }
        }
        #endregion

        #region Hiding Message
        public void Hide()
        {
            if(panel.IsValid())
                panel.gameObject.SetActive(false);

            if(messageDisplay.IsValid())
                messageDisplay.gameObject.SetActive(false);

            if (hideMessageCoroutine.IsValid())
            {
                source.StopCoroutine(hideMessageCoroutine);
                hideMessageCoroutine = null;
            }
        }

        private IEnumerator Hide (float duration)
        {
            yield return new WaitForSeconds(duration);

            Hide();
        }
        #endregion
    }
}
