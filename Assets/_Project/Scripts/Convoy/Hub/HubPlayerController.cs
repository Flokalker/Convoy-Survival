using UnityEngine;
using UnityEngine.InputSystem;

namespace ConvoySurvival.Hub
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class HubPlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraRoot;
        [SerializeField, Min(1f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(1f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.2f;

        private float verticalVelocity;
        private float pitch;

        public void Configure(Transform cameraPivot)
        {
            cameraRoot = cameraPivot;
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

            LockCursor(true);
        }

        private void Update()
        {
            HandleCursor();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                LockCursor(false);
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                LockCursor(true);
            }
        }

        private void HandleLook()
        {
            if (cameraRoot == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up * delta.x, Space.World);

            pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 moveInput = Vector2.zero;
            if (keyboard.wKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed) moveInput.y -= 1f;
            if (keyboard.dKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed) moveInput.x -= 1f;

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            bool sprint = keyboard.leftShiftKey.isPressed;
            float speed = sprint ? sprintSpeed : moveSpeed;
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            characterController.Move(move * speed * Time.deltaTime);

            if (grounded && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
