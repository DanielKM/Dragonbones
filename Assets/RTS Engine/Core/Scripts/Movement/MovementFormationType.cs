using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Movement
{
    [CreateAssetMenu(fileName = "NewMovementFormationType", menuName = "RTS Engine/Movement Formation Type", order = 151)]
    public class MovementFormationType : RTSEngineScriptableObject
    {
        [SerializeField, Tooltip("Unique code for each movement formation type.")]
        private string code = "unique_formation_type";
        public override string Key => code;

        [Header("Properties"), SerializeField, Tooltip("Create properties of type 'float' for this formation. Make sure each property has a unique name!")]
        private MovementFormationPropertyFloat[] floatProperties = new MovementFormationPropertyFloat[0];
        public IReadOnlyDictionary<string, float> DefaultFloatProperties => floatProperties
            .ToDictionary(prop => prop.name, prop => prop.value);

        [SerializeField, Tooltip("Create properties of type 'int' for this formation. Make sure each property has a unique name!")]
        private MovementFormationPropertyInt[] intProperties = new MovementFormationPropertyInt[0];
        public IReadOnlyDictionary<string, int> DefaultIntProperties => intProperties
            .ToDictionary(prop => prop.name, prop => prop.value);
    }
}
