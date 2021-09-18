using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FoW;
using System;
using RTSEngine.Game;

namespace RTSEngine
{
    public class FogOfWarRTSManager : MonoBehaviour
    {
        [SerializeField]
        //if this is true then all free buildings that have the portal component will be visible to all players.
        private bool freeBuildingsVisible = true;

        [Range(0.0f, 1.0f), SerializeField]
        private float minBuildingPlacementFogStregnth = 0.2f; //minimum fog value for the player to place new buildings
        public static byte MinBuildingPlacementFogStrength;

        //the fog of war managers attached to both the main and minimap cameras must be assigned here:
        [SerializeField]
        private FogOfWarLegacy mainCameraFogOfWar = null;
        [SerializeField]
        private FogOfWarLegacy minimapFogOfWar = null;

        //is the input position in fog or what?
        public static bool IsInFog(Vector3 position, byte minfog)
        {
            return FoWTeam.GetFogValue(position) >= minfog;
        }

        //other components:
        public static FogOfWarTeam FoWTeam; //the player faction fog of war team component

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            FoWTeam = GetComponent<FogOfWarTeam>(); //get the fog of war team component. It must be on the same game object as this component!
            MinBuildingPlacementFogStrength = (byte)(minBuildingPlacementFogStregnth * 255); //set the building placement min fog strength in byte

            //start listening to RTS Engine events:
            CustomEvents.UnitConversionComplete += OnUnitConverted;
            CustomEvents.UnitCreated += InitFactionEntityFoW;

            CustomEvents.BuildingPlaced += OnBuildingPlaced;
            CustomEvents.BuildingBuilt += InitFactionEntityFoW;

            CustomEvents.FactionEntityDead += DisableFactionEntityFoW;

            CustomEvents.ResourceAdded += OnResourceAdded;
        }

        private void OnDisable () {
            //stop listening to RTS Engine events:
            CustomEvents.UnitConversionComplete -= OnUnitConverted;
            CustomEvents.UnitCreated -= InitFactionEntityFoW;

            CustomEvents.BuildingPlaced += OnBuildingPlaced;
            CustomEvents.BuildingBuilt -= InitFactionEntityFoW;

            CustomEvents.FactionEntityDead -= DisableFactionEntityFoW;

            CustomEvents.ResourceAdded -= OnResourceAdded;
        }

        public void UpdateTeam (int teamID)
        {
            FoWTeam.team = teamID; //set the team to the player's faction

            //assign the player faction ID in the fog of war legacy display component:
            mainCameraFogOfWar.team = teamID;
            minimapFogOfWar.team = teamID;
            
            FoWTeam.Reinitialize(); //reload the fog of war in the map after assigning the player faction
        }

        //called whenever a unit is converted
        void OnUnitConverted(Unit source, Unit target)
        {
            if (target == null) //invalid target unit
                return;

            target.FoWUnit.team = target.FactionID; //change its FoW to the new team
            if (target.FactionID == GameManager.PlayerFactionID) //if this is the local unit, then enable that fog of war
                target.FoWUnit.enabled = true;
        }
        
        //called when a resource is initialised
        void OnResourceAdded(Resource resource)
        {
            resource.HideInFog.Init(gameMgr); //initialise the hide in fog component
        }

        private void OnBuildingPlaced(Building building)
        {
            building.HideInFog.Init(gameMgr);
        }

        private void InitFactionEntityFoW (FactionEntity factionEntity)
        {
            factionEntity.FoWUnit.enabled = true; //enable component (it should be disabled per default)

            factionEntity.HideInFog.Init(gameMgr);

            if (factionEntity.IsFree()) //if this one is a free unit then set the team id to -1 (all playable teams have >= 0 IDs)
            {
                factionEntity.FoWUnit.team = -1;

                if (factionEntity.Type == EntityTypes.building && freeBuildingsVisible == true) //if free buildings are shown to every team
                    factionEntity.FoWUnit.team = GameManager.PlayerFactionID; //set as local player faction ID so that it's visible
            }
            else //if it's a faction controlled unit
                factionEntity.FoWUnit.team = factionEntity.FactionID; //assign the faction ID to it.
        }

        private void DisableFactionEntityFoW (FactionEntity factionEntity)
        {
            factionEntity.FoWUnit.enabled = false; //disable the FoW component.
        }
    }
}
