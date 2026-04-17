using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ProceduralRoadSystem : MonoBehaviour
{
    [System.Serializable]
    private struct ActiveChunk
    {
        public RoadChunk chunk;
        public float startZ;
        public float endZ;
    }

    [SerializeField] private Transform target;
    [SerializeField] private RoadChunk[] chunkPrefabs;
    [SerializeField, Min(2)] private int chunksAhead = 8;
    [SerializeField, Min(1)] private int chunksBehind = 3;
    [SerializeField, Min(1)] private int poolCopiesPerPrefab = 6;
    [SerializeField] private bool randomizeChunkOrder = true;

    private readonly Queue<ActiveChunk> activeChunks = new Queue<ActiveChunk>();
    private readonly Dictionary<RoadChunk, Queue<RoadChunk>> pooledChunks = new Dictionary<RoadChunk, Queue<RoadChunk>>();
    private float nextSpawnZ;
    private float furthestSpawnedZ;

    public float FurthestSpawnedZ => furthestSpawnedZ;

    private void Start()
    {
        if (target == null && GameManager.Instance != null)
        {
            target = GameManager.Instance.TrackingTransform;
        }

        BuildPool();

        float startZ = target != null ? target.position.z : transform.position.z;
        nextSpawnZ = startZ;

        for (int i = 0; i < chunksAhead; i++)
        {
            if (!SpawnNextChunk())
            {
                break;
            }
        }
    }

    private void Update()
    {
        if (target == null || chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            return;
        }

        while (nextSpawnZ < target.position.z + chunksAhead * AverageChunkLength())
        {
            if (!SpawnNextChunk())
            {
                break;
            }
        }

        CleanupBehindTarget();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void BuildPool()
    {
        pooledChunks.Clear();
        if (chunkPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < chunkPrefabs.Length; i++)
        {
            RoadChunk prefab = chunkPrefabs[i];
            if (prefab == null || pooledChunks.ContainsKey(prefab))
            {
                continue;
            }

            Queue<RoadChunk> pool = new Queue<RoadChunk>(poolCopiesPerPrefab);
            for (int j = 0; j < poolCopiesPerPrefab; j++)
            {
                RoadChunk instance = Instantiate(prefab, transform);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }

            pooledChunks.Add(prefab, pool);
        }
    }

    private bool SpawnNextChunk()
    {
        RoadChunk prefab = PickPrefab();
        if (prefab == null || !pooledChunks.TryGetValue(prefab, out Queue<RoadChunk> pool) || pool.Count == 0)
        {
            return false;
        }

        RoadChunk chunk = pool.Dequeue();
        chunk.transform.SetPositionAndRotation(new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity);
        chunk.gameObject.SetActive(true);

        float length = Mathf.Max(5f, chunk.ChunkLength);
        ActiveChunk active = new ActiveChunk
        {
            chunk = chunk,
            startZ = nextSpawnZ,
            endZ = nextSpawnZ + length
        };

        activeChunks.Enqueue(active);
        nextSpawnZ += length;
        furthestSpawnedZ = Mathf.Max(furthestSpawnedZ, active.endZ);
        return true;
    }

    private void CleanupBehindTarget()
    {
        float cleanupZ = target.position.z - chunksBehind * AverageChunkLength();
        while (activeChunks.Count > 0 && activeChunks.Peek().endZ < cleanupZ)
        {
            ActiveChunk oldChunk = activeChunks.Dequeue();
            oldChunk.chunk.gameObject.SetActive(false);

            RoadChunk sourcePrefab = FindPrefabReference(oldChunk.chunk);
            if (sourcePrefab != null && pooledChunks.TryGetValue(sourcePrefab, out Queue<RoadChunk> pool))
            {
                pool.Enqueue(oldChunk.chunk);
            }
            else
            {
                Destroy(oldChunk.chunk.gameObject);
            }
        }
    }

    private RoadChunk PickPrefab()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            return null;
        }

        if (!randomizeChunkOrder)
        {
            for (int i = 0; i < chunkPrefabs.Length; i++)
            {
                int index = (activeChunks.Count + i) % chunkPrefabs.Length;
                if (chunkPrefabs[index] != null)
                {
                    return chunkPrefabs[index];
                }
            }

            return null;
        }

        for (int attempts = 0; attempts < chunkPrefabs.Length; attempts++)
        {
            int randomIndex = Random.Range(0, chunkPrefabs.Length);
            if (chunkPrefabs[randomIndex] != null)
            {
                return chunkPrefabs[randomIndex];
            }
        }

        return null;
    }

    private RoadChunk FindPrefabReference(RoadChunk instance)
    {
        for (int i = 0; i < chunkPrefabs.Length; i++)
        {
            if (chunkPrefabs[i] != null && instance.name.StartsWith(chunkPrefabs[i].name))
            {
                return chunkPrefabs[i];
            }
        }

        return null;
    }

    private float AverageChunkLength()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            return 50f;
        }

        float total = 0f;
        int count = 0;
        for (int i = 0; i < chunkPrefabs.Length; i++)
        {
            if (chunkPrefabs[i] == null)
            {
                continue;
            }

            total += Mathf.Max(5f, chunkPrefabs[i].ChunkLength);
            count++;
        }

        return count == 0 ? 50f : total / count;
    }
}
