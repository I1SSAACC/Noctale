using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    private Transform _target;

    private void LateUpdate()
    {
        if (_target == null)
            return;

        transform.position = _target.position;
        transform.LookAt(_target);
    }

    public void Follow(Transform target) =>
        _target = target;
}