using RTSEngine.UI;
using System;

namespace RTSEngine.Event
{
    public class MessageEventArgs : EventArgs
    {
        public MessageType Type { private set; get; }
        public string Message { private set; get; }

        public bool CustomDurationEnabled { private set; get; }
        public float CustomDuration { private set; get; }

        public MessageEventArgs(MessageType type,
                                      string message,
                                      bool customDurationEnabled = false,
                                      float customDuration = 0.0f)
        {
            this.Type = type;
            this.Message = message;

            this.CustomDurationEnabled = customDurationEnabled;
            this.CustomDuration = customDuration;
        }
    }
}