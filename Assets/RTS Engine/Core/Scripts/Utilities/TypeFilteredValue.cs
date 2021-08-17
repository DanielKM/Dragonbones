using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Utilities
{
    [System.Serializable]
    public abstract class TypeFilteredValue <T,V,E>
    {
        [SerializeField]
        public V allTypes;

        public abstract V GetFiltered(T t, E e);
    }

    [System.Serializable]
    public abstract class TypeFilteredValue <T,V>
    {
        [SerializeField]
        protected V allTypes;

        public abstract V GetFiltered(T t);
    }
}
