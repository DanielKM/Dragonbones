using RTSEngine.Multiplayer.Utilities;

namespace RTSEngine.Multiplayer.Server
{
    public interface IMultiplayerServerManager : IMonoBehaviour
    {
        ServerAccessData AccessData { get; }

        void Execute(IMultiplayerManager multiplayerMgr);
    }
}