using Mirror;
using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : NetworkBehaviour
{
    public const float InitialTimerValue = 15f;

    [SerializeField] private Collider _elevatorCollider;
    [SerializeField] private ElevatorView _view;
    [SerializeField] private int _maxPlayer = 1;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float _currentTimer = InitialTimerValue;

    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    private int _currentPlayerCount = 0;

    private readonly List<NetworkConnectionToClient> _playersInElevator = new();

    public event Action<List<NetworkConnectionToClient>> PlayersReadyToTransfer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(TimerCoroutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isServer == false)
            return;

        if (other.TryGetComponent(out ThirdPersonController _) == false)
            return;

        if (other.TryGetComponent(out NetworkIdentity playerIdentity) == false)
            return;

        NetworkConnectionToClient conn = playerIdentity.connectionToClient;
        _playersInElevator.Add(conn);
        _currentPlayerCount = _playersInElevator.Count;
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
        _playersInElevator.Remove(conn);
        _currentPlayerCount = _playersInElevator.Count;
    }

    [Server]
    public IEnumerator TimerCoroutine()
    {
        while (true)
        {
            _currentTimer -= Time.deltaTime;

            if (_currentTimer <= 0f)
            {
                if (_playersInElevator.Count > 0)
                    PlayersReadyToTransfer?.Invoke(_playersInElevator);

                ResetElevatorState();
            }

            yield return null;
        }
    }

    [Server]
    private void ResetElevatorState()
    {
        _playersInElevator.Clear();
        _currentPlayerCount = 0;
        _currentTimer = InitialTimerValue;
        _elevatorCollider.isTrigger = true;
        Debug.Log("Elevator: Сброшено состояние");
    }

    private void OnTimerChanged(float _, float newValue) =>
        _view.SetElapsedTime(newValue);

    private void OnPlayerCountChanged(int _, int newValue) =>
        _view.SetPlayerCount(newValue, _maxPlayer);
}
