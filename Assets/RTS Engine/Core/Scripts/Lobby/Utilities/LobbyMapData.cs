using System.Collections.Generic;

using UnityEngine;

using RTSEngine.NPC;
using RTSEngine.Lobby.Logging;
using RTSEngine.Logging;
using RTSEngine.Faction;

namespace RTSEngine.Lobby.Utilities
{
    [System.Serializable]
    public struct LobbyMapData
    {
        [Tooltip("The scene name that has the RTS Engine map to load when the game starts. Make sure the scene is added to the build settings.")]
        public string sceneName;

        [Tooltip("Name to display for the map in the UI menu.")]
        public string name;
        [Tooltip("Description to display for the map in the UI menu.")]
        public string description;

        [SerializeField, Tooltip("Minimum amount of factions allowed to play the map.")]
        public IntRange factionsAmount;

        [Tooltip("Types of factions available to select to play with in the map.")]
        public FactionTypeInfo[] factionTypes;
        public FactionTypeInfo GetFactionType(int index) => factionTypes[index];

        [Tooltip("NPC types to choose from.")]
        public NPCType[] npcTypes;
        public NPCType GetNPCType(int index) => npcTypes[index];

        public void Init(ILobbyManager lobbyMgr)
        {
            ILoggingService logger = lobbyMgr.GetService<ILobbyLoggingService>();

            logger.RequireTrue(factionsAmount.min >= 1,
                $"[{GetType().Name} - '{name}'] Minimum amount of factions must be at least 1.");

            logger.RequireTrue(factionTypes.Length > 0,
                $"[{GetType().Name} - '{name}'] At least one FactionTypeInfo asset must be assigned.");
            logger.RequireValid(factionTypes,
                $"[{GetType().Name} - '{name}'] Make sure all FactionTypeInfo assets are not null.");

            logger.RequireTrue(npcTypes.Length > 0,
                $"[{GetType().Name} - '{name}'] At least one NPCTypeInfo asset must be assigned.");
            logger.RequireValid(npcTypes,
                $"[{GetType().Name} - '{name}'] Make sure all NPCTypeInfo assets are not null.");
        }
    }
}
