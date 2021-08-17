using System;

using RTSEngine.Entities;

namespace RTSEngine.NPC.Event
{
    public class NPCTerritoryDefenseEngageEventArgs : EventArgs
    {
        public IBuilding LastDefenseCenter { private set; get; }
        public IBuilding NextDefenseCenter { private set; get; }
        public float DefenseRange { private set; get; }

        public NPCTerritoryDefenseEngageEventArgs(IBuilding lastDefenseCenter, IBuilding nextDefenseCenter, float defenseRange)
        {
            this.LastDefenseCenter = lastDefenseCenter;
            this.NextDefenseCenter = nextDefenseCenter;

            this.DefenseRange = defenseRange;
        }
    }
}