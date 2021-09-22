using RTSEngine.Multiplayer.Utilities;
using System;

namespace RTSEngine.Multiplayer.Event
{
    public class MultiplayerStateEventArgs : EventArgs
    {
        public MultiplayerState State { private set; get; }

        public MultiplayerStateEventArgs(MultiplayerState state)
        {
            this.State = state;
        }
    }
}