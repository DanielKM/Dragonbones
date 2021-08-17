using RTSEngine.Entities;
using System;

namespace RTSEngine.Event
{
    public class FactionUpdateArgs : EventArgs
    {
        public IEntity Source { private set; get; }
        public int TargetFactionID { private set; get; }

        public FactionUpdateArgs(IEntity source, int targetFactionID)
        {
            this.Source = source;
            this.TargetFactionID = targetFactionID;
        }
    }
}