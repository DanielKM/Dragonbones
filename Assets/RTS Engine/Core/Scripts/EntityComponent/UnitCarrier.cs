using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Movement;
using RTSEngine.UI;
using RTSEngine.Faction;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Audio;
using RTSEngine.Search;
using RTSEngine.Selection;
using RTSEngine.UnitExtension;
using RTSEngine.Utilities;

namespace RTSEngine.EntityComponent
{
    public partial class UnitCarrier : MonoBehaviour, IUnitCarrier
    {
        #region Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        /*
         * Action types and their parameters:
         * eject: target.position.x => 1 for call being from a destroyed carrier, else carrier is not destroyed, target.instance -> unit instance to remove
         * ejectAll: target.position.x => 1 for call being from a destroyed carrier, else carrier is not destroyed
         * callUnits: no parameters.
         * */
        public enum ActionType : byte { eject, ejectAll, callUnits }

        [SerializeField, Tooltip("Code to identify this component, unique within the entity")]
        private string code = "unit_carrier_code";
        public string Code => code;

        protected IFactionEntity factionEntity { private set; get; }
        public IEntity Entity => factionEntity;

        [SerializeField, Tooltip("Is the component enabled by default?")]
        private bool isActive = true;
        public bool IsActive => isActive;

        [SerializeField, Tooltip("Define the units that can be carried.")]
        private FactionEntityTargetPicker targetPicker = new FactionEntityTargetPicker();

        [SerializeField, Tooltip("The maximum amount of units that can be carried at the same time."), Min(0)]
        private int capacity = 5;
        public int MaxAmount => capacity;

        [SerializeField, Tooltip("Define how custom slots amount for units that get added to the carrier. Default slot value is 1 for undefined units.")]
        private EntityAmountHandler customUnitSlots = new EntityAmountHandler();

        public int CurrAmount { private set; get; }
        public bool HasMaxAmount => CurrAmount >= MaxAmount;

        private IUnit[] storedUnits = new IUnit[0];
        public IEnumerable<IUnit> StoredUnits => storedUnits.Where(unit => unit.IsValid()).ToList();

        [SerializeField, Tooltip("The possible positions that units can use to enter the carrier. Each unit added unit will seek the closest position to enter the carrier.")]
        private Transform[] addablePositions = new Transform[0];

        [SerializeField, Tooltip("If populated, then this defines the types of terrain areas where units can interact with this carrier.")]
        private TerrainAreaType[] forcedTerrainAreas = new TerrainAreaType[0];

        [SerializeField, Tooltip("What audio clip to play when a unit goes into the carrier?")]
        private AudioClipFetcher addUnitAudio = new AudioClipFetcher();

        [SerializeField, Tooltip("The possible positions that units can occupy when inside the carrier. When there is no carrier position available for a unit, it will be deactivated.")]
        private Transform[] carrierPositions = new Transform[0];
        private Stack<Transform> freeCarrierPositions = new Stack<Transform>();

        [SerializeField, Tooltip("The possible positions that a unit inside the carrier transports to when ejected from the carrier. Leave empty to use the same addable positions for ejectable positions.")]
        private Transform[] ejectablePositions = new Transform[0];

        [SerializeField, Tooltip("Can stored units be ejected individually through a task?")]
        private bool canEjectSingleUnit = true;
        [SerializeField, Tooltip("Defines information used to display a single unit ejection task in the task panel.")]
        private EntityComponentTaskUIAsset ejectSingleUnitTaskUI = null;

        [SerializeField, Tooltip("Can stored units be ejected all together through a task?")]
        private bool canEjectAllUnits = true;
        [SerializeField, Tooltip("Defines information used to display all units ejection task in the task panel.")]
        private EntityComponentTaskUIAsset ejectAllUnitsTaskUI = null;

        [SerializeField, Tooltip("What audio clip to play when a unit is ejected from the APC?")]
        private AudioClipFetcher ejectUnitAudio = new AudioClipFetcher();

        [SerializeField, Tooltip("Enable to allow stored units to be ejected when the carrier is destroyed.")]
        private bool ejectOnDestroy = true;

        [SerializeField, Tooltip("Units that are within this distance from the carrier are called.")]
        private float callUnitsRange = 20.0f;
        [SerializeField, Tooltip("Only call units that are idle?")]
        private bool callIdleOnly = false;
        [SerializeField, Tooltip("Can call units that have an attack component?")]
        private bool callAttackUnits = true;

