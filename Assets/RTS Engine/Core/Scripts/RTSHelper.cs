using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Attack;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Lobby;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Multiplayer;
using RTSEngine.Terrain;
using RTSEngine.UI;
using RTSEngine.Utilities;

namespace RTSEngine
{
    public static class RTSHelper
    {
        #region Attributes/Initialization
        public static Color SemiTransparentWhite
        {
            get
            {
                Color color = Color.white;
                color.a = 0.5f;
                return color;
            }
        }

        private static IGameManager GameMgr;
        private static IInputManager InputMgr;
        private static IAttackManager AttackMgr;
        private static IMovementManager MvtMgr;
        public static ILoggingService LoggingService;

        public static void Init(IGameManager gameMgr)
        {
            GameMgr = gameMgr;

            InputMgr = gameMgr.transform.GetComponentInChildren<IInputManager>();
            AttackMgr = gameMgr.transform.GetComponentInChildren<IAttackManager>();
            MvtMgr = gameMgr.transform.GetComponentInChildren<IMovementManager>();

            LoggingService = gameMgr.transform.GetComponentInChildren<ILoggingService>();
        }

        public static void Init(ILobbyManager lobbyMgr)
        {
            LoggingService = lobbyMgr.transform.GetComponent<ILoggingService>();
        }

        public static void Init(IMultiplayerManager multiplayerManager)
        {
            LoggingService = multiplayerManager.transform.GetComponent<ILoggingService>();
        }
        #endregion

        #region General Helper Methods
        public static bool IsValidIndex<T>(this int index, T[] array) => index >= 0 && index < array.Length;
        public static bool IsValidIndex<T>(this int index, List<T> list) => index >= 0 && index < list.Count;

        public static int GetNextIndex<T>(this int index, T[] array) => index >= 0 && index < array.Length - 1 ? index + 1 : 0;
        public static int GetNextIndex<T>(this int index, List<T> list) => index >= 0 && index < list.Count - 1 ? index + 1 : 0;

        public static void ShuffleList<T>(List<T> inputList)
        {
            if(inputList.Count > 0)
            {
                for(int i = 0; i < inputList.Count; i++)
                {
                    int swapID = UnityEngine.Random.Range(0, inputList.Count);
                    if(swapID != i)
                    {
                        T tempElement = inputList[swapID];
                        inputList[swapID] = inputList[i];
                        inputList[i] = tempElement;
                    }
                }
            }
        }

        //Swap two items:
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        public static List<int> GenerateRandomIndexList (int length)
        {
            List<int> indexList = new List<int>();

            int i = 0;
            while (i < length) 
            {
                indexList.Add(i);
                i++;
            }

            ShuffleList(indexList);

            return indexList;
        }

        //Check if a layer is inside a layer mask:
        public static bool IsInLayerMask (LayerMask mask, int layer)
        {
            return ((mask & (1 << layer)) != 0);
        }

        //a method to update the current rotation target
        public static Quaternion GetLookRotation(Transform transform, Vector3 targetPosition, bool reversed = false, bool fixYRotation = true)
        {
            if (reversed)
                targetPosition = transform.position - targetPosition;
            else
                targetPosition -= transform.position;

            if(fixYRotation == true)
                targetPosition.y = 0;
            if (targetPosition != Vector3.zero)
                return Quaternion.LookRotation(targetPosition);
            else
                return transform.rotation;
        }

        /// <summary>
        /// Sets the rotation of a Transform instance to the direction opposite from a Vector3 position.
        /// </summary>
        /// <param name="transform">Transform instance to set rotation for.</param>
        /// <param name="awayFrom">Vector3 position whose opposite direction the transform will look at.</param>
        public static void LookAwayFrom (Transform transform, Vector3 awayFrom, bool fixYRotation = false)
        {
            if (fixYRotation)
                awayFrom.y = transform.position.y;

            transform.LookAt(2 * transform.position - awayFrom);
        }

        //a method that converts time in seconds to a string MM:SS
        public static string TimeToString (float time)
        {
            if (time <= 0.0f)
                return "00:00";

            int seconds = Mathf.RoundToInt (time);
            int minutes = Mathf.FloorToInt (seconds / 60.0f);

            seconds -= minutes * 60;

            string minutesText = (minutes < 10 ? "0" : "") + minutes.ToString();
            string secondsText = (seconds < 10 ? "0" : "") + seconds.ToString();

            return minutesText + ":" + secondsText;
        }

