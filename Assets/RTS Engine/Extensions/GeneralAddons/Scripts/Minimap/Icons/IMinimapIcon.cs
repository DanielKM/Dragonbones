using RTSEngine.Effect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Minimap.Icons
{
    public interface IMinimapIcon : IEffectObject, IMonoBehaviour
    {
        void SetColor(Color color);
        void Toggle(bool enable);
    }
}
