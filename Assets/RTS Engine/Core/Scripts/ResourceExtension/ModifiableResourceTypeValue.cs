using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.ResourceExtension
{
    public class ModifiableResourceTypeValue
    {
        public int Amount { private set; get; }
        public int Capacity { private set; get; }

        public void UpdateValue (ResourceTypeValue value)
        {
            Amount += value.amount;
            Capacity += value.capacity;
        }

        public bool Has(ResourceTypeValue value)
        {
            return Amount >= value.amount && Capacity >= value.capacity;
        }

        public void Reset()
        {
            Amount = 0;
            Capacity = 0;
        }
    }
}
