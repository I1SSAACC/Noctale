using System;
using UnityEngine;

[Serializable]
public class Jumper
{
    [SerializeField] private float _force;

    private Mover _mover;

    public void Init(Mover mover) =>
        _mover = mover;

    public void Jump() =>
        _mover.SetVerticalVelocity(_force);
}