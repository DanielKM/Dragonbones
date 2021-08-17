using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Movement
{
    [System.Serializable]
    public struct MovementFormationSelector
    {

#if UNITY_EDITOR
        [HideInInspector]
        public MovementFormationType lastType;
#endif

        [Tooltip("What movement formation type to use?")]
        public MovementFormationType type;

        [Tooltip("Assign values for the formation properties")]
        public MovementFormationData properties;

        public float GetFloatPropertyValue(string propName, string fallbackPropName = default)
            => GetFormationPropertyValue(properties.FloatProperties, type.DefaultFloatProperties, propName, fallbackPropName);

        public int GetIntPropertyValue(string propName, string fallbackPropName = default)
            => GetFormationPropertyValue(properties.IntProperties, type.DefaultIntProperties, propName, fallbackPropName);

        private T GetFormationPropertyValue<T> (IReadOnlyDictionary<string, T> propDic, IReadOnlyDictionary<string, T> defaultPropDic, string propName, string fallbackPropName = default)
        {
            // Requested prop is found in the values propDic
            if (propDic.ContainsKey(propName))
                return propDic[propName];
            // Requested prop is not found but the fallback one is available
            else if (fallbackPropName != default && propDic.ContainsKey(fallbackPropName))
                return propDic[fallbackPropName];
            // The requested prop and fall back one are not found so we get the default value of the requested prop
            else if (RTSHelper.LoggingService.RequireTrue(defaultPropDic.ContainsKey(propName),
              $"[{GetType().Name}] Unable to find formation property '{propName}' in the default properties."))
                return defaultPropDic[propName];

            return default;
        }
    }
}
