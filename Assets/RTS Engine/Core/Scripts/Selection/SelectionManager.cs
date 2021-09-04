using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Event;

namespace RTSEngine.Selection
{
    public partial class SelectionManager : MonoBehaviour, ISelectionManager
    {
        #region Attributes
        // Key: entity code.
        // Value: list of currently selected entities that share the same code (key).
        private Dictionary<string, List<IEntity>> selectionDic;

        // Caches the entity instance that was first selected among all of the currently selected entities.
        private IEntity firstSelected;

        /// <summary>
        /// Total amount of currently selected entities.
        /// </summary>
        public int Count { private set; get; }

        // Cache the last selected entity type
        private EntityType lastSelectedEntityType = EntityType.all;

        // Is the current selection type exclusive (meaning that only entities of the same type are selected).
        private bool isCurrSelectionExclusive = false;

        [SerializeField, Tooltip("Define selection constraints for entity types.")]
        private EntitySelectionOptions[] selectionOptions = new EntitySelectionOptions[0];

        // Game services
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.mouseSelector = gameMgr.GetService<IMouseSelector>(); 

            // Initial state
            selectionDic = new Dictionary<string, List<IEntity>>();
            Count = 0;
        }
        #endregion

        #region Testing Entity Selection State
        public bool IsSelected(IEntity entity, bool localPlayerFaction = false)
            => selectionDic.TryGetValue(entity.Code, out List<IEntity> selectedList)
            && selectedList.Contains(entity)
            && (!localPlayerFaction || entity.IsLocalPlayerFaction());

        public bool IsSelectedOnly(IEntity entity, bool localPlayerFaction = false)
            => Count == 1
            && firstSelected == entity
            && (!localPlayerFaction || entity.IsLocalPlayerFaction());
        #endregion

        #region Getting Selected Entities
        public IEntity GetSingleSelectedEntity(EntityType requiredType, bool localPlayerFaction = false)
            => IsSelectedOnly(firstSelected, localPlayerFaction) && firstSelected.IsEntityTypeMatch(requiredType)
                ? firstSelected
                : null;

        public IEnumerable<IEntity> GetEntitiesList(EntityType requiredType, bool exclusiveType, bool localPlayerFaction)
        {
            List<IEntity> entities = new List<IEntity>();

            foreach (string entityCode in selectionDic.Keys)
            {
                // Reference entity to test.
                IEntity entity = selectionDic[entityCode][0];

                if (localPlayerFaction && !entity.IsLocalPlayerFaction())
                    return Enumerable.Empty<IEntity>();

                // If this matches the type we're looking for.
                if (entity.IsEntityTypeMatch(requiredType))
                {
                    // Use the Linq.Select method when adding entities range to create a new IEnumerable instance and not tie this one with the one in the selection dictionary
                    entities.AddRange(selectionDic[entityCode].Select(nextEntity => nextEntity));
                    continue;
                }

                // Entity type mismatch and we need all selected entities to be of the same type.
                if (exclusiveType == true)
                    return Enumerable.Empty<IEntity>();
            }

            return entities;
        }

        public IDictionary<string, IEnumerable<IEntity>> GetEntitiesDictionary(EntityType requiredType, bool localPlayerFaction)
        {
            return selectionDic
                .Where(kvp =>
                {
                    return (!localPlayerFaction || kvp.Value[0].IsLocalPlayerFaction())
                        && kvp.Value[0].IsEntityTypeMatch(requiredType);
                })
                // Use the Linq.Select method when adding entities range to create a new IEnumerable instance and not tie this one with the one in the selection dictionary
                .ToDictionary(pair => pair.Key, pair => pair.Value.Select(nextEntity => nextEntity));
        }
        #endregion

        #region Selecting Entities
        public bool Add(IEnumerable<IEntity> entities)
        {
            // ToList() to generate a separate collection to avoid the issue where the input collection is referencing a collection in this class
            // Select each entity individually and launch the selection update event after all entities are selected

            var args = new EntitySelectionEventArgs(SelectionType.multiple);

            IEntity refEntity = null;
            foreach (IEntity entity in entities)
                if (AddInternal(entity, SelectionType.multiple))
                {
                    entity.Selection.OnSelected(args);

                    if (!refEntity.IsValid())
                        refEntity = entity;
                }

            if(refEntity.IsValid())
            {
                globalEvent.RaiseEntitySelectedGlobal(refEntity, args);
                return true;
            }

            return false;
        }

