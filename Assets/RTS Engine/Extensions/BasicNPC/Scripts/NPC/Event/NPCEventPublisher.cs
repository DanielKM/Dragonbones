using RTSEngine.Event;
using RTSEngine.NPC.Attack;
using System;

namespace RTSEngine.NPC.Event
{
    public class NPCEventPublisher : NPCComponentBase, INPCEventPublisher
    {
        public event CustomEventHandler<INPCAttackManager, NPCAttackEngageEventArgs> AttackEngageOrder;
        public event CustomEventHandler<INPCAttackManager, EventArgs> AttackCancelled;

        public void RaiseAttackEngageOrder(INPCAttackManager sender, NPCAttackEngageEventArgs args)
        {
            var handler = AttackEngageOrder;
            handler?.Invoke(sender, args);
        }

        public void RaiseAttackCancelled(INPCAttackManager sender)
        {
            var handler = AttackCancelled;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<INPCDefenseManager, NPCTerritoryDefenseEngageEventArgs> TerritoryDefenseOrder;
        public event CustomEventHandler<INPCDefenseManager, EventArgs> TerritoryDefenseCancelled;

        public void RaiseTerritoryDefenseOrder(INPCDefenseManager sender, NPCTerritoryDefenseEngageEventArgs args)
        {
            var handler = TerritoryDefenseOrder;
            handler?.Invoke(sender, args);
        }

        public void RaiseTerritoryDefenseCancelled(INPCDefenseManager sender)
        {
            var handler = TerritoryDefenseCancelled;
            handler?.Invoke(sender, EventArgs.Empty);
        }

    }
}
