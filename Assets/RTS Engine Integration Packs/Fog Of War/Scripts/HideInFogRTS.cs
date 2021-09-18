using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FoW;
using RTSEngine.Game;
using RTSEngine.Selection;
using RTSEngine.Entities;

namespace RTSEngine
{
    public class HideInFogRTS : MonoBehaviour
    {
        [Range(0.0f, 1.0f), SerializeField]
        private float minFogStrength = 0.2f; //if the fog strength is smaller than this, the object will be visible
        private byte minFogStrengthByte;

        //components that will be enabled/disabled depending on the visibility of the object
        private Renderer _renderer;
        private Graphic graphic;
        private Canvas canvas;
        private Collider _collider;
        SelectionEntity selection;
        Building building;

        //holds the state of visibility of the object
        private bool lastVisible;
        [HideInInspector]
        private bool visible;
        [SerializeField]
        private bool visiblePostDiscovery = false;

        //allows us to set an initial visibility state for the object
        private bool initiated = false;

        //The list of objects that will have a visibility that depends on this object
        //useful when you have one component that checks for visibility in a building/unit and all other objects (models for example) have the same visibility without having to calculate anything
        [SerializeField]
        private GameObject[] sameVisibilityObjects = new GameObject[0];

        GameManager gameMgr;

        //initialize this component
        public void Init(GameManager gameMgr)
        {
            if (initiated) //already initiated.
                return;

            this.gameMgr = gameMgr;
            initiated = false; //for the initial state

            minFogStrengthByte = (byte)(minFogStrength * 255); //set the min fog strength value in byte

            selection = GetComponent<SelectionEntity>();
            if (selection.FactionEntity != null && selection.FactionEntity.Type == EntityTypes.building) //if this component has been assigned to a selection component of a building
                building = (Building)selection.FactionEntity; //get it

            //get the required components:
            _renderer = gameObject.GetComponent<Renderer>();
            graphic = gameObject.GetComponent<Graphic>();
            canvas = gameObject.GetComponent<Canvas>();
            _collider = gameObject.GetComponent<Collider>();
        }

        void Update()
        {
            //if this object checks its own visibility, determine the new visibility
            visible = !FogOfWarRTSManager.IsInFog(new Vector3(transform.position.x, 0.0f, transform.position.z), minFogStrengthByte);

            if (building != null && building.Placed == false) //If this is attached to a selection object of a building, as long as the building is not placed, show everything!
                visible = true;

            //if the state stays the same then don't change anything
            if (lastVisible == visible && initiated == true)
                return;

            //updating the visibility state:
            initiated = true;

            lastVisible = visible; //new visibility state

            if (selection) //if there's a valid selection component
            {
                if (visible == false) //no longer visible
                    gameMgr.SelectionMgr.Selected.Remove(selection.Source); //deselect

                if(selection.Source.AudioSourceComp) //if it has an audio source component
                    selection.Source.AudioSourceComp.volume = (visible == true) ? 1.0f : 0.0f; //update the audio source volume depending on the visibility status

                //Show or hide the minimap icon depending on the visibility:
                selection.ToggleMinimapIcon(visible);

                selection.ToggleSelection(visible, false); //if a unit/building is hidden in fog, then the player can't select it
            }
            
            //make sure to change the state of the affected objects as well
            foreach (GameObject obj in sameVisibilityObjects)
                obj.SetActive(visible);

            //enable/disable the components below depending on the visibility state
            if (_renderer != null)
                _renderer.enabled = visible;
            if (graphic != null)
                graphic.enabled = visible;
            if (canvas != null)
                canvas.enabled = visible;
            if (_collider != null)
                _collider.enabled = visible;

            if (visible && visiblePostDiscovery) //if this just became visible and it's supposed to stay visible after first discovery.
                enabled = false; //disable this component and let this be visible for now
        }
    }
}
