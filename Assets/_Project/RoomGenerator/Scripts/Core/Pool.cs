using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomGeneration
{
    public interface IDeactivatable<T>
    {
        public event Action<T> Deactivated;

        void ReturnInPool();
    }

    public class Pool<T> where T : MonoBehaviour, IDeactivatable<T>
    {
        private const int MaximumSize = 100;

        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Action<T> _created;
        private readonly Stack<T> _elements = new();
        private readonly int _size;

        private int _count;

        public Pool(T prefab, Transform parent, Action<T> created = null, int size = MaximumSize)
        {
            _prefab = prefab;
            _parent = parent;
            _created = created;
            _size = size;
        }

        public bool TryGet(out T element)
        {
            element = null;

            if (_elements.Count == 0 && _count >= _size)
                return false;

            if (_elements.Count > 0)
                element = _elements.Pop();
            else
                element = Create();

            element.Deactivated += Return;
            element.gameObject.SetActive(true);

            return true;
        }

        private void Return(T element)
        {
            if (element == null)
                return;

            element.Deactivated -= Return;
            element.gameObject.SetActive(false);
            _elements.Push(element);
        }

        private T Create()
        {
            _count++;

            T element = UnityEngine.Object.Instantiate(_prefab, _parent);

            if (_created != null)
                _created?.Invoke(element);

            return element;
        }
    }
}