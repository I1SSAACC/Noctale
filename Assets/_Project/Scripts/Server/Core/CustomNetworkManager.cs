using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;

public static class GameConstants
{
    public const string GameSceneName = "Game";
    public const string LobbySceneName = "Lobby";
    public const int GameSceneBuildIndex = 2;
    public const int LobbySceneBuildIndex = 1;
    public const float ElevatorWaitTime = 15f;
    public const int MaxPlayersInElevator = 2;
}

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private SceneInterestManagement _sceneInterestManagement;
    [SerializeField] private GameObject _gamePrefab;
    private static readonly List<Scene> s_activePrivateScenes = new();

    public IReadOnlyList<Scene> ActivePrivateScenes => s_activePrivateScenes;

    private new void Awake()
    {
        if (_sceneInterestManagement == null)
            _sceneInterestManagement = GetComponent<SceneInterestManagement>();

        if (_gamePrefab == null)
            Debug.LogError("Game prefab is not assigned in CustomNetworkManager.");
    }

    [Server]
    public void MovePlayersToPrivateGameScene(List<NetworkIdentity> players, Scene privateScene, SceneChangeHandler sceneChangeHandler)
    {
        if (players.Count == 0)
            return;

        if (privateScene.IsValid() == false)
        {
            Debug.LogError("Invalid private scene provided.");
            return;
        }

        s_activePrivateScenes.Add(privateScene);

        foreach (NetworkIdentity oldPlayer in players)
        {
            if (oldPlayer == null || oldPlayer.connectionToClient == null)
                continue;

            NetworkConnectionToClient connection = oldPlayer.connectionToClient;
            NetworkServer.Destroy(oldPlayer.gameObject);

            GameObject newPlayer = Instantiate(_gamePrefab, Vector3.zero, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(newPlayer, privateScene);
            NetworkServer.Spawn(newPlayer, connection);
            sceneChangeHandler.NotifyClientOfSceneChange(connection, privateScene.name);
        }
    }

    [Server]
    public Scene CreatePrivateSceneInstance()
    {
        string instanceId = System.Guid.NewGuid().ToString();
        Scene privateScene = SceneManager.CreateScene($"Game_{instanceId}",
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(GameConstants.GameSceneBuildIndex, LoadSceneMode.Additive);
        asyncLoad.completed += operation =>
        {
            Scene loadedScene = SceneManager.GetSceneByBuildIndex(GameConstants.GameSceneBuildIndex);
            if (loadedScene.IsValid())
            {
                GameObject[] rootObjects = loadedScene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    SceneManager.MoveGameObjectToScene(obj, privateScene);
                }
                SceneManager.UnloadSceneAsync(GameConstants.GameSceneBuildIndex);
            }
        };

        return privateScene;
    }

    [Server]
    public void UnregisterPrivateScene(Scene privateScene)
    {
        if (privateScene.IsValid() == false)
            return;

        s_activePrivateScenes.Remove(privateScene);
        SceneManager.UnloadSceneAsync(privateScene);
    }
}