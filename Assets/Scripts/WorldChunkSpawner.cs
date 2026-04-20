using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldChunkSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private int chunkSize = 96;
    [SerializeField] private int viewDistance = 2;
    [SerializeField] private int fieldPropsPerChunk = 28;
    [SerializeField] private int lootCratesPerChunk = 4;
    [SerializeField] private bool useImportedAssets = true;
    [SerializeField] private bool useImportedStructures = true;
    [SerializeField] private bool useImportedRoads = true;

    private readonly Dictionary<Vector2Int, GameObject> spawnedChunks = new();
    private readonly Dictionary<string, Material> materialCache = new();
    private readonly Dictionary<string, GameObject> prefabCache = new();
    private readonly Dictionary<string, Material> importedMaterialCache = new();

    private static readonly string[] NatureTreePaths =
    {
        "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Tree_M_2.prefab",
        "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_TreeSapling_02B.prefab",
        "Assets/Stylized Nature Environment/Prefabs/tree_a.prefab",
        "Assets/Stylized Nature Environment/Prefabs/tree_c.prefab",
        "Assets/Stylized Nature Environment/Prefabs/tree_f.prefab",
        "Assets/Stylized Nature Environment/Prefabs/tree_i.prefab",
        "Assets/Stylized Nature Environment/Prefabs/tree_k.prefab",
        "Assets/LowpolyStreetPack/Prefabs/Foliage/Trees_Big/Tree_B_V01.prefab",
        "Assets/LowpolyStreetPack/Prefabs/Foliage/Trees_Big/Tree_B_V02.prefab",
        "Assets/LowpolyStreetPack/Prefabs/Foliage/Trees_Big/Tree_E_V02.prefab",
    };

    private static readonly string[] VillageHousePaths =
    {
        "Assets/Free_Building_01/Prefab/Building.prefab",
        "Assets/ModularHousePack1/Prefabs/Houses/House1.prefab",
        "Assets/ModularHousePack1/Prefabs/Houses/House4.prefab",
        "Assets/ModularHousePack1/Prefabs/Houses/House8.prefab",
        "Assets/UrbanBuilding/Prefabs/urban_building_Prefab.prefab",
    };

    private static readonly string[] StructurePropPaths =
    {
        "Assets/AssetsStore/Weel/Assets/Prefabs/well.prefab",
        "Assets/Low_Poly_Mini_Village/Prefabs/Well1.prefab",
        "Assets/Low_Poly_Mini_Village/Prefabs/Wooden_barrel.prefab",
    };

    private static readonly string[] BushPaths =
    {
        "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_ForestPlant_B_02.prefab",
        "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Grass_M_1.prefab",
        "Assets/LowpolyStreetPack/Prefabs/Foliage/Bushes/Bush_A.prefab",
        "Assets/LowpolyStreetPack/Prefabs/Foliage/Bushes/Bush_C.prefab",
        "Assets/LowpolyStreetPack/Prefabs/Foliage/Bushes/Bush_D.prefab",
    };

    private static readonly string[] RockPaths =
    {
        "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Rock_01.prefab",
        "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Stone_01.prefab",
        "Assets/Low_Poly_Mini_Village/Prefabs/Rock_1.prefab",
        "Assets/Low_Poly_Mini_Village/Prefabs/Rock_2.prefab",
        "Assets/Low_Poly_Mini_Village/Prefabs/Rock_3.prefab",
    };

    private static readonly string[] LampPaths =
    {
        "Assets/LowpolyStreetPack/Prefabs/StreetProps/Signs/StreetSigns/StreetSign_A.prefab",
        "Assets/LowpolyStreetPack/Prefabs/StreetProps/Signs/StreetSigns/StreetSign_C.prefab",
        "Assets/LowpolyStreetPack/Prefabs/StreetProps/Signs/StreetSigns/StreetSign_F.prefab",
    };

    private static readonly string[] RoadDecorationPaths =
    {
        "Assets/LowpolyStreetPack/Prefabs/StreetProps/Bench/Bench_A.prefab",
        "Assets/LowpolyStreetPack/Prefabs/StreetProps/MailBox/MailBox.prefab",
    };

    private const string RoadIntersectionPath = "Assets/LowpolyStreetPack/Prefabs/Roads/Streets/Road_Intersection_A.prefab";
    private const string RoadStraightDoublePath = "Assets/LowpolyStreetPack/Prefabs/Roads/Streets/Road_Streight_Double_A.prefab";
    private const string RoadCrosswalkDoublePath = "Assets/LowpolyStreetPack/Prefabs/Roads/Streets/Road_Crosswalk_Double.prefab";

    private static readonly string[] MountainPaths =
    {
        "Assets/Stylized Nature Environment/Prefabs/mountain.prefab",
    };

    private static readonly string[] GroundMaterialPaths =
    {
        "Assets/ALP_Assets/Realistic Terrain Textures Lite/Materials/Ground001.mat",
        "Assets/ALP_Assets/Realistic Terrain Textures Lite/Materials/Ground002.mat",
        "Assets/ALP_Assets/Realistic Terrain Textures Lite/Materials/Ground004.mat",
    };

    private const string RoadSurfaceMaterialPath = "Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_Pavement01.mat";
    private const string RoadShoulderMaterialPath = "Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_Rough01.mat";
    private const string LakePrefabPath = "Assets/ocean&lakeShaderPack/prefab/lakePrefab.prefab";
    private const string LakeShoreMaterialPath = "Assets/ocean&lakeShaderPack/other/sandMatirial.mat";

    private void Start()
    {
        EnsureBackdrop();
        RefreshChunks();
    }

    private void Update()
    {
        RefreshChunks();
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    private void RefreshChunks()
    {
        if (player == null)
        {
            BrowserFpsController fpsController = FindFirstObjectByType<BrowserFpsController>();
            if (fpsController != null)
            {
                player = fpsController.transform;
            }
        }

        Vector2Int centerChunk = player != null
            ? GetChunkCoordinate(player.position)
            : Vector2Int.zero;
        HashSet<Vector2Int> requiredChunks = new();

        for (int z = -viewDistance; z <= viewDistance; z++)
        {
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                Vector2Int chunkCoord = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                requiredChunks.Add(chunkCoord);

                if (!spawnedChunks.ContainsKey(chunkCoord))
                {
                    spawnedChunks.Add(chunkCoord, CreateChunk(chunkCoord));
                }
            }
        }

        List<Vector2Int> chunksToRemove = new();
        foreach (KeyValuePair<Vector2Int, GameObject> chunk in spawnedChunks)
        {
            if (!requiredChunks.Contains(chunk.Key))
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            Destroy(spawnedChunks[chunkCoord]);
            spawnedChunks.Remove(chunkCoord);
        }
    }

    private Vector2Int GetChunkCoordinate(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkSize);
        return new Vector2Int(chunkX, chunkZ);
    }

    private GameObject CreateChunk(Vector2Int coord)
    {
        GameObject chunkRoot = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkRoot.transform.SetParent(transform);
        chunkRoot.transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

        CreateFieldGround(chunkRoot.transform);
        CreateGrassVariation(chunkRoot.transform, coord);
        CreateGroundDetailScatter(chunkRoot.transform, coord);
        CreateRoadNetwork(chunkRoot.transform);
        CreateRoadShoulders(chunkRoot.transform);
        CreateLakeFeature(chunkRoot.transform, coord);
        CreateFences(chunkRoot.transform, coord);
        CreatePonds(chunkRoot.transform, coord);
        CreateFieldProps(chunkRoot.transform, coord);
        CreateRoadsideStructures(chunkRoot.transform, coord);
        CreateStreetProps(chunkRoot.transform, coord);
        CreateLootCrates(chunkRoot.transform, coord);

        return chunkRoot;
    }

    private bool IsSettlementChunk(Vector2Int coord)
    {
        return Mathf.Abs(coord.x) <= 1 && Mathf.Abs(coord.y) <= 1;
    }

    private bool IsForestChunk(Vector2Int coord)
    {
        return Mathf.Abs(coord.x) >= 2 || Mathf.Abs(coord.y) >= 2;
    }

    private void EnsureBackdrop()
    {
        if (transform.Find("Backdrop") != null)
        {
            return;
        }

        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(transform);
        backdrop.transform.localPosition = Vector3.zero;

        Vector3[] mountainPositions =
        {
            new Vector3(48f, -1f, 210f),
            new Vector3(48f, -1f, -115f),
            new Vector3(210f, -1f, 48f),
            new Vector3(-115f, -1f, 48f),
            new Vector3(180f, -1f, 180f),
            new Vector3(-80f, -1f, 175f),
            new Vector3(185f, -1f, -85f),
            new Vector3(-95f, -1f, -95f),
        };

        for (int index = 0; index < mountainPositions.Length; index++)
        {
            float baseScale = 2.8f + index * 0.2f;
            Quaternion rotation = Quaternion.Euler(0f, index * 45f, 0f);
            TrySpawnVariant(MountainPaths, backdrop.transform, mountainPositions[index], rotation, Vector3.one * baseScale);
        }
    }

    private void CreateFieldGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "FieldGround";
        ground.transform.SetParent(parent);
        ground.transform.localPosition = new Vector3(chunkSize * 0.5f, 0f, chunkSize * 0.5f);
        ground.transform.localScale = new Vector3(chunkSize / 10f, 1f, chunkSize / 10f);
        Renderer renderer = ground.GetComponent<Renderer>();
        Material importedGround = LoadMaterial(GroundMaterialPaths[0]);
        if (importedGround != null)
        {
            renderer.material = importedGround;
            renderer.material.mainTextureScale = new Vector2(10f, 10f);
        }
        else
        {
            renderer.material = CreateMaterial(new Color(0.6f, 0.73f, 0.34f), 0f, 0.1f);
        }
    }

    private void CreateGrassVariation(Transform parent, Vector2Int coord)
    {
        for (int index = 0; index < 18; index++)
        {
            float x = Hash01(coord.x, coord.y, 30 + index * 5) * (chunkSize - 18f) + 9f;
            float z = Hash01(coord.x, coord.y, 31 + index * 5) * (chunkSize - 18f) + 9f;
            float width = Mathf.Lerp(5f, 16f, Hash01(coord.x, coord.y, 32 + index * 5));
            float depth = Mathf.Lerp(5f, 16f, Hash01(coord.x, coord.y, 33 + index * 5));
            if (IsOnRoadArea(x, z, 18f))
            {
                continue;
            }

            Color tint = Color.Lerp(
                new Color(0.48f, 0.64f, 0.26f),
                new Color(0.76f, 0.84f, 0.46f),
                Hash01(coord.x, coord.y, 34 + index * 5));
            float height = Mathf.Lerp(0.025f, 0.08f, Hash01(coord.x, coord.y, 35 + index * 5));
            CreateBox("GrassPatch", parent, new Vector3(x, height * 0.5f, z), new Vector3(width, height, depth), tint);
        }
    }

    private void CreateGroundDetailScatter(Transform parent, Vector2Int coord)
    {
        for (int index = 0; index < 20; index++)
        {
            float x = Hash01(coord.x, coord.y, 900 + index * 7) * (chunkSize - 10f) + 5f;
            float z = Hash01(coord.x, coord.y, 901 + index * 7) * (chunkSize - 10f) + 5f;
            if (IsOnRoadArea(x, z, 15f))
            {
                continue;
            }

            float selector = Hash01(coord.x, coord.y, 902 + index * 7);
            if (selector > 0.55f)
            {
                Vector3 scale = Vector3.one * Mathf.Lerp(0.8f, 1.8f, Hash01(coord.x, coord.y, 903 + index * 7));
                TrySpawnVariant(
                    BushPaths,
                    parent,
                    new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, Hash01(coord.x, coord.y, 904 + index * 7) * 360f, 0f),
                    scale);
            }
            else if (selector > 0.25f)
            {
                Vector3 scale = Vector3.one * Mathf.Lerp(0.7f, 1.35f, Hash01(coord.x, coord.y, 905 + index * 7));
                TrySpawnVariant(
                    RockPaths,
                    parent,
                    new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, Hash01(coord.x, coord.y, 906 + index * 7) * 360f, 0f),
                    scale);
            }
            else
            {
                Color dirtTint = Color.Lerp(
                    new Color(0.48f, 0.55f, 0.24f),
                    new Color(0.68f, 0.71f, 0.36f),
                    Hash01(coord.x, coord.y, 907 + index * 7));
                float width = Mathf.Lerp(2.5f, 6.5f, Hash01(coord.x, coord.y, 908 + index * 7));
                float depth = Mathf.Lerp(2.5f, 6.5f, Hash01(coord.x, coord.y, 909 + index * 7));
                GameObject patch = CreateBox("GroundPatch", parent, new Vector3(x, 0.01f, z), new Vector3(width, 0.02f, depth), dirtTint);
                Material importedPatch = LoadMaterial(GroundMaterialPaths[(index % (GroundMaterialPaths.Length - 1)) + 1]);
                if (importedPatch != null)
                {
                    Renderer patchRenderer = patch.GetComponent<Renderer>();
                    patchRenderer.material = importedPatch;
                    patchRenderer.material.mainTextureScale = new Vector2(Mathf.Max(1f, width * 0.35f), Mathf.Max(1f, depth * 0.35f));
                }
            }
        }
    }

    private void CreateLakeFeature(Transform parent, Vector2Int coord)
    {
        if (coord != Vector2Int.zero)
        {
            return;
        }

        GameObject lakePrefab = LoadPrefab(LakePrefabPath);
        Material shoreMaterial = LoadMaterial(LakeShoreMaterialPath);

        Vector3 lakeCenter = new Vector3(chunkSize * 0.5f + 18f, -0.35f, chunkSize * 0.5f - 26f);

        GameObject shore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shore.name = "LakeShore";
        shore.transform.SetParent(parent);
        shore.transform.localPosition = new Vector3(lakeCenter.x, -0.28f, lakeCenter.z);
        shore.transform.localScale = new Vector3(2.9f, 0.08f, 2.35f);
        Renderer shoreRenderer = shore.GetComponent<Renderer>();
        if (shoreMaterial != null)
        {
            shoreRenderer.material = shoreMaterial;
            shoreRenderer.material.mainTextureScale = new Vector2(4f, 4f);
        }
        else
        {
            shoreRenderer.material = CreateMaterial(new Color(0.76f, 0.68f, 0.49f), 0f, 0.08f);
        }

        if (lakePrefab != null)
        {
            GameObject lake = Instantiate(lakePrefab, parent);
            lake.name = "Lake";
            lake.transform.localPosition = lakeCenter;
            lake.transform.localRotation = Quaternion.identity;
            lake.transform.localScale = new Vector3(9f, 1f, 7f);
        }

        for (int index = 0; index < 10; index++)
        {
            float angle = index / 10f * Mathf.PI * 2f;
            Vector3 ringPosition = new Vector3(
                lakeCenter.x + Mathf.Cos(angle) * 18f,
                0f,
                lakeCenter.z + Mathf.Sin(angle) * 14f);

            TrySpawnVariant(
                BushPaths,
                parent,
                ringPosition,
                Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f),
                Vector3.one * 1.1f);
        }
    }

    private void CreateRoadNetwork(Transform parent)
    {
        if (useImportedRoads && TryCreateImportedRoadNetwork(parent))
        {
            return;
        }

        float roadHeight = 0.06f;
        float mainRoadWidth = 14f;
        float crossRoadWidth = 12f;

        CreateBox("RoadNorthSouth", parent, new Vector3(chunkSize * 0.5f, roadHeight, chunkSize * 0.5f), new Vector3(mainRoadWidth, 0.12f, chunkSize), new Color(0.23f, 0.24f, 0.25f));
        CreateBox("RoadEastWest", parent, new Vector3(chunkSize * 0.5f, roadHeight + 0.01f, chunkSize * 0.5f), new Vector3(chunkSize, 0.1f, crossRoadWidth), new Color(0.23f, 0.24f, 0.25f));
        CreateBox("IntersectionPlate", parent, new Vector3(chunkSize * 0.5f, roadHeight + 0.02f, chunkSize * 0.5f), new Vector3(18f, 0.14f, 18f), new Color(0.24f, 0.25f, 0.26f));

        CreateBox("EdgeLineWest", parent, new Vector3(chunkSize * 0.5f - 6f, 0.13f, chunkSize * 0.5f), new Vector3(0.18f, 0.02f, chunkSize), new Color(0.95f, 0.93f, 0.84f));
        CreateBox("EdgeLineEast", parent, new Vector3(chunkSize * 0.5f + 6f, 0.13f, chunkSize * 0.5f), new Vector3(0.18f, 0.02f, chunkSize), new Color(0.95f, 0.93f, 0.84f));
        CreateBox("EdgeLineNorth", parent, new Vector3(chunkSize * 0.5f, 0.13f, chunkSize * 0.5f - 5f), new Vector3(chunkSize, 0.02f, 0.18f), new Color(0.95f, 0.93f, 0.84f));
        CreateBox("EdgeLineSouth", parent, new Vector3(chunkSize * 0.5f, 0.13f, chunkSize * 0.5f + 5f), new Vector3(chunkSize, 0.02f, 0.18f), new Color(0.95f, 0.93f, 0.84f));

        for (int z = 8; z < chunkSize; z += 12)
        {
            CreateBox("LaneMarkNS", parent, new Vector3(chunkSize * 0.5f, 0.13f, z), new Vector3(0.45f, 0.02f, 5f), new Color(0.95f, 0.84f, 0.46f));
        }

        for (int x = 8; x < chunkSize; x += 12)
        {
            CreateBox("LaneMarkEW", parent, new Vector3(x, 0.14f, chunkSize * 0.5f), new Vector3(5f, 0.02f, 0.45f), new Color(0.95f, 0.84f, 0.46f));
        }
    }

    private void CreateRoadShoulders(Transform parent)
    {
        if (useImportedRoads)
        {
            return;
        }

        float center = chunkSize * 0.5f;
        float shoulderHeight = 0.03f;

        CreateBox("ShoulderNorth", parent, new Vector3(center, shoulderHeight, center - 9.5f), new Vector3(chunkSize, 0.05f, 3f), new Color(0.73f, 0.67f, 0.48f));
        CreateBox("ShoulderSouth", parent, new Vector3(center, shoulderHeight, center + 9.5f), new Vector3(chunkSize, 0.05f, 3f), new Color(0.73f, 0.67f, 0.48f));
        CreateBox("ShoulderWest", parent, new Vector3(center - 10.5f, shoulderHeight, center), new Vector3(3f, 0.05f, chunkSize), new Color(0.73f, 0.67f, 0.48f));
        CreateBox("ShoulderEast", parent, new Vector3(center + 10.5f, shoulderHeight, center), new Vector3(3f, 0.05f, chunkSize), new Color(0.73f, 0.67f, 0.48f));
    }

    private void CreateFences(Transform parent, Vector2Int coord)
    {
        if (IsSettlementChunk(coord))
        {
            return;
        }

        if (Hash01(coord.x, coord.y, 400) < 0.35f)
        {
            return;
        }

        float lineOffset = Hash01(coord.x, coord.y, 401) > 0.5f ? 24f : -24f;
        bool horizontal = Hash01(coord.x, coord.y, 402) > 0.5f;

        for (int i = 8; i < chunkSize; i += 10)
        {
            Vector3 postPosition = horizontal
                ? new Vector3(i, 0.6f, chunkSize * 0.5f + lineOffset)
                : new Vector3(chunkSize * 0.5f + lineOffset, 0.6f, i);
            CreateBox("FencePost", parent, postPosition, new Vector3(0.22f, 1.2f, 0.22f), new Color(0.57f, 0.4f, 0.22f));

            if (i >= chunkSize - 10)
            {
                continue;
            }

            Vector3 railPosition = horizontal
                ? new Vector3(i + 5f, 0.82f, chunkSize * 0.5f + lineOffset)
                : new Vector3(chunkSize * 0.5f + lineOffset, 0.82f, i + 5f);
            Vector3 railScale = horizontal
                ? new Vector3(10f, 0.12f, 0.12f)
                : new Vector3(0.12f, 0.12f, 10f);
            CreateBox("FenceRail", parent, railPosition, railScale, new Color(0.76f, 0.63f, 0.38f));
            railPosition.y += 0.38f;
            CreateBox("FenceRailTop", parent, railPosition, railScale, new Color(0.79f, 0.66f, 0.41f));
        }
    }

    private void CreatePonds(Transform parent, Vector2Int coord)
    {
        for (int index = 0; index < 2; index++)
        {
            if (Hash01(coord.x, coord.y, 200 + index) < 0.55f)
            {
                continue;
            }

            float x = Hash01(coord.x, coord.y, 220 + index * 5) * (chunkSize - 26f) + 13f;
            float z = Hash01(coord.x, coord.y, 221 + index * 5) * (chunkSize - 26f) + 13f;
            if (IsOnRoadArea(x, z, 18f))
            {
                continue;
            }

            float width = Mathf.Lerp(8f, 14f, Hash01(coord.x, coord.y, 222 + index * 5));
            float depth = Mathf.Lerp(7f, 13f, Hash01(coord.x, coord.y, 223 + index * 5));

            CreateBox("PondBasin", parent, new Vector3(x, -0.2f, z), new Vector3(width, 0.35f, depth), new Color(0.42f, 0.49f, 0.31f));
            CreateBox("PondWater", parent, new Vector3(x, -0.05f, z), new Vector3(width - 1f, 0.08f, depth - 1f), new Color(0.29f, 0.58f, 0.68f));
        }
    }

    private void CreateFieldProps(Transform parent, Vector2Int coord)
    {
        int propCount = IsForestChunk(coord) ? fieldPropsPerChunk + 10 : IsSettlementChunk(coord) ? Mathf.Max(10, fieldPropsPerChunk - 12) : fieldPropsPerChunk;
        for (int index = 0; index < propCount; index++)
        {
            float x = Hash01(coord.x, coord.y, index * 19 + 100) * (chunkSize - 18f) + 9f;
            float z = Hash01(coord.x, coord.y, index * 19 + 101) * (chunkSize - 18f) + 9f;
            if (IsOnRoadArea(x, z, 16f))
            {
                continue;
            }

            float propChoice = Hash01(coord.x, coord.y, index * 19 + 102);
            if (IsSettlementChunk(coord))
            {
                if (propChoice > 0.75f)
                {
                    CreateTree(parent, coord, index, x, z);
                }
                else if (propChoice > 0.35f)
                {
                    CreateBush(parent, coord, index, x, z);
                }
                else
                {
                    CreateRock(parent, coord, index, x, z);
                }

                continue;
            }

            if (propChoice > 0.42f)
            {
                CreateTree(parent, coord, index, x, z);
            }
            else if (propChoice > 0.22f)
            {
                CreateRock(parent, coord, index, x, z);
            }
            else
            {
                if (Hash01(coord.x, coord.y, index * 19 + 103) > 0.5f)
                {
                    CreateHayBale(parent, coord, index, x, z);
                }
                else
                {
                    CreateBush(parent, coord, index, x, z);
                }
            }
        }
    }

    private void CreateRock(Transform parent, Vector2Int coord, int index, float x, float z)
    {
        if (useImportedAssets)
        {
            Vector3 scale = Vector3.one * Mathf.Lerp(0.85f, 1.75f, Hash01(coord.x, coord.y, index * 13 + 810));
            if (TrySpawnVariant(
                RockPaths,
                parent,
                new Vector3(x, 0f, z),
                Quaternion.Euler(0f, Hash01(coord.x, coord.y, index * 13 + 811) * 360f, 0f),
                scale))
            {
                return;
            }
        }

        float width = Mathf.Lerp(1.6f, 3.4f, Hash01(coord.x, coord.y, index * 13 + 812));
        float height = Mathf.Lerp(1f, 2.2f, Hash01(coord.x, coord.y, index * 13 + 813));
        CreatePrimitivePart(
            PrimitiveType.Sphere,
            "Rock",
            parent,
            new Vector3(x, height * 0.35f, z),
            new Vector3(width, height, width * 0.9f),
            new Color(0.49f, 0.47f, 0.43f));
    }

    private void CreateTree(Transform parent, Vector2Int coord, int index, float x, float z)
    {
        if (TryCreateImportedTree(parent, coord, index, x, z))
        {
            return;
        }

        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.localPosition = new Vector3(x, 0f, z);

        float trunkHeight = Mathf.Lerp(2.6f, 4.2f, Hash01(coord.x, coord.y, index * 11 + 10));
        float crownHeight = Mathf.Lerp(2.2f, 3.6f, Hash01(coord.x, coord.y, index * 11 + 11));

        CreatePrimitivePart(PrimitiveType.Cylinder, "Trunk", tree.transform, new Vector3(0f, trunkHeight * 0.5f, 0f), new Vector3(0.45f, trunkHeight * 0.5f, 0.45f), new Color(0.44f, 0.28f, 0.16f));
        CreatePrimitivePart(PrimitiveType.Sphere, "Crown", tree.transform, new Vector3(0f, trunkHeight + crownHeight * 0.3f, 0f), new Vector3(2.8f, crownHeight, 2.8f), Color.Lerp(new Color(0.33f, 0.58f, 0.24f), new Color(0.41f, 0.67f, 0.28f), Hash01(coord.x, coord.y, index * 11 + 12)));
    }

    private void CreateHayBale(Transform parent, Vector2Int coord, int index, float x, float z)
    {
        float width = Mathf.Lerp(1.8f, 2.6f, Hash01(coord.x, coord.y, index * 13 + 20));
        float depth = Mathf.Lerp(1.8f, 2.8f, Hash01(coord.x, coord.y, index * 13 + 21));
        float height = Mathf.Lerp(1.1f, 1.8f, Hash01(coord.x, coord.y, index * 13 + 22));

        CreateBox("HayBale", parent, new Vector3(x, height * 0.5f, z), new Vector3(width, height, depth), new Color(0.82f, 0.71f, 0.3f));
    }

    private void CreateBush(Transform parent, Vector2Int coord, int index, float x, float z)
    {
        if (TryCreateImportedBush(parent, coord, index, x, z))
        {
            return;
        }

        float width = Mathf.Lerp(1.6f, 2.8f, Hash01(coord.x, coord.y, index * 7 + 50));
        float height = Mathf.Lerp(1f, 1.8f, Hash01(coord.x, coord.y, index * 7 + 51));
        CreatePrimitivePart(
            PrimitiveType.Sphere,
            "Bush",
            parent,
            new Vector3(x, height * 0.45f, z),
            new Vector3(width, height, width),
            Color.Lerp(new Color(0.38f, 0.63f, 0.26f), new Color(0.55f, 0.74f, 0.32f), Hash01(coord.x, coord.y, index * 7 + 52)));
    }

    private void CreateRoadsideStructures(Transform parent, Vector2Int coord)
    {
        if (useImportedStructures && IsSettlementChunk(coord))
        {
            CreateStructureCluster(parent, coord);
        }
    }

    private void CreateStreetProps(Transform parent, Vector2Int coord)
    {
        if (!IsSettlementChunk(coord))
        {
            return;
        }

        Vector3[] lampPositions =
        {
            new Vector3(chunkSize * 0.5f - 11.5f, 0f, 16f),
            new Vector3(chunkSize * 0.5f + 11.5f, 0f, 40f),
            new Vector3(16f, 0f, chunkSize * 0.5f - 11.5f),
            new Vector3(40f, 0f, chunkSize * 0.5f + 11.5f),
        };

        for (int index = 0; index < lampPositions.Length; index++)
        {
            TrySpawnVariant(LampPaths, parent, lampPositions[index], Quaternion.Euler(0f, Hash01(coord.x, coord.y, 700 + index) * 360f, 0f), Vector3.one);
        }

        if (Hash01(coord.x, coord.y, 711) > 0.55f)
        {
            Vector3 decorationPosition = new Vector3(chunkSize * 0.5f - 14f, 0f, chunkSize * 0.5f + 14f);
            TrySpawnVariant(RoadDecorationPaths, parent, decorationPosition, Quaternion.Euler(0f, 45f, 0f), Vector3.one);
        }

        if (Hash01(coord.x, coord.y, 712) > 0.3f)
        {
            Vector3 leftDecoration = new Vector3(chunkSize * 0.5f - 14f, 0f, 18f);
            TrySpawnVariant(RoadDecorationPaths, parent, leftDecoration, Quaternion.Euler(0f, 120f, 0f), Vector3.one * 1.05f);
        }

        if (Hash01(coord.x, coord.y, 713) > 0.3f)
        {
            Vector3 rightDecoration = new Vector3(18f, 0f, chunkSize * 0.5f + 14f);
            TrySpawnVariant(RoadDecorationPaths, parent, rightDecoration, Quaternion.Euler(0f, 210f, 0f), Vector3.one * 1.05f);
        }
    }

    private void CreateFarmStand(Transform parent, Vector2Int coord)
    {
        float zOffset = Hash01(coord.x, coord.y, 501) > 0.5f ? 22f : -22f;
        Vector3 basePosition = new Vector3(chunkSize * 0.5f + 18f, 0f, chunkSize * 0.5f + zOffset);
        GameObject stand = new GameObject("FarmStand");
        stand.transform.SetParent(parent);
        stand.transform.localPosition = basePosition;

        CreateBox("StandFloor", stand.transform, new Vector3(0f, 0.2f, 0f), new Vector3(8f, 0.4f, 6f), new Color(0.73f, 0.59f, 0.35f));
        CreateBox("StandRoof", stand.transform, new Vector3(0f, 3f, 0f), new Vector3(9f, 0.45f, 7f), new Color(0.83f, 0.34f, 0.24f));
        CreateBox("PostA", stand.transform, new Vector3(-3.2f, 1.5f, -2.2f), new Vector3(0.28f, 3f, 0.28f), new Color(0.58f, 0.4f, 0.23f));
        CreateBox("PostB", stand.transform, new Vector3(3.2f, 1.5f, -2.2f), new Vector3(0.28f, 3f, 0.28f), new Color(0.58f, 0.4f, 0.23f));
        CreateBox("PostC", stand.transform, new Vector3(-3.2f, 1.5f, 2.2f), new Vector3(0.28f, 3f, 0.28f), new Color(0.58f, 0.4f, 0.23f));
        CreateBox("PostD", stand.transform, new Vector3(3.2f, 1.5f, 2.2f), new Vector3(0.28f, 3f, 0.28f), new Color(0.58f, 0.4f, 0.23f));
        CreateBox("Counter", stand.transform, new Vector3(0f, 1f, 1.6f), new Vector3(7.2f, 1.2f, 1.4f), new Color(0.79f, 0.64f, 0.39f));
        CreateBox("CanopyStripe", stand.transform, new Vector3(0f, 2.55f, 0f), new Vector3(8.4f, 0.12f, 6.2f), new Color(0.97f, 0.89f, 0.76f));
    }

    private void CreateStructureCluster(Transform parent, Vector2Int coord)
    {
        Vector3[] structurePositions =
        {
            new Vector3(chunkSize * 0.5f + 20f, 0f, chunkSize * 0.5f - 24f),
            new Vector3(chunkSize * 0.5f - 24f, 0f, chunkSize * 0.5f + 22f),
        };

        int structureIndex = Mathf.Abs(coord.x * 17 + coord.y * 31);
        for (int index = 0; index < structurePositions.Length; index++)
        {
            string prefabPath = VillageHousePaths[(structureIndex + index) % VillageHousePaths.Length];
            GameObject prefab = LoadPrefab(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            GameObject instance = Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.localPosition = structurePositions[index];
            instance.transform.localRotation = Quaternion.Euler(0f, ((structureIndex + index * 3) * 47f) % 360f, 0f);
            instance.transform.localScale = GetStructureScale(prefabPath);
        }

        CreateStructureProp(parent, coord, new Vector3(chunkSize * 0.5f + 26f, 0f, chunkSize * 0.5f + 24f));
    }

    private void CreateStructureProp(Transform parent, Vector2Int coord, Vector3 position)
    {
        Vector3 scale = Vector3.one * Mathf.Lerp(0.9f, 1.2f, Hash01(coord.x, coord.y, 543));
        TrySpawnVariant(
            StructurePropPaths,
            parent,
            position,
            Quaternion.Euler(0f, Hash01(coord.x, coord.y, 544) * 360f, 0f),
            scale);
    }

    private void CreateLootCrates(Transform parent, Vector2Int coord)
    {
        int created = 0;
        int attempts = lootCratesPerChunk * 4;

        for (int index = 0; index < attempts && created < lootCratesPerChunk; index++)
        {
            float x = Hash01(coord.x, coord.y, index * 17 + 601) * (chunkSize - 20f) + 10f;
            float z = Hash01(coord.x, coord.y, index * 17 + 602) * (chunkSize - 20f) + 10f;
            if (!IsNearRoad(x, z, 10f))
            {
                continue;
            }

            GameObject crateRoot = new GameObject("RoadsideCrate");
            crateRoot.transform.SetParent(parent);
            crateRoot.transform.localPosition = new Vector3(x, 0.6f, z);

            BoxCollider rootCollider = crateRoot.AddComponent<BoxCollider>();
            rootCollider.isTrigger = true;
            rootCollider.center = new Vector3(0f, 0.55f, 0f);
            rootCollider.size = new Vector3(1.8f, 1.4f, 1.6f);

            GameObject crate = CreateBox("CrateBody", crateRoot.transform, Vector3.zero, new Vector3(1.4f, 1f, 1.2f), new Color(0.61f, 0.43f, 0.23f));
            CreateBox("CrateLid", crateRoot.transform, new Vector3(0f, 0.62f, 0f), new Vector3(1.52f, 0.18f, 1.32f), new Color(0.74f, 0.58f, 0.31f));
            CreateBox("CrateBandA", crateRoot.transform, new Vector3(0f, 0f, 0.42f), new Vector3(1.5f, 1.02f, 0.1f), new Color(0.24f, 0.2f, 0.18f));
            CreateBox("CrateBandB", crateRoot.transform, new Vector3(0f, 0f, -0.42f), new Vector3(1.5f, 1.02f, 0.1f), new Color(0.24f, 0.2f, 0.18f));

            LootChest collectible = crateRoot.AddComponent<LootChest>();
            collectible.Configure($"chest_{coord.x}_{coord.y}_{created}", "Loot Chest", 1, "E = Öffnen");

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(crateRoot.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = "Supply";
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.08f;
            textMesh.color = new Color(0.08f, 0.11f, 0.14f);
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            labelObject.AddComponent<BrowserPrototypeBillboard>();

            created++;
        }
    }

    private bool TryCreateImportedRoadNetwork(Transform parent)
    {
        GameObject intersectionPrefab = LoadPrefab(RoadIntersectionPath);
        GameObject straightPrefab = LoadPrefab(RoadStraightDoublePath);
        GameObject crosswalkPrefab = LoadPrefab(RoadCrosswalkDoublePath);
        if (intersectionPrefab == null || straightPrefab == null)
        {
            return false;
        }

        float center = chunkSize * 0.5f;
        float[] offsets = { -40f, -30f, -20f, -10f, 10f, 20f, 30f, 40f };

        SpawnRoadPiece(intersectionPrefab, "RoadIntersection", parent, new Vector3(center, 0.02f, center), 0f);

        for (int index = 0; index < offsets.Length; index++)
        {
            float offset = offsets[index];
            bool nearCenter = Mathf.Abs(offset) <= 10.1f;
            GameObject verticalPrefab = nearCenter && crosswalkPrefab != null ? crosswalkPrefab : straightPrefab;
            GameObject horizontalPrefab = nearCenter && crosswalkPrefab != null ? crosswalkPrefab : straightPrefab;

            SpawnRoadPiece(verticalPrefab, $"RoadVertical_{index}", parent, new Vector3(center, 0.02f, center + offset), 0f);
            SpawnRoadPiece(horizontalPrefab, $"RoadHorizontal_{index}", parent, new Vector3(center + offset, 0.021f, center), 90f);
        }

        ApplyRoadMaterials(parent);

        return true;
    }

    private void SpawnRoadPiece(GameObject prefab, string objectName, Transform parent, Vector3 localPosition, float yRotation)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(-90f, yRotation, 0f);
        instance.transform.localScale = Vector3.one;
    }

    private void ApplyRoadMaterials(Transform parent)
    {
        Material roadMaterial = LoadMaterial(RoadSurfaceMaterialPath);
        Material shoulderMaterial = LoadMaterial(RoadShoulderMaterialPath);
        if (roadMaterial == null && shoulderMaterial == null)
        {
            return;
        }

        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            string lowerName = renderer.gameObject.name.ToLowerInvariant();
            Material selected = lowerName.Contains("crosswalk") || lowerName.Contains("intersection") || lowerName.Contains("road")
                ? roadMaterial
                : shoulderMaterial;

            if (selected == null)
            {
                continue;
            }

            renderer.material = selected;
            renderer.material.mainTextureScale = lowerName.Contains("intersection")
                ? new Vector2(2f, 2f)
                : new Vector2(4f, 4f);
        }
    }

    private bool IsOnRoadArea(float x, float z, float halfWidth)
    {
        float center = chunkSize * 0.5f;
        bool onHorizontal = z > center - halfWidth && z < center + halfWidth;
        bool onVertical = x > center - halfWidth && x < center + halfWidth;
        return onHorizontal || onVertical;
    }

    private bool IsNearRoad(float x, float z, float distance)
    {
        float center = chunkSize * 0.5f;
        bool nearHorizontal = Mathf.Abs(z - center) < distance;
        bool nearVertical = Mathf.Abs(x - center) < distance;
        return nearHorizontal || nearVertical;
    }

    private GameObject CreateBox(string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        return CreatePrimitivePart(PrimitiveType.Cube, objectName, parent, localPosition, localScale, color);
    }

    private GameObject CreatePrimitivePart(
        PrimitiveType primitiveType,
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color
    )
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = objectName;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = CreateMaterial(color);
        return part;
    }

    private Material CreateMaterial(Color color, float metallic = 0f, float smoothness = 0.2f)
    {
        string key = $"{(Color32)color}-{metallic:0.00}-{smoothness:0.00}";
        if (materialCache.TryGetValue(key, out Material existingMaterial))
        {
            return existingMaterial;
        }

        Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        materialCache.Add(key, material);
        return material;
    }

    private bool TryCreateImportedTree(Transform parent, Vector2Int coord, int index, float x, float z)
    {
        if (!useImportedAssets)
        {
            return false;
        }

        Vector3 scale = Vector3.one * Mathf.Lerp(0.72f, 1.18f, Hash01(coord.x, coord.y, index * 5 + 901));
        return TrySpawnVariant(
            NatureTreePaths,
            parent,
            new Vector3(x, 0f, z),
            Quaternion.Euler(0f, Hash01(coord.x, coord.y, index * 5 + 902) * 360f, 0f),
            scale);
    }

    private bool TryCreateImportedBush(Transform parent, Vector2Int coord, int index, float x, float z)
    {
        if (!useImportedAssets)
        {
            return false;
        }

        Vector3 scale = Vector3.one * Mathf.Lerp(0.8f, 1.35f, Hash01(coord.x, coord.y, index * 9 + 910));
        return TrySpawnVariant(
            BushPaths,
            parent,
            new Vector3(x, 0f, z),
            Quaternion.Euler(0f, Hash01(coord.x, coord.y, index * 9 + 911) * 360f, 0f),
            scale);
    }

    private Vector3 GetStructureScale(string prefabPath)
    {
        if (prefabPath.Contains("Free_Building_01"))
        {
            return Vector3.one * 0.7f;
        }

        if (prefabPath.Contains("UrbanBuilding"))
        {
            return Vector3.one * 0.8f;
        }

        if (prefabPath.Contains("ModularHousePack1"))
        {
            return Vector3.one * 1.15f;
        }

        return Vector3.one;
    }

    private bool TrySpawnVariant(string[] assetPaths, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        if (assetPaths == null || assetPaths.Length == 0)
        {
            return false;
        }

        for (int attempt = 0; attempt < assetPaths.Length; attempt++)
        {
            string path = assetPaths[attempt % assetPaths.Length];
            GameObject prefab = LoadPrefab(path);
            if (prefab == null)
            {
                continue;
            }

            GameObject instance = Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            return true;
        }

        return false;
    }

    private GameObject LoadPrefab(string assetPath)
    {
        if (prefabCache.TryGetValue(assetPath, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        prefabCache.Add(assetPath, prefab);
        return prefab;
#else
        prefabCache.Add(assetPath, null);
        return null;
#endif
    }

    private Material LoadMaterial(string assetPath)
    {
        if (importedMaterialCache.TryGetValue(assetPath, out Material cachedMaterial))
        {
            return cachedMaterial;
        }

#if UNITY_EDITOR
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        importedMaterialCache.Add(assetPath, material);
        return material;
#else
        importedMaterialCache.Add(assetPath, null);
        return null;
#endif
    }

    private float Hash01(int x, int z, int salt)
    {
        float value = Mathf.Sin(x * 127.1f + z * 311.7f + salt * 74.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
