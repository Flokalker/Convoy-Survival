using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RoadGenerator : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform playerTruck;
    [SerializeField] private GameObject[] roadChunkPrefabs;

    [Header("Generation")]
    [SerializeField, Min(5f)] private float segmentLength = 50f;
    [SerializeField, Min(2)] private int preSpawnAmount = 8;
    [SerializeField, Min(10f)] private float safeDeleteDistance = 140f;
    [SerializeField, Min(5f)] private float spawnTriggerDistance = 70f;
    [SerializeField, Range(0f, 45f)] private float maxTurnAngleDelta = 12f;

    [Header("Pooling")]
    [SerializeField, Min(1)] private int poolCopiesPerPrefab = 4;
    [SerializeField] private bool disableInsteadOfDestroy = true;

    [Header("Segment Decor Spawning")]
    [SerializeField] private GameObject[] scrapPilePrefabs;
    [SerializeField, Range(0f, 1f)] private float scrapSpawnChance = 0.35f;
    [SerializeField, Min(0)] private int maxScrapPerSegment = 2;
    [SerializeField] private GameObject[] roadblockPrefabs;
    [SerializeField, Range(0f, 1f)] private float roadblockSpawnChance = 0.2f;
    [SerializeField, Min(0)] private int maxRoadblocksPerSegment = 1;

    private struct ActiveSegment
    {
        public GameObject gameObject;
        public Transform endSpawnPoint;
    }

    private readonly Queue<ActiveSegment> activeSegments = new Queue<ActiveSegment>();
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    private Vector3 nextSpawnPosition;
    private Quaternion nextSpawnRotation = Quaternion.identity;

    private void Start()
    {
        if (playerTruck == null)
        {
            Debug.LogError("RoadGenerator: Player truck transform is missing.", this);
            enabled = false;
            return;
        }

        if (roadChunkPrefabs == null || roadChunkPrefabs.Length == 0)
        {
            Debug.LogError("RoadGenerator: No road chunk prefabs assigned.", this);
            enabled = false;
            return;
        }

        nextSpawnPosition = transform.position;
        nextSpawnRotation = transform.rotation;

        BuildPools();
        for (int i = 0; i < preSpawnAmount; i++)
        {
            SpawnNextSegment();
        }
    }

    public void Configure(
        Transform truckTransform,
        GameObject[] chunkPrefabs,
        GameObject[] scrapPrefabs = null,
        GameObject[] blockPrefabs = null)
    {
        playerTruck = truckTransform;
        roadChunkPrefabs = chunkPrefabs;
        if (scrapPrefabs != null)
        {
            scrapPilePrefabs = scrapPrefabs;
        }

        if (blockPrefabs != null)
        {
            roadblockPrefabs = blockPrefabs;
        }
    }

    private void Update()
    {
        if (activeSegments.Count == 0)
        {
            SpawnNextSegment();
            return;
        }

        ActiveSegment latest = GetLatestSegment();
        float distanceToLastEnd = Vector3.Distance(playerTruck.position, latest.endSpawnPoint.position);
        if (distanceToLastEnd <= spawnTriggerDistance)
        {
            SpawnNextSegment();
        }

        CleanupOldSegments();
    }

    private void BuildPools()
    {
        pools.Clear();
        instanceToPrefab.Clear();

        for (int i = 0; i < roadChunkPrefabs.Length; i++)
        {
            GameObject prefab = roadChunkPrefabs[i];
            if (prefab == null || pools.ContainsKey(prefab))
            {
                continue;
            }

            Queue<GameObject> pool = new Queue<GameObject>(poolCopiesPerPrefab);
            for (int copy = 0; copy < poolCopiesPerPrefab; copy++)
            {
                GameObject instance = Instantiate(prefab, transform);
                instance.name = prefab.name;
                instance.SetActive(false);
                instanceToPrefab[instance] = prefab;
                pool.Enqueue(instance);
            }

            pools[prefab] = pool;
        }
    }

    private void SpawnNextSegment()
    {
        GameObject prefab = PickNextPrefab();
        if (prefab == null)
        {
            return;
        }

        GameObject segment = GetSegmentFromPool(prefab);
        segment.transform.SetPositionAndRotation(nextSpawnPosition, nextSpawnRotation);
        segment.SetActive(true);

        Transform spawnPoint = segment.transform.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            // Fallback keeps generation alive even if a prefab is missing SpawnPoint.
            Vector3 fallback = segment.transform.position + segment.transform.forward * segmentLength;
            nextSpawnPosition = fallback;
            nextSpawnRotation = segment.transform.rotation;
        }
        else
        {
            nextSpawnPosition = spawnPoint.position;
            nextSpawnRotation = spawnPoint.rotation;
        }

        SpawnDecorOnSegment(segment);

        activeSegments.Enqueue(new ActiveSegment
        {
            gameObject = segment,
            endSpawnPoint = spawnPoint != null ? spawnPoint : segment.transform
        });
    }

    private void CleanupOldSegments()
    {
        while (activeSegments.Count > 1)
        {
            ActiveSegment oldest = activeSegments.Peek();
            Vector3 toSegment = oldest.gameObject.transform.position - playerTruck.position;
            bool isBehindPlayer = Vector3.Dot(playerTruck.forward, toSegment) < 0f;
            bool isFarEnough = toSegment.sqrMagnitude >= safeDeleteDistance * safeDeleteDistance;

            if (!isBehindPlayer || !isFarEnough)
            {
                break;
            }

            activeSegments.Dequeue();
            DespawnSegment(oldest.gameObject);
        }
    }

    private GameObject PickNextPrefab()
    {
        if (roadChunkPrefabs == null || roadChunkPrefabs.Length == 0)
        {
            return null;
        }

        // Keep turns drivable for trucks by filtering chunks whose local forward
        // direction would deviate too much from the current road direction.
        List<GameObject> candidates = new List<GameObject>(roadChunkPrefabs.Length);
        for (int i = 0; i < roadChunkPrefabs.Length; i++)
        {
            GameObject prefab = roadChunkPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            Transform prefabSpawnPoint = prefab.transform.Find("SpawnPoint");
            Quaternion predictedExitRotation = prefabSpawnPoint != null
                ? nextSpawnRotation * prefabSpawnPoint.localRotation
                : nextSpawnRotation;

            float angle = Quaternion.Angle(nextSpawnRotation, predictedExitRotation);
            if (angle <= maxTurnAngleDelta)
            {
                candidates.Add(prefab);
            }
        }

        if (candidates.Count == 0)
        {
            // If everything is filtered out, prefer continuity over stalling.
            for (int i = 0; i < roadChunkPrefabs.Length; i++)
            {
                if (roadChunkPrefabs[i] != null)
                {
                    candidates.Add(roadChunkPrefabs[i]);
                }
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private GameObject GetSegmentFromPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        GameObject instance = Instantiate(prefab, transform);
        instance.name = prefab.name;
        instanceToPrefab[instance] = prefab;
        return instance;
    }

    private void DespawnSegment(GameObject segment)
    {
        if (segment == null)
        {
            return;
        }

        if (!instanceToPrefab.TryGetValue(segment, out GameObject prefab) || prefab == null)
        {
            if (disableInsteadOfDestroy)
            {
                segment.SetActive(false);
            }
            else
            {
                Destroy(segment);
            }

            return;
        }

        if (disableInsteadOfDestroy)
        {
            segment.SetActive(false);
            pools[prefab].Enqueue(segment);
        }
        else
        {
            Destroy(segment);
            instanceToPrefab.Remove(segment);
        }
    }

    private void SpawnDecorOnSegment(GameObject segment)
    {
        Transform runtimeParent = GetOrCreateRuntimeSpawnsRoot(segment.transform);
        ClearRuntimeChildren(runtimeParent);

        Transform scrapPoints = FindDeepChild(segment.transform, "ScrapSpawnPoints");
        Transform roadblockPoints = FindDeepChild(segment.transform, "RoadblockSpawnPoints");

        TrySpawnSet(scrapPilePrefabs, scrapPoints, scrapSpawnChance, maxScrapPerSegment, runtimeParent);
        TrySpawnSet(roadblockPrefabs, roadblockPoints, roadblockSpawnChance, maxRoadblocksPerSegment, runtimeParent);
    }

    private void TrySpawnSet(GameObject[] prefabs, Transform pointsRoot, float chance, int maxCount, Transform parent)
    {
        if (prefabs == null || prefabs.Length == 0 || pointsRoot == null || maxCount <= 0 || Random.value > chance)
        {
            return;
        }

        int spawnCount = Random.Range(1, maxCount + 1);
        List<Transform> points = new List<Transform>();
        for (int i = 0; i < pointsRoot.childCount; i++)
        {
            points.Add(pointsRoot.GetChild(i));
        }

        if (points.Count == 0)
        {
            return;
        }

        int clampedSpawnCount = Mathf.Min(spawnCount, points.Count);
        for (int i = 0; i < clampedSpawnCount; i++)
        {
            int pointIndex = Random.Range(0, points.Count);
            Transform point = points[pointIndex];
            points.RemoveAt(pointIndex);

            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
            if (selectedPrefab == null)
            {
                continue;
            }

            Instantiate(selectedPrefab, point.position, point.rotation, parent);
        }
    }

    private static Transform GetOrCreateRuntimeSpawnsRoot(Transform segment)
    {
        Transform existing = segment.Find("__RuntimeSpawns");
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject("__RuntimeSpawns");
        root.transform.SetParent(segment);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    private static void ClearRuntimeChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private ActiveSegment GetLatestSegment()
    {
        ActiveSegment latest = default;
        foreach (ActiveSegment segment in activeSegments)
        {
            latest = segment;
        }

        return latest;
    }
}
