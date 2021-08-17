using RTSEngine.ResourceExtension;

namespace RTSEngine.NPC.ResourceExtension
{
    public interface INPCCapacityResourceManager : INPCComponent
    {
        ResourceTypeInfo TargetCapacityResource { get; }
        bool IsTargetCapacityReached { get; }

        bool OnIncreaseCapacityRequest();
    }
}