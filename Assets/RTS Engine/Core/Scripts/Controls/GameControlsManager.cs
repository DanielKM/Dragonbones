using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Controls
{
    [System.Serializable]
    public struct ControlTypeWrapper
    {
        public ControlType type;
        public KeyCode currentKeyCode;
    }

    public enum KeyBehaviour { get, getDown, getUp };

    public class GameControlsManager : MonoBehaviour, IGameControlsManager 
    {
        private Dictionary<string, ControlTypeWrapper> controlsRegistered = null;

        // Game Services
        protected IGameLoggingService logger { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();

            controlsRegistered = new Dictionary<string, ControlTypeWrapper>();
        }

        public bool Get(ControlType controlType, bool requireValid = false) => Get(controlType, KeyBehaviour.get, requireValid);
        public bool GetDown(ControlType controlType, bool requireValid = false) => Get(controlType, KeyBehaviour.getDown, requireValid);
        public bool GetUp(ControlType controlType, bool requireValid = false) => Get(controlType, KeyBehaviour.getUp, requireValid);
        public bool Get(ControlType controlType, KeyBehaviour behaviour, bool requireValid = false)
        {
            if(!controlType.IsValid())
            {
                if (requireValid)
                    logger.LogWarning($"[{GetType().Name}] The input control type is invalid! Please follow the trace to find the component providing the input control type and assign it!");
                return false;
            }

            if (!controlsRegistered.TryGetValue(controlType.Key, out ControlTypeWrapper controlTypeWrapper))
            {
                controlTypeWrapper = new ControlTypeWrapper
                {
                    type = controlType,
                    currentKeyCode = controlType.DefaultKeyCode
                };

                controlsRegistered.Add(controlType.Key, controlTypeWrapper);
            }

            switch(behaviour)
            {
                case KeyBehaviour.get:
                    return Input.GetKey(controlTypeWrapper.currentKeyCode);
                case KeyBehaviour.getUp:
                    return Input.GetKeyUp(controlTypeWrapper.currentKeyCode);
                case KeyBehaviour.getDown:
                    return Input.GetKeyDown(controlTypeWrapper.currentKeyCode);

                default:
                    return false;
            }
        }
    }
}
