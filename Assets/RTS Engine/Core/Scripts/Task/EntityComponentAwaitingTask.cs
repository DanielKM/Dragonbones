using UnityEngine;

using RTSEngine.UI;
using RTSEngine.Game;

namespace RTSEngine.Task
{
    [System.Serializable]
    public class EntityComponentAwaitingTask
    {
        #region Attributes 
        public bool IsEnabled { private set; get; } = false;

        public EntityComponentTaskUIAttributes Current { private set; get; }

        [SerializeField, Tooltip("Change the mouse texture to the awaiting task's icon when it is active?")]
        private bool changeMouseCursor = true;
        [SerializeField, Tooltip("Custom default mouse cursor icon.")]
        private TaskCursorData customCursor = new TaskCursorData();
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            Disable();
        }
        #endregion

        public void Enable (EntityComponentTaskUIAttributes awaitingTask)
        {
            Current = awaitingTask;

            if (changeMouseCursor && Current.data.cursor.icon.IsValid())
            {
                Texture2D nextTexture = Current.data.cursor.icon.texture;

                Cursor.SetCursor(nextTexture, Current.data.cursor.hotspot, CursorMode.Auto);
            }

            IsEnabled = true;
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            if (customCursor.icon.IsValid())
                Cursor.SetCursor(customCursor.icon.texture, customCursor.hotspot, CursorMode.Auto);
            else
                Cursor.SetCursor(null, Vector3.zero, CursorMode.Auto);

            IsEnabled = false;
        }

    }
}
