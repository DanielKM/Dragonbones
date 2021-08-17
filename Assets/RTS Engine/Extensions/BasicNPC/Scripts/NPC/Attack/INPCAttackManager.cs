using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Faction;

namespace RTSEngine.NPC.Attack
{
    public interface INPCAttackManager : INPCComponent
    {
        bool IsAttacking { get; }

        void SetTargetFaction();
        bool SetTargetFaction(IFactionSlot newTargetFactionSlot, bool launchAttack);

        void ResetCurrentTarget();
        bool SetTargetEntity(FactionEntity nextTarget, bool resetCurrentTarget);
        bool SetTargetEntity(IEnumerable<IFactionEntity> factionEntities, bool resetCurrentTarget);
        bool IsValidTargetFactionEntity(IFactionEntity potentialTarget);

        bool LaunchAttack();
        void CancelAttack();
    }
}