using System.Collections;
using UnityEngine;

public class InputReader
{
    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string MouseX = "Mouse X";
    private const string MouseY = "Mouse Y";

    private const KeyCode KeyCrouch = KeyCode.LeftControl;
    private const KeyCode KeyJump = KeyCode.Space;
    private const KeyCode KeyInteract = KeyCode.E;
    private const KeyCode KeyLamp = KeyCode.F;

    private readonly InputEvents _events = new();
    private bool _isJumpKeyPressed;

    public InputEvents Events => _events;

    public void Init(Player player) =>
        player.StartCoroutine(UpdateInputRoutine());

    public bool IsMovement()
    {
        float horizontal = Input.GetAxisRaw(HorizontalAxis);
        float vertical = Input.GetAxisRaw(VerticalAxis);

        return vertical != 0;
    }

    private IEnumerator UpdateInputRoutine()
    {
        while (true)
        {
            yield return null;

            ReadMovement();
            ReadRotation();
            ReadCrouch();
            ReadJump();
            ReadInteract();
            ReadLamp();
        }
    }

    private void ReadMovement()
    {
        float horizontal = Input.GetAxisRaw(HorizontalAxis);
        float vertical = Input.GetAxisRaw(VerticalAxis);

        _events.MovementPressed?.Invoke(new(horizontal, vertical));
    }

    private void ReadRotation()
    {
        float mouseX = Input.GetAxis(MouseX);
        float mouseY = Input.GetAxis(MouseY);

        if (mouseX != 0 || mouseY != 0)
            _events.RotationPressed?.Invoke(new(mouseX, mouseY));
    }

    private void ReadCrouch()
    {
        if (Input.GetKeyDown(KeyCrouch))
            _events.CrouchToggled?.Invoke(true);

        if (Input.GetKeyUp(KeyCrouch))
            _events.CrouchToggled?.Invoke(false);
    }

    private void ReadJump()
    {
        if (Input.GetKeyDown(KeyJump) && _isJumpKeyPressed == false)
        {
            _isJumpKeyPressed = true;
            _events.JumpPressed?.Invoke();
        }

        if (Input.GetKeyUp(KeyJump))
            _isJumpKeyPressed = false;
    }

    private void ReadInteract()
    {
        if (Input.GetKeyDown(KeyInteract))
            _events.InteractPressed?.Invoke();
    }

    private void ReadLamp()
    {
        if (Input.GetKeyDown(KeyLamp))
            _events.LampPressed?.Invoke();
    }
}