using System;
using System.Linq;

using UnityEngine;

using RTSEngine.Audio;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI;
using RTSEngine.Selection;

namespace RTSEngine.Event
{
    public class EntitySelectionEventArgs : EventArgs
    {
        public SelectionType Type { private set; get; }

        public EntitySelectionEventArgs(SelectionType type)
        {
            this.Type = type;
        }
    }
}

namespace RTSEngine.Selection
{
    public abstract class EntitySelection : MonoBehaviour, IEntitySelection, IEntityPreInitializable
    {
        #region Class Attributes
        public IEntity Entity { private set; get; }

        [SerializeField, Tooltip("Colliders that define how the entity can be selected.")]
        private EntitySelectionCollider[] selectionColliders = new EntitySelectionCollider[0];

        [SerializeField, Tooltip("Can the player select this entity?")]
        private bool isActive = true;
        public bool IsActive { get { return isActive; } protected set { isActive = true; } }

        [SerializeField, Tooltip("Allow the player to select this entity only if it belongs to their faction?")]
        // If this is set to true then only the local player can select the entity associated to this.
        private bool selectOwnerOnly = false; 
        public bool SelectOwnerOnly { get { return selectOwnerOnly; } set { selectOwnerOnly = value; } }

        public bool CanSelect => isActive && !Entity.Health.IsDead && (!SelectOwnerOnly || RTSHelper.IsLocalPlayerFaction(Entity)) && extraSelectCondition;
        protected virtual bool extraSelectCondition => true;

        public bool IsSelected { private set; get; }

        [SerializeField, Tooltip("Audio clip to play when the entity is selected.")]
        protected AudioClipFetcher selectionAudio = new AudioClipFetcher();

        // Game services
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, EntitySelectionEventArgs> Selected;
        public event CustomEventHandler<IEntity, EventArgs> Deselected;

        private void RaiseSelected (EntitySelectionEventArgs args)
        {
            var handler = Selected;
            handler?.Invoke(Entity, args);
        }
        private void RaiseDeselected ()
        {
            var handler = Deselected;
            handler?.Invoke(Entity, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            this.Entity = entity;

            if (!logger.RequireValid(selectionColliders,
                $"[{GetType().Name} - {entity.Code}] 'Selection Colliders' field has unassigned elements!"))
                return;

            foreach (EntitySelectionCollider collider in selectionColliders)
                collider.OnEntityPostInit(gameMgr, Entity);

            IsSelected = false;

            OnInit();
        }

        protected virtual void OnInit() { }

        public void Disable() 
        {
            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Selection Collider(s) Methods
        public bool IsSelectionCollider(Collider collider)
        {
            return selectionColliders.Contains(collider.GetComponent<EntitySelectionCollider>());
        }
        #endregion

        #region Selection State Update
        public void OnSelected(EntitySelectionEventArgs args)
        {
            audioMgr.PlaySFX(selectionAudio.Fetch(), false);
            Entity.SelectionMarker?.Enable();

            IsSelected = true;
            RaiseSelected(args);
        }

        public void OnDeselected ()
        {
            Entity.SelectionMarker?.Disable();

            IsSelected = false;

            RaiseDeselected();
        }
        #endregion

        #region Launching Awaited Tasks
        public void OnAwaitingTaskAction(EntityComponentTaskUIAttributes taskData)
        {
            foreach (var sourceComponent in taskData.sourceTracker.EntityTargetComponents)
                sourceComponent.SetTarget(RTSHelper.ToTargetData(Entity), true);
        }
        #endregion

        #region Launching Direct Action (Right Mouse Click)
        public void OnDirectAction()
        {
            RTSHelper.SetTargetFirstMany(
                selectionMgr.GetEntitiesList(EntityType.all, exclusiveType: false, localPlayerFaction: true),
                RTSHelper.ToTargetData(Entity),
                playerCommand: true,
                includeMovement: false);
        }
        #endregion
    }
}
