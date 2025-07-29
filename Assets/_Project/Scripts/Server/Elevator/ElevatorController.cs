using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;
using TMPro;

public class ElevatorController : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _playersCountText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private SceneChangeHandler _sceneChangeHandler;
    [SerializeField] private float _waitTime = GameConstants.ElevatorWaitTime;
    [SerializeField] private int _maxPlayers = GameConstants.MaxPlayersInElevator;

    private readonly List<NetworkIdentity> _playersInElevator = new();
    private readonly SyncList<float> _remainingTime = new();
    private readonly SyncList<int> _currentPlayerCount = new();
    private float _timer;
    private bool _isCounting;

    // Как будто нигде не используется, попробовать убрать
    public bool IsCounting => _isCounting;

    private void Awake()
    {
        if (_playersCountText == null || _timerText == null)
            Debug.LogError("TextMeshPro components not assigned in ElevatorController.");
        if (_sceneChangeHandler == null)
            Debug.LogError("SceneChangeHandler component not assigned in ElevatorController.");
    }

    [Server]
    private void Start()
    {
        _remainingTime.Add(_waitTime);
        _currentPlayerCount.Add(0);
        StartTimer();
    }

    [Server]
    public void AddPlayer(NetworkIdentity player)
    {
        if (_playersInElevator.Contains(player) == false)
        {
            _playersInElevator.Add(player);
            _currentPlayerCount[0] = _playersInElevator.Count;
            UpdateUI();
        }

        if (_playersInElevator.Count >= _maxPlayers)
            MovePlayersToPrivateScene();
    }

    [Server]
    public void RemovePlayer(NetworkIdentity player)
    {
        if (_playersInElevator.Remove(player))
        {
            _currentPlayerCount[0] = _playersInElevator.Count;
            UpdateUI();
        }
    }

    [Server]
    private void Update()
    {
        if (_isCounting == false)
            return;

        _timer -= Time.deltaTime;
        _remainingTime[0] = _timer;

        if (_timer <= 0f)
            MovePlayersToPrivateScene();
    }

    [Server]
    private void StartTimer()
    {
        _timer = _waitTime;
        _isCounting = true;
    }

    [Server]
    private void ResetTimer()
    {
        _playersInElevator.Clear();
        _currentPlayerCount[0] = 0;
        _remainingTime[0] = _waitTime;
        _timer = _waitTime;
        _isCounting = true;
        UpdateUI();
    }

    [Server]
    private void MovePlayersToPrivateScene()
    {
        if (_playersInElevator.Count == 0)
            return;

        _isCounting = false;

        // Эта строчка странная, возможно CustomNetworkManager окажется пустым после приведения типов
        CustomNetworkManager networkManager = NetworkManager.singleton as CustomNetworkManager;

        Scene privateScene = networkManager.CreatePrivateSceneInstance();
        networkManager.MovePlayersToPrivateGameScene(_playersInElevator, privateScene, _sceneChangeHandler);
        ResetTimer();
    }

    [Server]
    private void UpdateUI() =>
        RpcUpdateUI(_currentPlayerCount[0], _remainingTime[0]);

    [ClientRpc]
    private void RpcUpdateUI(int playerCount, float remainingTime)
    {
        _playersCountText.text = $"{playerCount}/{_maxPlayers}";
        _timerText.text = $"{Mathf.CeilToInt(remainingTime)}s";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isServer == false)
            return;
        
        if (other.TryGetComponent(out NetworkIdentity playerIdentity))
            AddPlayer(playerIdentity);
    }

    private void OnTriggerExit(Collider other)
    {
        if (isServer == false)
            return;

        if (other.TryGetComponent(out NetworkIdentity playerIdentity))
            RemovePlayer(playerIdentity);
    }
}