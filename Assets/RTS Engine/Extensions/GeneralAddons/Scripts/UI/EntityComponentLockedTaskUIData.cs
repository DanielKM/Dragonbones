using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    [System.Serializable]
    public struct EntityComponentLockedTaskUIData
    {
        [SerializeField, Tooltip("If assigned, this icon is displayed instead of the original icon, when the task is locked (because of missing launch requirements for example).")]
        public Sprite icon;

        [SerializeField, Tooltip("This defines the color of the icon of the task when the task is locked (due to missing launch requirements for example).")]
        public Color color;
    }
}