        public static bool In<T>(this T obj, IEnumerable<T> args)
        {
            return args.Contains(obj);
        }

        //finds the index of an element inside a IReadOnlyList that satisfies a certain 'match' condition
        public static int FindIndex<T>(IReadOnlyList<T> list, Predicate<T> match)
        {
            int i = 0;
            foreach(T element in list)
            {
                if(match(element))
                    return i;
                i++;
            }
            return -1;
        }

        //finds the index of an element inside a IReadOnlyList
        public static int IndexOf<T>(IReadOnlyList<T> list, T elementToFind)
        {
            int i = 0;
            foreach(T element in list)
            {
                if(Equals(element, elementToFind))
                    return i;
                i++;
            }
            return -1;
        }

        public static void UpdateDropdownValue(ref Dropdown dropdownMenu, string lastOption, List<string> newOptions)
        {
            dropdownMenu.ClearOptions();
            dropdownMenu.AddOptions(newOptions);

            for(int i = 0; i < newOptions.Count; i++)
                if(newOptions[i] == lastOption)
                    dropdownMenu.value = i;

            dropdownMenu.value = 0;
        }

        public static bool IsPrefab(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            return
                !gameObject.scene.IsValid() &&
                !gameObject.scene.isLoaded &&
                gameObject.GetInstanceID() >= 0 &&
                // I noticed that ones with IDs under 0 were objects I didn't recognize
                !gameObject.hideFlags.HasFlag(HideFlags.HideInHierarchy);
                // I don't care about GameObjects *inside* prefabs, just the overall prefab.
        }
        #endregion

        #region RTS Engine General Helper Methods
        /// <summary>
        /// Find the first interface that implements a service interface in an implementation or returns the implementing class if it directly implements a service.
        /// </summary>
        /// <param name="implementation"></param>
        /// <returns></returns>
        public static Type GetSuperInterfaceType <T>(this Type implementation)
        {
            // Making sure that the implementation type is not an interface.
            if (!LoggingService.RequireTrue(!implementation.IsInterface,
                    $"[RTSHelper] You are not allowed to use the 'GetGameServ helper method with an interface type."))
                return null;

            // Get all interfaces of the implemetation type, except the IGameService one
            // Because if a higher level interface implements the IGameService one then both will appear from using the GetInterfaces() method.
            Type interfaceType = implementation
                .GetInterfaces()
                .Where(nextInterface => !nextInterface.Equals(typeof(T)))
                .FirstOrDefault(nextInterface => nextInterface.GetInterfaces().ToList().Contains(typeof(T)));

            // Goal here is to find the first interface from the above collection that implements IGameService at any levels of its interface implementation levels.
            return interfaceType.IsValid() ? interfaceType : implementation;
        }

        public static bool IsTerrainAreaOverlap (this IEnumerable<TerrainAreaType> a, IEnumerable<TerrainAreaType> b)
        {
            return !a.Any() 
                || !b.Any()
                || a.Intersect(b).Any();
        }

        /// <summary>
        /// Determines whether an entity instance belongs to the local player or not.
        /// </summary>
        /// <param name="entity">IEntity instance to test.</param>
        /// <returns>True if the entity belongs to the local player, otherwise false.</returns>
        public static bool HasAuthority(this IEntity entity)
        {
            return entity.IsValid()
                && (IsMasterInstance() || IsLocalPlayerFaction(entity));
        }

        public static bool HasAuthority(this IEnumerable<IEntity> entities)
        {
            return entities.All(instance => instance.IsValid())
                && (IsMasterInstance() || IsLocalPlayerFaction(entities));
        }

        public static bool IsMasterInstance()
        {
            return GameMgr.IsValid()
                && (!GameMgr.CurrBuilder.IsValid() || GameMgr.CurrBuilder.IsMaster);
        }

        public static bool IsLocalPlayerFaction(this IEntity entity) => 
            entity.IsValid() && !entity.IsFree && entity.Slot.Data.isLocalPlayer;

        public static bool IsLocalPlayerFaction(this IEnumerable<IEntity> entities) => 
            entities
            .All(instance => instance.IsValid()
                && !instance.IsFree 
                && instance.Slot.Data.isLocalPlayer);

