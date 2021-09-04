using RTSEngine.Scene;
using UnityEngine;

namespace RTSEngine.Demo
{
	public class MainMenu : MonoBehaviour {

        [SerializeField]
        private GameObject multiplayerButton = null;
        [SerializeField]
        private GameObject webGLMultiplayerMsg = null;
        [SerializeField]
        private GameObject exitButton = null;

        [SerializeField, Tooltip("Define properties for loading target scenes from this scene.")]
        private SceneLoader sceneLoader = new SceneLoader();

        private void Awake()
        {
#if UNITY_WEBGL
            multiplayerButton.SetActive(false);
            webGLMultiplayerMsg.SetActive(true);
            exitButton.SetActive(false);
#endif
        }

        public void LeaveGame ()
		{
			Application.Quit ();
		}

		public void LoadScene(string sceneName)
		{
            sceneLoader.LoadScene(sceneName, source: this);
		}
	}
}