        [SerializeField, Tooltip("Defines information used to display the task that calls units to get into the carrier in the task panel.")]
        private EntityComponentTaskUIAsset callUnitsTaskUI = null;

        [SerializeField, Tooltip("What audio clip to play when the carrier calls units in range?")]
        private AudioClipFetcher callUnitsAudio = new AudioClipFetcher();

        [SerializeField, Tooltip("If the unit carrier is marked as a free unit, how would it interact with other units?")]
        private FreeFactionBehaviour freeFactionBehaviour = new FreeFactionBehaviour { allowFreeFaction = false, allowLocalPlayer = true, allowRest = true };

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IUnitCarrier, EntityEventArgs<IUnit>> UnitAdded;
        public event CustomEventHandler<IUnitCarrier, EntityEventArgs<IUnit>> UnitRemoved;
        public event CustomEventHandler<IUnitCarrier, EntityEventArgs<IUnit>> UnitCalled;

        private void RaiseUnitAdded(EntityEventArgs<IUnit> args)
        {
            var handler = UnitAdded;
            handler?.Invoke(this, args);
        }
        private void RaiseUnitRemoved(EntityEventArgs<IUnit> args)
        {
            var handler = UnitRemoved;
            handler?.Invoke(this, args);
        }
        private void RaiseUnitCalled(EntityEventArgs<IUnit> args)
        {
            var handler = UnitCalled;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();

            this.factionEntity = entity as IFactionEntity;

            if (!logger.RequireTrue(addablePositions.Length > 0 && addablePositions.All(t => t.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] The field 'Addable Positions' is either empty or has unassigned elements!")

                || !logger.RequireTrue(ejectablePositions.Length == 0 || ejectablePositions.All(t => t.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] The field 'Ejectable Positions' must be either empty or populated with valid elements!")

                || !logger.RequireTrue(forcedTerrainAreas.Length == 0 || forcedTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Entity.Code}] The 'Forced Terrain Areas' field must be either empty or populated with valid elements!"))
                return;

            storedUnits = new IUnit[capacity];

            freeCarrierPositions = new Stack<Transform>();
            foreach (Transform t in carrierPositions.Reverse())
            {
                if (!logger.RequireValid(t,
                    $"[{GetType().Name}] The 'Carrier Positions' field has invalid elements."))
                    return;

                freeCarrierPositions.Push(t);
            }
            CurrAmount = 0;

            factionEntity.Health.EntityDead += HandlEntityDead;
        }

        public void Disable()
        {
            factionEntity.Health.EntityDead -= HandlEntityDead;
        }
        #endregion

        #region Handling Component Upgrade
        public void HandleComponentUpgrade(IEntityComponent sourceEntityComponent)
        {
            UnitCarrier sourceUnitCarrier = sourceEntityComponent as UnitCarrier;
            if (!sourceUnitCarrier.IsValid())
                return;

            foreach (IUnit storedUnit in sourceUnitCarrier.StoredUnits.ToArray())
            {
                sourceUnitCarrier.EjectActionLocal(storedUnit, destroyed: false, playerCommand: false);
                storedUnit.CarriableUnit.SetTargetLocal(Entity.ToTargetData(), playerCommand: false);
            }
        }
        #endregion

        #region Handling Events: IEntity (source)
        private void HandlEntityDead(IEntity sender, DeadEventArgs e)
        {
            if(!e.IsUpgrade)
                EjectAllAction(destroyed: true, playerCommand: false);
        }
        #endregion

        #region IAddableUnit/Adding Units
        public Vector3 GetAddablePosition(IUnit unit) => GetClosestPosition(unit, addablePositions);

        public ErrorMessage CanMove(IUnit unit, AddableUnitData addableData = default)
        {
            if (!Entity.CanLaunchTask)
                return ErrorMessage.taskSourceCanNotLaunch;

            else if (!unit.IsValid())
                return ErrorMessage.invalid;
            else if (!unit.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (unit.Health.IsDead)
                return ErrorMessage.dead;
            else if (!unit.CarriableUnit.IsValid())
                return ErrorMessage.carriableComponentMissing;
            else if (!unit.CanMove)
                return ErrorMessage.mvtDisabled;

            else if ((Entity.IsFree && !freeFactionBehaviour.IsEntityAllowed(unit))
                || (!addableData.allowDifferentFaction && !RTSHelper.IsSameFaction(unit, Entity)))
                return ErrorMessage.factionMismatch;

            else if (!targetPicker.IsValidTarget(unit))
                return ErrorMessage.entityCompTargetPickerUndefined;

            else if (CurrAmount + customUnitSlots.GetAmount(unit) > MaxAmount)
                return ErrorMessage.carrierCapacityReached;

            return ErrorMessage.none;
        }

        public ErrorMessage Move(IUnit unit, AddableUnitData addableData)
        {
            ErrorMessage errorMsg;
            if ((errorMsg = CanMove(unit, addableData)) != ErrorMessage.none)
            {
                if (addableData.playerCommand && Entity.IsLocalPlayerFaction())
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = Entity,
                        target = unit
                    });

                return errorMsg;
            }

            Vector3 addablePosition = GetAddablePosition(unit);

            return mvtMgr.SetPathDestination(unit,
                addablePosition,
                0.0f,
                Entity,
                new MovementSource
                {
                    component = addableData.sourceComponent,

                    targetAddableUnit = this,
                    targetAddableUnitPosition = addablePosition,

                    playerCommand = addableData.playerCommand
                });
        }

        // The same conditions for moving a unit to the carrier apply to adding it as well.
        public ErrorMessage CanAdd(IUnit unit, AddableUnitData addableData = default) => CanMove(unit, addableData);


        public ErrorMessage Add(IUnit unit, AddableUnitData addableData = default) => Add(unit, addableData.playerCommand);

        public ErrorMessage Add(IUnit unit, bool playerCommand)
        {
            ErrorMessage errorMsg;
            if ((errorMsg = CanAdd(unit, unit.CarriableUnit.GetAddableData(playerCommand))) != ErrorMessage.none)
                return errorMsg;

            if (freeCarrierPositions.Count > 0)
            {
                unit.MovementComponent.Controller.Enabled = false;
                unit.MovementComponent.TargetPositionMarker.Toggle(false);
                unit.MovementComponent.SetActiveLocal(false, playerCommand: false);

                Transform nextCarrierSlot = freeCarrierPositions.Pop();
                unit.transform.SetParent(nextCarrierSlot, true);
                unit.transform.localPosition = Vector3.zero;
            }
            else
            {
                unit.gameObject.SetActive(false);
                unit.transform.SetParent(Entity.transform, true);
            }


            unit.SetIdle(null);
            selectionMgr.Remove(unit);

            storedUnits[Array.IndexOf(storedUnits, null)] = unit;

            CurrAmount += customUnitSlots.GetAmount(unit);

            audioMgr.PlaySFX(Entity.AudioSourceComponent, addUnitAudio.Fetch(), false);

            unit.Health.EntityDead += OnCarriedUnitDead;

            RaiseUnitAdded(new EntityEventArgs<IUnit>(unit));
            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Events: Tracking Carrier Units
        private void OnCarriedUnitDead(IEntity sender, DeadEventArgs e)
        {
            // Directly eject unit since this is called from the local destroy method on the unit
            EjectActionLocal(sender as IUnit, false, false);
        }
        #endregion

        #region Handling Actions
        public ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand)
            => RTSHelper.LaunchEntityComponentAction(this, actionID, target, playerCommand);

        public ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            switch ((ActionType)actionID)
            {
                case ActionType.eject:

                    return EjectActionLocal(target.instance as IUnit, Mathf.RoundToInt(target.position.x) == 1 ? true : false, playerCommand);

                case ActionType.ejectAll:

                    return EjectAllActionLocal(Mathf.RoundToInt(target.position.x) == 1 ? true : false, playerCommand);

                case ActionType.callUnits:

                    return CallUnitsActionLocal(playerCommand);

                default:
                    return ErrorMessage.undefined;
            }
        }
        #endregion

