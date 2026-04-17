using System.Collections.Generic;
using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class RoadSegmentSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TruckController truck;
        [SerializeField] private TradingPostSystem tradingPostSystem;
        [SerializeField] private GameObject roadSegmentPrefab;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private GameObject hazardPrefab;
        [SerializeField] private GameObject gatePrefab;
        [SerializeField] private GameObject tradingPropPrefab;

        [Header("Road")]
        [SerializeField, Min(40f)] private float segmentLength = 90f;
        [SerializeField, Min(120f)] private float spawnAheadDistance = 420f;
        [SerializeField, Min(60f)] private float recycleBehindDistance = 140f;
        [SerializeField, Min(2)] private int startupSegments = 7;
        [SerializeField, Min(2f)] private float laneWidth = 3.5f;

        [Header("Trading Posts")]
        [SerializeField, Min(500f)] private float tradingPostInterval = 5000f;

        private readonly List<SegmentRecord> activeSegments = new List<SegmentRecord>();
        private Transform segmentRoot;
        private float nextSegmentStartZ;
        private float runStartZ;
        private float nextTradingPostDistance;

        public float LaneWidth => laneWidth;

        public void ConfigureLaneSpacingOnTruck()
        {
            if (truck == null)
            {
                return;
            }

            truck.ConfigureLaneSettings(laneWidth, 1);
        }

        public void Configure(
            TruckController truckController,
            TradingPostSystem tradingSystem,
            GameObject roadPrefab,
            GameObject zombie,
            GameObject pickup,
            GameObject hazard,
            GameObject gate,
            GameObject tradingProp)
        {
            truck = truckController;
            tradingPostSystem = tradingSystem;
            roadSegmentPrefab = roadPrefab;
            zombiePrefab = zombie;
            pickupPrefab = pickup;
            hazardPrefab = hazard;
            gatePrefab = gate;
            tradingPropPrefab = tradingProp;
        }

        private void Start()
        {
            if (truck == null)
            {
                return;
            }

            segmentRoot = new GameObject("RoadSegments").transform;
            runStartZ = truck.transform.position.z;
            nextSegmentStartZ = Mathf.Floor(runStartZ / segmentLength) * segmentLength;
            nextTradingPostDistance = tradingPostInterval;

            for (int i = 0; i < startupSegments; i++)
            {
                SpawnNextSegment();
            }
        }

        private void Update()
        {
            if (truck == null)
            {
                return;
            }

            float targetAhead = truck.transform.position.z + spawnAheadDistance;
            while (nextSegmentStartZ < targetAhead)
            {
                SpawnNextSegment();
            }

            CleanupBehindTruck();
        }

        private void SpawnNextSegment()
        {
            float segmentStart = nextSegmentStartZ;
            float segmentEnd = segmentStart + segmentLength;
            float distanceAtEnd = Mathf.Max(0f, segmentEnd - runStartZ);
            bool isTradingSegment = distanceAtEnd >= nextTradingPostDistance;

            GameObject segmentObject = new GameObject(isTradingSegment ? "TradingSegment" : "RoadSegment");
            segmentObject.transform.SetParent(segmentRoot, false);

            SpawnRoadGeometry(segmentObject.transform, segmentStart);

            if (isTradingSegment)
            {
                SpawnTradingPostContent(segmentObject.transform, segmentStart);
                nextTradingPostDistance += tradingPostInterval;
            }
            else
            {
                SpawnRegularContent(segmentObject.transform, segmentStart);
            }

            activeSegments.Add(new SegmentRecord(segmentObject, segmentEnd));
            nextSegmentStartZ += segmentLength;
        }

        private void SpawnRoadGeometry(Transform parent, float segmentStart)
        {
            if (roadSegmentPrefab == null)
            {
                return;
            }

            GameObject road = Instantiate(roadSegmentPrefab, parent);
            road.name = "Road";
            road.transform.position = new Vector3(0f, 0f, segmentStart + segmentLength * 0.5f);

            Vector3 scale = road.transform.localScale;
            scale.z = segmentLength;
            road.transform.localScale = scale;
        }

        private void SpawnRegularContent(Transform parent, float segmentStart)
        {
            if (zombiePrefab == null || pickupPrefab == null || hazardPrefab == null)
            {
                return;
            }

            int zombieCount = Random.Range(2, 5);
            int pickupCount = Random.Range(3, 7);
            int hazardCount = Random.Range(1, 4);

            if (segmentStart < runStartZ + segmentLength * 1.5f)
            {
                zombieCount = 1;
                hazardCount = 1;
            }

            for (int i = 0; i < zombieCount; i++)
            {
                SpawnOnLane(zombiePrefab, parent, segmentStart, 0.9f, 10f);
            }

            for (int i = 0; i < pickupCount; i++)
            {
                SpawnOnLane(pickupPrefab, parent, segmentStart, 1.2f, 8f);
            }

            for (int i = 0; i < hazardCount; i++)
            {
                SpawnOnLane(hazardPrefab, parent, segmentStart, 0.75f, 12f);
            }
        }

        private void SpawnTradingPostContent(Transform parent, float segmentStart)
        {
            float centerZ = segmentStart + segmentLength * 0.5f;
            float gateZ = segmentStart + 14f;
            TradingGateController gateController = null;

            if (gatePrefab != null)
            {
                GameObject gate = Instantiate(gatePrefab, parent);
                gate.name = "TradingGate";
                gate.transform.position = new Vector3(0f, 0f, gateZ);
                gateController = gate.GetComponent<TradingGateController>();
                gateController?.SetBlocked(true);
            }

            if (tradingPropPrefab != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    float zOffset = -18f + (i / 2) * 20f;
                    GameObject prop = Instantiate(tradingPropPrefab, parent);
                    prop.transform.position = new Vector3(side * 8f, 0f, centerZ + zOffset);
                    prop.name = "TradingProp_" + (i + 1);
                }
            }

            if (pickupPrefab != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    SpawnOnLane(pickupPrefab, parent, segmentStart, 1.2f, 10f);
                }
            }

            GameObject zoneObject = new GameObject("TradingZone");
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.position = new Vector3(0f, 1f, gateZ - 3f);

            BoxCollider zoneCollider = zoneObject.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector3(20f, 4f, 18f);

            TradingPostZone zone = zoneObject.AddComponent<TradingPostZone>();
            zone.Configure(tradingPostSystem, gateController);
        }

        private void SpawnOnLane(GameObject prefab, Transform parent, float segmentStart, float y, float edgePadding)
        {
            if (prefab == null)
            {
                return;
            }

            int lane = Random.Range(-1, 2);
            float x = lane * laneWidth;
            float z = Random.Range(segmentStart + edgePadding, segmentStart + segmentLength - edgePadding);

            GameObject instance = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity, parent);
            instance.name = prefab.name;
        }

        private void CleanupBehindTruck()
        {
            float threshold = truck.transform.position.z - recycleBehindDistance;
            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                if (activeSegments[i].SegmentEndZ >= threshold)
                {
                    continue;
                }

                if (activeSegments[i].Root != null)
                {
                    Destroy(activeSegments[i].Root);
                }

                activeSegments.RemoveAt(i);
            }
        }

        private readonly struct SegmentRecord
        {
            public SegmentRecord(GameObject root, float segmentEndZ)
            {
                Root = root;
                SegmentEndZ = segmentEndZ;
            }

            public GameObject Root { get; }
            public float SegmentEndZ { get; }
        }
    }
}
