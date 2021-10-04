using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField, Tooltip("Disable to force the target scene to load directly without the use of an async operation.")]
    private bool loadAsync = true;

    public string sceneToLoad = "";

    public void LoadNewScene()
    {
        if(!loadAsync)
        {
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        // onSceneLoadStart.Invoke();
        StartCoroutine(LoadSceneAsync(sceneToLoad));
    }

    public void ChangeSceneName(string newSceneName)
    {
        sceneToLoad = newSceneName;
    }

    public void LoadMainMenu(string newSceneName)
    {
        if(!loadAsync)
        {
            SceneManager.LoadScene(newSceneName);
            return;
        }

        // onSceneLoadStart.Invoke();
        StartCoroutine(LoadSceneAsync(newSceneName));
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