        public static bool IsLocalPlayerFaction(this IFactionSlot factionSlot)
            => factionSlot.IsValid() && factionSlot.Data.isLocalPlayer;

        public static bool IsLocalPlayerFaction(int factionID) => 
            GameMgr.GetFactionSlot(factionID).Data.isLocalPlayer;

        public static bool IsFactionEntity(this IEntity entity, int factionID) 
            => entity.IsValid() && entity.FactionID == factionID;

        public static bool IsSameFaction(this IEntity entity1, IEntity entity2) 
            => entity1.IsValid() && entity2.IsValid() && entity1.FactionID == entity2.FactionID;
        public static bool IsSameFaction(this IEntity entity1, int factionID) 
            => entity1.IsValid()  && entity1.FactionID == factionID;
        public static bool IsSameFaction(this IFactionManager factionMgr, IEntity entity) 
            => factionMgr.IsValid() && entity.IsValid() && entity.FactionID == factionMgr.FactionID;
        public static bool IsSameFaction(this IFactionManager factionMgr, int factionID) 
            => factionMgr.IsValid() && factionID == factionMgr.FactionID;
        public static bool IsSameFaction(this IFactionSlot factionSlot, IEntity entity)
            => factionSlot.IsValid() && entity.IsValid() && entity.FactionID == factionSlot.ID;
        public static bool IsSameFaction(this int factionID1, int factionID2) => factionID1 == factionID2;

        public static bool IsFriendlyFaction(this IEntity source, IEntity target)
            => source.IsValid() && target.IsValid()
            && (target.Type == EntityType.resource
                || source.FactionID == target.FactionID);

        public static bool IsFriendlyFaction(this IEntity source, int factionID)
            => source.IsValid()
            && (source.Type == EntityType.resource
                || source.FactionID == factionID);

        public static bool IsFriendlyFaction(this IEntity source, IFactionSlot slot)
            => source.IsValid() && slot.IsValid()
            && (source.Type == EntityType.resource
                || source.FactionID == slot.ID);

        public static bool IsFriendlyFaction(this int sourceFactionID, int targetFactionID) => sourceFactionID == targetFactionID;

        public static bool IsValidFaction(this int factionID) => factionID >= 0 && factionID < GameMgr.FactionCount;

        public static bool IsActiveFaction(this IFactionSlot factionSlot) 
            => factionSlot.IsValid() && factionSlot.State == FactionSlotState.active;

        public static bool IsNPCFaction(this IEntity entity)
            => entity.IsValid() && !entity.IsFree && entity.Slot.Data.role == FactionSlotRole.npc;

        public static bool IsNPCFaction(this IFactionSlot factionSlot)
            => factionSlot.IsValid() && factionSlot.Data.role == FactionSlotRole.npc;

        // Disabled since it conflicts with the IsValid method that takes System.Object as parameter
        /*public static bool IsValid(this IMonoBehaviour monoBehaviour)
            => monoBehaviour != null && !monoBehaviour.Equals(null);*/

        public static bool IsValid(this UnityEngine.Object obj)
            => obj != null && !obj.Equals(null);

        public static bool IsValid(this System.Object obj)
            => obj != null && !obj.Equals(null);

        public static IEnumerable<T> FromGameObject<T>(this IEnumerable<GameObject> gameObjects) where T : IMonoBehaviour
            => gameObjects.Select(obj => obj.IsValid() ? obj.GetComponent<T>() : default);

        public static bool IsEntityTypeMatch(this IEntity entity, EntityType testType)
            => testType == EntityType.all || entity.Type == testType;

