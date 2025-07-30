using Mirror;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct SceneMessage : NetworkMessage
{
    public string sceneName;
}

public static class ElevatorConstants
{
    public const float InitialTimerValue = 15f;
    public const string GameSceneName = "Game";
    public const string LobbySceneName = "Lobby";
    public const int GameSceneBuildIndex = 2;
    public const int MaxPlayers = 2;
    public const float SpawnDelay = 0.1f;
    public const float DecimalTimerThreshold = 5f;
}

public class Elevator : NetworkBehaviour
{
    [SerializeField] private Collider _elevatorCollider;
    [SerializeField] private ElevatorView _view;
    [SerializeField] private GameObject _playerPrefab;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float _currentTimer = ElevatorConstants.InitialTimerValue;

    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    private int _currentPlayerCount = 0;

    private readonly List<NetworkConnectionToClient> _playersInElevator = new();

    public override void OnStartServer()
    {
        StartCoroutine(TimerCoroutine());
        Debug.Log("Elevator: Server initialized, timer started.");
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<SceneMessage>(OnSceneMessageReceived);
        Debug.Log("Elevator: Client initialized, handler for SceneMessage registered.");
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<SceneMessage>();
        Debug.Log($"Elevator: Client disconnected, was active: {NetworkClient.active}");
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

        NetworkConnectionToClient conn = playerIdentity.connectionToClient;
        int connectionId = conn.connectionId;

        if (_playersInElevator.Contains(conn) == false && _playersInElevator.Count < ElevatorConstants.MaxPlayers)
        {
            _playersInElevator.Add(conn);
            _currentPlayerCount = _playersInElevator.Count;

            Debug.Log($"Elevator: Player {playerIdentity.netId} entered elevator, connectionId: {connectionId}, current count: {_currentPlayerCount}");

            if (_playersInElevator.Count == ElevatorConstants.MaxPlayers)
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

        NetworkConnectionToClient conn = playerIdentity.connectionToClient;
        int connectionId = conn.connectionId;

        if (_playersInElevator.Contains(conn))
        {
            _playersInElevator.Remove(conn);
            _currentPlayerCount = _playersInElevator.Count;

            if (other.TryGetComponent(out Collider playerCollider))
                playerCollider.isTrigger = false;

            Debug.Log($"Elevator: Player {playerIdentity.netId} exited elevator, connectionId: {connectionId}, current count: {_currentPlayerCount}");

            if (_playersInElevator.Count < ElevatorConstants.MaxPlayers)
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

                ResetElevatorState();
            }

            yield return null;
        }
    }

    [Server]
    private IEnumerator TransferPlayersToGameScene()
    {
        Debug.Log($"Elevator: Server loading scene {ElevatorConstants.GameSceneName}");

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
            Debug.Log($"Elevator: Server loading progress: {asyncLoad.progress * 100:F1}%");
            yield return null;
        }

        Scene gameScene = SceneManager.GetSceneByBuildIndex(ElevatorConstants.GameSceneBuildIndex);

        if (gameScene.IsValid() == false)
        {
            Debug.LogError($"Elevator: Server loaded scene {ElevatorConstants.GameSceneName} is invalid");
            yield break;
        }

        if (gameScene.isLoaded == false)
        {
            Debug.LogError($"Elevator: Server loaded scene {ElevatorConstants.GameSceneName} is not loaded");
            yield break;
        }

        Debug.Log($"Elevator: Server loaded scene: {gameScene.name}, Players: {_playersInElevator.Count}");

        NetworkConnectionToClient[] players = _playersInElevator.ToArray();

        foreach (NetworkConnectionToClient conn in players)
        {
            if (conn.isAuthenticated == false)
            {
                Debug.LogWarning($"Elevator: Connection {conn.connectionId} is not authenticated, skipping");
                continue;
            }

            if (conn.identity == null)
            {
                Debug.LogWarning($"Elevator: Connection {conn.connectionId} has no identity, skipping");
                continue;
            }

            if (_playerPrefab == null)
            {
                Debug.LogError($"Elevator: PlayerPrefab is not assigned for connection {conn.connectionId}");
                continue;
            }

            GameObject newPlayer = Instantiate(_playerPrefab);
            if (newPlayer == null)
            {
                Debug.LogError($"Elevator: Failed to instantiate player prefab for connection {conn.connectionId}");
                continue;
            }

            NetworkIdentity newPlayerIdentity = newPlayer.GetComponent<NetworkIdentity>();
            if (newPlayerIdentity == null)
            {
                Debug.LogError($"Elevator: Instantiated player prefab for connection {conn.connectionId} is missing NetworkIdentity");
                Destroy(newPlayer);
                continue;
            }

            if (newPlayer.GetComponent<ThirdPersonController>() == null)
            {
                Debug.LogError($"Elevator: Instantiated player prefab for connection {conn.connectionId} is missing ThirdPersonController");
                Destroy(newPlayer);
                continue;
            }

            if (newPlayer.GetComponent<NetworkTransformBase>() == null)
            {
                Debug.LogError($"Elevator: Instantiated player prefab for connection {conn.connectionId} is missing NetworkTransformBase (NetworkTransform or NetworkTransformReliable)");
                Destroy(newPlayer);
                continue;
            }

            newPlayer.transform.position = new Vector3(0f, 1f, 0f);
            SceneManager.MoveGameObjectToScene(newPlayer, gameScene);

            NetworkServer.AddPlayerForConnection(conn, newPlayer);
            yield return new WaitForSeconds(ElevatorConstants.SpawnDelay);

            if (newPlayerIdentity.netId == 0)
            {
                Debug.LogError($"Elevator: Player prefab for connection {conn.connectionId} has invalid netId: 0");
                Destroy(newPlayer);
                continue;
            }

            Debug.Log($"Elevator: Player added for connection {conn.connectionId}, netId: {newPlayerIdentity.netId}, scene: {gameScene.name}, position: {newPlayer.transform.position}");

            conn.Send(new SceneMessage() { sceneName = ElevatorConstants.GameSceneName });
        }

