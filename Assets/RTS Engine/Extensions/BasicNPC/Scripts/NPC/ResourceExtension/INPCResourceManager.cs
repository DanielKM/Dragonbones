using RTSEngine.ResourceExtension;
using System.Collections.Generic;

namespace RTSEngine.NPC.ResourceExtension
{
    public interface INPCResourceManager : INPCComponent
    {
        void OnIncreaseMissingResourceRequest(IEnumerable<ResourceInput> resourceInputs);
    }
}
