using System.Linq;

using UnityEngine;

using RTSEngine.Faction;
using RTSEngine.Utilities;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public class FactionTypeFilteredResourceType : TypeFilteredValue<FactionTypeInfo, ResourceTypeInfo>
    {
        [System.Serializable]
        public struct Element
        {
            public FactionTypeInfo[] factionTypes;
            public ResourceTypeInfo resourceType;
        }
        [SerializeField]
        private Element[] typeSpecific = new Element[0];

        public override ResourceTypeInfo GetFiltered(FactionTypeInfo factionType)
        {
            ResourceTypeInfo filtered = allTypes;

            foreach(Element elem in typeSpecific)
                if(elem.factionTypes.Contains(factionType))
                {
                    filtered = elem.resourceType;
                    break;
                }

            return filtered;
        }
    }
}
