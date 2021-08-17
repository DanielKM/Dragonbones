using RTSEngine.Event;
using RTSEngine.ResourceExtension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine.UI
{
    public class ResourceTaskUI : BaseTaskUI<ResourceTaskUIAttributes>
    {
        protected override Sprite Icon => Attributes.resourceHandler.Type.Icon;

        protected override Color IconColor => Color.white;

        protected override bool IsTooltipEnabled => Attributes.tooltipEnabled;

        protected override string TooltipDescription => Attributes.tooltipText;

        [SerializeField, Tooltip("Child UI Text object used to display the resource's current amount (and capacity if applicable)")]
        private Text amountTextUI = null;

        protected IResourceManager resourceMgr { private set; get; } 

        protected override void OnInit()
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
        }

        protected override void OnDisabled()
        {
            if(Attributes.resourceHandler.IsValid())
                Attributes.resourceHandler.FactionResourceAmountUpdated -= HandleResourceAmountUpdated;
        }

        protected override void OnPreReload()
        {
            if(Attributes.resourceHandler.IsValid())
                Attributes.resourceHandler.FactionResourceAmountUpdated -= HandleResourceAmountUpdated;
        }

        protected override void OnReload()
        {
            Attributes.resourceHandler.FactionResourceAmountUpdated += HandleResourceAmountUpdated;

            UpdateAmountText();
        }

        private void HandleResourceAmountUpdated(IFactionResourceHandler resourceHandler, ResourceUpdateEventArgs args)
        {
            UpdateAmountText();
        }

        private void UpdateAmountText()
        {
            amountTextUI.text = Attributes.resourceHandler.Type.HasCapacity
                ? $"{Attributes.resourceHandler.Amount}/{Attributes.resourceHandler.Capacity}"
                : $"{Attributes.resourceHandler.Amount}";


            if (Attributes.resourceHandler.Type.HasCapacity)
                image.color = Attributes.resourceHandler.FreeAmount <= 0
                    ? Attributes.maxCapacityColor
                    : Color.white;
        }
    }
}
