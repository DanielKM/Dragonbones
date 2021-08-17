using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Determinism
{
    [System.Serializable]
    public class TimeModifiedTimer
    {
        public float DefaultValue { protected set; get; }
        public float CurrValue { private set; get; }

        public TimeModifiedTimer()
        {
            this.DefaultValue = 0.0f;
            this.CurrValue =  0.0f;
        }

        public TimeModifiedTimer(float defaultValue, bool assignCurrValue = true)
        {
            this.DefaultValue = defaultValue;

            this.CurrValue = assignCurrValue ? this.DefaultValue : 0.0f;
        }

        public TimeModifiedTimer(FloatRange defaultValueRange, bool assignCurrValue = true)
        {
            this.DefaultValue = defaultValueRange.RandomValue;

            this.CurrValue = assignCurrValue ? this.DefaultValue : 0.0f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when the timer is done (value is equal or below zero).</returns>
        public bool ModifiedDecrease()
        {
            CurrValue -= Time.deltaTime * TimeModifier.CurrentModifier;

            return CurrValue < 0.0f;
        }

        public void Reload()
        {
            CurrValue = DefaultValue;
        }

        public void Reload(float newDefaultValue)
        {
            DefaultValue = newDefaultValue;
            Reload();
        }

        public void Reload(FloatRange newDefaultValueRange)
        {
            DefaultValue = newDefaultValueRange.RandomValue;
            Reload();
        }
    }
}
