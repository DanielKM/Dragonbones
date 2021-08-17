using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI.Utilities;
using RTSEngine.Event;
using UnityEngine.UI;
using RTSEngine.Determinism;
using System;

namespace RTSEngine.UI
{
    [System.Serializable]
    public struct UIOption
    {
        [Tooltip("Value assigned for this option.")]
        public float value;
        [Tooltip("Descriptive label of this option.")]
        public string label;
    }

    public class TimeModifierUIHandler : MonoBehaviour, IPostRunGameService
    {
        [SerializeField, Tooltip("Define the potential time modifier options for this map. If no element is defined, the default time modifier will be added as the only element.")]
        private UIOption[] options = new UIOption[0];
        [SerializeField, Tooltip("Default option index. If out of range of the above array, it would default to 0.")]
        private int currOptionID = -1;

        [SerializeField, Tooltip("UI Button object used to allow the player to go through the available time modifier options.")]
        private Button updateOptionButton = null;
        [SerializeField, Tooltip("UI Text object used to display the current time modifier label out of the above options.")]
        private Text optionLabelText = null;

        //Services
        protected IGameLoggingService logger { private set; get; }
        protected ITimeModifier timeModifier { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.timeModifier = gameMgr.GetService<ITimeModifier>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            if (!logger.RequireTrue(options.Length > 0,
              $"[{GetType().Name}] No Time Modifier UI options have been assigned. Time modifier will remain set to default value of {TimeModifier.CurrentModifier}",
              type: LoggingType.info))
            {
                options = new UIOption[]
                {
                    new UIOption
                    {
                        value = TimeModifier.CurrentModifier,
                        label = "default"
                    }
                };
            }

            timeModifier.ModifierUpdated += HandleTimeModifierUpdated;

            // In case, the current local player is not allowed to update the time
            if (!RTSHelper.IsMasterInstance())
            {
                if(updateOptionButton)
                    updateOptionButton.interactable = false;
            }
            else
            {
                if (!currOptionID.IsValidIndex(options))
                    currOptionID = 0;
                SetTimeModifierOption(currOptionID);
            }
        }

        private void OnDestroy()
        {
            timeModifier.ModifierUpdated -= HandleTimeModifierUpdated;
        }

        public void OnButtonClick()
        {
            SetTimeModifierOption(currOptionID.GetNextIndex(options));
        }

        private void SetTimeModifierOption(int optionID)
        {
            if (!logger.RequireTrue(optionID.IsValidIndex(options)
                && timeModifier.SetModifier(options[optionID].value, playerCommand: false) == ErrorMessage.none,
              $"[{GetType().Name}] Unable to update the time modifier to option of index '{optionID}'"))
                return;

            currOptionID = optionID;

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (optionLabelText)
                optionLabelText.text = $"*{TimeModifier.CurrentModifier}";
        }

        private void HandleTimeModifierUpdated(ITimeModifier sender, EventArgs args)
        {
            RefreshUI();
        }
    }
}
