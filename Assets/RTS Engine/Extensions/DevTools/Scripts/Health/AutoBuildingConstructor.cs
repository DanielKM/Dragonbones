using System;
using System.Collections.Generic;
using RTSEngine.Entities;

namespace RTSEngine.DevTools.Health
{
    public class AutoBuildingConstructor : DevToolComponentBase
    {
        private List<IBuilding> constructionBuildings = null;

        protected override void OnPostRunInit()
        {
            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;
            globalEvent.BuildingBuiltGlobal += HandleBuildingBuiltGlobal;

            UpdateLabel();

            constructionBuildings = new List<IBuilding>();
        }

        private void OnDestroy()
        {
            globalEvent.BuildingPlacedGlobal -= HandleBuildingPlacedGlobal;
        }

        private void HandleBuildingPlacedGlobal(IBuilding building, EventArgs args)
        {
            constructionBuildings.Add(building);

            if (!IsActive 
                || !RoleFilter.IsAllowed(building.Slot))
                return;

            building.Health.Add(building.Health.MaxHealth, null);
        }

        private void HandleBuildingBuiltGlobal(IBuilding building, EventArgs args)
        {
            constructionBuildings.Remove(building);
        }

        public override void OnUIInteraction() 
        {
            IsActive = !IsActive;

            if(IsActive)
            {
                foreach (IBuilding building in constructionBuildings.ToArray())
                    if (RoleFilter.IsAllowed(building.Slot))
                        building.Health.Add(building.Health.MaxHealth, null);
            }

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (Label)
            {
                string colorCode = IsActive ? "green" : "red";
                Label.text = $"<color={colorCode}>Auto-Construct Buildings</color>";
            }
        }
    }
}
