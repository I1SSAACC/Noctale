using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomGeneration
{
    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] private int _numberOfRooms = 3;

        private RoomSpawner _roomSpawner;
        private readonly List<Room> _activeRooms = new();

        private void Awake()
        {
            _roomSpawner = GetComponentInChildren<RoomSpawner>(true);

            if (_roomSpawner == null)
                Debug.LogError("RoomSpawner не найден на сцене", this);
        }

        private void Start()
        {
            Room firstRoom = _roomSpawner.Spawn(typeof(FirstRoom));
            _activeRooms.Add(firstRoom);
            SubscribeRoom(firstRoom);

            for (int i = _activeRooms.Count; i < _numberOfRooms; i++)
                NextRoom();
        }

        private void NextRoom()
        {
            Type roomType = UnityEngine.Random.value < 0.7f ? typeof(DefaultRoom) : typeof(ShelterRoom);
            Room nextRoom = _roomSpawner.Spawn(roomType);

            Room lastRoom = _activeRooms[^1];
            Vector3 exitPosition = lastRoom.ExitPosition;
            Vector3 entrancePosition = nextRoom.EntrancePosition;

            nextRoom.transform.position += exitPosition - entrancePosition;
            _activeRooms.Add(nextRoom);
            SubscribeRoom(nextRoom);
        }

        private void SubscribeRoom(Room room)
        {
            room.Deactivated += OnRoomDeactivated;
            room.PlayerEntered += OnPlayerEnteredRoom;
        }

        private void UnsubscribeRoom(Room room)
        {
            room.Deactivated -= OnRoomDeactivated;
            room.PlayerEntered -= OnPlayerEnteredRoom;
        }

        private void OnRoomDeactivated(Room room)
        {
            UnsubscribeRoom(room);
            _activeRooms.Remove(room);
            room.gameObject.SetActive(false);
        }

        private void OnPlayerEnteredRoom(Room room)
        {
            NextRoom();
            Debug.Log($"Игрок вошел в комнату: {room.name}");
        }
    }
}