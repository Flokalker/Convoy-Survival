using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public enum LocalForwardAxis
    {
        Forward,
        Back,
        Right,
        Left
    }

    [Header("Driving")]
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float reverseAcceleration = 14f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float reverseMaxSpeed = 10f;
    [SerializeField] private float turnSpeed = 85f;
    [SerializeField] private float turnSpeedAtMaxSpeed = 45f;
    [SerializeField] private LocalForwardAxis forwardAxis = LocalForwardAxis.Right;

    [Header("Physics")]
    [SerializeField] private float normalDrag = 0.35f;
    [SerializeField] private float brakeDrag = 2.6f;
    [SerializeField] private float brakeAcceleration = 30f;
    [SerializeField] private float lateralGrip = 7f;

    private Rigidbody rb;
    private bool isDriving;

    public Vector3 DriveForward => GetDriveForward();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.linearDamping = normalDrag;
        rb.angularDamping = 1.5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void SetDriving(bool driving)
    {
        isDriving = driving;
        if (!isDriving)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetForwardAxis(LocalForwardAxis axis)
    {
        forwardAxis = axis;
    }

    private void FixedUpdate()
    {
        if (!isDriving)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float forwardInput = 0f;
        if (keyboard.wKey.isPressed)
        {
            forwardInput += 1f;
        }
        if (keyboard.sKey.isPressed)
        {
            forwardInput -= 1f;
        }

        float turnInput = 0f;
        if (keyboard.dKey.isPressed)
        {
            turnInput += 1f;
        }
        if (keyboard.aKey.isPressed)
        {
            turnInput -= 1f;
        }

        Vector3 driveForward = GetDriveForward();
        Vector3 driveRight = Vector3.Cross(Vector3.up, driveForward).normalized;
        float speedAlongForward = Vector3.Dot(rb.linearVelocity, driveForward);
        float speedAlongRight = Vector3.Dot(rb.linearVelocity, driveRight);
        bool hasThrottleInput = !Mathf.Approximately(forwardInput, 0f);
        bool isBraking = hasThrottleInput
            && !Mathf.Approximately(speedAlongForward, 0f)
            && Mathf.Sign(forwardInput) != Mathf.Sign(speedAlongForward);

        rb.linearDamping = isBraking || !hasThrottleInput ? brakeDrag : normalDrag;

        if (forwardInput > 0f && speedAlongForward < maxSpeed)
        {
            Vector3 force = driveForward * (forwardInput * acceleration);
            rb.AddForce(force, ForceMode.Acceleration);
        }
        else if (forwardInput < 0f && speedAlongForward > -reverseMaxSpeed)
        {
            Vector3 force = driveForward * (forwardInput * reverseAcceleration);
            rb.AddForce(force, ForceMode.Acceleration);
        }

        if (isBraking)
        {
            rb.AddForce(-driveForward * Mathf.Sign(speedAlongForward) * brakeAcceleration, ForceMode.Acceleration);
        }

        Vector3 lateralCorrection = -driveRight * speedAlongRight * lateralGrip;
        rb.AddForce(lateralCorrection, ForceMode.Acceleration);

        float speedFactor = Mathf.Clamp01(Mathf.Abs(speedAlongForward) / maxSpeed);
        float currentTurnSpeed = Mathf.Lerp(turnSpeed, turnSpeedAtMaxSpeed, speedFactor);
        float steeringDirection = speedAlongForward >= 0f ? 1f : -1f;
        float turnStrength = turnInput * currentTurnSpeed * steeringDirection * Time.fixedDeltaTime;
        Quaternion targetRotation = rb.rotation * Quaternion.Euler(0f, turnStrength, 0f);
        rb.MoveRotation(targetRotation);
    }

    private Vector3 GetDriveForward()
    {
        Vector3 localDirection = Vector3.forward;
        switch (forwardAxis)
        {
            case LocalForwardAxis.Back:
                localDirection = Vector3.back;
                break;
            case LocalForwardAxis.Right:
                localDirection = Vector3.right;
                break;
            case LocalForwardAxis.Left:
                localDirection = Vector3.left;
                break;
        }

        Vector3 worldDirection = transform.TransformDirection(localDirection);
        worldDirection.y = 0f;
        return worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : transform.forward;
    }
}
