using RTSEngine.Entities;
using System;
using UnityEngine;

namespace RTSEngine.Faction
{
    [System.Serializable]
    public struct FreeFactionBehaviour
    {
        [Tooltip("Allow interaction with other free faction entities?")]
        public bool allowFreeFaction;
        [Tooltip("Allow interaction with the local player faction?")]
        public bool allowLocalPlayer;
        [Tooltip("Allow interaction with faction entities that are neither the local player nor free faction entities?")]
        public bool allowRest;

        public bool IsEntityAllowed(IUnit unit)
        {
            if (unit.IsFree)
                return allowFreeFaction;
            else if (unit.IsLocalPlayerFaction())
                return allowLocalPlayer;

            return allowRest;
        }
    }
}
