using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private VehicleController truckController;
    [SerializeField] private ProceduralRoadSystem proceduralRoadSystem;

    [Header("Runtime Tracking")]
    [SerializeField] private float proceduralDistanceTravelled;
    [SerializeField] private float generatedRoadDistance;

    private Vector3 startPosition;

    public Transform TrackingTransform => truckController != null && truckController.IsOccupied
        ? truckController.transform
        : (playerController != null ? playerController.transform : null);

    public Transform CurrentThreatTarget => truckController != null && truckController.IsOccupied
        ? truckController.transform
        : (playerController != null ? playerController.transform : null);

    public float ProceduralDistanceTravelled => proceduralDistanceTravelled;
    public float GeneratedRoadDistance => generatedRoadDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Transform tracking = TrackingTransform;
        if (tracking != null)
        {
            startPosition = tracking.position;
        }

        if (proceduralRoadSystem != null && tracking != null)
        {
            proceduralRoadSystem.SetTarget(tracking);
        }
    }

    private void Update()
    {
        Transform tracking = TrackingTransform;
        if (tracking == null)
        {
            return;
        }

        proceduralDistanceTravelled = Vector3.Distance(startPosition, tracking.position);
        if (proceduralRoadSystem != null)
        {
            proceduralRoadSystem.SetTarget(tracking);
            generatedRoadDistance = Mathf.Max(0f, proceduralRoadSystem.FurthestSpawnedZ - startPosition.z);
        }
    }
}
