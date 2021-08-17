using UnityEngine;
using UnityEngine.EventSystems;

using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Selection
{
    [RequireComponent(typeof(Collider))]
    public class EntitySelectionCollider : MonoBehaviour, IEntityPostInitializable
    {
        #region Attributes
        public IEntity Entity { private set; get; }

        // Game services
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            this.Entity = entity;

            // Selection objects must be direct children of their parent entity objects
            // Parent entity objects will be set to ignore raycast layer.
            // While selection collider objects will be assigned the entity selection layer defined in the mouse selection layer mask
            gameObject.layer = LayerMask.NameToLayer(gameMgr.GetService<IMouseSelector>().EntitySelectionLayer);

            Collider collider = GetComponent<Collider>();
            if (!logger.RequireValid(collider,
                $"[{GetType().Name} - {Entity.Code}] A Collider must be attached for the selection to work."))
                return;

            // In order for collision detection to work, we must assign the following settings to the collider and rigidbody.
            collider.isTrigger = true;
            collider.enabled = true;

            Rigidbody rigidbody = GetComponent<Rigidbody>();

            if (rigidbody == null)
                rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        public void Disable() { }
        #endregion

        #region Handling MouseEnter/MouseExit
        void OnMouseEnter()
        {
            if (!Entity.Health.IsDead
                && Entity.Selection.IsActive
                && !EventSystem.current.IsPointerOverGameObject()
                && !placementMgr.IsPlacingBuilding) 
                globalEvent.RaiseEntityMouseEnterGlobal(Entity);
        }

        void OnMouseExit()
        {
            globalEvent.RaiseEntityMouseExitGlobal(Entity);
        }
        #endregion
    }
}
