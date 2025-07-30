using Mirror;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct SceneMessage : NetworkMessage
{
    public LoadSceneMode loadMode;
    public int buildIndex;
    public int instanceId;
}

public struct SceneLoadedMessage : NetworkMessage
{
    public int instanceId;
}

public static class ElevatorConstants
{
    public const float InitialTimerValue = 15f;
    public const string GameSceneName = "Game";
    public const string LobbySceneName = "Lobby";
    public const float DecimalTimerThreshold = 5f;
    public const int GameSceneBuildIndex = 2; // Установлен на основе лога
    public const float ClientLoadTimeout = 30f; // Таймаут для загрузки сцены
    public const float PlayerDestroyDelay = 0.5f; // Задержка перед уничтожением старого префаба
}

public class Elevator : NetworkBehaviour
{
    [SerializeField] private int _maxPlayers = 2;
    [SerializeField] private Collider _elevatorCollider;
    [SerializeField] private ElevatorView _view;
    [SerializeField] private GameObject _playerPrefab;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float _currentTimer = ElevatorConstants.InitialTimerValue;

    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    private int _currentPlayerCount = 0;

    private readonly List<int> _playersInElevator = new();
    private readonly Dictionary<int, List<int>> _sceneInstanceToPlayers = new();
    private readonly Dictionary<int, Scene> _sceneInstances = new();
    private readonly Dictionary<int, GameObject> _pendingPlayerPrefabs = new();
    private static int _sceneInstanceCounter = 0;

    public override void OnStartServer()
    {
        if (_view == null)
        {
            Debug.LogError("Elevator: View reference is not assigned.");
            return;
        }

        if (_elevatorCollider == null)
        {
            Debug.LogError("Elevator: Collider reference is not assigned.");
            return;
        }

        if (_playerPrefab == null)
        {
            Debug.LogError("Elevator: PlayerPrefab is not assigned.");
            return;
        }

        if (_playerPrefab.GetComponent<NetworkIdentity>() == null)
        {
            Debug.LogError("Elevator: PlayerPrefab is missing NetworkIdentity component.");
            return;
        }

        StartCoroutine(TimerCoroutine());
        NetworkServer.RegisterHandler<SceneLoadedMessage>(OnSceneLoadedMessageReceived, false);
        Debug.Log("Elevator: Server initialized, handler for SceneLoadedMessage registered.");
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<SceneMessage>(OnSceneMessageReceived, false);
        Debug.Log("Elevator: Client initialized, handler for SceneMessage registered.");
    }

    public override void OnStopClient()
    {
        Debug.Log($"Elevator: Client disconnected, was active: {NetworkClient.active}");
    }

