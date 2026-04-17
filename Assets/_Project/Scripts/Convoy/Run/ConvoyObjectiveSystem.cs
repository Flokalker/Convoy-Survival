using ConvoySurvival.Core;
using ConvoySurvival.Run.World;
using UnityEngine;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    public class ConvoyObjectiveSystem : MonoBehaviour
    {
        [SerializeField] private TruckController truck;
        [SerializeField] private GameObject waypointPrefab;
        [SerializeField, Min(2f)] private float laneWidth = 3.5f;
        [SerializeField, Min(200f)] private float firstRadioPingMin = 500f;
        [SerializeField, Min(300f)] private float firstRadioPingMax = 900f;
        [SerializeField, Min(1200f)] private float radioPingSpacingMin = 1800f;
        [SerializeField, Min(1600f)] private float radioPingSpacingMax = 3200f;
        [SerializeField, Min(1500f)] private float waypointDistanceAheadMin = 2200f;
        [SerializeField, Min(1800f)] private float waypointDistanceAheadMax = 3600f;
        [SerializeField, Min(80f)] private float markerSpawnAheadDistance = 280f;
        [SerializeField, Min(1)] private int rewardMin = 18;
        [SerializeField, Min(2)] private int rewardMax = 40;

        private ObjectiveWaypointMarker activeMarker;
        private PrototypeSessionStateManager session;
        private float startZ;
        private float nextRadioDistance;
        private float pendingWaypointDistance = -1f;
        private int pendingReward;
        private string pendingTransmission = string.Empty;
        private string currentObjectiveText = "Awaiting radio contact...";

        public string CurrentObjectiveText => currentObjectiveText;

        public void Configure(TruckController truckController, GameObject waypointMarkerPrefab, float laneSpacing)
        {
            truck = truckController;
            waypointPrefab = waypointMarkerPrefab;
            laneWidth = Mathf.Max(1f, laneSpacing);
        }

        private void Start()
        {
            session = PrototypeSessionStateManager.Instance;
            if (truck != null)
            {
                startZ = truck.transform.position.z;
            }

            nextRadioDistance = Random.Range(firstRadioPingMin, firstRadioPingMax);
            SetObjectiveText("Radio: scanning channels for survivor signals.");
        }

        private void Update()
        {
            if (truck == null || waypointPrefab == null)
            {
                return;
            }

            if (activeMarker != null)
            {
                return;
            }

            float runDistance = Mathf.Max(0f, truck.transform.position.z - startZ);
            if (pendingWaypointDistance < 0f)
            {
                if (runDistance < nextRadioDistance)
                {
                    return;
                }

                BeginNewRadioObjective(runDistance);
                return;
            }

            float distanceToWaypoint = pendingWaypointDistance - runDistance;
            if (distanceToWaypoint > markerSpawnAheadDistance)
            {
                return;
            }

            SpawnObjectiveMarker();
        }

        public void CompleteCurrentObjective(ObjectiveWaypointMarker marker, int rewardScrap)
        {
            if (marker == null || marker != activeMarker)
            {
                return;
            }

            if (session != null && rewardScrap > 0)
            {
                session.AddScrap(rewardScrap);
            }

            SetObjectiveText("Complete: +" + rewardScrap + " scrap");
            Destroy(marker.gameObject);
            activeMarker = null;
            pendingWaypointDistance = -1f;
            pendingReward = 0;
            pendingTransmission = string.Empty;

            float runDistance = truck != null ? Mathf.Max(0f, truck.transform.position.z - startZ) : nextRadioDistance;
            nextRadioDistance = runDistance + Random.Range(radioPingSpacingMin, radioPingSpacingMax);
        }

        private void BeginNewRadioObjective(float runDistance)
        {
            pendingReward = Random.Range(rewardMin, rewardMax + 1);
            pendingWaypointDistance = runDistance + Random.Range(waypointDistanceAheadMin, waypointDistanceAheadMax);
            pendingTransmission = GetTransmissionSnippet();

            float km = pendingWaypointDistance / 1000f;
            SetObjectiveText(string.Format(
                "Radio: \"{0}\" Waypoint marked at {1:0.0} km (+{2} scrap)",
                pendingTransmission,
                km,
                pendingReward));
        }

        private void SpawnObjectiveMarker()
        {
            float targetWorldZ = startZ + pendingWaypointDistance;
            float spawnZ = Mathf.Max(targetWorldZ, truck.transform.position.z + 36f);
            Vector3 position = new Vector3(
                RandomLaneX(),
                1.5f,
                spawnZ);

            GameObject markerObject = Instantiate(waypointPrefab, position, Quaternion.identity);
            activeMarker = markerObject.GetComponent<ObjectiveWaypointMarker>();
            if (activeMarker == null)
            {
                activeMarker = markerObject.AddComponent<ObjectiveWaypointMarker>();
            }

            activeMarker.Configure(this, pendingReward);
            SetObjectiveText("Reach the waypoint signal and secure the cache.");
        }

        private float RandomLaneX()
        {
            int lane = Random.Range(-1, 2);
            return lane * laneWidth;
        }

        private void SetObjectiveText(string value)
        {
            currentObjectiveText = value;
        }

        private static string GetTransmissionSnippet()
        {
            string[] transmissions =
            {
                "Static... convoy survivors requesting escort.",
                "Mayday on emergency band. Supply cache still intact.",
                "This is Outpost Theta, we need fuel and parts.",
                "Unknown voice: gate code available for trusted runners."
            };

            return transmissions[Random.Range(0, transmissions.Length)];
        }
    }
}
