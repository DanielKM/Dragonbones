using RTSEngine.ResourceExtension;
using System;

namespace RTSEngine.Event
{
    public class ResourceUpdateEventArgs : EventArgs
    {
        public ResourceTypeInfo ResourceType { private set; get; }
        public ResourceTypeValue UpdateValue { private set; get; }

        public ResourceUpdateEventArgs(ResourceTypeInfo resourceType, ResourceTypeValue updateValue)
        {
            this.ResourceType = resourceType;
            this.UpdateValue = updateValue;
        }

    }
}