using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Faction;

namespace RTSEngine.NPC.BuildingExtension
{
    [RequireComponent(typeof(IBuilding))]
    public class NPCBuildingRegulatorDataInput : MonoBehaviour
    {
        [SerializeField]
        private NPCBuildingRegulatorData allTypes = null;

        [System.Serializable]
        public struct InputElement
        {
            public bool ignoreFactionType;
            public FactionTypeInfo factionType;

            public bool ignoreNPCType;
            public NPCType npcType;

            public NPCBuildingRegulatorData regulatorData;
        }
        [SerializeField]
        private InputElement[] typeSpecific = new InputElement[0];

        public NPCBuildingRegulatorData GetFiltered(FactionTypeInfo factionType, NPCType npcType)
        {
            NPCBuildingRegulatorData filtered = allTypes;

            foreach (InputElement nextElement in typeSpecific)
                if ((nextElement.ignoreFactionType || nextElement.factionType == factionType)
                    && (nextElement.ignoreNPCType || nextElement.npcType== npcType))
                {
                    filtered = nextElement.regulatorData;
                    break;
                }

            return filtered;
        }

    }
}
