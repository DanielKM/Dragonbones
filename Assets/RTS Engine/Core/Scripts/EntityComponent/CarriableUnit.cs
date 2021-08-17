using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UI;
using System.Collections.Generic;
using System.Linq;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public class CarriableUnit : FactionEntityTargetComponent<IFactionEntity>, ICarriableUnit
    {
        #region Attributes
        /*
         * Action types and their parameters:
         * eject: Remove the unit instance from its current carrier 
         * */
        public enum ActionType : byte { eject }

        protected IUnit unit { private set; get; }

        public override bool IsIdle => true;

        public IUnitCarrier CurrCarrier { private set; get; }

        [SerializeField, Tooltip("Defines information used to display a task in the task panel when the faction entity is selected, to allow the unit to be ejected from its carrier, if it has one.")]
        private EntityComponentTaskUIAsset ejectionTaskUI = null;

        [SerializeField, Tooltip("Allow the unit to enter carriers from other factions.")]
        private bool allowDifferentFactions = true;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.unit = factionEntity as IUnit;

            CurrCarrier = null;
        }
        #endregion

        #region Handling Upgrades
        protected override void OnComponentUpgraded(FactionEntityTargetComponent<IFactionEntity> sourceFactionEntityTargetComponent)
        {
            ICarriableUnit sourceCarriableUnit = sourceFactionEntityTargetComponent as ICarriableUnit;
            if(sourceCarriableUnit.CurrCarrier.IsValid())
            {
                IUnitCarrier targetCarrier = sourceCarriableUnit.CurrCarrier;

                targetCarrier.EjectAction(unit, destroyed: false, playerCommand: false);

                SetTarget(targetCarrier.Entity.ToTargetData(), playerCommand: false);
            }
        }
        #endregion

        #region Handling AddableUnitData
        public AddableUnitData GetAddableData(bool playerCommand)
        {
            return new AddableUnitData
            {
                allowDifferentFaction = allowDifferentFactions,
                sourceComponent = this,
                playerCommand = playerCommand
            };
        }
        #endregion

        #region Searching/Updating Target
        public override ErrorMessage IsTargetValid(TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IFactionEntity> potentialTarget = testTarget;

            if (!potentialTarget.instance.IsValid())
                return ErrorMessage.invalid;
            else if (!potentialTarget.instance.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (!potentialTarget.instance.UnitCarrier.IsValid())
                return ErrorMessage.carrierMissing;

            return potentialTarget.instance.UnitCarrier.CanMove(
                unit,
                GetAddableData(playerCommand));
        }

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target) => true;

        protected override void OnTargetPostLocked(bool playerCommand, bool sameTarget)
        {
            if (sameTarget)
                return;

            Target.instance.UnitCarrier.Move(
                unit,
                GetAddableData(playerCommand));

            Target.instance.UnitCarrier.UnitAdded += HandleTargetCarrierUnitAdded;

            Stop();
        }
        #endregion

        #region Stopping
        protected override void OnStop(TargetData<IFactionEntity> lastTarget)
        {
        }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand)
            => RTSHelper.LaunchEntityComponentAction(this, actionID, target, playerCommand);

        public override ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            switch ((ActionType)actionID)
            {
                case ActionType.eject:

                    return EjectActionLocal(playerCommand);

                default:
                    return ErrorMessage.undefined;
            }
        }

        public ErrorMessage EjectAction(bool playerCommand)
        {
            return LaunchAction(
                (byte)ActionType.eject,
                new TargetData<IEntity> { },
                playerCommand);
        }

        public ErrorMessage EjectActionLocal(bool playerCommand)
        {
            if (!CurrCarrier.IsValid())
                return ErrorMessage.invalid;

            ErrorMessage ejectionErrorMsg = CurrCarrier.LaunchActionLocal(
                (byte)UnitCarrier.ActionType.eject,
                new TargetData<IEntity>
                {
                    instance = unit,
                    position = Vector3.zero // This refers to the fact that the unit is not being ejected due to it being destroyed
                },
                playerCommand);

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);

            return ejectionErrorMsg;
        }
        #endregion

        #region Handling Events: Target Carrier Unit Added/Removed
        private void HandleTargetCarrierUnitAdded(IUnitCarrier carrier, EntityEventArgs<IUnit> args)
        {
            if (args.Entity != unit)
                return;

            CurrCarrier = carrier;

            CurrCarrier.UnitAdded -= HandleTargetCarrierUnitAdded;
            CurrCarrier.UnitRemoved += HandleTargetCarrierUnitRemoved;
        }

        private void HandleTargetCarrierUnitRemoved(IUnitCarrier carrier, EntityEventArgs<IUnit> args)
        {
            if (carrier != CurrCarrier)
                return;

            // If this unit is confirmed to be added to its target carrier
            if (args.Entity == unit)
            {
                Stop();

                CurrCarrier.UnitRemoved -= HandleTargetCarrierUnitRemoved;

                CurrCarrier = null;
            }
        }
        #endregion

        #region Task UI
        public override bool OnTaskUIRequest(out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes)
        {
            if (!base.OnTaskUIRequest(out taskUIAttributes, out disabledTaskCodes))
                return false;

            if (ejectionTaskUI.IsValid())
            {
                if (CurrCarrier.IsValid())
                    taskUIAttributes = taskUIAttributes.Append(
                        new EntityComponentTaskUIAttributes
                        {
                            data = ejectionTaskUI.Data,

                            locked = false
                        });
                else
                    disabledTaskCodes = disabledTaskCodes.Append(ejectionTaskUI.Key);
            }

            return true;
        }

        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            if (!base.OnTaskUIClick(taskAttributes) // Check if this is not the set target task.
                && ejectionTaskUI.IsValid() && taskAttributes.data.code == ejectionTaskUI.Key)
            {
                EjectAction(playerCommand: true);

                return true;
            }

            return false;
        }
        #endregion
    }
}