        public bool Add(IEntity entity, SelectionType type)
        {
            if (AddInternal(entity, type))
            {
                var args = new EntitySelectionEventArgs(type);

                entity.Selection.OnSelected(args);

                globalEvent.RaiseEntitySelectedGlobal(entity, args);
                return true;
            }

            return false;
        }

        private bool AddInternal(IEntity entity, SelectionType type)
        {
            if (!entity.IsValid())
                return false;

            // Overwrite the selection type if the player is forcing multiple selection
            if (mouseSelector.MultipleSelectionKeyDown == true)
                type = SelectionType.multiple;

            // If the last selection type was exclusive and this doesn't match the last selected entity type
            // Force single selection now (so that all currently selected entities will be deselected)
            if (isCurrSelectionExclusive && entity.Type != lastSelectedEntityType)
                type = SelectionType.single;

            // Will the selection be marked as exclusive in case the entity is successfully selected? by default no.
            bool exclusiveOnSuccess = false;

            // Find the current entity type selection options and update accordingly
            foreach (EntitySelectionOptions options in selectionOptions)
                if (entity.Type == options.entityType)
                {
                    if (options.exclusive == true)
                    {
                        exclusiveOnSuccess = true;

                        // If the last selected entity does not match with the current type
                        // Force selection type to single (All previous selected entities will be deselected)
                        if (entity.Type != lastSelectedEntityType)
                            type = SelectionType.single;
                    }

                    // If the selection type is multiple but that's not allowed for this entity type
                    // Force selection type to single (All previous selected entities will be deselected)
                    if (type == SelectionType.multiple && options.allowMultiple == false)
                        type = SelectionType.single;

                    break;
                }

            switch (type)
            {
                case SelectionType.single:

                    // Remove all currently selected entities
                    RemoveAll();

                    break;

                case SelectionType.multiple: //multiple selection:

                    // If the multiple selection key is down & entity is already selected -> remove it from the already selected group and stop here.
                    if (mouseSelector.MultipleSelectionKeyDown && IsSelected(entity))
                    {
                        Remove(entity);
                        return false;
                    }

                    break;
            }

            if (entity.Selection.CanSelect && !IsSelected(entity))
            {
                // If there's at least another entity of the same type that is already selected then add this entity to the same list
                if (selectionDic.TryGetValue(entity.Code, out List<IEntity> targetList))
                    targetList.Add(entity);
                else
                    selectionDic.Add(entity.Code, new List<IEntity> { entity });

                Count++;

                // Set firstSelected entity
                if (Count == 1)
                    firstSelected = entity;

                lastSelectedEntityType = entity.Type;
                isCurrSelectionExclusive = exclusiveOnSuccess;

                return true;
            }

            return false;
        }
        #endregion

        #region Deselecting Entities
        public void Remove(IEnumerable<IEntity> entities)
        {
            // ToList() to generate a separate collection to avoid the issue where the input collection is referencing a collection in this class.
            foreach (IEntity entity in entities.ToList())
                Remove(entity);
        }

        public bool Remove(IEntity entity)
        {
            if (selectionDic.TryGetValue(entity.Code, out List<IEntity> selectedList))
            {
                if (!selectedList.Remove(entity))
                    return false;

                Count--;

                // If the selection list is now empty, remove the whole entry from the dictionary
                if (selectedList.Count == 0)
                    selectionDic.Remove(entity.Code);

                if (Count == 1) //if only one entity is left selected, assign it as the single selected entity
                    firstSelected = GetEntitiesList(EntityType.all, false, false).FirstOrDefault();

                entity.Selection.OnDeselected();

                globalEvent.RaiseEntityDeselectedGlobal(entity);

                return true;
            }

            return false;
        }

        public void RemoveAll()
        {
            // Copy keys into new array because the dic will be modified during the foreach loop.
            IEnumerable<string> keys = selectionDic.Keys.ToList();

            Count = 0; //reset selection count
            IEntity refEntity = null; // this would be valid if at least one entity was selected prior to the call of this method

            foreach (string code in keys)
                foreach (IEntity entity in selectionDic[code])
                {
                    entity.Selection.OnDeselected();
                    if (!refEntity.IsValid())
                        refEntity = entity;
                }

            // Reset state
            selectionDic.Clear();
            firstSelected = null;

            if (refEntity.IsValid())
                globalEvent.RaiseEntityDeselectedGlobal(refEntity);
        }
        #endregion
    }
}
