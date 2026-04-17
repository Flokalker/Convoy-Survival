using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class InputHandler : MonoBehaviour
{
    [Header("Mouse")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool FirePressed { get; private set; }
    public bool EnterExitPressed { get; private set; }
    public bool BrakeHeld { get; private set; }
    public bool HandbrakeHeld { get; private set; }

    public float VehicleThrottle { get; private set; }
    public float VehicleSteer { get; private set; }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        MoveInput = ReadMove(keyboard);
        LookInput = mouse != null ? mouse.delta.ReadValue() * mouseSensitivity : Vector2.zero;

        JumpPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        SprintHeld = keyboard != null && keyboard.leftShiftKey.isPressed;
        InteractPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
        FirePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        EnterExitPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;
        BrakeHeld = keyboard != null && keyboard.sKey.isPressed;
        HandbrakeHeld = keyboard != null && keyboard.leftCtrlKey.isPressed;

        VehicleThrottle = MoveInput.y;
        VehicleSteer = MoveInput.x;
    }

    private static Vector2 ReadMove(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;
        if (keyboard.wKey.isPressed)
        {
            move.y += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            move.y -= 1f;
        }

        if (keyboard.aKey.isPressed)
        {
            move.x -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            move.x += 1f;
        }

        return move.sqrMagnitude > 1f ? move.normalized : move;
    }
}
