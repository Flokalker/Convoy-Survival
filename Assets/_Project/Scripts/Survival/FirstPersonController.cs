using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputHandler))]
public class FirstPersonController : MonoBehaviour
{
    private static bool forceCursorUnlocked = true;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private Transform cameraRoot;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 8f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.25f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.25f;
    [SerializeField] private Transform groundCheck;

    [Header("Look")]
    [SerializeField, Range(-89f, 0f)] private float minPitch = -75f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

    private float pitch;
    private Vector3 verticalVelocity;
    private bool canControl = true;

    public InputHandler InputHandler => inputHandler;
    public bool CanControl => canControl;

    public static void SetForceCursorUnlocked(bool enabled)
    {
        forceCursorUnlocked = enabled;
        if (enabled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        inputHandler = GetComponent<InputHandler>();
    }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (inputHandler == null)
        {
            inputHandler = GetComponent<InputHandler>();
        }

        if (cameraRoot == null)
        {
            cameraRoot = transform;
        }

        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            groundCheck = check.transform;
        }
    }

    private void Start()
    {
        if (!forceCursorUnlocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!canControl)
        {
            return;
        }

        if (forceCursorUnlocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        HandleLook();
        HandleMovement();
    }

    public void SetControllable(bool enabledState)
    {
        canControl = enabledState;

        if (!enabledState)
        {
            verticalVelocity = Vector3.zero;
        }
    }

    private void HandleLook()
    {
        Vector2 lookInput = inputHandler.LookInput;
        transform.Rotate(Vector3.up * lookInput.x, Space.World);

        pitch = Mathf.Clamp(pitch - lookInput.y, minPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        Vector2 moveInput = inputHandler.MoveInput;
        float speed = inputHandler.SprintHeld ? sprintSpeed : moveSpeed;
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        characterController.Move(moveDirection * speed * Time.deltaTime);

        if (isGrounded && inputHandler.JumpPressed)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }
}
