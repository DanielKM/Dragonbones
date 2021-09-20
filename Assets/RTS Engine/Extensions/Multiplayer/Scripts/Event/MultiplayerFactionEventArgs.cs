using RTSEngine.Multiplayer.Utilities;
using System;

namespace RTSEngine.Multiplayer.Event
{
    public class MultiplayerFactionEventArgs : EventArgs
    {
        public float LastRTT { private set; get; }

        public MultiplayerFactionEventArgs(float lastRTT)
        {
            LastRTT = lastRTT;
        }
    }
}