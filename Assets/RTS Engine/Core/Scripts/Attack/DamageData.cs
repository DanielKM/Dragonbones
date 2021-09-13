using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public struct DamageData
    {
        [Tooltip("Default damage value to deal units that do not have a custom damage enabled.")]
        public int unit;
        [Tooltip("Default damage value to deal buildings that do not have a custom damage enabled.")]
        public int building;
        [Tooltip("Default damage value to deal spells that do not have a custom damage enabled.")]
        public int spell;

        [Tooltip("Define custom damage values for unit and building types.")]
        public CustomDamageData[] custom;

        public int Get (IFactionEntity target)
        {
            foreach (CustomDamageData cd in custom)
                if (cd.code.Contains(target))
                    return cd.damage;

            if(target.Type == EntityType.unit)
            {
                return unit;
            }
            else if(target.Type == EntityType.building)
            {
                return building;
            } else 
            {
                return spell;
            }
        }
    }
}
