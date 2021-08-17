using RTSEngine.Movement;
using System;

namespace RTSEngine.Event
{
    public class MovementEventArgs : EventArgs
    {
        public MovementSource Source { private set; get; }

        public MovementEventArgs(MovementSource source)
        {
            this.Source = source;
        }
    }
}
