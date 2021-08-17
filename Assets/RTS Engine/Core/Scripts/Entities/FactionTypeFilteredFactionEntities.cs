using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Faction;
using RTSEngine.Utilities;

namespace RTSEngine.Entities
{
    [System.Serializable]
    public class FactionTypeFilteredFactionEntities : TypeFilteredValue<FactionTypeInfo, IEnumerable<IFactionEntity>>
    {
        [SerializeField, EnforceType(typeof(IFactionEntity))]
        protected new GameObject[] allTypes = new GameObject[0];

        [System.Serializable]
        public struct Element
        {
            public FactionTypeInfo[] factionTypes;
            [EnforceType(typeof(IFactionEntity))]
            public GameObject[] factionEntities;
        }
        [SerializeField]
        private Element[] typeSpecific = new Element[0];

        public IEnumerable<IFactionEntity> GetAll()
        {
            IEnumerable<IFactionEntity> all = allTypes.FromGameObject<IFactionEntity>();

            foreach (Element element in typeSpecific)
                all = all
                    .Concat(element.factionEntities.FromGameObject<IFactionEntity>());

            return all;
        }

        public override IEnumerable<IFactionEntity> GetFiltered(FactionTypeInfo factionType)
        {
            IEnumerable<IFactionEntity> filtered = Enumerable.Empty<IFactionEntity>();
            filtered = filtered
                .Concat(allTypes.FromGameObject<IFactionEntity>());

            if(factionType != null)
                foreach(Element element in typeSpecific)
                    if (element.factionTypes.Contains(factionType))
                        filtered = filtered
                            .Concat(element.factionEntities.FromGameObject<IFactionEntity>());

            return filtered;
        }

        public IEnumerable<IFactionEntity> GetFiltered(FactionTypeInfo factionType, out IEnumerable<IFactionEntity> rest)
        {
            IEnumerable<IFactionEntity> filtered = Enumerable.Empty<IFactionEntity>();
            filtered = filtered
                .Concat(allTypes.FromGameObject<IFactionEntity>());

            rest = Enumerable.Empty<IFactionEntity>();

            foreach (Element element in typeSpecific)
            {
                if (factionType.IsValid() && element.factionTypes.Contains(factionType))
                    filtered = filtered
                        .Concat(element.factionEntities.FromGameObject<IFactionEntity>());
                else
                    rest = rest
                        .Concat(element.factionEntities.FromGameObject<IFactionEntity>());
            }

            return filtered;
        }

    }
}
