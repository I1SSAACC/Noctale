using System;
using UnityEngine;

namespace RoomGeneration
{
    public abstract class Room : MonoBehaviour, IDeactivatable<Room>
    {
        private DoorTrgger _doorTrigger;
        private Transform _entrancePoint;
        private Transform _exitPoint;

        public event Action<Room> Deactivated;
        public event Action<Room> PlayerEntered;

        public Vector3 EntrancePosition => _entrancePoint.position;

        public Vector3 ExitPosition => _exitPoint.position;

        public void Init()
        {
            EntrancePoint entrancePoint = GetComponentInChildren<EntrancePoint>(true);

            if (entrancePoint == null)
                throw new NullReferenceException("Не удалось найти Entrance point в комнате: " + name);

            _entrancePoint = entrancePoint.transform;

            ExitPoint exitPoint = GetComponentInChildren<ExitPoint>(true);

            if (exitPoint == null)
                throw new NullReferenceException("Не удалось найти Exit point в комнате: " + name);

            _exitPoint = exitPoint.transform;

            _doorTrigger = GetComponentInChildren<DoorTrgger>(true);

            if (_doorTrigger == null)
                throw new NullReferenceException("Не удалось найти Door trigger в комнате: " + name);

            _doorTrigger.PlayerEntered += OnPlayerEntered;
        }

        public void ReturnInPool() =>
            Deactivated?.Invoke(this);

        private void OnPlayerEntered() =>
            PlayerEntered?.Invoke(this);
    }
}