        #region Ejecting Units Action
        public Vector3 GetEjectablePosition(IUnit unit) => GetClosestPosition(unit, ejectablePositions.Length > 0 ? ejectablePositions : addablePositions);

        public ErrorMessage EjectAllAction(bool destroyed, bool playerCommand)
        {
            return LaunchAction(
                (byte)ActionType.ejectAll,
                new TargetData<IEntity>
                {
                    position = new Vector3(destroyed ? 1.0f : 0.0f, 0.0f, 0.0f)
                },
                playerCommand);
        }

        private ErrorMessage EjectAllActionLocal(bool destroyed, bool playerCommand)
        {
            if (!canEjectAllUnits)
                return ErrorMessage.inactive;

            ErrorMessage errorMessage;
            foreach(IUnit storedUnit in storedUnits)
                if (storedUnit.IsValid() && (errorMessage = EjectActionLocal(storedUnit, destroyed, playerCommand)) != ErrorMessage.none)
                    return errorMessage;

            return ErrorMessage.none;
        }

        public ErrorMessage EjectAction(IUnit unit, bool destroyed, bool playerCommand)
        {
            if (!canEjectSingleUnit)
                return ErrorMessage.inactive;

            return LaunchAction(
                (byte)ActionType.eject,
                new TargetData<IEntity>
                {
                    instance = unit,
                    position = new Vector3(destroyed ? 1.0f : 0.0f, 0.0f, 0.0f)
                },
                playerCommand);
        }

