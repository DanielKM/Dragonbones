using RTSEngine.Multiplayer.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Multiplayer.Server
{
    public struct MultiplayerFactionManagerTrackerData
    {
        public IMultiplayerFactionManager multiFactionMgr;
        public int factionID;

        public int logSize;

        public int maxInputCount;

        public float initialRTT;
    }
}
