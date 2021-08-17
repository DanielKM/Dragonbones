using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace RTSEngine.Entities
{
    [System.Serializable]
    public struct CodeCategoryField
    {
        [EntityCodeInput, Tooltip("Input codes of entities.")]
        public string[] codes;
        [EntityCategoryInput(isDefiner: false), Tooltip("Input categories of entities.")]
        public string[] categories;

        public bool Contains(IEntity entity) => Contains(entity.Code, entity.Category);

        public bool Contains(string code, IEnumerable<string> category) => (codes != null && codes.Contains(code)) || (categories != null && category.Intersect(categories).Any());
    }
}