        private ErrorMessage EjectActionLocal(IUnit unit, bool destroyed, bool playerCommand)
        {
            if (!unit.IsValid() || !storedUnits.Contains(unit))
                return ErrorMessage.invalid;

            // Only eject if the ejection position is in a movable area for the unit:
            if (!mvtMgr.TryGetMovablePosition(
                GetEjectablePosition(unit),
                unit.MovementComponent.Controller.Radius,
                unit.MovementComponent.Controller.NavigationAreaMask,
                out Vector3 ejectionPosition))
            {
                if (playerCommand && Entity.IsLocalPlayerFaction())
                        playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                        {
                            message = ErrorMessage.mvtDisabled,

                            source = Entity,
                            target = unit
                        });

                return ErrorMessage.mvtDisabled;
            }


            // UNIT CARRIER SLOT
            if (unit.IsInteractable)
            {
                freeCarrierPositions.Push(unit.transform.parent);
                unit.transform.position = ejectionPosition;

                unit.transform.SetParent(null, true);

                unit.MovementComponent.TargetPositionMarker.Toggle(true, ejectionPosition);
                unit.MovementComponent.Controller.Enabled = true;
                unit.MovementComponent.SetActiveLocal(true, playerCommand: false);
            }
            else
            {
                unit.transform.position = ejectionPosition;
                unit.gameObject.SetActive(true);

                unit.transform.SetParent(null, true);
            }

            unit.SetIdle(null);

            storedUnits[Array.IndexOf(storedUnits, unit)] = null;

            CurrAmount -= customUnitSlots.GetAmount(unit);

            audioMgr.PlaySFX(Entity.AudioSourceComponent, ejectUnitAudio.Fetch(), false);

            unit.Health.EntityDead -= OnCarriedUnitDead;

            RaiseUnitRemoved(new EntityEventArgs<IUnit>(unit));
            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);

            // Destroying stored units due to carrier being destroyed?
            if (destroyed && !ejectOnDestroy)
                unit.Health.Destroy(false, null);

            // Unit was ejected normally, not due to carrier destruction
            // See if there is a rallypoint to send the unit to.
            else if (factionEntity.Rallypoint.IsValid())
                factionEntity.Rallypoint.SendAction(unit, playerCommand: false);

