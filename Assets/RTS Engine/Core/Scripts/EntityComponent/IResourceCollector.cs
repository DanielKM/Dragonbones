using RTSEngine.Entities;
using RTSEngine.ResourceExtension;

namespace RTSEngine.EntityComponent
{
    public interface IResourceCollector : IEntityTargetComponent
    {
        TargetData<IResource> Target { get; }
        bool InProgress { get; }

        bool IsResourceTypeCollectable(ResourceTypeInfo resourceType);
    }
}