using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UnitExtension;
using RTSEngine.BuildingExtension;
using RTSEngine.SpellCastExtension;
using RTSEngine.Attack;
using RTSEngine.ResourceExtension;
using RTSEngine.Health;

namespace RTSEngine.Determinism
{
    public class InputManager : MonoBehaviour, IInputManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IEntity)), Tooltip("Entity prefabs that can be used in this map are defined here. Spawnable prefabs are prefabs with the IEntity component placed in a path that ends with: '../Resources/Prefabs'")]
        private GameObject[] spawnablePrefabs = new GameObject[0];
        private List<IEntity> entityPrefabs = new List<IEntity>();

        [SerializeField, Tooltip("Enable to allow this component to scan for all usable prefabs in the project and add them to the Spawnable Prefabs list when the game starts.")]
        private bool autoGenerateSpawnablePrefabs = true;

        // The entities spawned through this component can be referenced using each entity's unique key.
        private static Dictionary<int, IEntity> spawnedEntities = new Dictionary<int, IEntity>();
        // Allows to determine the next key to use for the next entity that reigsters itself in this component.
        private int nextKey = 0;

        // Stores inputs received before the IInputAdder of the current game is initialized, in case there is supposed to be one.
        private List<CommandInput> awaitingInputAdderInputs;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IUnitManager unitMgr {private set; get;}
        protected IBuildingManager buildingMgr {private set; get;}
        protected ISpellCastManager spellMgr {private set; get;}
        protected IResourceManager resourceMgr {private set; get;}
        protected IMovementManager mvtMgr {private set; get;}
        protected IAttackManager attackMgr {private set; get;}
        protected ITimeModifier timeModifier { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.spellMgr = gameMgr.GetService<ISpellCastManager>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.timeModifier = gameMgr.GetService<ITimeModifier>(); 

            spawnedEntities = new Dictionary<int, IEntity>();
            nextKey = -1;

            awaitingInputAdderInputs = new List<CommandInput>();
            // If the input adder is not ready yet, we subscribe to the event that will be triggered when it is enabled
            if (gameMgr.CurrBuilder.IsValid() && !gameMgr.CurrBuilder.IsInputAdderReady)
                gameMgr.CurrBuilder.InputAdderReady += HandleInputAdderReady;

            gameMgr.GameStartRunning += HandleGameStartRunning;
        }

        private void OnDestroy()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }

        private void HandleGameStartRunning(IGameManager gameMgr, EventArgs args)
        {
            entityPrefabs = (autoGenerateSpawnablePrefabs
                ? Resources
                .LoadAll("Prefabs", typeof(GameObject))
                .Cast<GameObject>()
                : spawnablePrefabs)
                .Where(prefabObj => prefabObj.IsValid())
                .Select(prefabObj => prefabObj.GetComponent<IEntity>())
                .ToList();

            foreach (IFactionSlot faction in gameMgr.FactionSlots)
                faction.InitDefaultFactionEntities();
        }

        private void HandleInputAdderReady(IGameBuilder sender, EventArgs args)
        {
            if(awaitingInputAdderInputs.Count > 0)
            {
                logger.Log($"[{GetType().Name}] '{typeof(IInputAdder).Name}' instance is now ready to relay inputs. Relayed {awaitingInputAdderInputs.Count} late cached inputs...");
                gameMgr.CurrBuilder.InputAdder.AddInput(awaitingInputAdderInputs);
            }

            awaitingInputAdderInputs.Clear();

            gameMgr.CurrBuilder.InputAdderReady -= HandleInputAdderReady;
        }
        #endregion

        #region Registering Entities
        public int RegisterEntity(IEntity newEntity)
        {
            if (!logger.RequireValid(newEntity,
                $"[InputManager] Register an invalid entity is not allowed!"))
                return -1;

            nextKey++;
            spawnedEntities.Add(nextKey, newEntity);
            return nextKey;
        }
        #endregion

        #region Sending Input
        public ErrorMessage SendInput(CommandInput newInput, IEntity source, IEntity target)
        {
            if (!logger.RequireValid(source,
                $"[{GetType().Name}] Can not process input without a valid source!"))
                return ErrorMessage.invalid;

            if (!RTSHelper.IsMasterInstance() && !newInput.playerCommand 
                || (newInput.playerCommand && !RTSHelper.IsLocalPlayerFaction(source)))
                return ErrorMessage.noAuthority;

            // New input can now be processed after checking for the game's permissions over the input's source.

            // If we're creating an object, then look in the spawnable prefabs list
            // Otherwise, use the unique key assigned to each IEntity as the sourceID
            newInput.sourceID = newInput.isSourcePrefab
                ? entityPrefabs.FindIndex(0, entityPrefabs.Count, prefab => prefab.Code == source.Code)
                : source.Key;

            newInput.targetID = target.IsValid() ? target.Key : -1;

            newInput.sourceCode = source?.Code;

            return SendInputFinal(newInput);
        }

        public ErrorMessage SendInput(CommandInput newInput, IEnumerable<IEntity> source, IEntity target)
        {
            if (!logger.RequireValid(source,
                $"[{GetType().Name}] Can not process input without a valid source!"))
                return ErrorMessage.invalid;

            if (!RTSHelper.IsMasterInstance() && !newInput.playerCommand 
                || (newInput.playerCommand && !RTSHelper.IsLocalPlayerFaction(source)))
                return ErrorMessage.noAuthority;

            newInput.code = EntitiesToKeyString(source);

            newInput.targetID = target.IsValid() ? target.Key : -1;

            return SendInputFinal(newInput);
        }

        public ErrorMessage SendInput(CommandInput newInput)
        {
            if (!RTSHelper.IsMasterInstance())
                return ErrorMessage.noAuthority;

            return SendInputFinal(newInput);
        }

        private ErrorMessage SendInputFinal(CommandInput newInput)
        {
            if (gameMgr.State == GameStateType.frozen)
                return ErrorMessage.gameFrozen;
            else if (gameMgr.CurrBuilder.IsValid())
            {
                // If the input adder instance is not ready yet then cache the received inputs to be relayed when the input adder is ready
                if (!gameMgr.CurrBuilder.IsInputAdderReady)
                    awaitingInputAdderInputs.Add(newInput);
                else
                    gameMgr.CurrBuilder.InputAdder.AddInput(newInput);
            }
            else
                LaunchInput(newInput);

            return ErrorMessage.none;
        }
        #endregion

        #region Launching Input
        public void LaunchInput(IEnumerable<CommandInput> inputs)
        {
            foreach (CommandInput input in inputs)
                LaunchInput(input);
        }

        public void LaunchInput(CommandInput input)
        {
            switch ((InputMode)input.sourceMode)
            {
                case InputMode.master:
                    OnMasterInput(input);
                    break;

                case InputMode.create:
                    OnCreateInput(input);
                    break;

                case InputMode.faction:
                    OnFactionInput(input);
                    break;

                case InputMode.entity:
                    OnEntityInput(input);
                    break;
                case InputMode.entityGroup:
                    OnEntityGroupInput(input);
                    break;

                case InputMode.health:
                    OnHealthInput(input);
                    break;

                case InputMode.custom:
                    OnCustomInput(input);
                    break;

                default:
                    logger.LogError($"[{GetType().Name}] Undefined input source type of ID: {input.sourceID}!");
                    break;
            }
        }

        private void OnMasterInput(CommandInput input)
        {
            switch((InputMode)input.targetMode)
            {
                case InputMode.setTimeModifier:

                    timeModifier.SetModifierLocal(input.floatValue, input.playerCommand);

                    break;

                default:
                    logger.LogError($"[{GetType().Name}] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.master}!");
                    break;
            }
        }

        private IEntity GetInputSourceEntity (CommandInput input)
        {
            try
            {
                return input.isSourcePrefab
                    ? entityPrefabs[input.sourceID]
                    : spawnedEntities[input.sourceID];
            }
            catch
            {
                logger.LogError($"[{GetType().Name}] Unable to get prefab ({input.isSourcePrefab}) or instance ({!input.isSourcePrefab}) of ID: {input.sourceID}");
                print($"[DEV] Requested entity code: {input.sourceCode}!");
                return null;
            }
        }

        protected virtual void OnCustomInput(CommandInput input) { }

        private void OnCreateInput(CommandInput input)
        {
            IEntity prefab = GetInputSourceEntity(input);
            spawnedEntities.TryGetValue(input.targetID, out IEntity target);

            switch ((InputMode)input.targetMode)
            {
                case InputMode.unit:

                    InitUnitParametersInput unitParamsInput = JsonUtility.FromJson<InitUnitParametersInput>(input.code);
                    InitUnitParameters unitParams = new InitUnitParameters
                    {
                        factionID = unitParamsInput.factionID,
                        free = unitParamsInput.free,

                        setInitialHealth = unitParamsInput.setInitialHealth,
                        initialHealth = unitParamsInput.initialHealth,

                        rallypoint = (target as IFactionEntity)?.Rallypoint,
                        creatorEntityComponent = target?.EntityComponents.ContainsKey(unitParamsInput.creatorEntityComponentCode) == true ? target.EntityComponents[unitParamsInput.creatorEntityComponentCode] : null,

                        gotoPosition = unitParamsInput.gotoPosition,

                        playerCommand = unitParamsInput.playerCommand
                    };

                    unitMgr.CreateUnitLocal(
                        prefab as IUnit,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        unitParams);


                    break;
                case InputMode.spell:

                    var spellParams = JsonUtility.FromJson<InitSpellParameters>(input.code);

                    spellMgr.CreatePlacedSpellLocal(
                        prefab as ISpell,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        spellParams);

                    break;
                case InputMode.building:

                    var buildingParams = JsonUtility.FromJson<InitBuildingParameters>(input.code);
                    buildingParams.buildingCenter = (target as IBuilding)?.BorderComponent;

                    buildingMgr.CreatePlacedBuildingLocal(
                        prefab as IBuilding,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        buildingParams);

                    break;

                case InputMode.resource:

                    var resourceParams = JsonUtility.FromJson<InitResourceParameters>(input.code);

                    resourceMgr.CreateResourceLocal(
                        prefab as IResource,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        resourceParams);

                    break;
                default:
                    logger.LogError($"[{GetType().Name}] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.create}!");
                    break;
            }
        }

        private void OnFactionInput(CommandInput input)
        {
            switch ((InputMode)input.targetMode)
            {
                case InputMode.factionDestroy:

                    gameMgr.OnFactionDefeatedLocal(input.intValues.Item1); //the input.value holds the faction ID of the faction to destroy
                    break;

                default:
                    logger.LogError($"[{GetType().Name}] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.faction}!");
                    break;
            }
        }

        private void OnEntityInput(CommandInput input)
        {
            IEntity sourceEntity = GetInputSourceEntity(input);

            spawnedEntities.TryGetValue(input.targetID, out IEntity target);
            TargetData<IEntity> targetData = new TargetData<IEntity> { instance = target, position = input.targetPosition, opPosition = input.opPosition };

            switch ((InputMode)input.targetMode)
            {
                case InputMode.setFaction:

                    sourceEntity.SetFactionLocal(target, input.intValues.Item1);
                    break;

                case InputMode.setComponentActive:

                    sourceEntity.EntityComponents[input.code].SetActiveLocal(input.intValues.Item1 == 1 ? true : false, input.playerCommand);
                    break;

                case InputMode.setComponentTargetFirst:

                    sourceEntity.SetTargetFirstLocal(
                        targetData,
                        input.playerCommand);

                    break;

                case InputMode.setComponentTarget:

                    sourceEntity.EntityTargetComponents[input.code].SetTargetLocal(
                        targetData,
                        input.playerCommand);
                    break;

                case InputMode.launchComponentAction:

                    sourceEntity.EntityComponents[input.code].LaunchActionLocal(
                        (byte)input.intValues.Item1,
                        targetData,
                        input.playerCommand);
                    break;

                case InputMode.attack:

                    attackMgr.LaunchAttackLocal(new LaunchAttackData<IEntity>
                    {
                        source = sourceEntity,
                        targetEntity = target as IFactionEntity,
                        targetPosition = input.targetPosition,
                        playerCommand = input.playerCommand
                    });
                    break;

                case InputMode.movement:

                    string[] mvtParams = input.code.Split('.');
                    IEntityTargetComponent sourceComponent = null;
                    IAddableUnit targetAddableUnit = null;

                    sourceEntity?.EntityTargetComponents.TryGetValue(mvtParams[0], out sourceComponent);
                    target?.AddableUnitComponents.TryGetValue(mvtParams[1], out targetAddableUnit);

                    mvtMgr.SetPathDestinationLocal(
                        sourceEntity,
                        input.targetPosition,
                        input.floatValue,
                        target,
                        new MovementSource
                        {
                            playerCommand = input.playerCommand,
                            component = sourceComponent,
                            targetAddableUnit = targetAddableUnit,
                            targetAddableUnitPosition = input.opPosition,
                            isAttackMove = input.intValues.Item1 == 1,
                            isOriginalAttackMove = input.intValues.Item2 == 1
                        });
                    break;

                default:
                    logger.LogError($"[{GetType().Name}] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.entity}!");
                    break;
            }
        }

        private void OnEntityGroupInput(CommandInput input)
        {
            List<IEntity> sourceEntities = KeyStringToEntities(input.code).Cast<IEntity>().ToList(); //get the units list
            spawnedEntities.TryGetValue(input.targetID, out IEntity target); //attempt to get the target Entity instance for this unit input

            if (sourceEntities.Count > 0) //if there's actual units in the list
            {
                switch ((InputMode)input.targetMode)
                {
                    case InputMode.attack:

                        //if the target mode is attack -> make the unit group launch an attack on the target.
                        attackMgr.LaunchAttackLocal(new LaunchAttackData<IEnumerable<IEntity>>
                        {
                            source = sourceEntities,
                            targetEntity = target as IFactionEntity,
                            targetPosition = input.targetPosition,
                            playerCommand = input.playerCommand
                        });
                        break;

                    case InputMode.movement:

                        mvtMgr.SetPathDestinationLocal(
                            sourceEntities,
                            input.targetPosition,
                            input.floatValue,
                            target as IEntity,
                            input.playerCommand);
                        break;

                    default:
                        logger.LogError($"[{GetType().Name}] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.entityGroup}!");
                        break;
                }
            }
        }

        private void OnHealthInput(CommandInput input)
        {
            IEntity sourceEntity = GetInputSourceEntity(input);

            spawnedEntities.TryGetValue(input.targetID, out IEntity target);

            IEntityHealth sourceHealth;

            switch((EntityType)input.intValues.Item1)
            {
                case EntityType.unit:
                    sourceHealth = (sourceEntity as IUnit).Health;
                    break;

                case EntityType.building:
                    sourceHealth = (sourceEntity as IBuilding).Health;
                    break;

                case EntityType.resource:
                    sourceHealth = (sourceEntity as IResource).Health;
                    break;

                default:
                    logger.LogError($"[{GetType().Name} - {InputMode.health}] Invalid source entity type of ID: {input.intValues.Item1}!");
                    return;
            }

            switch((InputMode)input.targetMode)
            {
                case InputMode.healthSetMax:

                    sourceHealth.SetMaxLocal(input.intValues.Item2, target);
                    break;

                case InputMode.healthAddCurr:

                    sourceHealth.AddLocal(input.intValues.Item2, target);
                    break;

                case InputMode.healthDestroy:

                    sourceHealth.DestroyLocal(input.intValues.Item2 == 1 ? true : false, target); 
                    break;

                default:
                    logger.LogError($"[{GetType().Name}] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.health}!");
                    break;
            }
        }
        #endregion

        #region Helper Methods
        private string EntitiesToKeyString(IEnumerable<IEntity> entities)
        {
            return String.Join(".", entities.Select(entity => $"{entity.Key}"));
        }

        private IEnumerable<IEntity> KeyStringToEntities(string inputString)
        {
            if (String.IsNullOrEmpty(inputString))
                return Enumerable.Empty<IEntity>();

            string[] entityKeys = inputString.Split('.');
            return entityKeys.Select(key =>
            {
                spawnedEntities.TryGetValue(Int32.Parse(key), out IEntity entity);
                logger.RequireValid(entity,
                    $"[InputManager] Attempting to break down entity key string into entities and unable to find entity with key: {key}");
                return entity;

            });
        }

        // Temporary handling of int values in an input command.
        public IntValues ToIntValues(int int1) => new IntValues { Item1 = int1 };
        public IntValues ToIntValues(int int1, int int2) => new IntValues { Item1 = int1, Item2 = int2 };
        #endregion
    }
}