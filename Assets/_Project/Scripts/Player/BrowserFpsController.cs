using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class BrowserFpsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform respawnPoint;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 7f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVerticalVelocity = -2f;

    [Header("Ground Check")]
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Look")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.15f;
    [SerializeField] private bool invertY = false;
    [SerializeField, Range(-89f, 0f)] private float minPitch = -80f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Respawn")]
    [SerializeField] private float respawnYThreshold = -25f;

    [Header("Physics Push")]
    [SerializeField, Min(0f)] private float pushPower = 1.25f;

    private Vector3 verticalVelocity;
    private float pitch;
    private bool isCursorLocked;
    private Vector3 initialSpawnPosition;

    public event Action<bool> CursorLockStateChanged;

    public bool IsCursorLocked => isCursorLocked;

    public void ConfigureReferences(Transform cameraRootTransform, Transform groundCheckTransform, Transform respawnPointTransform)
    {
        cameraRoot = cameraRootTransform;
        groundCheck = groundCheckTransform;
        respawnPoint = respawnPointTransform;
    }

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (cameraRoot == null)
        {
            cameraRoot = transform;
        }

        if (groundCheck == null)
        {
            Transform autoGroundCheck = new GameObject("GroundCheck").transform;
            autoGroundCheck.SetParent(transform);
            autoGroundCheck.localPosition = new Vector3(0f, 0.1f, 0f);
            groundCheck = autoGroundCheck;
        }
    }

    private void Start()
    {
        initialSpawnPosition = transform.position;
        pitch = NormalizePitch(cameraRoot.localEulerAngles.x);
        SetCursorLocked(lockCursorOnStart);
    }

    private void Update()
    {
        HandleCursorInput();
        HandleLook();
        HandleMovement();
        HandleRespawn();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isCursorLocked)
        {
            SetCursorLocked(false);
        }
    }

    private void HandleCursorInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }
    }

    private void HandleLook()
    {
        if (!isCursorLocked || cameraRoot == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
        float yaw = mouseDelta.x;
        float pitchInput = invertY ? mouseDelta.y : -mouseDelta.y;

        transform.Rotate(Vector3.up * yaw, Space.World);

        pitch = Mathf.Clamp(pitch + pitchInput, minPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;
        Vector2 moveInput = Vector2.zero;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed)
            {
                moveInput.y += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                moveInput.y -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                moveInput.x += 1f;
            }

            if (keyboard.aKey.isPressed)
            {
                moveInput.x -= 1f;
            }
        }

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        bool isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedVerticalVelocity;
        }

        bool wantsSprint = keyboard != null && keyboard.leftShiftKey.isPressed;
        float currentSpeed = wantsSprint ? sprintSpeed : moveSpeed;

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        bool wantsJump = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        if (isGrounded && wantsJump)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleRespawn()
    {
        if (transform.position.y >= respawnYThreshold)
        {
            return;
        }

        Respawn();
    }

    private void Respawn()
    {
        Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : initialSpawnPosition;
        Quaternion targetRotation = respawnPoint != null
            ? Quaternion.Euler(0f, respawnPoint.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        bool previousEnabledState = characterController.enabled;
        characterController.enabled = false;
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        characterController.enabled = previousEnabledState;

        pitch = 0f;
        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.identity;
        }

        verticalVelocity = Vector3.zero;
    }

    public void SetCursorLocked(bool locked)
    {
        isCursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        CursorLockStateChanged?.Invoke(isCursorLocked);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (pushPower <= 0f)
        {
            return;
        }

        Rigidbody attachedBody = hit.collider.attachedRigidbody;
        if (attachedBody == null || attachedBody.isKinematic)
        {
            return;
        }

        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (pushDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        attachedBody.AddForce(pushDirection.normalized * pushPower, ForceMode.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private static float NormalizePitch(float rawPitch)
    {
        if (rawPitch > 180f)
        {
            rawPitch -= 360f;
        }

        return rawPitch;
    }
}
