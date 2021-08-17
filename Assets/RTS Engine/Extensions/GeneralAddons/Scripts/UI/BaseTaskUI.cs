using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Selection;
using System;

namespace RTSEngine.UI
{
    public abstract class BaseTaskUI<T> : MonoBehaviour, ITaskUI<T> where T : ITaskUIAttributes
    {
        #region Attributes
        public bool IsEnabled { private set; get; } = false;

        public T Attributes { private set; get; }

        protected abstract Sprite Icon { get; }
        protected abstract Color IconColor { get; }

        protected abstract bool IsTooltipEnabled { get; }
        protected abstract string TooltipDescription { get; }

        // Is this TaskUI the soruce of the currently displayed tooltip?
        private bool isCurrentTooltipSource;

        [SerializeField, Tooltip("UI Image component to display the task's icon.")]
        protected Image image = null;

        protected Button button = null;

        // Game services
        protected IGameManager gameMgr { private set; get; } 
        protected IGameService handlerService { private set; get; }
        protected IGameLoggingService logger { private set; get; }


        protected ISelectionManager selectionMgr { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; } 
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IGameService handlerService)
        {
            this.gameMgr = gameMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>();

            this.handlerService = handlerService;

            button = GetComponent<Button>();

            if (!logger.RequireValid(image,
                $"[{GetType().Name}] The 'Image' field must be assigned!")
                || !logger.RequireValid(button,
                $"[{GetType().Name}] This component must be attached to a game object that has a '{typeof(Button).Name}' component attached to it!"))
                return;

            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.mouseSelector = gameMgr.GetService<IMouseSelector>(); 
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            isCurrentTooltipSource = false;

            OnInit();

            Disable();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            Disable();   
        }
        #endregion

        #region Disabling Task UI
        public void Disable()
        {
            this.enabled = false;

            image.enabled = false;
            button.interactable = false;

            IsEnabled = false;

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Reloading Attributes
        public void Reload(T attributes)
        {
            OnPreReload();
            this.Attributes = attributes;

            image.sprite = Icon;
            image.color = IconColor;

            this.enabled = true;
            image.enabled = true;
            button.interactable = true;

            // If the reload happens while this TaskUI is displaying its tooltip then update the tooltip
            if (isCurrentTooltipSource)
            {
                globalEvent.RaiseShowTooltipGlobal(
                    this,
                    new MessageEventArgs(MessageType.info, message: TooltipDescription));
            }

            IsEnabled = true;

            OnReload();
        }

        protected virtual void OnPreReload() { }

        protected virtual void OnReload() { }
        #endregion

        #region Handling Task UI Tooltip
        public void DisplayTaskTooltip()
        {
            if (enabled
                && IsTooltipEnabled)
            {
                globalEvent.RaiseShowTooltipGlobal(
                    this,
                    new MessageEventArgs(MessageType.info, message: TooltipDescription));

                isCurrentTooltipSource = true;
            }
        }

        public void HideTaskTooltip ()
        {
            globalEvent.RaiseHideTooltipGlobal(this);

            isCurrentTooltipSource = false;
        }
        #endregion

        #region Interacting with Task UI
        public void Click()
        {
            //gameMgr.GetService<IGameUIManager>().PrioritizeServiceUI(handlerService);

            OnClick();
        }
        protected virtual void OnClick() { }
        #endregion
    }
}
