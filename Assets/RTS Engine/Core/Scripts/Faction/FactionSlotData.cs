using RTSEngine.Lobby;
using RTSEngine.NPC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Faction
{
    [System.Serializable]
    public struct FactionSlotData
    {
        [Tooltip("Role of the faction that determines its players' permissions.")]
        public FactionSlotRole role;

        [Tooltip("Name of the faction to appear in the UI elements.")]
        public string name;
        [Tooltip("Selection and main color of the faction entities.")]
        public Color color;
        [Tooltip("Type of the faction.")]
        public FactionTypeInfo type;

        [Tooltip("NPC type of the faction.")]
        public NPCType npcType;

        [Tooltip("Is the faction directly controlled by the local player?")]
        public bool isLocalPlayer;
    }
}
