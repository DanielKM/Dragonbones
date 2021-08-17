using RTSEngine.Lobby.Service;
using RTSEngine.UI.Utilities;

namespace RTSEngine.Lobby.UI
{
    public interface ILobbyPlayerMessageUIHandler : ILobbyService
    {
        ITextMessage Message { get; }
    }
}
