using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Lobby
{
    public struct LobbyGameData
    {
        public int mapID;

        public int defeatConditionID;
        public int timeModifierID;
        public int initialResourcesID;

        public List<int> factionSlotIndexSeed;
    }
}
