using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Task
{
    public class TaskManager : MonoBehaviour, ITaskManager
    {
        #region Attributes
        [SerializeField, Tooltip("Cursor and awaiting task settings")]
        private EntityComponentAwaitingTask awaitingTask = new EntityComponentAwaitingTask();
        public EntityComponentAwaitingTask AwaitingTask => awaitingTask;

        protected IGameManager gameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            awaitingTask.Init(gameMgr);
        }
        #endregion
    }
}