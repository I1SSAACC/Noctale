using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [SerializeField] private Mover _mover;
    [SerializeField] private Rotator _rotator;
    [SerializeField] private Jumper _jumper;

    private readonly InputReader _inputReader = new();
    private bool _isCrouch;

    private void Awake()
    {;
        _mover.Init(this);
        _rotator.Init(this);
        _jumper.Init(_mover);
        _inputReader.Init(this);
    }

    private void Update() =>
        _jumper.ResetJumpState(_mover.IsGrounded);

    private void OnEnable()
    {
        InputEvents events = _inputReader.Events;
        events.MovementPressed += OnMovePressed;
        events.RotationPressed += OnRotatePressed;
        events.CrouchToggled += OnCrouchToggled;
        events.JumpPressed += OnJumpPressed;
        events.InteractPressed += OnInteractPressed;
        events.LampPressed += OnLampPressed;
    }

    private void OnDisable()
    {
        InputEvents events = _inputReader.Events;
        events.MovementPressed -= OnMovePressed;
        events.RotationPressed -= OnRotatePressed;
        events.CrouchToggled -= OnCrouchToggled;
        events.JumpPressed -= OnJumpPressed;
        events.InteractPressed -= OnInteractPressed;
        events.LampPressed -= OnLampPressed;
    }

    private void OnMovePressed(MovementInput input) =>
        _mover.Move(input);

    private void OnRotatePressed(RotationInput input) =>
        _rotator.Rotate(input);

    private void OnCrouchToggled(bool isPressed)
    {
        if (_inputReader.IsMovement())
        {
            if (isPressed)
                Debug.Log("Ride pressed");
            else
                Debug.Log("Ride unpressed");

            return;
        }

        if (_isCrouch && isPressed == false)
            Debug.Log("Ride unpressed");

        _isCrouch = isPressed;

        if (isPressed)
            Debug.Log("Crouch pressed");
        else
            Debug.Log("Crouch unpressed");
    }

    private void OnJumpPressed()
    {
        Debug.Log($"Попытка прыжка, IsGrounded: {_mover.IsGrounded}");
        if (_mover.IsGrounded == false)
            return;

        _jumper.Jump();
    }


    private void OnInteractPressed() =>
        Debug.Log("Interact pressed");

    private void OnLampPressed() =>
        Debug.Log("Lamp pressed");
}