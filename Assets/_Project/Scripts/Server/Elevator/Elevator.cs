using Mirror;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct SceneMessage : NetworkMessage
{
    public LoadSceneMode loadMode;
    public string sceneName;
}

public struct SceneLoadedMessage : NetworkMessage
{
    public string sceneName;
}

public class Elevator : NetworkBehaviour
{
    private const float InitialTimerValue = 15f;
    private const string GameSceneBaseName = "Game";

    [SerializeField] private int _maxPlayers = 2;
    [SerializeField] private Collider _elevatorCollider;
    [SerializeField] private ElevatorView _view;
    [SerializeField] private GameObject _playerPrefab;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float _currentTimer = InitialTimerValue;

    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    private int _currentPlayerCount = 0;

    private readonly List<NetworkConnectionToClient> _playersInElevator = new();

    private readonly Dictionary<string, List<NetworkConnectionToClient>> _sceneToPlayers = new();
    private static int _sceneInstanceCounter = 0;

    public override void OnStartServer()
    {
        StartCoroutine(TimerCoroutine());
        NetworkServer.RegisterHandler<SceneLoadedMessage>(OnSceneLoadedMessageReceived, false);
    }

    public override void OnStartClient() =>
        NetworkClient.RegisterHandler<SceneMessage>(OnSceneMessageReceived, false);

    private void OnSceneLoadedMessageReceived(NetworkConnectionToClient conn, SceneLoadedMessage msg)
    {
        Debug.Log($"ÅÑËÈ ÝÒÎ ÑÐÀÁÀÒÛÂÀÅÒ, ÓÆÅ ÍÅÏËÎÕÎ");  

        //if (_sceneToPlayers.TryGetValue(msg.sceneName, out var players) && players.Contains(conn))
        //{
            GameObject oldPlayer = conn.identity.gameObject;
            GameObject newPlayer = Instantiate(_playerPrefab);
            Scene gameScene = SceneManager.GetSceneByName(msg.sceneName);
            SceneManager.MoveGameObjectToScene(newPlayer, gameScene);

            NetworkServer.ReplacePlayerForConnection(conn, newPlayer, ReplacePlayerOptions.KeepAuthority);
            NetworkServer.Destroy(oldPlayer);
        //}
    }

    private void OnSceneMessageReceived(SceneMessage msg) =>
        StartCoroutine(LoadSceneAsync(msg));

    [Client]
    private IEnumerator LoadSceneAsync(SceneMessage msg)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(msg.sceneName, msg.loadMode);

        if (asyncLoad == null)
        {
            Debug.LogError($"Client failed to load scene: {msg.sceneName}. Scene not found in Build Settings.");
            yield break;
        }

        while (asyncLoad.isDone == false)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(msg.sceneName);

        if (loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
            Debug.Log($"Client loaded and set active scene: {msg.sceneName}");
        }
        else
        {
            Debug.LogError($"Client failed to load scene: {msg.sceneName}");
            yield break;
        }

        SceneLoadedMessage loadedMsg = new()
        {
            sceneName = msg.sceneName
        };

        NetworkClient.Send(loadedMsg);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isServer == false)
            return;

        if (other.TryGetComponent(out ThirdPersonController _) == false)
            return;

        if (other.TryGetComponent(out NetworkIdentity playerIdentity) == false)
            return;

        NetworkConnectionToClient connection = playerIdentity.connectionToClient;

        if (_playersInElevator.Contains(connection) == false && _playersInElevator.Count < _maxPlayers)
        {
            _playersInElevator.Add(connection);
            _currentPlayerCount = _playersInElevator.Count;

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

        if (!other.TryGetComponent(out NetworkIdentity playerIdentity))
            return;

        NetworkConnectionToClient connection = playerIdentity.connectionToClient;

        if (_playersInElevator.Contains(connection))
        {
            _playersInElevator.Remove(connection);
            _currentPlayerCount = _playersInElevator.Count;

            if (_playersInElevator.Count < _maxPlayers)
                _elevatorCollider.isTrigger = true;
        }
    }

    [Server]
    private IEnumerator TimerCoroutine()
    {
        while (true)
        {
            _currentTimer -= Time.deltaTime;

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
        string uniqueSceneName = $"{GameSceneBaseName}_{_sceneInstanceCounter++}";
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(GameSceneBaseName, LoadSceneMode.Additive);

        while (asyncLoad.isDone == false)
            yield return null;

        Scene gameScene = SceneManager.GetSceneByName(GameSceneBaseName);
        
        Debug.Log($"Server loaded scene: {uniqueSceneName}, isLoaded: {gameScene.isLoaded}");

        if (gameScene.isLoaded == false)
        {
            Debug.LogError($"Elevator: Failed to load game scene {uniqueSceneName}!");
            yield break;
        }

        List<NetworkConnectionToClient> playersToTransfer = new(_playersInElevator);
        _sceneToPlayers[uniqueSceneName] = playersToTransfer;

        foreach (NetworkConnectionToClient connection in playersToTransfer)
        {
            SceneMessage sceneMsg = new()
            {
                sceneName = GameSceneBaseName, // Èñïîëüçóåì áàçîâîå èìÿ ñöåíû, òàê êàê óíèêàëüíûå èìåíà íå ïîääåðæèâàþòñÿ
                loadMode = LoadSceneMode.Additive
            };

            connection.Send(sceneMsg);
            yield return new WaitForEndOfFrame();
        }
    }

    [Server]
    private void ResetTimerAndPlayers()
    {
        _playersInElevator.Clear();
        _currentPlayerCount = 0;
        _currentTimer = InitialTimerValue;
        _elevatorCollider.isTrigger = true;
    }

    [Server]
    private void UpdatePlayerColliders()
    {
        foreach (NetworkConnectionToClient connection in _playersInElevator)
        {
            if (connection.identity.TryGetComponent(out Collider playerCollider))
                playerCollider.isTrigger = true;
        }
    }

    private void OnTimerChanged(float _, float newValue) =>
        _view.SetElapsedTime(newValue);

    private void OnPlayerCountChanged(int _, int newValue) =>
        _view.SetPlayerCount(newValue, _maxPlayers);
}