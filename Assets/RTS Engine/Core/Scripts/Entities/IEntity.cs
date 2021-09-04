using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Selection;
using RTSEngine.Health;
using RTSEngine.Event;
using RTSEngine.Animation;
using RTSEngine.Upgrades;
using RTSEngine.Task;
using RTSEngine.Faction;
using RTSEngine.UnitExtension;

namespace RTSEngine.Entities
{
    public interface IEntity : IMonoBehaviour
    {
        EntityType Type { get; }

        bool IsInitialized { get; }

        IReadOnlyDictionary<string, IEntityComponent> EntityComponents { get; }

        IPendingTasksHandler PendingTasksHandler { get; }

        IReadOnlyDictionary<string, IEntityTargetComponent> EntityTargetComponents { get; }

        event CustomEventHandler<IEntity, EventArgs> EntityInitiated;

        ErrorMessage SetTargetFirst(TargetData<IEntity> target, bool playerCommand);
        ErrorMessage SetTargetFirstLocal(TargetData<IEntity> target, bool playerCommand);

        IEnumerable<IAttackComponent> AttackComponents { get; }
        IAttackComponent AttackComponent { get; }
        bool CanAttack { get; }

        IReadOnlyDictionary<string, IAddableUnit> AddableUnitComponents { get; }

        IMovementComponent MovementComponent { get; }
        bool CanMove { get; }

        string Code { get; }
        IEnumerable<string> Category { get; }
        string Name { get; }
        string Description { get; }
        Sprite Icon { get; }
        bool IsFree { get; }
        float Radius { get; }

        GameObject Model { get; }

        float Duration { get; }

        bool IsInteractable { get; }
        bool IsSearchable { get; }

        int FactionID { get; }
        IFactionSlot Slot { get; }

        Color SelectionColor { get; }
        IAnimatorController AnimatorController { get; }
        IEntitySelection Selection { get; }
        IEntitySelectionMarker SelectionMarker { get; }

        AudioSource AudioSourceComponent { get; }

        IEntityHealth Health { get; }
        IEntityWorkerManager WorkerMgr { get; }

        bool CanLaunchTask { get; }
        //a variable to check that an entity is interactable, can launch tasks, can set targets and is initiated.
        void SetIdle(IEntityTargetComponent exception = null, bool includeMovement = true);
        bool IsIdle { get; }

        ErrorMessage SetFaction(IEntity source, int targetFactionID);

        ErrorMessage SetFactionLocal(IEntity source, int targetFactionID);

        void UpgradeComponent(UpgradeElement<IEntityComponent> upgradeElement);

        int Key { get; }

        event CustomEventHandler<IEntity, FactionUpdateArgs> FactionUpdateComplete;
    }
}
