using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.UI
{
    public class HoverHealthBarUIHandler : MonoBehaviour, IPreRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Enable or disable hover health bars in the game.")]
        private bool isActive = true;
        public bool IsActive
        {
            set
            {
                isActive = value;
                // Disabling hover health bars? Hide the current active one if there is one.
                if (!isActive)
                    Hide();
            }
            get
            {
                return isActive;
            }
        }

        [SerializeField, Tooltip("Enable to only display the hover health bar for the player faction's units and buildings.")]
        private bool playerFactionOnly = true;

        [SerializeField, Tooltip("What types of entites are allowed to have a hover health bar.")]
        private EntityType[] allowEntityTypes = new EntityType[0];

        [SerializeField, EnforceType(sameScene: true), Tooltip("Separate/independent UI canvas to hold the hover health bar UI elements.")]
        private Canvas canvas = null; 
        [SerializeField, Tooltip("Actual UI elements of the hover health bar that are placed in the above canvas.")]
        private ProgressBarUI healthBar = new ProgressBarUI(); 
        
        // The health bar will be showing this selection entity's health when it's enabled.
        // The hover health bar is active only if this is points to a valid entity
        protected IEntity currEntity { private set; get; }

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            if (!logger.RequireValid(canvas,
                $"[{GetType().Name}] The field 'Canvas' must be assigned!"))
                return;

            healthBar.Init(gameMgr);

            Hide();

            globalEvent.EntityMouseEnterGlobal += HandleEntityMouseEnterGlobal;
            globalEvent.EntityMouseExitGlobal += HandleEntityMouseExitGlobal;
        }

        private void OnDestroy ()
        {
            globalEvent.EntityMouseEnterGlobal -= HandleEntityMouseEnterGlobal;
            globalEvent.EntityMouseExitGlobal -= HandleEntityMouseExitGlobal;
        }
        #endregion

        #region Enabling/Disabling Hover Health Bar
        private void HandleEntityMouseEnterGlobal(IEntity entity, EventArgs e) => Enable(entity);
        private void HandleEntityMouseExitGlobal(IEntity entity, EventArgs e) => Hide(entity);
        private void HandleEntityDead(IEntity sender, DeadEventArgs e) => Hide(sender);

        private void Enable(IEntity source)
        {
            if (!isActive
                || !source.IsValid()
                || (allowEntityTypes.Length > 0 && !allowEntityTypes.Contains(source.Type))
                || (playerFactionOnly && !RTSHelper.IsLocalPlayerFaction(source)))
                return;

            currEntity = source; 
             
            canvas.gameObject.SetActive(true);

            // Make the hover health bar canvas a child object of the source entity
            // And update its position so that it shown over the entity
            canvas.transform.SetParent(source.transform, true);
            canvas.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, source.Health.HoverHealthBarY, 0.0f);

            healthBar.Toggle(true);

            // Initial health bar update, later updates will be triggered from the entity's health update event
            UpdateHealthBar(); 

            currEntity.Health.EntityHealthUpdated += HandleEntityHealthUpdated;
            currEntity.Health.EntityDead += HandleEntityDead;
        }

        private void Hide() => Hide(null);

        private void Hide (IEntity source)
        {
            // If there's a current active source and it's not the input one attempting to disable this -> do not disable
            // Since this could be called by the mouse exit event from an entity that is positioned near the entity for which the hover health bar is active
            if (currEntity.IsValid() && currEntity != source) 
                return;

            canvas.gameObject.SetActive(false);
            canvas.transform.SetParent(null, true);

            if (currEntity.IsValid())
            {
                currEntity.Health.EntityHealthUpdated -= HandleEntityHealthUpdated;
                currEntity.Health.EntityDead -= HandleEntityDead;
            }

            currEntity = null;
        }
        #endregion

        #region Handling Hover Health Bar Update
        private void HandleEntityHealthUpdated(IEntity sender, HealthUpdateEventArgs e) => UpdateHealthBar();

        private void UpdateHealthBar()
        {
            if (!currEntity.IsValid()) 
                return;

            healthBar.Update(currEntity.Health.CurrHealth / (float)currEntity.Health.MaxHealth);
        }
        #endregion
    }
}
