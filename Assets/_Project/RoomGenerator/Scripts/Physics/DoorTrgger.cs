using System;
using UnityEngine;

namespace RoomGeneration
{
    [RequireComponent(typeof(Collider))]
    public class DoorTrgger : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private Color _gizmosColor;
        private Collider _collider;
#endif
        public event Action PlayerEntered;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out TempPlayer _))
                PlayerEntered?.Invoke();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(_collider == null)
                _collider = GetComponent<Collider>();

            Gizmos.color = _gizmosColor;

            if (_collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, boxCollider.size);
            }
        }
#endif
    }
}