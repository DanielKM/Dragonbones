using UnityEngine;

using RTSEngine.Faction;

namespace RTSEngine.NPC
{
    [CreateAssetMenu(fileName = "NewNPCTypeInfo", menuName = "RTS Engine/NPC/NPC Type", order = 51)]
    public class NPCType : RTSEngineScriptableObject 
    {
        [SerializeField, Tooltip("Name of the NPC type to be displayed in UI elements.")]
        private string _name = "New NPC Type";
        public string Name => _name;

        [SerializeField, Tooltip("Unique code for each type of NPC.")]
        private string code = "new_npc_type";
        public override string Key => code;

        [SerializeField, Tooltip("Defines NPC Manager prefabs to be used with different faction types that the NPC faction can take."), InspectorName("NPC Managers")]
        private FactionTypeFilteredNPCManagerInput npcManagers = new FactionTypeFilteredNPCManagerInput();
        public INPCManager GetNPCManagerPrefab(FactionTypeInfo factionType) => npcManagers.GetFiltered(factionType);
    }
}
