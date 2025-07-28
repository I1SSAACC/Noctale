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
                throw new NullReferenceException("�� ������� ����� Entrance point � �������: " + name);

            _entrancePoint = entrancePoint.transform;

            ExitPoint exitPoint = GetComponentInChildren<ExitPoint>(true);

            if (exitPoint == null)
                throw new NullReferenceException("�� ������� ����� Exit point � �������: " + name);

            _exitPoint = exitPoint.transform;

            _doorTrigger = GetComponentInChildren<DoorTrgger>(true);

            if (_doorTrigger == null)
                throw new NullReferenceException("�� ������� ����� Door trigger � �������: " + name);

            _doorTrigger.PlayerEntered += OnPlayerEntered;
        }

        public void ReturnInPool() =>
            Deactivated?.Invoke(this);

        private void OnPlayerEntered() =>
            PlayerEntered?.Invoke(this);
    }
}