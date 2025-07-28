using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomGeneration
{
    public class RoomSpawner : MonoBehaviour
    {
        [SerializeField] private Room[] _rooms;

        private readonly Dictionary<Type, List<Pool<Room>>> _roomPools = new();

        private void Awake() =>
            InitializePools();

        public Room Spawn(Type roomType)
        {
            if (_roomPools.TryGetValue(roomType, out List<Pool<Room>> pools) && pools.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, pools.Count);
                Pool<Room> randomPool = pools[randomIndex];

                if(randomPool.TryGet(out Room room) == false)
                    throw new InvalidOperationException($"Не удалось получить номер из пула типа {roomType.Name}");

                return room;
            }

            throw new Exception("Запрашиваемый тип отсутствует в списке префабов");
        }

        private void InitializePools()
        {
            foreach (Room room in _rooms)
            {
                if (room == null) 
                    continue;

                Type roomType = room.GetType();

                if (_roomPools.ContainsKey(roomType) == false)
                    _roomPools[roomType] = new();

                _roomPools[roomType].Add(new(room, transform, r => r.Init()));
            }
        }
    }
}