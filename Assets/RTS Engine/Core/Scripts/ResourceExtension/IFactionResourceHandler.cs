using RTSEngine.Event;

namespace RTSEngine.ResourceExtension
{
    public interface IFactionResourceHandler
    {
        ResourceTypeInfo Type { get; }

        int Amount { get; }
        int Capacity { get; }
        int FreeAmount { get; }

        event CustomEventHandler<IFactionResourceHandler, ResourceUpdateEventArgs> FactionResourceAmountUpdated;

        void UpdateAmount(ResourceTypeValue updateValue);
    }
}