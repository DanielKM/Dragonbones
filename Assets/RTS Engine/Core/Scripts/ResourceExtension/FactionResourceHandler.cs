using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.ResourceExtension
{
    public class FactionResourceHandler : IFactionResourceHandler
    {
        #region Attributes
        private int factionID;
        public ResourceTypeInfo Type { private set; get; }

        public int Amount { private set; get; }
        public int Capacity { private set; get; }
        public int FreeAmount => Capacity - Amount;

        // Game services
        protected IGlobalEventPublisher globalEventPublisher { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IFactionResourceHandler, ResourceUpdateEventArgs> FactionResourceAmountUpdated;

        private void RaiseFactionResourceAmountUpdated(ResourceUpdateEventArgs args)
        {
            CustomEventHandler<IFactionResourceHandler, ResourceUpdateEventArgs> handler = FactionResourceAmountUpdated;

            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public FactionResourceHandler(
            IFactionSlot factionSlot,
            IGameManager gameMgr,
            ResourceTypeInfo data,
            ResourceTypeValue startingAmount)
        {
            this.factionID = factionSlot.ID;
            this.Type = data;

            this.globalEventPublisher = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            Amount = startingAmount.amount;
            Capacity = startingAmount.capacity;
        }
        #endregion

        #region Updating Amount
        public void UpdateAmount(ResourceTypeValue updateValue)
        {
            Capacity += updateValue.capacity;
            Amount += updateValue.amount;

            ResourceUpdateEventArgs eventArgs = new ResourceUpdateEventArgs(
                    Type,
                    updateValue);

            globalEventPublisher.RaiseFactionSlotResourceAmountUpdatedGlobal(factionID.ToFactionSlot(), eventArgs);
            RaiseFactionResourceAmountUpdated(eventArgs);
        }
        #endregion
    }
}
