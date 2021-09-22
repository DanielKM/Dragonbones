using System;

using UnityEngine;
using UnityEngine.Serialization;

namespace RTSEngine
{
    [System.Serializable]
    public abstract class Range<T> where T : IComparable
    {
        //max and min values:
        [SerializeField, FormerlySerializedAs("min")]
        private T _min;
        public T min => _min;

        [SerializeField, FormerlySerializedAs("max")]
        private T _max;
        public T max => _max;

        protected Range()
        {
            this._min = default;
            this._max = default;
        }

        protected Range(T min, T max)
        {
            this._min = min;
            this._max = max;
        }

        public abstract T RandomValue { get; }

        public T Clamp(T value)
        {
            if (value.CompareTo(_min) < 0)
                return _min;
            else if (value.CompareTo(_max) > 0)
                return _max;

            return value;
        }
    }

    [System.Serializable]
    public class FloatRange : Range<float>
    {
        public FloatRange () : base (1.0f,3.0f) { }
        public FloatRange(float min, float max) : base(min, max) { }

        public override float RandomValue => UnityEngine.Random.Range(min, max);
    }

    [System.Serializable]
    public class IntRange : Range<int>
    {
        public IntRange () : base (1,3) { }
        public IntRange(int min, int max) : base(min, max) { }

        public override int RandomValue => UnityEngine.Random.Range(min, max);
    }
}