using UnityEngine;

namespace RTSEngine.Movement
{
    [System.Serializable]
    public struct MovementCurve
    {
        [Tooltip("Only when the distance between the source and target is higher than this value, the movement curve will be used.")]
        public float minDistance;
        [Tooltip("Only when the height difference between the source and target is lower than this value, the movement curve will be used.")]
        public float maxHeightDifference;
        [Tooltip("Define the movement path using this curve.")]
        public AnimationCurve curve;
        [Tooltip("Determines the movement height by evaluating the curve and multiplying the height accordingly.")]
        public float heightMultiplier;
    }
}
