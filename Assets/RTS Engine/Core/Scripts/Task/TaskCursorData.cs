using UnityEngine;

namespace RTSEngine.Task
{
    [System.Serializable]
    public struct TaskCursorData
    {
        [Tooltip("Leave unassigned to use the default mouse cursor.")]
        public Sprite icon;
        [Tooltip("If the mouse cursor sprite has a different hotspot, assign it here.")]
        public Vector2 hotspot;
    }
}
