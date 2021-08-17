using RTSEngine.Game;

namespace RTSEngine.Entities
{
    public interface IEntityPreInitializable : IEntityInitializable
    {
        void OnEntityPreInit(IGameManager gameMgr, IEntity entity);
    }
}
