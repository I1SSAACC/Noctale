using System;
using UnityEngine;

[Serializable]
public class Gravity
{
    [SerializeField] private float _force;

    private CharacterController _controller;

    private float _maximumVerticalVelocity;
    private float _currentVerticalVelocity;

    public void Init (CharacterController characterController) =>
        _controller = characterController;

    public float GetUpdateVelocity()
    {
        UpdateVelosity();

        return _currentVerticalVelocity;
    }

    private void UpdateVelosity()
    {
        bool isGrounded = _controller.isGrounded && _currentVerticalVelocity != _maximumVerticalVelocity;
        _currentVerticalVelocity -= isGrounded ? Mathf.Epsilon : _force * Time.deltaTime;
    }

    public void SetVerticalVelocity(float value)
    {
        _currentVerticalVelocity = value;
        _maximumVerticalVelocity = value;
    }
}