using System;

namespace RTSEngine.NPC.Event
{
    public class NPCRegulatorUpdateEventArgs : EventArgs
    {
        public int Count { private set; get; }
        public int PendingAmount { private set; get; }

        public NPCRegulatorUpdateEventArgs(int count, int pendingAmount)
        {
            this.Count = count;

            this.PendingAmount = pendingAmount;
        }
    }
}
