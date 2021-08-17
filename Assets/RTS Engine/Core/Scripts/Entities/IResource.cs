using RTSEngine.Audio;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.ResourceExtension;

namespace RTSEngine.Entities
{
    public interface IResource : IEntity
    {
        ResourceTypeInfo ResourceType { get; }

        bool CanCollect { get; }
        bool CanCollectOutsideBorder { get; }
        AudioClipFetcher CollectionAudio { get; }

        new IResourceHealth Health { get; }
        new IResourceWorkerManager WorkerMgr { get; }

        void Init(IGameManager gameMgr, InitResourceParameters initParams);
    }
}
