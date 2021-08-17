using RTSEngine.Multiplayer.Utilities;
using System;

namespace RTSEngine.Multiplayer.Event
{
    public class MultiplayerStateEventArgs : EventArgs
    {
        public MultiplayerState State;

        public MultiplayerStateEventArgs(MultiplayerState state)
        {
            this.State = state;
        }
    }
}