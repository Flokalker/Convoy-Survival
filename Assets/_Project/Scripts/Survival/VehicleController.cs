using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Motor))]
[RequireComponent(typeof(VehicleStats))]
public class VehicleController : MonoBehaviour, IInteractable
{
    [SerializeField] private Motor motor;
    [SerializeField] private VehicleStats vehicleStats;
    [SerializeField] private Transform driverSeat;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(0f)] private float enterExitCooldown = 0.35f;

    private FirstPersonController currentDriver;
    private CharacterController driverCharacterController;
    private float nextAllowedSwapTime;

    public bool IsOccupied => currentDriver != null;
    public Transform CurrentDriverTransform => currentDriver != null ? currentDriver.transform : null;
    public VehicleStats Stats => vehicleStats;

    public string InteractionPrompt => IsOccupied ? "Truck occupied" : "Press E to enter truck";

    private void Reset()
    {
        motor = GetComponent<Motor>();
        vehicleStats = GetComponent<VehicleStats>();
    }

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<Motor>();
        }

        if (vehicleStats == null)
        {
            vehicleStats = GetComponent<VehicleStats>();
        }

        if (driverSeat == null)
        {
            driverSeat = transform;
        }

        motor.SetControlEnabled(false);
    }

    private void Update()
    {
        if (!IsOccupied || Time.time < nextAllowedSwapTime)
        {
            return;
        }

        if (currentDriver.InputHandler.EnterExitPressed)
        {
            ExitTruck();
        }
    }

    public bool CanInteract(FirstPersonController player)
    {
        return player != null && !IsOccupied && Time.time >= nextAllowedSwapTime;
    }

    public void Interact(FirstPersonController player)
    {
        if (CanInteract(player))
        {
            EnterTruck(player);
        }
    }

    public void EnterTruck(FirstPersonController player)
    {
        currentDriver = player;
        driverCharacterController = player.GetComponent<CharacterController>();

        if (driverCharacterController != null)
        {
            driverCharacterController.enabled = false;
        }

        currentDriver.SetControllable(false);
        currentDriver.transform.SetParent(driverSeat);
        currentDriver.transform.localPosition = Vector3.zero;
        currentDriver.transform.localRotation = Quaternion.identity;

        motor.SetDriverInput(currentDriver.InputHandler);
        motor.SetControlEnabled(true);

        nextAllowedSwapTime = Time.time + enterExitCooldown;
    }

    public void ExitTruck()
    {
        if (!IsOccupied)
        {
            return;
        }

        Transform driverTransform = currentDriver.transform;
        driverTransform.SetParent(null);
        driverTransform.position = exitPoint != null ? exitPoint.position : transform.position + transform.right * 2f;
        driverTransform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (driverCharacterController != null)
        {
            driverCharacterController.enabled = true;
        }

        currentDriver.SetControllable(true);

        motor.SetControlEnabled(false);
        motor.SetDriverInput(null);

        currentDriver = null;
        driverCharacterController = null;
        nextAllowedSwapTime = Time.time + enterExitCooldown;
    }
}
