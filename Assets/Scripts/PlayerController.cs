using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float gravity = 25f;

    private CharacterController characterController;
    private Vector3 velocity;
    private Vector3 lastPosition;

    public float TravelDistance { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical);
        input = Vector3.ClampMagnitude(input, 1f);

        if (input.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        Vector3 movement = input * moveSpeed;

        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y -= gravity * Time.deltaTime;
        movement.y = velocity.y;

        characterController.Move(movement * Time.deltaTime);

        Vector3 flatDelta = transform.position - lastPosition;
        flatDelta.y = 0f;
        TravelDistance += flatDelta.magnitude;
        lastPosition = transform.position;
    }
}
