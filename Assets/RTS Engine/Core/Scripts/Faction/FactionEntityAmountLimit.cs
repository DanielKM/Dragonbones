using RTSEngine.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Faction
{
    [System.Serializable]
    public class FactionEntityAmountLimit
    {
        [SerializeField, Tooltip("Codes and categories for the units and buildings whose amounts would be limited.")]
        private CodeCategoryField definer;
        public CodeCategoryField Definer => definer;

        [SerializeField, Tooltip("The maximum amount for the units and buildings that are defined by the above codes/categories.")]
        private int maxAmount = 5;
        public int MaxAmount => maxAmount;

        private int currentAmount;

        public FactionEntityAmountLimit(CodeCategoryField definer, int maxAmount)
        {
            this.definer = definer;
            this.maxAmount = maxAmount;
        }

        public bool Contains(string code, IEnumerable<string> category) => definer.Contains(code, category);
        public bool IsMaxAmountReached(string code, IEnumerable<string> category) => Contains(code, category) && currentAmount >= maxAmount;
        public void Update(int value) => currentAmount += value;
    }
}
