using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Faction;

namespace RTSEngine.NPC.UnitExtension
{
    [RequireComponent(typeof(IUnit))]
    public class NPCUnitRegulatorDataInput : MonoBehaviour
    {
        [SerializeField]
        private NPCUnitRegulatorData allTypes = null;

        [System.Serializable]
        public struct InputElement
        {
            public bool ignoreFactionType;
            public FactionTypeInfo factionType;

            public bool ignoreNPCType;
            public NPCType npcType;

            public NPCUnitRegulatorData regulatorData;
        }
        [SerializeField]
        private InputElement[] typeSpecific = new InputElement[0];

        public NPCUnitRegulatorData GetFiltered(FactionTypeInfo factionType, NPCType npcType)
        {
            NPCUnitRegulatorData filtered = allTypes;

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
