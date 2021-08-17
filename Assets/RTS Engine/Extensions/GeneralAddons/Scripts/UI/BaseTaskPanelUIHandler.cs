using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Selection;

namespace RTSEngine.UI
{
    public abstract class BaseTaskPanelUIHandler<T> : MonoBehaviour, IPostRunGameService where T : ITaskUIAttributes
    {
        #region Attributes
        [SerializeField, EnforceType(prefabOnly: true), Tooltip("Prefab of the task to be used as the base task in the task panel.")]
        protected GameObject taskUIPrefab = null;

        // Game services
        protected IGameManager gameMgr { private set; get; } 
        protected IGameLoggingService logger { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (taskUIPrefab != null
                && taskUIPrefab.GetComponent<ITaskUI<T>>() == null)
                taskUIPrefab = null;
        }
#endif
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            if (!logger.RequireValid(taskUIPrefab,
                $"[{GetType().Name}] The 'Task UI Prefab' Field must be assigned!"))
                return;

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            Disable();
        }
        #endregion

        #region Disabling Task Panel
        public abstract void Disable();
        #endregion

        #region Creating Task UI elements
        protected ITaskUI<T> Create(List<ITaskUI<T>> taskList, Transform taskParent)
        {
            var nextTask = UnityEngine.Object.Instantiate(taskUIPrefab)
                .GetComponent<ITaskUI<T>>();
            nextTask.Init(gameMgr, this as IGameService);

            taskList.Add(nextTask);

            nextTask.transform.SetParent(taskParent.transform, true);
            nextTask.transform.localScale = Vector3.one;

            return nextTask;
        }
        #endregion
    }
}
