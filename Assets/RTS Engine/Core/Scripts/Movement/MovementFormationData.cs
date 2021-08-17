using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Movement
{
    [System.Serializable]
    public class MovementFormationData
    {
        [SerializeField, Tooltip("Create properties of type 'float' for this formation. Make sure each property has a unique name!")]
        private MovementFormationPropertyFloat[] floatProperties = new MovementFormationPropertyFloat[0];
        public IReadOnlyDictionary<string, float> FloatProperties => floatProperties
                    .ToDictionary(prop => prop.name, prop => prop.value);

        [SerializeField, Tooltip("Create properties of type 'int' for this formation. Make sure each property has a unique name!")]
        private MovementFormationPropertyInt[] intProperties = new MovementFormationPropertyInt[0];
        public IReadOnlyDictionary<string, int> IntProperties => intProperties
                    .ToDictionary(prop => prop.name, prop => prop.value);
    }
}
