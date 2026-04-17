using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVerticalVelocity = -2f;

    [Header("Look")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.15f;
    [SerializeField, Range(-89f, 0f)] private float minPitch = -80f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private readonly List<CameraState> disabledCameras = new List<CameraState>();
    private readonly List<AudioListenerState> disabledAudioListeners = new List<AudioListenerState>();

    private float pitch;
    private float verticalVelocity;
    private bool isCursorLocked;
    private AudioListener playerAudioListener;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>(true);
    }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (playerCamera != null)
        {
            playerAudioListener = playerCamera.GetComponent<AudioListener>();
        }
    }

    private void Start()
    {
        if (playerCamera != null)
        {
            pitch = NormalizePitch(playerCamera.transform.localEulerAngles.x);
        }

        DisableOtherSceneCameras();

        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = true;
        }

        SetCursorLocked(lockCursorOnStart);
    }

    private void Update()
    {
        HandleCursorInput();
        HandleLook();
        HandleMovement();
    }

    private void OnDisable()
    {
        RestoreDisabledSceneCameras();
        SetCursorLocked(false);
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
        if (!isCursorLocked || playerCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseDelta.x, Space.World);

        pitch = Mathf.Clamp(pitch - mouseDelta.y, minPitch, maxPitch);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = ReadMoveInput();
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        bool isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalVelocity;
        }

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
    }

    private void DisableOtherSceneCameras()
    {
        disabledCameras.Clear();
        disabledAudioListeners.Clear();

        Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Camera sceneCamera in sceneCameras)
        {
            if (sceneCamera == null || sceneCamera == playerCamera)
            {
                continue;
            }

            if (sceneCamera.transform.IsChildOf(transform))
            {
                continue;
            }

            disabledCameras.Add(new CameraState(sceneCamera, sceneCamera.enabled));
            sceneCamera.enabled = false;
        }

        AudioListener[] audioListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AudioListener audioListener in audioListeners)
        {
            if (audioListener == null)
            {
                continue;
            }

            if (playerCamera != null && audioListener.transform.IsChildOf(playerCamera.transform))
            {
                continue;
            }

            disabledAudioListeners.Add(new AudioListenerState(audioListener, audioListener.enabled));
            audioListener.enabled = false;
        }
    }

    private void RestoreDisabledSceneCameras()
    {
        foreach (CameraState state in disabledCameras)
        {
            if (state.Camera != null)
            {
                state.Camera.enabled = state.WasEnabled;
            }
        }

        foreach (AudioListenerState state in disabledAudioListeners)
        {
            if (state.AudioListener != null)
            {
                state.AudioListener.enabled = state.WasEnabled;
            }
        }
    }

    private void SetCursorLocked(bool locked)
    {
        isCursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed)
        {
            input.y += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (keyboard.aKey.isPressed)
        {
            input.x -= 1f;
        }

        return input;
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private readonly struct CameraState
    {
        public CameraState(Camera camera, bool wasEnabled)
        {
            Camera = camera;
            WasEnabled = wasEnabled;
        }

        public Camera Camera { get; }

        public bool WasEnabled { get; }
    }

    private readonly struct AudioListenerState
    {
        public AudioListenerState(AudioListener audioListener, bool wasEnabled)
        {
            AudioListener = audioListener;
            WasEnabled = wasEnabled;
        }

        public AudioListener AudioListener { get; }

        public bool WasEnabled { get; }
    }
}
