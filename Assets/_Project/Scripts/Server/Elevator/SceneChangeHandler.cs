using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class SceneChangeHandler : NetworkBehaviour
{
    [TargetRpc]
    public void NotifyClientOfSceneChange(NetworkConnectionToClient target, string sceneName)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.completed += operation =>
        {
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
                if (currentScene.name != NetworkManager.singleton.onlineScene)
                    SceneManager.UnloadSceneAsync(currentScene);
            }
        };
    }
}