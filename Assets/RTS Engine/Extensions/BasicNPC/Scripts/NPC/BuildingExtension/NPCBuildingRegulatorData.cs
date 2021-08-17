using RTSEngine.BuildingExtension;
using System.Collections.Generic;

using UnityEngine;

namespace RTSEngine.NPC.BuildingExtension
{
    [CreateAssetMenu(fileName = "NewBuildingRegulatorData", menuName = "RTS Engine/NPC/Basic NPC/NPC Building Regulator Data", order = 53)]
    public class NPCBuildingRegulatorData : NPCRegulatorData
    {
        [Header("Building")]
        [SerializeField, Tooltip("Should the building type be regulated per building center or for the whole faction overall?")]
        private bool regulatePerBuildingCenter = true;
        public bool RegulatePerBuildingCenter => regulatePerBuildingCenter;

        [SerializeField, Tooltip("Make the NPC faction place the building only around specific entities. Define the place around entities in order of priority. Leave empty to place around a valid building center (that has a Border component).")]
        private List<BuildingPlaceAroundData> placeAround = new List<BuildingPlaceAroundData>();
        public IEnumerable<BuildingPlaceAroundData> AllPlaceAroundData => placeAround;

        [SerializeField, Tooltip("Enable this option to force the building to be placed around the above defined place around data or else the building placement would be discarded. Disable to allow the building to be placed around a building center if all defined place around conditions fail.")]
        private bool forcePlaceAround = false;
        public bool ForcePlaceAround => forcePlaceAround;

        [SerializeField, Tooltip("Enable this option to allow the building to be rotated to look at its place around entity according to above placement data.")]
        private bool canRotate = false;
        public bool CanRotate => canRotate;
    }
}
