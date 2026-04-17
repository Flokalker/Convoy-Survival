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
    [SerializeField, Min(0.1f)] private float crouchSpeed = 2.25f;
    [SerializeField] private bool allowDoubleTapSprint = true;
    [SerializeField, Min(0.05f)] private float doubleTapSprintWindow = 0.4f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.45f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVerticalVelocity = -2f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
    [SerializeField, Min(1f)] private float maxFallSpeed = 30f;

    [Header("Ground Check")]
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Crouch")]
    [SerializeField, Min(0.5f)] private float crouchHeight = 1.2f;
    [SerializeField, Min(0.1f)] private float crouchTransitionSpeed = 8f;
    [SerializeField] private LayerMask crouchObstructionMask = ~0;

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
    private bool jumpConsumed;
    private bool doubleTapSprintActive;
    private bool isCrouching;
    private int forwardTapCount;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastForwardTapTime = float.NegativeInfinity;
    private Vector3 initialSpawnPosition;
    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraLocalY;
    private float crouchingCameraLocalY;
    private readonly Collider[] groundCheckResults = new Collider[8];
    private readonly Collider[] crouchCheckResults = new Collider[8];

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
        standingHeight = characterController.height;
        standingCenter = characterController.center;
        standingCameraLocalY = cameraRoot != null ? cameraRoot.localPosition.y : standingCenter.y;
        crouchHeight = Mathf.Min(crouchHeight, standingHeight);
        float crouchRatio = crouchHeight / Mathf.Max(0.01f, standingHeight);
        crouchingCameraLocalY = standingCameraLocalY * crouchRatio;
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

    private void OnDisable()
    {
        if (isCursorLocked)
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
        bool isHoldingForward = false;
        bool pressedForwardThisFrame = false;
        bool wantsCrouch = false;
        bool wantsSprintKey = false;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed)
            {
                moveInput.y += 1f;
                isHoldingForward = true;
            }

            pressedForwardThisFrame = keyboard.wKey.wasPressedThisFrame;
            wantsSprintKey = keyboard.leftShiftKey.isPressed;
            wantsCrouch = keyboard.leftCtrlKey.isPressed;

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

        UpdateCrouchState(wantsCrouch);

        bool isGrounded = IsGrounded();
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedVerticalVelocity;
        }

        if (allowDoubleTapSprint && pressedForwardThisFrame)
        {
            forwardTapCount = Time.time <= lastForwardTapTime + doubleTapSprintWindow ? forwardTapCount + 1 : 1;
            lastForwardTapTime = Time.time;

            if (forwardTapCount >= 2)
            {
                doubleTapSprintActive = true;
                forwardTapCount = 0;
            }
        }

        if (!isHoldingForward || moveInput.y <= 0f)
        {
            doubleTapSprintActive = false;
            if (!isHoldingForward)
            {
                forwardTapCount = 0;
            }
        }

        bool wantsSprint = (wantsSprintKey || (allowDoubleTapSprint && doubleTapSprintActive)) && moveInput.y > 0f && !isCrouching;
        float currentSpeed = isCrouching ? crouchSpeed : wantsSprint ? sprintSpeed : moveSpeed;
        float movementControl = isGrounded ? 1f : airControl;

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 horizontalVelocity = moveDirection * (currentSpeed * movementControl);

        bool jumpHeld = keyboard != null && keyboard.spaceKey.isPressed;
        if (isGrounded && !jumpHeld)
        {
            jumpConsumed = false;
        }

        bool wantsJump = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        if (wantsJump)
        {
            lastJumpPressedTime = Time.time;
        }

        bool canUseBufferedJump = Time.time <= lastJumpPressedTime + jumpBufferTime;
        bool canUseCoyoteJump = isGrounded || Time.time <= lastGroundedTime + coyoteTime;
        if (!jumpConsumed && canUseBufferedJump && canUseCoyoteJump)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpConsumed = true;
            lastJumpPressedTime = float.NegativeInfinity;
            isGrounded = false;
        }

        verticalVelocity.y = Mathf.Max(verticalVelocity.y + gravity * Time.deltaTime, -maxFallSpeed);

        CollisionFlags collisionFlags = characterController.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);
        if ((collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity.y > 0f)
        {
            verticalVelocity.y = 0f;
        }

        if ((collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedVerticalVelocity;
        }
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
        jumpConsumed = false;
        doubleTapSprintActive = false;
        isCrouching = false;
        forwardTapCount = 0;
        lastJumpPressedTime = float.NegativeInfinity;
        lastGroundedTime = Time.time;
        lastForwardTapTime = float.NegativeInfinity;
        ApplyCharacterHeight(standingHeight);
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

    private void UpdateCrouchState(bool wantsCrouch)
    {
        if (wantsCrouch)
        {
            isCrouching = true;
            doubleTapSprintActive = false;
        }
        else if (isCrouching && CanStandUp())
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        ApplyCharacterHeight(targetHeight);
    }

    private void ApplyCharacterHeight(float targetHeight)
    {
        float newHeight = Mathf.MoveTowards(characterController.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        characterController.height = newHeight;
        characterController.center = new Vector3(standingCenter.x, newHeight * 0.5f, standingCenter.z);

        if (cameraRoot != null)
        {
            Vector3 localPosition = cameraRoot.localPosition;
            float targetCameraY = isCrouching ? crouchingCameraLocalY : standingCameraLocalY;
            localPosition.y = Mathf.MoveTowards(localPosition.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
            cameraRoot.localPosition = localPosition;
        }
    }

    private bool CanStandUp()
    {
        float radius = characterController.radius * 0.95f;
        Vector3 worldCenter = transform.TransformPoint(standingCenter);
        Vector3 bottom = worldCenter + Vector3.down * ((standingHeight * 0.5f) - radius);
        Vector3 top = worldCenter + Vector3.up * ((standingHeight * 0.5f) - radius);

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            crouchCheckResults,
            crouchObstructionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = crouchCheckResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool IsGrounded()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            groundCheck.position,
            groundCheckRadius,
            groundCheckResults,
            groundMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = groundCheckResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
