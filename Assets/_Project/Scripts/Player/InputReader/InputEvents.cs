using System;

public class InputEvents
{
    public Action<bool> CrouchToggled;
    public Action<MovementInput> MovementPressed;
    public Action<RotationInput> RotationPressed;
    public Action JumpPressed;
    public Action InteractPressed;
    public Action LampPressed;
}