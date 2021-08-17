using System;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Selection;

namespace RTSEngine.UI
{
    public class SingleSelectionPanelUIHandler : MonoBehaviour, IPreRunGameService
    {
        [SerializeField, Tooltip("The single selection menu, parent of the rest of the following UI objects.")]
        private GameObject panel = null; 
        [SerializeField, Tooltip("Displays the icon of the selected entity.")]
        private Image icon = null; 
        [SerializeField, Tooltip("Displays the name of the selected entity.")]
        private Text nameText = null; 
        [SerializeField, Tooltip("Displays the description of the selected entity.")]
        public Text descriptionText = null; 
        [SerializeField, Tooltip("Displays the active workers of the selected entity.")]
        public Text workersText = null;

        [SerializeField, Tooltip("Displays the amount of health of the selected entity.")]
        private Text healthText = null; 
        [SerializeField, Tooltip("Handles the health bar of the selected entity.")]
        private ProgressBarUI healthBar = new ProgressBarUI();

        // Holds the entity currently displayed in the single selection panel
        private IEntity currEntity;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameUITextDisplayManager textDisplayer { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.textDisplayer = gameMgr.GetService<IGameUITextDisplayManager>(); 

            healthBar.Init(gameMgr);

            globalEvent.EntitySelectedGlobal += HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal += HandleEntitySelectionUpdate;

            Hide();
        }

        public void Disable()
        {
            globalEvent.EntitySelectedGlobal -= HandleEntitySelectionUpdate;
            globalEvent.EntityDeselectedGlobal -= HandleEntitySelectionUpdate;
        }

        private void HandleEntitySelectionUpdate(IEntity entity, EventArgs e)
        {
            if (selectionMgr.Count == 1)
                Show(entity);
            else
                Hide();
        }

        private void Show(IEntity entity)
        {
            //either valid selected entity or one that is not currently being displayed in the panel
            if (!entity.IsValid()
                || entity == currEntity)
                return;

            panel.SetActive(true);

            if (nameText && textDisplayer.EntityNameToText(entity, out string entityName))
                nameText.text = entityName; //display that the entity belongs to a faction or free one?

            if(descriptionText && textDisplayer.EntityDescriptionToText(entity, out string entityDescription)) 
                descriptionText.text = entityDescription;

            if(icon)
                icon.sprite = entity.Icon;

            if (entity.WorkerMgr.IsValid())
            {
                ShowWorkerUI(entity.WorkerMgr);

                entity.WorkerMgr.WorkerAdded += HandleWorkerAdded;
                entity.WorkerMgr.WorkerRemoved += HandleWorkerRemoved;
            }

            ShowHealthUI(entity);
            entity.Health.EntityHealthUpdated += HandleEntityHealthUpdated;

            currEntity = entity;
        }

        private void Hide () 
        {
            panel.SetActive(false);

            HideWorkerUI();
            HideHealthUI();

            if (!currEntity.IsValid())
                return;

            if (currEntity.WorkerMgr.IsValid())
            {
                currEntity.WorkerMgr.WorkerAdded -= HandleWorkerAdded;
                currEntity.WorkerMgr.WorkerRemoved -= HandleWorkerRemoved;
            }

            currEntity.Health.EntityHealthUpdated -= HandleEntityHealthUpdated;

            currEntity = null;
        }

        private void HandleWorkerAdded(IEntity sender, EntityEventArgs<IUnit> e)
        {
            ShowWorkerUI(sender.WorkerMgr);
        }

        private void HandleWorkerRemoved(IEntity sender, EntityEventArgs<IUnit> e)
        {
            ShowWorkerUI(sender.WorkerMgr);
        }

        private void ShowWorkerUI(IEntityWorkerManager workerMgr)
        {
            if(workersText && workerMgr.ShowWorkers)
            {
                workersText.gameObject.SetActive(true);
                workersText.text = $"{workerMgr.Amount} / {workerMgr.MaxAmount}";
            }
        }

        private void HideWorkerUI ()
        {
            workersText.gameObject.SetActive(false);
        }

        private void HandleEntityHealthUpdated(IEntity sender, HealthUpdateEventArgs e)
        {
            ShowHealthUI(sender);
        }

        public void ShowHealthUI(IEntity entity)
        {
            if(healthText) //show the faction entity health:
            {
                healthText.gameObject.SetActive(true);
                healthText.text = entity.Health.CurrHealth.ToString() + "/" + entity.Health.MaxHealth.ToString();
            }

            //health bar:
            healthBar.Toggle(true);

            //Update the health bar:
            healthBar.Update(entity.Health.CurrHealth / (float)entity.Health.MaxHealth);
        }

        //hides the health related UI elements:
        private void HideHealthUI ()
        {
            healthText?.gameObject.SetActive(false);
            healthBar.Toggle(false);
        }
    }
}
