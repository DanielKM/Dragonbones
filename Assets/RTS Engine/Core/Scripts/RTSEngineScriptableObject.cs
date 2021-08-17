using UnityEngine;

namespace RTSEngine
{
    public abstract class RTSEngineScriptableObject : ScriptableObject
    {
        public abstract string Key { get; }
    }
}
