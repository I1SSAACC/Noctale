using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class BillboardText : MonoBehaviour
{
    [Tooltip("Если не указан, будет использоваться Camera.main")]
    [SerializeField] private Camera _cameraToLookAt;

    private void Awake()
    {
        if (_cameraToLookAt == null)
        {
            _cameraToLookAt = Camera.main;
            if (_cameraToLookAt == null)
                Debug.LogError($"[{nameof(BillboardText)}] Нет камеры с тегом MainCamera в сцене.");
        }
    }

    private void LateUpdate()
    {
        if (_cameraToLookAt == null) return;

        Vector3 direction = (_cameraToLookAt.transform.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = lookRot;
    }
}