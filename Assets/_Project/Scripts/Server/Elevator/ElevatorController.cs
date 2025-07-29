using UnityEngine;
using Mirror;
using TMPro;
using System.Collections.Generic;

public class ElevatorController : NetworkBehaviour
{
    private const float TimerDuration = 15f;
    private const int MaxPlayers = 2;
    private const float DecimalTimerThreshold = 5f;
    private const string PlayersTextFormat = "{0}/{1}";
    private const string TimerIntegerFormat = "{0}s";
    private const string TimerDecimalFormat = "{0:F1}s";

    [SerializeField] private TextMeshPro _playersText;
    [SerializeField] private TextMeshPro _timerText;

    private readonly List<NetworkIdentity> _playersInElevator = new();
    private float _timer = TimerDuration;

    [SyncVar(hook = nameof(OnPlayersTextChanged))]
    private string _playersDisplayText = string.Empty;

    [SyncVar(hook = nameof(OnTimerTextChanged))]
    private string _timerDisplayText = string.Empty;

    private void Awake()
    {
        if (_playersText == null)
            Debug.LogError("PlayersText not assigned in ElevatorController.");

        if (_timerText == null)
            Debug.LogError("TimerText not assigned in ElevatorController.");

        UpdatePlayersText();
        UpdateTimerText();
    }

    private void Update()
    {
        if (isServer == false)
            return;

        if (_playersInElevator.Count == 0)
        {
            UpdatePlayersText();
            UpdateTimerText();
            return;
        }

        _timer -= Time.deltaTime;
        UpdatePlayersText();
        UpdateTimerText();

        if (_timer <= 0)
        {
            TransferPlayers();
            ResetElevator();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isServer == false)
            return;

        NetworkIdentity playerIdentity = other.GetComponent<NetworkIdentity>();
        if (playerIdentity == null)
            return;

        if (_playersInElevator.Contains(playerIdentity) == false && _playersInElevator.Count < MaxPlayers)
        {
            _playersInElevator.Add(playerIdentity);
            UpdatePlayersText();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isServer == false)
            return;

        NetworkIdentity playerIdentity = other.GetComponent<NetworkIdentity>();
        if (playerIdentity == null)
            return;

        _playersInElevator.Remove(playerIdentity);
        UpdatePlayersText();
    }

    [Server]
    private void UpdatePlayersText()
    {
        _playersDisplayText = string.Format(PlayersTextFormat, _playersInElevator.Count, MaxPlayers);
    }

    [Server]
    private void UpdateTimerText()
    {
        if (_timer > DecimalTimerThreshold)
            _timerDisplayText = string.Format(TimerIntegerFormat, Mathf.FloorToInt(_timer));
        else
            _timerDisplayText = string.Format(TimerDecimalFormat, _timer);
    }

    [Server]
    private void TransferPlayers()
    {
        if (_playersInElevator.Count == 0)
            return;

        CustomNetworkManager networkManager = NetworkManager.singleton as CustomNetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("CustomNetworkManager singleton not found.");
            return;
        }

        networkManager.MovePlayersToGameScene(_playersInElevator);
    }

    [Server]
    private void ResetElevator()
    {
        _playersInElevator.Clear();
        _timer = TimerDuration;
        UpdatePlayersText();
        UpdateTimerText();
    }

    private void OnPlayersTextChanged(string _, string newText)
    {
        if (_playersText != null)
            _playersText.text = newText;
    }

    private void OnTimerTextChanged(string _, string newText)
    {
        if (_timerText != null)
            _timerText.text = newText;
    }
}