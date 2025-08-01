using System;
using UnityEngine;

[Serializable]
public class Rotator
{
    [SerializeField] private float _rotationSpeed = 1.0f;
    [SerializeField] private Limiter _xRotationLimiter = new(-90.0f, 90.0f);

    private Transform _transform;
    private Transform _cameraTransform;

    public void Init(Player player)
    {
        _transform = player.transform;
        _cameraTransform = Camera.main.transform;
        
        if(_cameraTransform.TryGetComponent(out CameraFollower cameraFollower))
            cameraFollower.Follow(_transform.GetComponentInChildren<CameraTarget>().transform);
    }

    public void Rotate(RotationInput rotationInput)
    {
        float yaw = rotationInput.MouseX * _rotationSpeed * Time.deltaTime;
        _transform.Rotate(0, yaw, 0, Space.World);        

        float pitch = -rotationInput.MouseY * _rotationSpeed * Time.deltaTime;
        float currentXRotation = _cameraTransform.localEulerAngles.x;
        currentXRotation = NormalizeAngle(currentXRotation);
        float newXRotation = Mathf.Clamp(currentXRotation + pitch, _xRotationLimiter.Min, _xRotationLimiter.Max);

        _cameraTransform.localRotation = Quaternion.Euler(newXRotation, 0, 0);
        _cameraTransform.rotation = Quaternion.Euler(newXRotation, _transform.eulerAngles.y, 0);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}