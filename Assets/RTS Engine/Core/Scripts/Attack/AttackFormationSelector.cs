using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Movement;
using RTSEngine.EntityComponent;
using UnityEngine.Assertions;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackFormationSelector
    {
        [SerializeField, Tooltip("Minimum and maximum stopping distance when targeting a unit.")]
        public FloatRange unitStoppingDistance = new FloatRange(2.0f, 6.0f); 
        [SerializeField, Tooltip("Minimum and maximum stopping distance when targeting a building.")]
        private FloatRange buildingStoppingDistance = new FloatRange(5.0f, 10.0f); 
        [SerializeField, Tooltip("Minimum and maximum stopping distance when not targeting a specific target.")]
        private FloatRange noTargetStoppingDistance = new FloatRange(5.0f, 10.0f);

        [SerializeField, Tooltip("Enforce minimum stopping distance when engaging with a target.")]
        private bool enforceMinStoppingDistance = false;
        [SerializeField, Tooltip("The minimum stopping distance to enforce when engaging a target.")]
        private float minStoppingDistance = 0.5f;

        [SerializeField, Tooltip("How far does the attack target need to move in order to recalculate the attacker's unit movement."), Min(0), Space()]
        private float updateMvtDistance = 2.0f; 

        [SerializeField, Tooltip("Attack movement formation for this unit type.")]
        private MovementFormationSelector movementFormation = new MovementFormationSelector { };
        public MovementFormationSelector MovementFormation => movementFormation;

        private IAttackComponent source;

        public void Init(IAttackComponent source)
        {
            this.source = source;


            Assert.IsNotNull(MovementFormation.type,
                $"[UnitMovement - '{source.Entity.Code}'] The attack movement formation type must be assigned!");
        }

        public bool MustUpdateAttackPosition (Vector3 lastTargetPosition, Vector3 currTargetPosition, Vector3 currAttackPosition, IFactionEntity target)
        {
            return Vector3.Distance(lastTargetPosition, currTargetPosition) > updateMvtDistance
                && Vector3.Distance(currTargetPosition, currAttackPosition) > GetStoppingDistance(target, min:false);
        }

        /// <summary>
        /// Get the appropriate stopping distance for an attack depending on the target type.
        /// </summary>
        /// <param name="target">FactionEntity instance that represents the potential target for the unit.</param>
        /// <param name="min">True to get the minimum value of the stopping range and false to get the maximum value of the stopping range.</param>
        /// <returns>Stopping distance for the unit's movement to launch an attack.</returns>
        public float GetStoppingDistance (IEntity target, bool min = true)
        {
            float stoppingDistance;

            EntityType targetType = target.IsValid() ? target.Type : EntityType.all;
            switch(targetType)
            {
                case EntityType.unit:
                    stoppingDistance = min ? unitStoppingDistance.min : unitStoppingDistance.max;
                    break;
                case EntityType.building:
                    stoppingDistance = min ? buildingStoppingDistance.min : buildingStoppingDistance.max;
                    break;
                default:
                    stoppingDistance = min ? noTargetStoppingDistance.min : noTargetStoppingDistance.max;
                    break;
            }

            return stoppingDistance + (target.IsValid() ? target.Radius : 0.0f);
        }

        public bool IsTargetInRange (Vector3 attackPosition, TargetData<IFactionEntity> target)
        {
            float distance = Vector3.Distance(attackPosition, RTSHelper.GetAttackTargetPosition(target));
            return distance <= GetStoppingDistance(target.instance, min: false)
                && (!enforceMinStoppingDistance || distance >= Mathf.Min(GetStoppingDistance(target.instance, min: true), minStoppingDistance));
        }
    }
}