    private void OnSceneLoadedMessageReceived(NetworkConnectionToClient conn, SceneLoadedMessage msg)
    {
        int connectionId = conn.connectionId;
        Debug.Log($"Elevator: Received SceneLoadedMessage from connection {connectionId}, instanceId: {msg.instanceId}");

        if (_sceneInstanceToPlayers.TryGetValue(msg.instanceId, out List<int> players) == false)
        {
            Debug.LogWarning($"Elevator: No players found for instanceId {msg.instanceId}");
            return;
        }

        if (players.Contains(connectionId) == false)
        {
            Debug.LogWarning($"Elevator: Connection {connectionId} not authorized for instanceId {msg.instanceId}");
            return;
        }

        if (_sceneInstances.TryGetValue(msg.instanceId, out Scene gameScene) == false)
        {
            Debug.LogError($"Elevator: Scene instance {msg.instanceId} not found in _sceneInstances");
            return;
        }

        if (gameScene.IsValid() == false)
        {
            Debug.LogError($"Elevator: Scene instance {msg.instanceId} is invalid");
            return;
        }

        if (gameScene.isLoaded == false)
        {
            Debug.LogError($"Elevator: Scene instance {msg.instanceId} is not loaded");
            return;
        }

        if (_pendingPlayerPrefabs.TryGetValue(connectionId, out GameObject oldPlayer) == false || oldPlayer == null)
        {
            Debug.LogError($"Elevator: No pending player prefab found for connection {connectionId}, instanceId: {msg.instanceId}");
            return;
        }

        GameObject newPlayer = Instantiate(_playerPrefab);
        if (newPlayer == null)
        {
            Debug.LogError($"Elevator: Failed to instantiate player prefab for connection {connectionId}");
            return;
        }

        NetworkIdentity newPlayerIdentity = newPlayer.GetComponent<NetworkIdentity>();
        if (newPlayerIdentity == null)
        {
            Debug.LogError($"Elevator: Instantiated player prefab for connection {connectionId} is missing NetworkIdentity");
            Destroy(newPlayer);
            return;
        }

        if (newPlayer.GetComponent<ThirdPersonController>() == null)
        {
            Debug.LogError($"Elevator: Instantiated player prefab for connection {connectionId} is missing ThirdPersonController");
            Destroy(newPlayer);
            return;
        }

        if (newPlayer.GetComponent<NetworkTransform>() == null)
        {
            Debug.LogError($"Elevator: Instantiated player prefab for connection {connectionId} is missing NetworkTransform");
            Destroy(newPlayer);
            return;
        }

        SceneManager.MoveGameObjectToScene(newPlayer, gameScene);
        NetworkServer.ReplacePlayerForConnection(conn, newPlayer, ReplacePlayerOptions.KeepAuthority);

        Debug.Log($"Elevator: Player replaced for connection {connectionId}, old netId: {oldPlayer.GetComponent<NetworkIdentity>().netId}, new netId: {newPlayerIdentity.netId}, scene: {gameScene.name}");

        // Уничтожаем старый префаб с задержкой
        StartCoroutine(DestroyOldPlayerWithDelay(oldPlayer, connectionId));
        _pendingPlayerPrefabs.Remove(connectionId);

        // Отправляем клиенту команду выгрузить сцену Lobby
        TargetUnloadLobbyScene(conn);
    }

    [Server]
    private IEnumerator DestroyOldPlayerWithDelay(GameObject oldPlayer, int connectionId)
    {
        yield return new WaitForSeconds(ElevatorConstants.PlayerDestroyDelay);

        if (oldPlayer != null)
        {
            NetworkIdentity oldPlayerIdentity = oldPlayer.GetComponent<NetworkIdentity>();
            NetworkServer.Destroy(oldPlayer);
            Debug.Log($"Elevator: Destroyed old player prefab for connection {connectionId}, old netId: {oldPlayerIdentity.netId}");
        }
    }

    [TargetRpc]
    private void TargetUnloadLobbyScene(NetworkConnectionToClient target)
    {
        Scene lobbyScene = SceneManager.GetSceneByName(ElevatorConstants.LobbySceneName);
        if (lobbyScene.IsValid() && lobbyScene.isLoaded)
        {
            Debug.Log($"Elevator: Client unloading scene {ElevatorConstants.LobbySceneName}");
            SceneManager.UnloadSceneAsync(lobbyScene);
        }
        else
        {
            Debug.LogWarning($"Elevator: Client could not find or unload scene {ElevatorConstants.LobbySceneName}");
        }
    }

    private void OnSceneMessageReceived(SceneMessage msg)
    {
        Debug.Log($"Elevator: Client received SceneMessage, buildIndex: {msg.buildIndex}, instanceId: {msg.instanceId}");
        StartCoroutine(LoadSceneAsync(msg));
    }

