using UnityEngine;
using UnityEngine.SceneManagement;

using RTSEngine.Multiplayer.Utilities;
using System;

namespace RTSEngine.Multiplayer.Server
{
    public class MultiplayerServerManager : MonoBehaviour, IMultiplayerServerManager
    {
        #region Attributes
        public static IMultiplayerServerManager Singleton { private set; get; }

        [SerializeField, Tooltip("Network Address where the server will start.")]
        private string networkAddress = "localhost";
        [SerializeField, Tooltip("Network Port where the server will start.")]
        private string port = "7777";

        public ServerAccessData AccessData => new ServerAccessData
        {
            networkAddress = networkAddress,
            port = port
        };

        [SerializeField, Tooltip("Scene that contains the main multiplayer components that use this component to automatically start the server.")]
        private string mainMultiplayerScene = "main_menu";

        private IMultiplayerManager multiplayerMgr;
        #endregion

        #region Initializing/Terminating
        private void Awake()
        {
            if(!Singleton.IsValid())
            {
                Singleton = this;
            }
            else
            {
                Destroy(this.gameObject);
                return;
            }

            DontDestroyOnLoad(this.gameObject);

            SceneManager.LoadScene(mainMultiplayerScene);

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Starting Server
        public void Execute(IMultiplayerManager multiplayerMgr)
        {
            this.multiplayerMgr = multiplayerMgr;

            this.multiplayerMgr.UpdateServerAccessData(AccessData);

            this.multiplayerMgr.LaunchServer();

            OnExecuted();
        }

        protected virtual void OnExecuted() { }
        #endregion
    }
}
