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

namespace RTSEngine.SpellCastExtension
{
    public class SpellPlacer : MonoBehaviour, ISpellCastPlacer, IEntityPreInitializable
    {
        #region Attributes
        public ISpell Spell { private set; get; }
        public Vector3 spellCasterLocation;
        public Vector3 spellCastLocation;
        
        public bool CanPlace { private set; get; }

        [SerializeField, Tooltip("If populated then this defines the types of terrain areas where the rallypoint can be placed at. When empty, all terrain area types would be valid.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];
        public IEnumerable<TerrainAreaType> PlacableTerrainAreas => placableTerrainAreas.ToList();

        [SerializeField, Tooltip("Can the spell be placed outside the range (defined by the range)?")]
        private bool canPlaceOutsideRange = false;
        public bool CanPlaceOutsideRange => canPlaceOutsideRange;

        public bool Placed { get; private set; } = false;

        // The value of this field will updated during the placement of the spell until the spell is placed and the center is set in the spell component.
        public ISpellRange PlacementCenter { private set; get; }

        // How many colliders is the spell overlapping with at any given time? It is the size of this list.
        private List<Collider> overlappedColliders;

        private Collider boundaryCollider = null;

        // Additional placement conditions that can be hooked up into the spell
        private IEnumerable<ISpellPlacerCondition> Conditions;

        public bool IsPlacementStarted { private set; get; }

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ISpellCastManager spellMgr { private set; get; }
        protected ISpellCastPlacement placementMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        #endregion

        #region Events
        public event CustomEventHandler<ISpell, EventArgs> SpellPlacementStatusUpdated;
        #endregion

        #region Raising Events
        private void RaiseSpellPlacementStatusUpdated ()
        {
            CustomEventHandler<ISpell, EventArgs> handler = SpellPlacementStatusUpdated;
            handler?.Invoke(Spell, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.spellMgr = gameMgr.GetService<ISpellCastManager>();
            this.placementMgr = gameMgr.GetService<ISpellCastPlacement>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>(); 

            this.Spell = entity as ISpell;

            if(!logger.RequireTrue(placableTerrainAreas.Length == 0 || placableTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Spell.Code}] The 'Placable Terrain Areas' field must be either empty or populated with valid elements!"))
                return;

            // Boundary collider is only used to detect collisions and therefore having it as trigger is just enough.
            boundaryCollider = gameObject.GetComponent<Collider>();
            if (!logger.RequireValid(boundaryCollider,
                $"[{GetType().Name} - {Spell.Code}] Spell object must have a Collider component attached to it to detect obstacles while placing the spell!"))
                return;
            boundaryCollider.isTrigger = true;

            // This allows the boundary collider to be ignored for mouse clicks and mouse hovers.
            boundaryCollider.gameObject.layer = 2;

            Conditions = gameObject.GetComponents<ISpellPlacerCondition>();

            // If the spell is not a placement instance then it is placed by default.
            Placed = !Spell.IsPlacementInstance;

            OnInit();
        }

        protected virtual void OnInit() { }

        public void Disable() { }
        #endregion

        #region Handling Placement Status
        public void OnPlacementStart(SpellCastPlacement spellCastPlacement)
        {
            CanPlace = false;

            overlappedColliders = new List<Collider>();

            IsPlacementStarted = true;

            spellCasterLocation = spellCastPlacement.spellCaster.transform.position;

            spellCastLocation = spellCastPlacement.currentSpell.instance.transform.position;
        }

        public void OnPositionUpdate(Vector3 newSpellCasterLocation, Vector3 newSpellCastLocation)
        {
            
            if (Placed
                || !RTSHelper.HasAuthority(Spell))
                return;

            //if the spell is not in range of a spell center, not on the map or not around the entity that is has to be around within a certain range
            //--> not placable
            TogglePlacementStatus(
                IsSpellInRange(newSpellCasterLocation, newSpellCastLocation)
                // && IsSpellOnMap()
                // && overlappedColliders.Count(collider => collider != null) <= 0
                // && (!Conditions.Any() || Conditions.All(condition => condition.CanPlaceSpell())                )
                );
        }

        private void TogglePlacementStatus (bool enable)
        {
            CanPlace = enable;

            if(Spell.IsLocalPlayerFaction())
                Spell.SelectionMarker?.Enable((enable) ? Color.green : Color.red);

            RaiseSpellPlacementStatusUpdated();
            // globalEvent.RaiseSpellPlacementStatusUpdatedGlobal(Spell);

            OnPlacementStatusUpdated();
        }

        protected virtual void OnPlacementStatusUpdated() { }
        #endregion

        #region Handling Placement Conditions
        private void OnTriggerEnter(Collider other)
        {
            // Ignore colliders that belong to this spell (its selection colliders namely) and ones attached to effect objects
            if (!IsPlacementStarted
                || Placed 
                || Spell.Selection.IsSelectionCollider(other)
                || other.gameObject.GetComponent<IEffectObject>().IsValid())
                return;

            overlappedColliders.Add(other);

            OnPositionUpdate(spellCasterLocation, spellCastLocation);
        }

        private void OnTriggerExit(Collider other)
        {
            // Ignore colliders that belong to this spell (its selection colliders namely) and ones attached to effect objects
            if (!IsPlacementStarted
                || Placed 
                || Spell.Selection.IsSelectionCollider(other)
                || other.gameObject.GetComponent<IEffectObject>().IsValid())
                return;

            overlappedColliders.Remove(other);

            OnPositionUpdate(spellCasterLocation, spellCastLocation);
        }

        public bool IsSpellInRange(Vector3 position, Vector3 secondPosition)
        {
            bool inRange = false; //true if the building is inside its faction's territory

            float dist = Vector3.Distance(position, secondPosition);
            //check if the building is still inside this building center's territory

            if (dist < 40) //still inside the center's territory
                inRange = true; //building is in range
            else
            {
                inRange = false; //building is not in range
                PlacementCenter = null; //set the current center to null, so we can find another one
            }

            if (canPlaceOutsideRange)
                inRange = true;
            
        
            // Debug.Log(inRange);
            return inRange; //return whether the building is in range a building center or not
        }

        public bool IsSpellOnMap()
        {
            Ray ray = new Ray(); //create a new ray
            RaycastHit[] hits; //this will hold the registerd hits by the above ray

            BoxCollider boxCollider = boundaryCollider.GetComponent<BoxCollider>();

            //Start by checking if the middle point of the spell's collider is over the map.
            //Set the ray check source point which is the center of the collider in the game world:
            ray.origin = new Vector3(transform.position.x + boxCollider.center.x, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z);

            ray.direction = Vector3.down; //The direction of the ray is always down because we want check if there's terrain right under the spell's object:

            int i = 4; //we will check the four corners and the center
            while (i > 0) //as long as the spell is still on the map/terrain
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

            return true; //at this stage, we're sure that the center and all corners of the spell are on the map, so return true
        }
        #endregion

    }
}

