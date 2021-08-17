using RTSEngine.Entities;

namespace RTSEngine.Health
{
    public interface IResourceHealth : IEntityHealth
    {
        IResource Resource { get; }
    }
}