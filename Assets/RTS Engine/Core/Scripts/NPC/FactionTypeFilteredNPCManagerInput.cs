using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Faction;

namespace RTSEngine.NPC
{
    [System.Serializable]
    public class FactionTypeFilteredNPCManagerInput
    {
        [SerializeField, EnforceType(typeof(INPCManager), prefabOnly: true), Tooltip("NPC Manager for faction types that are not defined in the below elements input list.")]
        private GameObject allTypesPrefab = null;

        [System.Serializable]
        public struct ElementInput
        {
            public List<FactionTypeInfo> types;
            [EnforceType(typeof(INPCManager), prefabOnly: true)]
            public GameObject prefab;
        }
        [SerializeField, Tooltip("Each element allows to define a group of faction types that share one NPC Manager prefab.")]
        private List<ElementInput> typeSpecific = new List<ElementInput>();

        public INPCManager GetFiltered(FactionTypeInfo searchType)
        {
            INPCManager filtered = allTypesPrefab.GetComponent<INPCManager>();

            foreach (ElementInput e in typeSpecific)
                if (e.types.Contains(searchType))
                {
                    filtered = e.prefab.GetComponent<INPCManager>();
                    break;
                }

            return filtered;
        }
    }
}
