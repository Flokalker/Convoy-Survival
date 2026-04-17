using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PlayerInteract : MonoBehaviour
{
    [System.Serializable]
    private struct VehicleCameraProfile
    {
        public string vehicleName;
        public Vector3 offset;
        public Vector3 targetPositionOffset;
        public float followSpeed;
    }

    [Header("Car")]
    [SerializeField] private string[] targetCarNames = { "car scene", "Slatermk193", "RV" };
    [SerializeField] private float enterDistance = 3.2f;
    [SerializeField] private Vector3 exitOffset = new Vector3(2f, 0f, 0f);

    [Header("Camera Per Car")]
    [SerializeField] private VehicleCameraProfile[] vehicleCameraProfiles =
    {
        new VehicleCameraProfile
        {
            vehicleName = "car scene",
            offset = new Vector3(0f, 1.4f, -11.3f),
            targetPositionOffset = new Vector3(0f, 0.6f, 0f),
            followSpeed = 8f
        },
        new VehicleCameraProfile
        {
            vehicleName = "Slatermk193",
            offset = new Vector3(0f, 1.4f, -11.3f),
            targetPositionOffset = new Vector3(0f, 0.6f, 0f),
            followSpeed = 8f
        },
        new VehicleCameraProfile
        {
            vehicleName = "RV",
            offset = new Vector3(0f, 2.1f, -14.5f),
            targetPositionOffset = new Vector3(0f, 1.0f, 0f),
            followSpeed = 7f
        }
    };

    [Header("References")]
    [SerializeField] private BrowserFpsController fpsController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private BrowserPrototypeInteractor prototypeInteractor;

    private readonly List<Transform> availableCars = new List<Transform>();
    private Transform targetCar;
    private CarController carController;
    private Camera playerCamera;
    private CameraFollow cameraFollow;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private bool isInCar;

    private void Awake()
    {
        if (fpsController == null)
        {
            fpsController = GetComponent<BrowserFpsController>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (prototypeInteractor == null)
        {
            prototypeInteractor = GetComponent<BrowserPrototypeInteractor>();
        }
    }

    private void Start()
    {
        availableCars.Clear();
        HashSet<string> uniqueNames = new HashSet<string>();
        foreach (string carName in targetCarNames)
        {
            if (string.IsNullOrWhiteSpace(carName) || !uniqueNames.Add(carName))
            {
                continue;
            }

            GameObject carObject = GameObject.Find(carName);
            if (carObject == null)
            {
                Debug.LogWarning("PlayerInteract: Auto '" + carName + "' wurde nicht gefunden.");
                continue;
            }

            ConfigureDriveComponents(carObject);

            availableCars.Add(carObject.transform);
        }

        if (availableCars.Count == 0)
        {
            Debug.LogError("PlayerInteract: Keine gueltigen Autos fuer Interaktion gefunden.");
            enabled = false;
            return;
        }

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteract: Keine MainCamera gefunden.");
            enabled = false;
            return;
        }

        originalCameraParent = playerCamera.transform.parent;
        originalCameraLocalPosition = playerCamera.transform.localPosition;
        originalCameraLocalRotation = playerCamera.transform.localRotation;

        cameraFollow = playerCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = playerCamera.gameObject.AddComponent<CameraFollow>();
        }

        cameraFollow.enabled = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (!isInCar)
        {
            Transform closestCar = FindClosestCarInRange();
            if (closestCar != null)
            {
                EnterCar(closestCar);
            }
        }
        else
        {
            ExitCar();
        }
    }

    private void EnterCar(Transform carToEnter)
    {
        if (carToEnter == null)
        {
            return;
        }

        targetCar = carToEnter;
        ConfigureDriveComponents(targetCar.gameObject);
        carController = targetCar.GetComponent<CarController>();
        if (carController == null)
        {
            return;
        }

        isInCar = true;

        if (fpsController != null)
        {
            fpsController.SetCursorLocked(true);
            fpsController.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (prototypeInteractor != null)
        {
            prototypeInteractor.enabled = false;
        }

        transform.SetParent(targetCar);
        transform.localPosition = new Vector3(0f, 0.2f, 0f);

        playerCamera.transform.SetParent(null);
        cameraFollow.SetTarget(targetCar);
        ApplyCameraProfile(targetCar.name);
        cameraFollow.enabled = true;
        cameraFollow.SnapToTarget();

        carController.SetDriving(true);
    }

    private void ExitCar()
    {
        isInCar = false;
        carController.SetDriving(false);

        cameraFollow.enabled = false;

        transform.SetParent(null);
        Vector3 worldExitOffset = targetCar.TransformDirection(exitOffset);
        Vector3 exitPosition = targetCar.position + worldExitOffset;

        transform.position = exitPosition;
        transform.rotation = Quaternion.Euler(0f, targetCar.eulerAngles.y, 0f);

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (prototypeInteractor != null)
        {
            prototypeInteractor.enabled = true;
        }

        playerCamera.transform.SetParent(originalCameraParent);
        playerCamera.transform.localPosition = originalCameraLocalPosition;
        playerCamera.transform.localRotation = originalCameraLocalRotation;

        if (fpsController != null)
        {
            fpsController.enabled = true;
            fpsController.SetCursorLocked(true);
        }
    }

    private Transform FindClosestCarInRange()
    {
        float enterDistanceSqr = enterDistance * enterDistance;
        float bestDistanceSqr = float.MaxValue;
        Transform bestCar = null;

        for (int i = 0; i < availableCars.Count; i++)
        {
            Transform car = availableCars[i];
            if (car == null)
            {
                continue;
            }

            float currentDistanceSqr = (car.position - transform.position).sqrMagnitude;
            if (currentDistanceSqr > enterDistanceSqr || currentDistanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = currentDistanceSqr;
            bestCar = car;
        }

        return bestCar;
    }

    private void ConfigureDriveComponents(GameObject carObject)
    {
        if (carObject == null)
        {
            return;
        }

        CarController controller = carObject.GetComponent<CarController>();
        if (controller == null)
        {
            controller = carObject.AddComponent<CarController>();
            Debug.Log("PlayerInteract: CarController automatisch hinzugefuegt auf '" + carObject.name + "'.");
        }

        controller.SetForwardAxis(GetForwardAxisForVehicle(carObject.name));
    }

    private CarController.LocalForwardAxis GetForwardAxisForVehicle(string vehicleName)
    {
        if (string.Equals(vehicleName, "RV", System.StringComparison.OrdinalIgnoreCase))
        {
            return CarController.LocalForwardAxis.Forward;
        }

        return CarController.LocalForwardAxis.Right;
    }

    private void ApplyCameraProfile(string vehicleName)
    {
        if (cameraFollow == null || string.IsNullOrWhiteSpace(vehicleName))
        {
            return;
        }

        for (int i = 0; i < vehicleCameraProfiles.Length; i++)
        {
            VehicleCameraProfile profile = vehicleCameraProfiles[i];
            if (!string.Equals(profile.vehicleName, vehicleName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            cameraFollow.Configure(profile.offset, profile.targetPositionOffset, profile.followSpeed);
            return;
        }
    }
}
