public readonly struct RotationInput
{
    public RotationInput(float mouseX, float mouseY)
    {
        MouseX = mouseX;
        MouseY = mouseY;
    }

    public float MouseX { get; }

    public float MouseY { get; }
}