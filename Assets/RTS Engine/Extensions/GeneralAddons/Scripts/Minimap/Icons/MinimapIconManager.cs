using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Effect;

namespace RTSEngine.Minimap.Icons
{
    public class MinimapIconManager : MonoBehaviour, IMinimapIconManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IMinimapIcon), prefabOnly: true), Tooltip("Prefab cloned to spawn a new minimap icon")]
        private GameObject prefab = null;

        [SerializeField, Tooltip("How high should the minimap icons be?")]
        private float height = 20.0f;

        [SerializeField, Tooltip("Size the minimap icon that represents each entity."), Min(0.0f)]
        protected float iconSize = 0.5f;

        private Dictionary<int, IMinimapIcon> activeIcons = null;

        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>(); 

            if (!logger.RequireValid(prefab,
                $"[{GetType().Name}] The 'Prefab' field must be assigned!"))
                return;

            activeIcons = new Dictionary<int, IMinimapIcon>();

            // Only display the building's minimap icon when the building is placed.
            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal += HandleEntityFactionUpdateCompleteGlobal;

            globalEvent.EntityVisibilityUpdateGlobal += HandleEntityVisiblityUpdateGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.EntityInitiatedGlobal -= HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal -= HandleEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal -= HandleEntityFactionUpdateCompleteGlobal;

            globalEvent.EntityVisibilityUpdateGlobal -= HandleEntityVisiblityUpdateGlobal;
        }
        #endregion

        #region Handling Events
        private void HandleEntityInitiatedGlobal(IEntity entity, EventArgs args)
        {
            if(entity.IsInteractable)
                Spawn(entity);
        }

        private void HandleEntityDeadGlobal(IEntity entity, EventArgs args)
        {
            Despawn(entity);
        }

        private void HandleEntityFactionUpdateStartGlobal(IEntity entity, FactionUpdateArgs args)
        {
            Despawn(entity);
        }

        private void HandleEntityFactionUpdateCompleteGlobal(IEntity entity, FactionUpdateArgs args)
        {
            Spawn(entity);
        }

        private void HandleEntityVisiblityUpdateGlobal(IEntity entity, VisibilityEventArgs args)
        {
            // Despawn the minimap icon first in all cases
            Despawn(entity);

            // In case the entity is now visible, show the minimap icon
            if(args.IsVisible)
                Spawn(entity);
        }
        #endregion

        #region Spawning/Despawning Minimap Icons
        /// <summary>
        /// Creates a new minimap icon or gets an inactive one.
        /// </summary>
        private IMinimapIcon Spawn(IEntity source)
        {
            if (!logger.RequireTrue(!activeIcons.ContainsKey(source.Key),
              $"[{GetType().Name}] There is already an active minimap icon for the requested entity. Make sure to use Despawn() first!",
              source: source))
                return null;

            IMinimapIcon nextIcon = effectObjPool.Spawn(
                prefab,
                new Vector3(source.transform.position.x, height, source.transform.position.z),
                prefab.transform.rotation,
                null,
                false)
                .GetComponent<IMinimapIcon>();

            activeIcons.Add(source.Key, nextIcon);

            nextIcon.SetColor(source.SelectionColor);

            nextIcon.transform.localScale = Vector3.one * iconSize;
            nextIcon.transform.SetParent(source.transform, true);

            return nextIcon;
        }

        private void Despawn(IEntity source)
        {
            if (activeIcons.TryGetValue(source.Key, out IMinimapIcon icon))
            {
                icon.Deactivate(useDisableTime: false);
                activeIcons.Remove(source.Key);
            }
        }
        #endregion
    }
}