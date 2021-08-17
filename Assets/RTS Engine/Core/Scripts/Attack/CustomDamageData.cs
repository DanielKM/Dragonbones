using RTSEngine.Entities;
using UnityEngine;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public struct CustomDamageData
    {
        [Tooltip("Define the codes or categories of entities that will be dealt a custom damage value.")]
        public CodeCategoryField code;
        [Tooltip("Input the custom damage value to deal.")]
        public int damage;
    }
}
