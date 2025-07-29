using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public struct SceneMessage : NetworkMessage
{
    public string sceneName;
    public LoadSceneMode loadMode;
}

public class ElevatorController : NetworkBehaviour
{
    private const float InitialTimerValue = 15f;
    private const float DecimalTimerThreshold = 5f;
    private const int MaxPlayers = 2;
    private const string PlayerTag = "Player";
    private const string GameSceneBaseName = "Game";

    [SerializeField] private TextMeshPro _timerText;
    [SerializeField] private TextMeshPro _playerCountText;
    [SerializeField] private Collider _elevatorCollider;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float _currentTimer = InitialTimerValue;
    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    private int _currentPlayerCount;

    private readonly List<NetworkConnectionToClient> _playersInElevator = new List<NetworkConnectionToClient>();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> _sceneToPlayers = new Dictionary<string, List<NetworkConnectionToClient>>();
    private static int _sceneInstanceCounter = 0;

    public override void OnStartServer()
    {
        StartCoroutine(TimerCoroutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isServer == false)
            return;

        if (other.CompareTag(PlayerTag) == false)
            return;

        NetworkIdentity playerIdentity = other.GetComponent<NetworkIdentity>();
        if (playerIdentity == null)
            return;

        NetworkConnectionToClient connection = playerIdentity.connectionToClient;
        if (_playersInElevator.Contains(connection) == false && _playersInElevator.Count < MaxPlayers)
        {
            _playersInElevator.Add(connection);
            _currentPlayerCount = _playersInElevator.Count;

            if (_playersInElevator.Count == MaxPlayers)
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

        if (other.CompareTag(PlayerTag) == false)
            return;

        NetworkIdentity playerIdentity = other.GetComponent<NetworkIdentity>();
        if (playerIdentity == null)
            return;

        NetworkConnectionToClient connection = playerIdentity.connectionToClient;
        if (_playersInElevator.Contains(connection))
        {
            _playersInElevator.Remove(connection);
            _currentPlayerCount = _playersInElevator.Count;

            if (_playersInElevator.Count < MaxPlayers)
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
        if (gameScene.isLoaded == false)
        {
            Debug.LogError($"ElevatorController: Failed to load game scene {uniqueSceneName}!");
            yield break;
        }

        CustomNetworkManager networkManager = FindObjectOfType<CustomNetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("ElevatorController: CustomNetworkManager not found!");
            yield break;
        }

        List<NetworkConnectionToClient> playersToTransfer = new List<NetworkConnectionToClient>(_playersInElevator);
        _sceneToPlayers[uniqueSceneName] = playersToTransfer;

        foreach (NetworkConnectionToClient connection in playersToTransfer)
        {
            GameObject oldPlayer = connection.identity.gameObject;
            GameObject newPlayer = Instantiate(networkManager.playerPrefab);
            SceneManager.MoveGameObjectToScene(newPlayer, gameScene);

            NetworkServer.ReplacePlayerForConnection(connection, newPlayer, true);
            NetworkServer.Destroy(oldPlayer);

            SceneMessage sceneMsg = new SceneMessage
            {
                sceneName = GameSceneBaseName,
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
            GameObject player = connection.identity.gameObject;
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null)
                playerCollider.isTrigger = true;
        }
    }

    private void OnTimerChanged(float oldValue, float newValue)
    {
        if (newValue <= DecimalTimerThreshold)
            _timerText.text = newValue.ToString("F1") + "s";
        else
            _timerText.text = Mathf.CeilToInt(newValue).ToString() + "s";
    }

    private void OnPlayerCountChanged(int oldValue, int newValue)
    {
        _playerCountText.text = $"{newValue}/{MaxPlayers}";
    }
}