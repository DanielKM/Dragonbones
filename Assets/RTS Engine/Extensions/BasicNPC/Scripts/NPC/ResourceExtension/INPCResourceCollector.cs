using RTSEngine.Entities;

namespace RTSEngine.NPC.ResourceExtension
{
    public interface INPCResourceCollector : INPCComponent
    {
        void AddResourceToCollect(IResource resource);
        void RemoveResourceToCollect(IResource resource);

        void OnResourceCollectionRequest(IResource resource, int targetCollectorsAmount, out int assignedCollectors, bool force = false);
    }
}