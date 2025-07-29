using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class CustomNetworkManager : NetworkManager
{
    public const string GameSceneName = "Game";

    [SerializeField] private GameObject _playerPrefab;

    private static readonly Dictionary<uint, List<NetworkConnectionToClient>> s_playerGroups = new();
    private static uint s_nextGroupId = 1;

    private void Awake()
    {
        if (_playerPrefab == null)
            Debug.LogError("Player prefab not assigned in CustomNetworkManager.");
    }

    [Server]
    public void MovePlayersToGameScene(List<NetworkIdentity> players)
    {
        if (players.Count == 0)
            return;

        uint groupId = s_nextGroupId++;
        List<NetworkConnectionToClient> connections = new();
        s_playerGroups[groupId] = connections;

        StartCoroutine(MovePlayersCoroutine(players, groupId, connections));
    }

    [Server]
    private IEnumerator MovePlayersCoroutine(List<NetworkIdentity> players, uint groupId, List<NetworkConnectionToClient> connections)
    {
        Scene gameScene = SceneManager.GetSceneByName(GameSceneName);
        if (gameScene.IsValid() == false)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Additive);
            yield return asyncLoad;
            gameScene = SceneManager.GetSceneByName(GameSceneName);
            if (gameScene.IsValid() == false)
            {
                Debug.LogError($"Failed to load scene {GameSceneName}.");
                yield break;
            }
        }

        // Сохраняем соединения игроков группы
        foreach (NetworkIdentity oldPlayer in players)
        {
            if (oldPlayer == null || oldPlayer.connectionToClient == null)
                continue;

            NetworkConnectionToClient connection = oldPlayer.connectionToClient;
            connections.Add(connection);
        }

        // Уведомляем клиентов и переносим игроков
        foreach (NetworkIdentity oldPlayer in players)
        {
            if (oldPlayer == null || oldPlayer.connectionToClient == null)
                continue;

            NetworkConnectionToClient connection = oldPlayer.connectionToClient;

            // Уведомляем клиента о загрузке сцены Game через SceneChangeHandler
            SceneChangeHandler sceneChangeHandler = oldPlayer.GetComponent<SceneChangeHandler>();
            if (sceneChangeHandler == null)
            {
                Debug.LogError($"SceneChangeHandler not found on player {oldPlayer.name}.");
                continue;
            }
            sceneChangeHandler.NotifyClientOfSceneChange(connection, GameSceneName);

            // Даем время клиенту начать загрузку
            yield return new WaitForSeconds(0.1f);

            // Уничтожаем старый объект игрока
            NetworkServer.Destroy(oldPlayer.gameObject);

            // Создаем новый объект игрока в сцене Game
            GameObject newPlayer = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(newPlayer, gameScene);

            NetworkIdentity newPlayerIdentity = newPlayer.GetComponent<NetworkIdentity>();
            if (newPlayerIdentity == null)
            {
                Debug.LogError($"NetworkIdentity not found on new player {newPlayer.name}.");
                Destroy(newPlayer);
                continue;
            }

            // Спавним игрока только для его соединения
            NetworkServer.Spawn(newPlayer, connection);

            // Устанавливаем клиента готовым
            NetworkServer.SetClientReady(connection);
        }

        // Спавним объекты сцены Game для всех соединений группы
        foreach (GameObject sceneObject in gameScene.GetRootGameObjects())
        {
            NetworkIdentity sceneIdentity = sceneObject.GetComponent<NetworkIdentity>();
            if (sceneIdentity == null)
                continue;

            foreach (NetworkConnectionToClient conn in connections)
            {
                if (conn.isReady)
                    NetworkServer.Spawn(sceneObject, conn);
            }
        }
    }

    [Server]
    public void RemovePlayerGroup(uint groupId)
    {
        if (s_playerGroups.ContainsKey(groupId) == false)
            return;

        foreach (NetworkConnectionToClient conn in s_playerGroups[groupId])
        {
            if (conn.identity != null)
                NetworkServer.Destroy(conn.identity.gameObject);
        }

        s_playerGroups.Remove(groupId);
    }
}