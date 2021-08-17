using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
    public partial class Healer
    {
        [System.Serializable]
        public class HealerTargetPicker : FactionEntityTargetPicker
        {
            [SerializeField, Tooltip("Allow the healer to heal units that are in range of the faction entity but not stored in it?")]
            private bool healExternal = true;
            [SerializeField, Tooltip("Allow the healer to heal units stored inside the same faction entity?")]
            private bool healStored = true;

            public bool IsValidTarget(IEntityTargetComponent healer, IFactionEntity factionEntity)
            {
                if(factionEntity.Type == EntityType.unit)
                {
                    IUnit unit = factionEntity as IUnit;
                    IFactionEntity healerEntity = healer.Entity as IFactionEntity;

                    // Unit has an active carrier where it stored
                    if (unit.CarriableUnit.IsValid() && unit.CarriableUnit.CurrCarrier.IsValid())
                    {
                        // If the carrier is different than the healer or it is the healer but we can not heal stored units
                        if (!healerEntity.UnitCarrier.IsUnitStored(unit) || !healStored)
                            return false;
                    }
                    else if (!healExternal) // Unit is not carried by a UnitCarrier but we can not heal non-stored units
                        return false;
                }

                return base.IsValidTarget(factionEntity);
            }
        }
    }
}
