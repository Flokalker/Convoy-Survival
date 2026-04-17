using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleStats))]
public class Motor : MonoBehaviour
{
    [System.Serializable]
    public class Axle
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public Transform leftVisual;
        public Transform rightVisual;
        public bool steering;
        public bool motor;
    }

    [Header("Wheels")]
    [SerializeField] private Axle[] axles;
    [SerializeField, Min(100f)] private float motorTorque = 1800f;
    [SerializeField, Min(100f)] private float brakeTorque = 2500f;
    [SerializeField, Range(1f, 60f)] private float maxSteerAngle = 28f;

    [Header("Fuel")]
    [SerializeField, Min(0.001f)] private float fuelBurnPerSecondAtFullThrottle = 0.6f;

    [Header("Drag")]
    [SerializeField, Min(0f)] private float idleDrag = 0.02f;

    private Rigidbody rb;
    private VehicleStats vehicleStats;
    private InputHandler driverInput;
    private bool canControl;

    public float SpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        vehicleStats = GetComponent<VehicleStats>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    private void FixedUpdate()
    {
        ApplyDrivingPhysics();
    }

    private void Update()
    {
        UpdateWheelVisuals();
    }

    public void SetDriverInput(InputHandler inputHandler)
    {
        driverInput = inputHandler;
    }

    public void SetControlEnabled(bool enabledState)
    {
        canControl = enabledState;
    }

    private void ApplyDrivingPhysics()
    {
        float throttle = canControl && driverInput != null ? driverInput.VehicleThrottle : 0f;
        float steer = canControl && driverInput != null ? driverInput.VehicleSteer : 0f;
        bool braking = !canControl || (driverInput != null && driverInput.BrakeHeld);
        bool handbrake = canControl && driverInput != null && driverInput.HandbrakeHeld;

        bool outOfFuel = vehicleStats.Fuel <= 0f;
        bool canAccelerate = !outOfFuel && vehicleStats.CurrentDurability > 0f;

        float speedLimiter = SpeedKmh >= vehicleStats.MaxSpeed ? 0f : 1f;
        float appliedTorque = canAccelerate ? throttle * motorTorque * speedLimiter : 0f;

        if (canControl && Mathf.Abs(throttle) > 0.01f && canAccelerate)
        {
            vehicleStats.ConsumeFuel(Mathf.Abs(throttle) * fuelBurnPerSecondAtFullThrottle * Time.fixedDeltaTime);
        }

        foreach (Axle axle in axles)
        {
            if (axle.leftWheel == null || axle.rightWheel == null)
            {
                continue;
            }

            if (axle.steering)
            {
                float steerAngle = steer * maxSteerAngle;
                axle.leftWheel.steerAngle = steerAngle;
                axle.rightWheel.steerAngle = steerAngle;
            }

            if (axle.motor)
            {
                axle.leftWheel.motorTorque = appliedTorque;
                axle.rightWheel.motorTorque = appliedTorque;
            }

            float finalBrake = (braking || handbrake) ? brakeTorque : 0f;
            axle.leftWheel.brakeTorque = finalBrake;
            axle.rightWheel.brakeTorque = finalBrake;
        }

        rb.linearDamping = Mathf.Abs(throttle) < 0.01f ? idleDrag : 0f;
    }

    private void UpdateWheelVisuals()
    {
        foreach (Axle axle in axles)
        {
            SyncWheelPose(axle.leftWheel, axle.leftVisual);
            SyncWheelPose(axle.rightWheel, axle.rightVisual);
        }
    }

    private static void SyncWheelPose(WheelCollider wheel, Transform visual)
    {
        if (wheel == null || visual == null)
        {
            return;
        }

        wheel.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
        visual.position = wheelPosition;
        visual.rotation = wheelRotation;
    }
}