    [Client]
    private IEnumerator LoadSceneAsync(SceneMessage msg)
    {
        Debug.Log($"Elevator: Client loading scene with buildIndex: {msg.buildIndex}, mode: {msg.loadMode}, instanceId: {msg.instanceId}");

        if (msg.buildIndex < 0 || msg.buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Elevator: Invalid buildIndex {msg.buildIndex}, sceneCountInBuildSettings: {SceneManager.sceneCountInBuildSettings}, instanceId: {msg.instanceId}");
            yield break;
        }

        Scene existingScene = SceneManager.GetSceneByBuildIndex(msg.buildIndex);
        if (existingScene.IsValid() && existingScene.isLoaded)
        {
            Debug.Log($"Elevator: Scene with buildIndex {msg.buildIndex} already loaded, unloading before loading new instance");
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(msg.buildIndex);
            if (unloadOp != null)
            {
                while (unloadOp.isDone == false)
                    yield return null;
            }
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(msg.buildIndex, LoadSceneMode.Additive);

        if (asyncLoad == null)
        {
            Debug.LogError($"Elevator: Client failed to start loading scene with buildIndex: {msg.buildIndex}. Scene not found in Build Settings.");
            yield break;
        }

        float startTime = Time.time;
        while (asyncLoad.isDone == false)
        {
            Debug.Log($"Elevator: Client loading progress: {asyncLoad.progress * 100:F1}% for buildIndex: {msg.buildIndex}, instanceId: {msg.instanceId}");
            if (Time.time - startTime > ElevatorConstants.ClientLoadTimeout)
            {
                Debug.LogError($"Elevator: Client timed out loading scene with buildIndex: {msg.buildIndex}, instanceId: {msg.instanceId}");
                yield break;
            }
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByBuildIndex(msg.buildIndex);

        if (loadedScene.IsValid() == false)
        {
            Debug.LogError($"Elevator: Client loaded scene with buildIndex: {msg.buildIndex} is invalid, instanceId: {msg.instanceId}");
            yield break;
        }

        if (loadedScene.isLoaded == false)
        {
            Debug.LogError($"Elevator: Client loaded scene with buildIndex: {msg.buildIndex} is not loaded, instanceId: {msg.instanceId}");
            yield break;
        }

        bool setActiveResult = SceneManager.SetActiveScene(loadedScene);
        if (setActiveResult == false)
        {
            Debug.LogError($"Elevator: Client failed to set active scene: {loadedScene.name}, instanceId: {msg.instanceId}");
            yield break;
        }

        Debug.Log($"Elevator: Client loaded and set active scene: {loadedScene.name}, instanceId: {msg.instanceId}");

        if (NetworkClient.active == false)
        {
            Debug.LogError($"Elevator: Client is not active, cannot send SceneLoadedMessage for instanceId: {msg.instanceId}");
            yield break;
        }

        SceneLoadedMessage loadedMsg = new()
        {
            instanceId = msg.instanceId
        };

        NetworkClient.Send(loadedMsg);
        Debug.Log($"Elevator: Client sent SceneLoadedMessage for instanceId: {msg.instanceId}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isServer == false)
            return;

        if (other.TryGetComponent(out ThirdPersonController _) == false)
            return;

        if (other.TryGetComponent(out NetworkIdentity playerIdentity) == false)
        {
            Debug.LogError($"Elevator: Collider {other.name} has no NetworkIdentity");
            return;
        }

        int connectionId = playerIdentity.connectionToClient.connectionId;
        Debug.Log($"Elevator: PlayerIdentity netId: {playerIdentity.netId}, connectionId: {connectionId}");

        if (_playersInElevator.Contains(connectionId) == false && _playersInElevator.Count < _maxPlayers)
        {
            _playersInElevator.Add(connectionId);
            _currentPlayerCount = _playersInElevator.Count;

            Debug.Log($"Elevator: Player {playerIdentity.netId} entered elevator, connectionId: {connectionId}, current count: {_currentPlayerCount}");

            if (_playersInElevator.Count == _maxPlayers)
            {
                _elevatorCollider.isTrigger = false;
                UpdatePlayerColliders();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isServer == false)
            return;

        if (other.TryGetComponent(out ThirdPersonController _) == false)
            return;

        if (other.TryGetComponent(out NetworkIdentity playerIdentity) == false)
            return;

        int connectionId = playerIdentity.connectionToClient.connectionId;

        if (_playersInElevator.Contains(connectionId))
        {
            _playersInElevator.Remove(connectionId);
            _currentPlayerCount = _playersInElevator.Count;

            if (other.TryGetComponent(out Collider playerCollider))
                playerCollider.isTrigger = false;

            Debug.Log($"Elevator: Player {playerIdentity.netId} exited elevator, connectionId: {connectionId}, current count: {_currentPlayerCount}");

            if (_playersInElevator.Count < _maxPlayers)
                _elevatorCollider.isTrigger = true;
        }
    }

    [Server]
    private IEnumerator TimerCoroutine()
    {
        while (true)
        {
            float deltaTime = Time.deltaTime;
            _currentTimer -= deltaTime;

            if (_currentTimer <= 0f)
            {
                if (_playersInElevator.Count > 0)
                    yield return StartCoroutine(TransferPlayersToGameScene());

                ResetTimerAndPlayers();
            }

            yield return null;
        }
    }

    [Server]
    private IEnumerator TransferPlayersToGameScene()
    {
        int instanceId = _sceneInstanceCounter++;
        Debug.Log($"Elevator: Server loading scene {ElevatorConstants.GameSceneName}, instanceId: {instanceId}");

        if (ElevatorConstants.GameSceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Elevator: Invalid buildIndex {ElevatorConstants.GameSceneBuildIndex}, sceneCountInBuildSettings: {SceneManager.sceneCountInBuildSettings}");
            yield break;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(ElevatorConstants.GameSceneBuildIndex, LoadSceneMode.Additive);

        if (asyncLoad == null)
        {
            Debug.LogError($"Elevator: Server failed to load scene: {ElevatorConstants.GameSceneName}, buildIndex: {ElevatorConstants.GameSceneBuildIndex}");
            yield break;
        }

        while (asyncLoad.isDone == false)
        {
            Debug.Log($"Elevator: Server loading progress: {asyncLoad.progress * 100:F1}% for instanceId: {instanceId}");
            yield return null;
        }

        Scene gameScene = SceneManager.GetSceneByBuildIndex(ElevatorConstants.GameSceneBuildIndex);

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene candidateScene = SceneManager.GetSceneAt(i);
            if (candidateScene.buildIndex == ElevatorConstants.GameSceneBuildIndex && candidateScene.IsValid() && candidateScene.isLoaded)
            {
                gameScene = candidateScene;
                break;
            }
        }

        if (gameScene.IsValid() == false)
        {
            Debug.LogError($"Elevator: Server loaded scene {ElevatorConstants.GameSceneName} is invalid, instanceId: {instanceId}");
            yield break;
        }

        if (gameScene.isLoaded == false)
        {
            Debug.LogError($"Elevator: Server loaded scene {ElevatorConstants.GameSceneName} is not loaded, instanceId: {instanceId}");
            yield break;
        }

        Debug.Log($"Elevator: Server loaded scene: {gameScene.name}, instanceId: {instanceId}, Players: {_playersInElevator.Count}");

        _sceneInstances[instanceId] = gameScene;
        List<int> playersToTransfer = new(_playersInElevator);
        _sceneInstanceToPlayers[instanceId] = playersToTransfer;

        foreach (int connectionId in playersToTransfer)
        {
            if (NetworkServer.connections.TryGetValue(connectionId, out NetworkConnectionToClient connection) == false)
            {
                Debug.LogWarning($"Elevator: Connection {connectionId} not found, skipping for instanceId: {instanceId}");
                continue;
            }

            if (connection.isAuthenticated == false)
            {
                Debug.LogWarning($"Elevator: Connection {connectionId} is not authenticated, skipping for instanceId: {instanceId}");
                continue;
            }

            if (connection.identity == null)
            {
                Debug.LogWarning($"Elevator: Connection {connectionId} has no identity, skipping for instanceId: {instanceId}");
                continue;
            }

            _pendingPlayerPrefabs[connectionId] = connection.identity.gameObject;
            Debug.Log($"Elevator: Stored pending player prefab for connection {connectionId}, netId: {connection.identity.netId}, instanceId: {instanceId}");

            Debug.Log($"Elevator: Sending SceneMessage to connection {connectionId} for instance {instanceId}");
            SceneMessage sceneMsg = new()
            {
                buildIndex = ElevatorConstants.GameSceneBuildIndex,
                loadMode = LoadSceneMode.Additive,
                instanceId = instanceId
            };

            connection.Send(sceneMsg);
            yield return new WaitForSeconds(ElevatorConstants.ClientLoadTimeout);
        }

        // Очистка неподключенных клиентов
        foreach (int connectionId in playersToTransfer)
        {
            if (_pendingPlayerPrefabs.ContainsKey(connectionId))
            {
                Debug.LogWarning($"Elevator: Connection {connectionId} did not respond for instanceId: {instanceId}, removing from pending");
                _pendingPlayerPrefabs.Remove(connectionId);
                _playersInElevator.Remove(connectionId);
                _sceneInstanceToPlayers[instanceId].Remove(connectionId);
            }
        }
    }

    [ClientRpc]
    private void RpcResetElevatorState()
    {
        if (_view == null)
        {
            Debug.LogError("ElevatorView is not assigned.");
            return;
        }

        _view.SetPlayerCount(0, _maxPlayers);
        _view.SetElapsedTime(ElevatorConstants.InitialTimerValue);
    }

    [Server]
    private void ResetTimerAndPlayers()
    {
        foreach (int instanceId in _sceneInstances.Keys)
        {
            Scene scene = _sceneInstances[instanceId];
            if (scene.IsValid() && scene.isLoaded)
            {
                Debug.Log($"Elevator: Unloading scene {scene.name}, instanceId: {instanceId}");
                SceneManager.UnloadSceneAsync(scene);
            }
        }
        _sceneInstances.Clear();
        _sceneInstanceToPlayers.Clear();
        _pendingPlayerPrefabs.Clear();

        List<int> activeConnections = new();
        foreach (int connectionId in _playersInElevator)
        {
            if (NetworkServer.connections.TryGetValue(connectionId, out NetworkConnectionToClient connection) &&
                connection.isAuthenticated &&
                connection.identity != null)
            {
                if (connection.identity.TryGetComponent(out Collider playerCollider))
                    playerCollider.isTrigger = false;
                activeConnections.Add(connectionId);
            }
            else
            {
                Debug.LogWarning($"Elevator: Removing disconnected connection {connectionId} from elevator");
            }
        }
        _playersInElevator.Clear();
        _playersInElevator.AddRange(activeConnections);

        _currentPlayerCount = _playersInElevator.Count;
        _currentTimer = ElevatorConstants.InitialTimerValue;
        _elevatorCollider.isTrigger = true;
        Debug.Log($"Elevator: Reset elevator state, player count: {_currentPlayerCount}");
        RpcResetElevatorState();
    }

    [Server]
    private void UpdatePlayerColliders()
    {
        foreach (int connectionId in _playersInElevator)
        {
            if (NetworkServer.connections.TryGetValue(connectionId, out NetworkConnectionToClient connection) &&
                connection.identity != null &&
                connection.identity.TryGetComponent(out Collider playerCollider))
            {
                playerCollider.isTrigger = true;
            }
        }
    }

    private void OnTimerChanged(float _, float newValue) =>
        _view?.SetElapsedTime(newValue);

    private void OnPlayerCountChanged(int _, int newValue) =>
        _view?.SetPlayerCount(newValue, _maxPlayers);
}