using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Determinism
{
    [System.Serializable]
    public struct TimeModifiedFloat
    {
        [SerializeField]
        private float value;
        public float Value => TimeModifier.ApplyModifier(value);

        public TimeModifiedFloat(float value)
        {
            this.value = value;
        }
    }
}
