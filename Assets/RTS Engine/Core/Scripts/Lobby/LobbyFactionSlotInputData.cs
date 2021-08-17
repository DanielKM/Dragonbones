using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Lobby
{
    [System.Serializable]
    public class LobbyFactionSlotInputData
    {
        public string name = "";

        public int colorID = 0;

        public int factionTypeID = 0;
        public int npcTypeID = 0;
    }
}
