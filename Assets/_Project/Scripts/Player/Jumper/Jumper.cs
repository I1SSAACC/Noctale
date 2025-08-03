using System;
using UnityEngine;

[Serializable]
public class Jumper
{
    [SerializeField] private float _force = 5f;

    private Mover _mover;
    private bool _isJumping;

    public void Init(Mover mover) =>
        _mover = mover;

    public void Jump()
    {
        if (_isJumping)
            return;

        _mover.SetVerticalVelocity(_force);
        _isJumping = true;
    }

    public void ResetJumpState(bool isGrounded)
    {
        if (isGrounded && _isJumping)
            _isJumping = false;
    }
}