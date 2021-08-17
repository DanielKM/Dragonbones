using RTSEngine.Game;
using RTSEngine.UI.Utilities;

namespace RTSEngine.UI
{
    public interface IGamePlayerMessageUIHandler : IPostRunGameService
    {
        ITextMessage Message { get; }
    }
}
