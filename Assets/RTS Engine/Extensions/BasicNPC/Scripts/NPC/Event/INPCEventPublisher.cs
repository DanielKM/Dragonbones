using RTSEngine.Event;
using RTSEngine.NPC.Attack;
using System;

namespace RTSEngine.NPC.Event
{
    public interface INPCEventPublisher : INPCComponent
    {
        event CustomEventHandler<INPCAttackManager, NPCAttackEngageEventArgs> AttackEngageOrder;
        event CustomEventHandler<INPCAttackManager, EventArgs> AttackCancelled;
        event CustomEventHandler<INPCDefenseManager, NPCTerritoryDefenseEngageEventArgs> TerritoryDefenseOrder;
        event CustomEventHandler<INPCDefenseManager, EventArgs> TerritoryDefenseCancelled;

        void RaiseAttackEngageOrder(INPCAttackManager sender, NPCAttackEngageEventArgs args);
        void RaiseAttackCancelled(INPCAttackManager sender);
        void RaiseTerritoryDefenseOrder(INPCDefenseManager sender, NPCTerritoryDefenseEngageEventArgs args);
        void RaiseTerritoryDefenseCancelled(INPCDefenseManager sender);
    }
}