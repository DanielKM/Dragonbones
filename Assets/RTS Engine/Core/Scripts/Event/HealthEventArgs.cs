using RTSEngine.Entities;
using System;

namespace RTSEngine.Event
{
    public class HealthUpdateEventArgs : EventArgs
    {
        public int Value { private set; get; }
        public IEntity Source { private set; get; }

        public HealthUpdateEventArgs(int value, IEntity source)
        {
            this.Value = value;
            this.Source = source;
        }
    }

    public class DeadEventArgs : EventArgs
    {
        public bool IsUpgrade { private set; get; }
        public IEntity Source { private set; get; }

        public DeadEventArgs(bool isUpgrade, IEntity source)
        {
            this.IsUpgrade = isUpgrade;
            this.Source = source;
        }
    }
}