        yield return StartCoroutine(UnloadLobbyScene());
    }

    [Server]
    private IEnumerator UnloadLobbyScene()
    {
        Scene lobbyScene = SceneManager.GetSceneByName(ElevatorConstants.LobbySceneName);
        if (lobbyScene.IsValid() && lobbyScene.isLoaded)
        {
            Debug.Log($"Elevator: Server unloading scene {ElevatorConstants.LobbySceneName}");
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(lobbyScene);
            while (unloadOp.isDone == false)
                yield return null;
        }
    }

    [Client]
    private void OnSceneMessageReceived(SceneMessage msg)
    {
        Debug.Log($"Elevator: Client received SceneMessage, sceneName: {msg.sceneName}");
        StartCoroutine(LoadSceneAsync(msg));
    }

    [Client]
    private IEnumerator LoadSceneAsync(SceneMessage msg)
    {
        Debug.Log($"Elevator: Client loading scene: {msg.sceneName}");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(msg.sceneName, LoadSceneMode.Additive);

        if (asyncLoad == null)
        {
            Debug.LogError($"Elevator: Client failed to start loading scene: {msg.sceneName}");
            yield break;
        }

        while (asyncLoad.isDone == false)
        {
            Debug.Log($"Elevator: Client loading progress: {asyncLoad.progress * 100:F1}% for scene: {msg.sceneName}");
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName(msg.sceneName);

        if (loadedScene.IsValid() == false)
        {
            Debug.LogError($"Elevator: Client loaded scene {msg.sceneName} is invalid");
            yield break;
        }

        if (loadedScene.isLoaded == false)
        {
            Debug.LogError($"Elevator: Client loaded scene {msg.sceneName} is not loaded");
            yield break;
        }

        bool setActiveResult = SceneManager.SetActiveScene(loadedScene);
        if (setActiveResult == false)
        {
            Debug.LogError($"Elevator: Client failed to set active scene: {loadedScene.name}");
            yield break;
        }

        Debug.Log($"Elevator: Client loaded and set active scene: {loadedScene.name}");

        Scene lobbyScene = SceneManager.GetSceneByName(ElevatorConstants.LobbySceneName);
        if (lobbyScene.IsValid() && lobbyScene.isLoaded)
        {
            Debug.Log($"Elevator: Client unloading scene {ElevatorConstants.LobbySceneName}");
            SceneManager.UnloadSceneAsync(lobbyScene);
        }
    }

    [Server]
    private void ResetElevatorState()
    {
        _playersInElevator.Clear();
        _currentPlayerCount = 0;
        _currentTimer = ElevatorConstants.InitialTimerValue;
        _elevatorCollider.isTrigger = true;
        Debug.Log("Elevator: Reset elevator state");
        RpcResetElevatorState();
    }

    [ClientRpc]
    private void RpcResetElevatorState()
    {
        if (_view == null)
        {
            Debug.LogError("Elevator: View is not assigned");
            return;
        }

        _view.SetPlayerCount(0, ElevatorConstants.MaxPlayers);
        _view.SetElapsedTime(ElevatorConstants.InitialTimerValue);
    }

    [Server]
    private void UpdatePlayerColliders()
    {
        foreach (NetworkConnectionToClient conn in _playersInElevator)
        {
            if (conn.identity != null && conn.identity.TryGetComponent(out Collider playerCollider))
                playerCollider.isTrigger = true;
        }
    }

    private void OnTimerChanged(float _, float newValue) =>
        _view?.SetElapsedTime(newValue);

    private void OnPlayerCountChanged(int _, int newValue) =>
        _view?.SetPlayerCount(newValue, ElevatorConstants.MaxPlayers);
}