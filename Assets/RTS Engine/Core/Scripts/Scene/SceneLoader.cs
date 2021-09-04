using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace RTSEngine.Scene
{
    [System.Serializable]
    public class SceneLoader 
    {
        [SerializeField, Tooltip("Disable to force the target scene to load directly without the use of an async operation.")]
        private bool loadAsync = true;

        [SerializeField, Tooltip("Triggered when the scene loading process starts.")]
        private UnityEvent onSceneLoadStart = new UnityEvent();

        public SceneLoader()
        {

        }

        public void LoadScene(string sceneName, MonoBehaviour source)
        {
            if(!loadAsync)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            onSceneLoadStart.Invoke();
            source.StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while(!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}
