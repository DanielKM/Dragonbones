using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.UnitExtension;
using RTSEngine.Faction;
using RTSEngine.Event;

namespace RTSEngine.EntityComponent
{

    /*
     * none: No limitations over the amount of times to launch the task
     * launchTimes: A maximum amount of the unit creation launch times must be specified, the task will be locked when that amount is reached.
     * activeInstances: A maximum amount of active units created through the unit creation task must be specified. Task will be locked when that amount is reached and will be unlocked when one of the created instances is dead.
     * */
    public enum UnitCreationTaskLimit { none, launchTimes, activeInstances }

    [System.Serializable]
    public class UnitCreationTask : FactionEntityCreationTask<IUnit> 
    {
        [Space(), SerializeField, EnforceType(typeof(IUnit)), Tooltip("Unit prefab to be created."), Header("Unit Creation Task Properties"),]
        protected GameObject prefabObject = null;
        public override GameObject PrefabObject => prefabObject;

        [SerializeField, Tooltip("Choose how to limit launching the unit creation task.")]
        private UnitCreationTaskLimit limit = UnitCreationTaskLimit.none;

        [SerializeField, Tooltip("Total maximum amount of launch times or total amount of created instances through this task, depending on the 'Limit' field."), Min(1)]
        private int maxAmount = 10;

        // Holds the currently created instances through this unit creation tasks
        private List<IUnit> createdInstances;
        public IEnumerable<IUnit> CreatedInstances => createdInstances;

        protected IUnitManager unitMgr { private set; get; }

        protected override void OnInit()
        {
            this.unitMgr = gameMgr.GetService<IUnitManager>();

            createdInstances = new List<IUnit>();

            Entity.Slot.FactionMgr.OwnFactionEntityAdded += HandleOwnFactionEntityAdded;
        }

        protected override void OnDisabled()
        {
            Entity.Slot.FactionMgr.OwnFactionEntityAdded -= HandleOwnFactionEntityAdded;
        }

        private void HandleOwnFactionEntityAdded(IFactionManager factionMgr, EntityEventArgs<IFactionEntity> args)
        {
            if (args.Entity.Type != EntityType.unit
                || args.Entity.Code != Prefab.Code)
                return;

            IUnit createdUnit = args.Entity as IUnit;

            // If the faction creates the unit creatable through this component and it is the same entity component that created it
            if(createdUnit.CreatorEntityComponent == EntityComponent)
            {
                createdInstances.Add(createdUnit);

                // Track the unit's death to keep the createdInstances list updated
                createdUnit.Health.EntityDead += HandleEntityDead;
            }
        }

        private void HandleEntityDead(IEntity entity, DeadEventArgs args)
        {
            createdInstances.Remove(entity as IUnit);
        }

        public override ErrorMessage CanStart()
        {
            switch(limit)
            {
                case UnitCreationTaskLimit.launchTimes:
                    if (LaunchTimes >= maxAmount)
                        return ErrorMessage.unitCreatorMaxLaunchTimesReached;
                    break;

                case UnitCreationTaskLimit.activeInstances:
                    if (createdInstances.Count + PendingAmount >= maxAmount)
                        return ErrorMessage.unitCreatorMaxActiveInstancesReached;
                    break;
            }

            return base.CanStart();
        }
    }
}
