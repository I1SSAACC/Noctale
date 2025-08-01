public readonly struct MovementInput
{
    public MovementInput(float horizontal, float vertical)
    {
        Horizontal = horizontal;
        Vertical = vertical;
    }

    public float Horizontal { get; }

    public float Vertical { get; }
}