using UnityEngine;

using RTSEngine.Event;
using RTSEngine.UI.Utilities;
using RTSEngine.Logging;
using RTSEngine.Audio;

namespace RTSEngine.UI
{
    public abstract class PlayerMessageUIHandlerBase : MonoBehaviour, IMonoBehaviour
    {
        #region Attributes
        [SerializeField, Tooltip("Handles displaying the player message.")]
        private TextMessage message = new TextMessage();
        public ITextMessage Message => message;

        [Header("Audio")]
        [SerializeField, Tooltip("Audio clip played when an informational message is displayed for the player.")]
        private AudioClipFetcher infoMessageAudio = new AudioClipFetcher();
        [SerializeField, Tooltip("Audio clip played when a warning message is displayed for the player.")]
        private AudioClipFetcher warningMessageAudio = new AudioClipFetcher();
        [SerializeField, Tooltip("Audio clip played when an error message is displayed for the player.")]
        private AudioClipFetcher errorMessageAudio = new AudioClipFetcher();

        // Services
        protected IAudioManager audioMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected void InitBase(ILoggingService logger, IAudioManager audioMgr)
        {
            this.audioMgr = audioMgr;

            message.Init(this, logger);
        }

        private void OnDestroy()
        {
            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Displaying Player Message
        protected void DisplayMessage(MessageEventArgs args)
        {
            message.Display(args);

            switch(args.Type)
            {
                case MessageType.info:
                    audioMgr.PlaySFX(infoMessageAudio);
                    break;

                case MessageType.warning:
                    audioMgr.PlaySFX(warningMessageAudio);
                    break;

                case MessageType.error:
                    audioMgr.PlaySFX(errorMessageAudio);
                    break;
            }
        }
        #endregion
    }
}
