using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Search;
using RTSEngine.NPC;
using RTSEngine.Effect;

namespace RTSEngine.BuildingExtension
{
    public class BuildingPlacer : MonoBehaviour, IBuildingPlacer, IEntityPreInitializable
    {
        #region Attributes
        public IBuilding Building { private set; get; }

        public bool CanPlace { private set; get; }

        [SerializeField, Tooltip("If populated then this defines the types of terrain areas where the rallypoint can be placed at. When empty, all terrain area types would be valid.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];
        public IEnumerable<TerrainAreaType> PlacableTerrainAreas => placableTerrainAreas.ToList();

        [SerializeField, Tooltip("Can the building be placed outside the faction's territory (defined by the Border)?")]
        private bool canPlaceOutsideBorder = false;
        public bool CanPlaceOutsideBorder => canPlaceOutsideBorder;

        public bool Placed { get; private set; } = false;

        // The value of this field will updated during the placement of the building until the building is placed and the center is set in the Building component.
        public IBorder PlacementCenter { private set; get; }

        // How many colliders is the building overlapping with at any given time? It is the size of this list.
        private List<Collider> overlappedColliders;

        private Collider boundaryCollider = null;

        // Additional placement conditions that can be hooked up into the building
        private IEnumerable<IBuildingPlacerCondition> Conditions;

        public bool IsPlacementStarted { private set; get; }

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        #endregion

        #region Events
        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStatusUpdated;
        #endregion

        #region Raising Events
        private void RaiseBuildingPlacementStatusUpdated ()
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacementStatusUpdated;
            handler?.Invoke(Building, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>(); 

            this.Building = entity as IBuilding;

            if(!logger.RequireTrue(placableTerrainAreas.Length == 0 || placableTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Building.Code}] The 'Placable Terrain Areas' field must be either empty or populated with valid elements!"))
                return;

            // Boundary collider is only used to detect collisions and therefore having it as trigger is just enough.
            boundaryCollider = gameObject.GetComponent<Collider>();
            if (!logger.RequireValid(boundaryCollider,
                $"[{GetType().Name} - {Building.Code}] Building object must have a Collider component attached to it to detect obstacles while placing the building!"))
                return;
            boundaryCollider.isTrigger = true;

            // This allows the boundary collider to be ignored for mouse clicks and mouse hovers.
            boundaryCollider.gameObject.layer = 2;

            Conditions = gameObject.GetComponents<IBuildingPlacerCondition>();

            // If the building is not a placement instance then it is placed by default.
            Placed = !Building.IsPlacementInstance;

            OnInit();
        }

        protected virtual void OnInit() { }

        public void Disable() { }
        #endregion

        #region Handling Placement Status
        public void OnPlacementStart()
        {
            CanPlace = false;

            overlappedColliders = new List<Collider>();

            IsPlacementStarted = true;
        }

        public void OnPositionUpdate()
        {
            if (Placed
                || !RTSHelper.HasAuthority(Building))
                return;

            //if the building is not in range of a building center, not on the map or not around the entity that is has to be around within a certain range
            //--> not placable
            TogglePlacementStatus(
                IsBuildingInBorder()
                && IsBuildingOnMap()
                && overlappedColliders.Count(collider => collider != null) <= 0
                && (!Conditions.Any() || Conditions.All(condition => condition.CanPlaceBuilding())) );
        }

        private void TogglePlacementStatus (bool enable)
        {
            CanPlace = enable;

            if(Building.IsLocalPlayerFaction())
                Building.SelectionMarker?.Enable((enable) ? Color.green : Color.red);

            RaiseBuildingPlacementStatusUpdated();
            globalEvent.RaiseBuildingPlacementStatusUpdatedGlobal(Building);

            OnPlacementStatusUpdated();
        }

        protected virtual void OnPlacementStatusUpdated() { }
        #endregion

        #region Handling Placement Conditions
        private void OnTriggerEnter(Collider other)
        {
            // Ignore colliders that belong to this building (its selection colliders namely) and ones attached to effect objects
            if (!IsPlacementStarted
                || Placed 
                || Building.Selection.IsSelectionCollider(other)
                || other.gameObject.GetComponent<IEffectObject>().IsValid())
                return;

            overlappedColliders.Add(other);

            OnPositionUpdate();
        }

        private void OnTriggerExit(Collider other)
        {
            // Ignore colliders that belong to this building (its selection colliders namely) and ones attached to effect objects
            if (!IsPlacementStarted
                || Placed 
                || Building.Selection.IsSelectionCollider(other)
                || other.gameObject.GetComponent<IEffectObject>().IsValid())
                return;

            overlappedColliders.Remove(other);

            OnPositionUpdate();
        }

        public bool IsBuildingInBorder()
        {
            bool inRange = false; //true if the building is inside its faction's territory
            if (PlacementCenter.IsValid()) //if the building is already linked to a building center
            {
                //check if the building is still inside this building center's territory
                if (PlacementCenter.IsInBorder(transform.position)) //still inside the center's territory
                    inRange = true; //building is in range
                else
                {
                    inRange = false; //building is not in range
                    PlacementCenter = null; //set the current center to null, so we can find another one
                }
            }

            if (!PlacementCenter.IsValid()) //if at this point, the building doesn't have a building center.
            {
                foreach (IBuilding center in Building.FactionMgr.BuildingCenters)
                {
                    if (!center.BorderComponent.IsActive) //if the border of this center is not active yet
                        continue;

                    if (center.BorderComponent.IsInBorder(Building.transform.position) && center.BorderComponent.AllowBuildingInBorder(Building)) //if the building is inside this center's territory and it's allowed to have this building around this center
                    {
                        inRange = true; //building center found
                        PlacementCenter = center.BorderComponent;
                        break; //leave the loop
                    }
                }
            }

            if (canPlaceOutsideBorder)
                inRange = true;
            
            if ((PlacementCenter.IsValid() || canPlaceOutsideBorder) && inRange) //if, at this point, the building has a center assigned
            {
                //Sometimes borders collide with each other but the priority of the borders is determined by the order of the creation of the borders
                //That's why we need to check for other factions' borders and make sure the building isn't inside one of them:

                foreach(IBorder border in buildingMgr.AllBorders) //loop through all borders
                {
                    if (!border.IsActive || border.Building.IsFriendlyFaction(Building)) //if the border is not active or it belongs to the building's faction
                        continue; //off to the next one

                    if (border.IsInBorder(Building.transform.position) 
                        && (!PlacementCenter.IsValid() || border.SortingOrder > PlacementCenter.SortingOrder)) //if the building is inside this center's territory
                    {
                        //and if the border has a priority over the one that the building belongs to:
                        return false;
                    }

                }
            }

            return inRange; //return whether the building is in range a building center or not
        }

        public bool IsBuildingOnMap()
        {
            Ray ray = new Ray(); //create a new ray
            RaycastHit[] hits; //this will hold the registerd hits by the above ray

            BoxCollider boxCollider = boundaryCollider.GetComponent<BoxCollider>();

            //Start by checking if the middle point of the building's collider is over the map.
            //Set the ray check source point which is the center of the collider in the game world:
            ray.origin = new Vector3(transform.position.x + boxCollider.center.x, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z);

            ray.direction = Vector3.down; //The direction of the ray is always down because we want check if there's terrain right under the building's object:

            int i = 4; //we will check the four corners and the center
            while (i > 0) //as long as the building is still on the map/terrain
            {
                hits = Physics.RaycastAll(ray, placementMgr.TerrainMaxDistance); //apply the raycast and store the hits

                bool hitTerrain = false; //did one the hits hit the terrain?
                foreach(RaycastHit rh in hits) //go through all hits
                    if (terrainMgr.IsTerrainArea(rh.transform.gameObject, placableTerrainAreas)) 
                        hitTerrain = true;

                if (hitTerrain == false) //if there was no registerd terrain hit
                    return false; //stop and return false

                i--;

                //If we reached this stage, then applying the last raycast, we successfully detected that there was a terrain under it, so we'll move to the next corner:
                switch (i)
                {
                    case 0:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                    case 1:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 2:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 3:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                }
            }

            return true; //at this stage, we're sure that the center and all corners of the building are on the map, so return true
        }
        #endregion

    }
}

