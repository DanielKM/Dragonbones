using RTSEngine.Event;
using RTSEngine.Logging;

namespace RTSEngine.UI.Utilities
{
    public interface ITextMessage
    {
        void Init(IMonoBehaviour source, ILoggingService logging);

        void Display(MessageEventArgs args);
        void Hide();
        void Display(string message, MessageType type = MessageType.info);
    }
}