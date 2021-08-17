using System.Linq;

using UnityEngine;

using RTSEngine.Faction;

namespace RTSEngine.Utilities
{
    [System.Serializable]
    public struct FactionSlotRoleFilter
    {
        [Tooltip("Allowed faction slot roles. Leave empty for all roles!")]
        public FactionSlotRole[] allowedFactionSlotRoles;
        [Tooltip("Only allow the faction slot if it represents the local player?")]
        public bool localFactionOnly;

        public bool IsAllowed (IFactionSlot testSlot)
        {
            // Faction slot role check
            return testSlot.IsValid()
                && ((!allowedFactionSlotRoles.Any() || allowedFactionSlotRoles.Contains(testSlot.Data.role))
                // Local faction check
                && (!localFactionOnly || testSlot.IsLocalPlayerFaction()));
        }
    }
}
