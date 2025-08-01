using System;
using UnityEngine;

[Serializable]
public struct Limiter
{
    [SerializeField] private float _min;
    [SerializeField] private float _max;

    public Limiter(float min, float max)
    {
        _min = min;
        _max = max;
    }

    public readonly float Min => _min;

    public readonly float Max => _max;
}