            return ErrorMessage.none;
        }
        #endregion

        #region Calling Units Action
        public ErrorMessage CanCallUnit(TargetData<IEntity> testTarget, bool playerCommand)
        {
            IUnit unit = testTarget.instance as IUnit;

            ErrorMessage errorMsg;
            if ((errorMsg = CanAdd(unit)) != ErrorMessage.none)
                return errorMsg;
            else if (callIdleOnly && !unit.IsIdle)
                return ErrorMessage.carrierIdleOnlyAllowed;
            else if (!callAttackUnits && unit.CanAttack)
                return ErrorMessage.carrierAttackerNotAllowed;

            return ErrorMessage.none;
        }

        public ErrorMessage CallUnitsAction(bool playerCommand)
        {
            return LaunchAction((byte)ActionType.callUnits, null, playerCommand);
        }

        private ErrorMessage CallUnitsActionLocal(bool playerCommand)
        {
            audioMgr.PlaySFX(Entity.AudioSourceComponent, callUnitsAudio.Fetch(), false);

            gridSearch.Search(
                Entity.transform.position,
                callUnitsRange,
                MaxAmount - CurrAmount,
                CanCallUnit,
                playerCommand,
                out IEnumerable<IUnit> unitsInRange
                );

            foreach (IUnit unit in unitsInRange)
            {
                Move(
                    unit,
                    new AddableUnitData
                    {
                        sourceComponent = null,
                        playerCommand = playerCommand
                    });

                RaiseUnitCalled(new EntityEventArgs<IUnit>(unit));
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Task UI
        public bool OnTaskUIRequest(
            out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            disabledTaskCodes = Enumerable.Empty<string>();

            if (!Entity.CanLaunchTask
                || !IsActive
                || !RTSHelper.IsLocalPlayerFaction(Entity))
                return false;

            // In the case of single unit ejection task, the task's code is appended by the unit's index in the "storedUnits" list since each individual unit's ejection is a unique task.
            // Not all properties used in the single unit ejection task are used: displayType is forced to single, fixedSlotIndex is disabled and icon is replaced by unit's icon.
            if (canEjectSingleUnit && ejectSingleUnitTaskUI.IsValid())
            {
                IEnumerable<int> storedUnitIDs = Enumerable.Range(0, storedUnits.Length);
                for (int ID = 0; ID < storedUnits.Length; ID++)
                {
                    if (!ejectSingleUnitTaskUI.Data.enabled || !storedUnits[ID].IsValid())
                        disabledTaskCodes = disabledTaskCodes.Append($"{ejectSingleUnitTaskUI.Data.code}_{ID}");
                    else
                    {
                        taskUIAttributes = taskUIAttributes.Append(
                            new EntityComponentTaskUIAttributes
                            {
                                data = new EntityComponentTaskUIData
                                {
                                    enabled = true,

                                    code = $"{ejectSingleUnitTaskUI.Data.code}_{ID}",
                                    displayType = EntityComponentTaskUIData.DisplayType.singleSelection,
                                    icon = storedUnits[ID].Icon,

                                    forceSlot = false,

                                    panelCategory = ejectSingleUnitTaskUI.Data.panelCategory,
                                    tooltipEnabled = ejectSingleUnitTaskUI.Data.tooltipEnabled,
                                    hideTooltipOnClick = ejectSingleUnitTaskUI.Data.hideTooltipOnClick,
                                    description = ejectSingleUnitTaskUI.Data.description,
                                },

                                locked = false,
                            });
                    }
                }
            }

            if (canEjectAllUnits && ejectAllUnitsTaskUI.IsValid())
            {
                if (CurrAmount == 0 || !ejectAllUnitsTaskUI.Data.enabled)
                    disabledTaskCodes = disabledTaskCodes.Append(ejectAllUnitsTaskUI.Data.code);
                else
                    taskUIAttributes = taskUIAttributes.Append(new EntityComponentTaskUIAttributes
                    {
                        data = ejectAllUnitsTaskUI.Data,
                    });
            }

            if (callUnitsTaskUI.IsValid())
            {
                if (HasMaxAmount || !callUnitsTaskUI.Data.enabled)
                    disabledTaskCodes = disabledTaskCodes.Append(callUnitsTaskUI.Data.code);
                else
                    taskUIAttributes = taskUIAttributes.Append(new EntityComponentTaskUIAttributes
                    {
                        data = callUnitsTaskUI.Data,
                    });
            }

            return true;
        }

        public bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            string taskCode = taskAttributes.data.code;

            if (ejectAllUnitsTaskUI.IsValid() && taskCode == ejectAllUnitsTaskUI.Data.code)
                EjectAllAction(false, true);
            else if (callUnitsTaskUI.IsValid() && taskCode == callUnitsTaskUI.Data.code)
                CallUnitsAction(true);
            else
            {
                // Check the creation of the eject single unit tasks in "OnTaskUIRequest()" for info on how the task code is set.
                string[] splits = taskCode.Split('_');
                EjectAction(storedUnits[int.Parse(splits[splits.Length - 1])], false, true);
            }

            return true;
        }
        #endregion

        #region Activating/Deactivating Component
        public ErrorMessage SetActive(bool active, bool playerCommand) => RTSHelper.SetEntityComponentActive(this, active, playerCommand);

        public ErrorMessage SetActiveLocal(bool active, bool playerCommand)
        {
            isActive = active;

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);

            return ErrorMessage.none;
        }
        #endregion

        #region Helper Methods
        public bool IsUnitStored(IUnit unit) => storedUnits.Contains(unit);

        private Vector3 GetClosestPosition(IUnit unit, Transform[] transforms)
        {
            Vector3 closestPosition = transforms
                .Select(t => t.position)
                .OrderBy(pos => (pos - unit.transform.position).sqrMagnitude)
                .First();

            logger.RequireTrue(terrainMgr.GetTerrainAreaPosition(closestPosition, forcedTerrainAreas, out Vector3 closestPositionAdjusted),
                $"[{GetType().Name} - {Entity.Code}] Unable to find a valid position on the defined terrain areas!");

            return closestPositionAdjusted;
        }
        #endregion
    }
}
