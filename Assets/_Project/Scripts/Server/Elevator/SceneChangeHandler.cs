using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class SceneChangeHandler : NetworkBehaviour
{
    [TargetRpc]
    public void NotifyClientOfSceneChange(NetworkConnectionToClient _, string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).IsValid())
        {
            Debug.Log($"Scene {sceneName} is already loaded, setting as active.");
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start loading scene {sceneName}. Ensure it is in Build Settings.");
            return;
        }

        asyncLoad.completed += operation =>
        {
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                bool setActiveSuccess = SceneManager.SetActiveScene(loadedScene);
                if (setActiveSuccess == false)
                {
                    Debug.LogError($"Failed to set {sceneName} as active scene.");
                    return;
                }

                if (currentScene.IsValid() && currentScene.name != sceneName)
                {
                    SceneManager.UnloadSceneAsync(currentScene);
                    Debug.Log($"Unloaded old scene {currentScene.name}. Active scene is now {sceneName}.");
                }
            }
            else
            {
                Debug.LogError($"Loaded scene {sceneName} is invalid.");
            }
        };
    }
}