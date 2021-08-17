using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackTargetPicker : FactionEntityTargetPicker
    {
        [SerializeField, Tooltip("Target and attack units?")]
        private bool engageUnits = true;
        [SerializeField, Tooltip("Target and attack buildings?")]
        private bool engageBuildings = true;

        public override bool IsValidTarget(IFactionEntity factionEntity)
        {
            return ((factionEntity.Type == EntityType.building && engageBuildings)
                    || (factionEntity.Type == EntityType.unit && engageUnits))
                    && base.IsValidTarget(factionEntity);
        }
    }
}