        /// <summary>
        /// Searches for a building center that allows the given building type to be built inside its territory.
        /// </summary>
        /// <param name="building">Code of the building type to place/build.</param>
        /// <returns></returns>
        public static bool GetEntityFirst<T>(this IEnumerable<T> set, out T entity, Func<T, bool> condition) where T : IEntity
        {
            //if(center.BorderComponent.AllowBuildingInBorder(code))
            entity = default;

            foreach(T instance in set)
                if(condition(instance))
                {
                    entity = instance;
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Sorts a set of instances that extend the IEntity interface into a ChainedSortedList based on the entities code.
        /// </summary>
        /// <typeparam name="T">A type that extends IEntity.</typeparam>
        /// <param name="allComponents">An IEnumerable of instances that extend the IEntity interface.</param>
        /// <param name="filter">Determines what entities are eligible to be added to the chained sorted list and which are not.</param>
        /// <returns>ChainedSortedList instance of the sorted entities based on their code.</returns>
        public static ChainedSortedList<string, T> SortEntitiesByCode <T> (IEnumerable<T> allComponents, System.Func<T, bool> filter) where T : IEntity
        {
            //this will hold the resulting chained sorted list.
            ChainedSortedList<string, T> sortedComponents = new ChainedSortedList<string, T>();

            //go through the input entities
            foreach(T comp in allComponents)
                if(filter(comp)) //only if the entity returns true according to the assigned filter
                    sortedComponents.Add(comp.Code, comp); //and add them based on their code

            return sortedComponents;
        }

        /// <summary>
        /// Gets the direction of a list of entities in regards to a target position.
        /// </summary>
        /// <param name="entities">List of IEntity instances.</param>
        /// <param name="targetPosition">Vector3 that represents the position the entities will get their direction to.</param>
        /// <returns>Vector3 that represents the direction of the entities towards the target position.</returns>
        public static Vector3 GetEntitiesDirection (IEnumerable<IEntity> entities, Vector3 targetPosition)
        {
            Vector3 direction = Vector3.zero;
            int count = 0;

            foreach (IEntity entity in entities) //make a sum of each unit's direction towards the target position
            {
                direction += (targetPosition - entity.transform.position).normalized;
                count++;
            }

            return direction / count;
        }

        public static TargetData<T> ToTargetData<T>(this T entity) where T : IEntity
        {
            return entity.IsValid()
                ? new TargetData<T> { instance = entity, position = entity.transform.position }
                : Vector3.zero;
        }

        //Tests whether a set of faction entities are spawned with a certain amount for a particular faction.
        public static bool TestFactionEntityRequirements (this IEnumerable<FactionEntityRequirement> requirements, IFactionManager factionMgr)
            => requirements.All(req => req.TestFactionEntityRequirement(factionMgr));

        public static bool TestFactionEntityRequirement (this FactionEntityRequirement req, IFactionManager factionMgr)
        {
            int requiredAmount = req.amount;

            foreach (IFactionEntity factionEntity in factionMgr.FactionEntities)
            {
                // When a faction entity can launch tasks, it means it is viable to be used
                // For example, buildings can only launch tasks when they are constructed
                if (!factionEntity.CanLaunchTask)
                    continue;

                if (req.codes.Contains(factionEntity.Code, factionEntity.Category))
                    requiredAmount--;

                if (requiredAmount <= 0)
                    return true;
            }

            return requiredAmount < 0;
        }

        public static T GetClosestEntity<T> (Vector3 searchPosition, IEnumerable<T> entities) where T : IEntity
        {
            return entities
                .OrderBy(entity => (entity.transform.position - searchPosition).sqrMagnitude)
                .FirstOrDefault();
        }

        public static T GetClosestEntity<T> (Vector3 searchPosition, IEnumerable<T> entities, System.Func<T, bool> condition) where T : IEntity
        {
            return entities
                .Where(entity => condition(entity))
                .OrderBy(entity => (entity.transform.position - searchPosition).sqrMagnitude)
                .FirstOrDefault();
        }

        public static IEnumerable<T> FilterEntities<T>(IEnumerable<T> entities, System.Func<T, bool> condition) where T : IEntity
        {
            return entities
                .Where(entity => condition(entity));
        }

        public static bool Contains(this IEnumerable<EntityType> types, EntityType testType)
            => types.Any(type => type == EntityType.all || type == testType);

        public static IFactionSlot ToFactionSlot(this int factionID)
            => GameMgr.GetFactionSlot(factionID);

        public static bool TryGameInit(this Action<IGameManager> sourceInitMethod)
        {
            if (!LoggingService.RequireValid(GameMgr,
                $"[RTSHelper] Unable to initialize without a valid '{typeof(IGameManager).Name}' instance! This can only be called when a game is active."))
                return false;

            sourceInitMethod(GameMgr);

            return true;
        }
        #endregion

        #region RTS Engine Entity Component Helper Methods
        public static bool TryGetEntityComponentWithCode (IEntity entity, string code, out IEntityComponent component)
        {
            component = null;

            if (!entity.IsValid())
                return false;

            component = entity.transform
                .GetComponentsInChildren<IEntityComponent>()
                .Where(c => c.Code == code)
                .FirstOrDefault();

            return component.IsValid();
        }

        public static bool OnSingleTaskUIRequest(
            IEntityComponent entityComponent, out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes, EntityComponentTaskUIAsset taskUIAsset, bool extraCondition = true)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            disabledTaskCodes = Enumerable.Empty<string>();

            if (!entityComponent.Entity.CanLaunchTask
                || !entityComponent.IsActive
                || !entityComponent.Entity.IsLocalPlayerFaction())
                return false;

            if(taskUIAsset.IsValid()
                && extraCondition)
                taskUIAttributes = taskUIAttributes.Append(
                    new EntityComponentTaskUIAttributes
                    {
                        data = taskUIAsset.Data,

                        locked = false
                    });

            return true;
        }
        public static ErrorMessage SetEntityComponentActive(IEntityComponent entityComponent, bool active, bool playerCommand)
        {
            CommandInput newInput = new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.setComponentActive,

                playerCommand = playerCommand,

                code = entityComponent.Code,

                intValues = InputMgr.ToIntValues(active ? 1 : 0)
            };

            return InputMgr.SendInput(newInput, entityComponent.Entity, null);
        }

