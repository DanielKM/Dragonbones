using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public interface IBorderObject : IMonoBehaviour
    {
        void Init(IGameManager gameMgr, IBorder border);
    }
}