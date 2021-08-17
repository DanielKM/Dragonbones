using RTSEngine.Game;

namespace RTSEngine.UI
{
    public interface ITaskUI<T> : IMonoBehaviour where T : ITaskUIAttributes
    {
        bool IsEnabled { get; }

        T Attributes { get; }

        void Init(IGameManager gameMgr, IGameService gameService);
        void Disable();

        void Click();

        void Reload(T attributes);
    }
}
