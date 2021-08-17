using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Entities
{
    public interface IEntityPostInitializable : IEntityInitializable
    {
        void OnEntityPostInit(IGameManager gameMgr, IEntity entity);
    }
}
