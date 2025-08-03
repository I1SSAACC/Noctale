using System;
using UnityEngine;

[Serializable]
public class Gravity
{
    private const float GroundedVelocity = -0.01f;
    private const float MinimumVerticalVelocity = -20f;

    [SerializeField] private float _force = 9.81f;

    private CharacterController _controller;
    private float _currentVerticalVelocity;

    public void Init(CharacterController characterController) =>
        _controller = characterController;

    public float GetUpdateVelocity()
    {
        UpdateVelocity();

        return _currentVerticalVelocity;
    }

    private void UpdateVelocity()
    {
        if (_controller.isGrounded)
        {
            if (_currentVerticalVelocity < 0f)
                _currentVerticalVelocity = GroundedVelocity;
        }
        else
        {
            _currentVerticalVelocity -= _force * Time.deltaTime;
            if (_currentVerticalVelocity < MinimumVerticalVelocity)
                _currentVerticalVelocity = MinimumVerticalVelocity;
        }
    }

    public void SetVerticalVelocity(float value) =>
        _currentVerticalVelocity = value;
}