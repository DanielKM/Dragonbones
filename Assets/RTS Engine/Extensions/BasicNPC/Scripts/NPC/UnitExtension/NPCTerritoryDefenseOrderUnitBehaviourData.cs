using UnityEngine;

namespace RTSEngine.NPC.UnitExtension
{
    [System.Serializable]
    public struct NPCTerritoryDefenseOrderUnitBehaviourData
    {
        [Tooltip("Enable to allow the units to be aware of all enemies inside the territory of the building center where the defense order was issued.")]
        public bool defend;
        [Tooltip("Enable to force changing the building center whose territory would be defended by the units. The force only applies to units who do not have an active attack target at the time of the defense order.")]
        public bool forceChangeDefenseCenter;

        [Tooltip("Send back units to their spawn positions when the territory defense mode is cancelled?")]
        public bool sendBackOnDefenseCancel;
    }
}
