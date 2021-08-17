using UnityEngine;

namespace RTSEngine.Controls
{
    [CreateAssetMenu(fileName = "newControlType", menuName = "RTS Engine/Control Type", order = 200)]
    public class ControlType : RTSEngineScriptableObject
    {
        [SerializeField, Tooltip("Unique identifier for the control, this is used to identify the key to allow multiple components to use the same control key.")]
        private string key = "unique_key_identifier";
        public override string Key => key;

        [SerializeField, Tooltip("Default key code used to trigger the control.")]
        private KeyCode defaultKeyCode = KeyCode.None;
        public KeyCode DefaultKeyCode => defaultKeyCode;
    }
}
