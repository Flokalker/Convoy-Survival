using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RoadGeneratorAutoSetup : MonoBehaviour
{
    [Header("Global Toggle")]
    [SerializeField] private bool enableRuntimeRoadGeneration = false;

    [Header("Scene Scope")]
    [SerializeField] private string targetSceneName = "BrowserPrototype";
    [SerializeField] private bool onlyWhenNoGeneratorExists = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstallInLoadedScene()
    {
        // Disabled by default: world stays static unless explicitly enabled in-scene.
    }

    private void Awake()
    {
        if (!enableRuntimeRoadGeneration)
        {
            RoadGenerator existing = FindObjectOfType<RoadGenerator>();
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            Destroy(gameObject);
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && activeScene.name != targetSceneName)
        {
            return;
        }

        RoadGenerator existingGenerator = FindObjectOfType<RoadGenerator>();
        if (onlyWhenNoGeneratorExists && existingGenerator != null)
        {
            return;
        }

        Transform truckOrPlayer = FindTruckOrPlayerTransform();
        if (truckOrPlayer == null)
        {
            Debug.LogWarning("RoadGeneratorAutoSetup: Could not find Truck/Player transform. Skipping setup.", this);
            return;
        }

        RoadGenerator roadGenerator = existingGenerator != null
            ? existingGenerator
            : new GameObject("RoadGenerator").AddComponent<RoadGenerator>();

        GeneratedTemplates templates = CreateDefaultTemplates();
        roadGenerator.Configure(truckOrPlayer, templates.roadChunks, templates.scrapPrefabs, templates.roadblockPrefabs);
    }

    private static Transform FindTruckOrPlayerTransform()
    {
        GameObject truck = FindWithTagSafe("Truck");
        if (truck != null)
        {
            return truck.transform;
        }

        GameObject player = FindWithTagSafe("Player");
        if (player != null)
        {
            return player.transform;
        }

        GameObject fallback = GameObject.Find("Player");
        return fallback != null ? fallback.transform : null;
    }

    private static GameObject FindWithTagSafe(string tagName)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tagName);
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private struct GeneratedTemplates
    {
        public GameObject[] roadChunks;
        public GameObject[] scrapPrefabs;
        public GameObject[] roadblockPrefabs;
    }

    private static GeneratedTemplates CreateDefaultTemplates()
    {
        GameObject templateRoot = new GameObject("__GeneratedRoadTemplates");
        templateRoot.SetActive(false);

        GameObject roadA = CreateRoadChunkTemplate("RoadChunk_Straight_A", 48f, 8f, 0f, templateRoot.transform);
        GameObject roadB = CreateRoadChunkTemplate("RoadChunk_Straight_B", 56f, 8f, 0f, templateRoot.transform);
        GameObject roadC = CreateRoadChunkTemplate("RoadChunk_SoftTurn", 52f, 8f, 7f, templateRoot.transform);

        GameObject scrap = CreateScrapTemplate(templateRoot.transform);
        GameObject roadblock = CreateRoadblockTemplate(templateRoot.transform);

        return new GeneratedTemplates
        {
            roadChunks = new[] { roadA, roadB, roadC },
            scrapPrefabs = new[] { scrap },
            roadblockPrefabs = new[] { roadblock }
        };
    }

    private static GameObject CreateRoadChunkTemplate(string name, float length, float width, float endYawDelta, Transform parent)
    {
        GameObject chunkRoot = new GameObject(name);
        chunkRoot.transform.SetParent(parent);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadMesh";
        road.transform.SetParent(chunkRoot.transform, false);
        road.transform.localScale = new Vector3(width, 1f, length);
        road.transform.localPosition = new Vector3(0f, -0.5f, length * 0.5f);

        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.SetParent(chunkRoot.transform, false);
        spawnPoint.transform.localPosition = new Vector3(0f, 0f, length);
        spawnPoint.transform.localRotation = Quaternion.Euler(0f, endYawDelta, 0f);

        GameObject scrapPoints = new GameObject("ScrapSpawnPoints");
        scrapPoints.transform.SetParent(chunkRoot.transform, false);
        CreatePoint(scrapPoints.transform, "ScrapPoint_A", new Vector3(-2.4f, 0.2f, length * 0.35f));
        CreatePoint(scrapPoints.transform, "ScrapPoint_B", new Vector3(2.2f, 0.2f, length * 0.6f));

        GameObject blockPoints = new GameObject("RoadblockSpawnPoints");
        blockPoints.transform.SetParent(chunkRoot.transform, false);
        CreatePoint(blockPoints.transform, "RoadblockPoint_A", new Vector3(0f, 0.2f, length * 0.7f));

        return chunkRoot;
    }

    private static GameObject CreateScrapTemplate(Transform parent)
    {
        GameObject root = new GameObject("ScrapPile_RuntimePrefab");
        root.transform.SetParent(parent);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.65f, 0.35f, 0.65f);
        body.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        return root;
    }

    private static GameObject CreateRoadblockTemplate(Transform parent)
    {
        GameObject root = new GameObject("Roadblock_RuntimePrefab");
        root.transform.SetParent(parent);

        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.transform.SetParent(root.transform, false);
        barrier.transform.localScale = new Vector3(4f, 1.5f, 1.2f);
        barrier.transform.localPosition = new Vector3(0f, 0.75f, 0f);

        return root;
    }

    private static void CreatePoint(Transform parent, string pointName, Vector3 localPosition)
    {
        GameObject point = new GameObject(pointName);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = localPosition;
    }
}
