using System.Collections.Generic;

namespace RTSEngine.ResourceExtension
{
    public interface IFactionSlotResourceManager
    {
        IReadOnlyDictionary<ResourceTypeInfo, IFactionResourceHandler> ResourceHandlers { get; }
        float ResourceNeedRatio { get; set; }
    }
}