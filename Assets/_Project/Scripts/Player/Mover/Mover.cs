using System;
using UnityEngine;

[Serializable]
public class Mover
{
    [SerializeField] private float _speed;
    [SerializeField] private Gravity _gravity;

    private CharacterController _characterController;

    public void Init(Player player)
    {
        _characterController = player.GetComponent<CharacterController>();
        _gravity.Init(_characterController);
    }

    public bool IsGrounded =>
    _characterController.isGrounded;

    public void Move(MovementInput input)
    {
        Vector3 localDirection = new(input.Horizontal * _speed, _gravity.GetUpdateVelocity(), input.Vertical * _speed);
        localDirection = _characterController.transform.TransformDirection(localDirection);
        _characterController.Move(localDirection * Time.deltaTime);
    }

    public void SetVerticalVelocity(float velocity) =>
        _gravity.SetVerticalVelocity(velocity);
}