        public static ErrorMessage LaunchEntityComponentAction (IEntityComponent entityComponent, byte actionID, TargetData<IEntity> target, bool playerCommand)
        {
            return InputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.launchComponentAction,

                sourcePosition = entityComponent.Entity.transform.position,
                targetPosition = target.position,
                opPosition = target.opPosition,

                intValues = InputMgr.ToIntValues(actionID),

                code = entityComponent.Code,
                playerCommand = playerCommand
            },
            source: entityComponent.Entity,
            target: target.instance);
        }

        public static void SetTargetFirstMany(this IEnumerable<IEntity> entities, TargetData<IEntity> target, bool playerCommand, bool includeMovement = false)
        {
            var entityGroups = entities
                .GroupBy(entity =>
                {
                    if (entity.CanAttack && entity.AttackComponent.IsTargetValid(target, playerCommand) == ErrorMessage.none)
                        return 0;
                    else if (includeMovement && entity.CanMove)
                        return 1;
                    else
                        return -1;
                });

            foreach(var group in entityGroups)
            {
                switch(group.Key)
                {
                    case 0:

                        AttackMgr.LaunchAttack(
                            new LaunchAttackData<IEnumerable<IEntity>>
                            {
                                source = group,
                                targetEntity = target.instance as IFactionEntity,
                                targetPosition = target.instance.IsValid() ? target.instance.transform.position : target.position,
                                playerCommand = playerCommand
                            });

                        break;

                    case 1:

                        MvtMgr.SetPathDestination(
                            group,
                            target.position,
                            target.instance.IsValid() ? target.instance.Radius : 0.0f,
                            target.instance,
                            playerCommand);

                        break;

                    default:

                        foreach (IEntity entity in group)
                            entity.SetTargetFirst(target, playerCommand);

                        break;
                }
            }
        }

        public delegate ErrorMessage IsTargetValidDelegate(TargetData<IEntity> target, bool playerCommand);
        #endregion

        #region RTS Engine Attack Helper Methods
        public static Vector3 GetAttackTargetPosition(TargetData<IFactionEntity> target)
            => target.instance.IsValid() ? target.instance.Selection.transform.position : target.opPosition;

        public static ErrorMessage IsAttackLOSBlocked (PathDestinationInputData pathDestinationInput, Vector3 testPosition)
        {
            IEntity entity = pathDestinationInput.refMvtComp?.Entity;
            if (entity == null
                || !entity.CanAttack)
                return ErrorMessage.undefined;

            return entity.AttackComponent.LineOfSight.IsObstacleBlocked(testPosition, pathDestinationInput.targetPosition)
                ? ErrorMessage.LOSObstacleBlocked
                : ErrorMessage.none;
        }
        #endregion

        #region RTS Engine Determinism Helper Methods

        #endregion
    }
}

