using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PostApocRoadtrip.World
{
    [ExecuteAlways]
    public class RoadtripWorldBootstrap : MonoBehaviour
    {
        private const float RoadWidth = 9.5f;
        private const float RoadLength = 960f;
        private const float ShoulderWidth = 2.8f;
        private const float VisualTailLength = 520f;

        private readonly struct ChunkBlueprint
        {
            public ChunkBlueprint(string regionId, string chunkId, string displayName, float startZ, float endZ, float loadDistance, float unloadDistance, string summary, params string[] poiIds)
            {
                RegionId = regionId;
                ChunkId = chunkId;
                DisplayName = displayName;
                StartZ = startZ;
                EndZ = endZ;
                LoadDistance = loadDistance;
                UnloadDistance = unloadDistance;
                Summary = summary;
                PoiIds = poiIds ?? new string[0];
            }

            public string RegionId { get; }
            public string ChunkId { get; }
            public string DisplayName { get; }
            public float StartZ { get; }
            public float EndZ { get; }
            public float LoadDistance { get; }
            public float UnloadDistance { get; }
            public string Summary { get; }
            public string[] PoiIds { get; }
        }

        [SerializeField] private bool autoBuildInEditor = true;
        [SerializeField] private bool rebuildOnPlay = true;
        [SerializeField] private bool spawnPreviewVehicle = true;
        [SerializeField] private int environmentSeed = 7;

        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

#if UNITY_EDITOR
        private bool buildQueued;
#endif

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                return;
            }

#if UNITY_EDITOR
            QueueEditorBuild();
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorBuild();
            }
#endif
        }

        [ContextMenu("Rebuild World")]
        public void RebuildWorld()
        {
            BuildWorld(!Application.isPlaying);
        }

#if UNITY_EDITOR
        private void QueueEditorBuild()
        {
            if (!autoBuildInEditor || buildQueued || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            buildQueued = true;
            EditorApplication.delayCall += DelayedEditorBuild;
        }

        private void DelayedEditorBuild()
        {
            buildQueued = false;

            if (this == null || Application.isPlaying || !autoBuildInEditor)
            {
                return;
            }

            BuildWorld(true);
        }
#endif

        private void BuildWorld(bool immediate)
        {
            Random.InitState(environmentSeed);
            foreach (var generatedMaterial in materials.Values)
            {
                DestroyComponent(generatedMaterial, immediate);
            }

            materials.Clear();

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            var worldRoot = EnsureSceneRoot("World");
            ClearChildren(worldRoot, immediate);

            var road = CreateGroup(worldRoot, "Road");
            var terrain = CreateGroup(worldRoot, "Terrain");
            var environment = CreateGroup(worldRoot, "Environment");
            var buildings = CreateGroup(worldRoot, "Buildings");
            var vehicles = CreateGroup(worldRoot, "Vehicles");
            var props = CreateGroup(worldRoot, "Props");
            var lighting = CreateGroup(worldRoot, "Lighting");
            var streamingSystem = CreateGroup(worldRoot, "StreamingSystem");
            var regions = CreateGroup(worldRoot, "Regions");
            var eventZones = CreateGroup(worldRoot, "EventZones");
            var futureHooks = CreateGroup(worldRoot, "FutureGameplayHooks");
            var prototypeGameplay = CreateGroup(worldRoot, "PrototypeGameplaySystems");
            var useRuntimeStreaming = !(Application.isPlaying && Application.platform == RuntimePlatform.WebGLPlayer);

            var spawnPoints = CreateGroup(futureHooks, "SpawnPoints");
            var fuelStops = CreateGroup(futureHooks, "FuelStops");
            var shopStops = CreateGroup(futureHooks, "ShopStops");
            var lootAreas = CreateGroup(futureHooks, "LootAreas");
            var zombieSpawnAreas = CreateGroup(futureHooks, "ZombieSpawnAreas");
            var eventTriggers = CreateGroup(futureHooks, "EventTriggers");

            ConfigureAtmosphere(lighting);
            EnsureMainCamera();
            ConfigureStreamingSystem(streamingSystem, useRuntimeStreaming);
            BuildRegionBlueprint(regions);
            QualitySettings.shadowDistance = 80f;
            QualitySettings.pixelLightCount = 2;

            BuildRoad(road, props);
            BuildTerrain(terrain);
            BuildSuburbanApproach(environment, buildings, vehicles, props);
            BuildServiceStrip(environment, buildings, vehicles, props);
            BuildRestBasin(environment, buildings, vehicles, props);
            BuildBridgeNarrows(environment, buildings, vehicles, props);
            BuildCheckpoint(environment, buildings, vehicles, props);
            BuildBurnoutMile(environment, buildings, vehicles, props);
            BuildCatastropheSetDressing(environment, props);
            BuildCombatCoverAndVerticality(props);
            BuildZombiePresence(environment);
            BuildBackdrop(environment, props);

            CreateHookMarker(spawnPoints, "PlayerStart", MarkerKind.SpawnPoint, new Vector3(0f, 0.1f, 12f), new Vector3(8f, 3f, 10f), "Primary start point at the calm suburban edge.", new Color(0.45f, 0.95f, 0.9f));
            CreateHookMarker(spawnPoints, "BridgeFallback", MarkerKind.SpawnPoint, new Vector3(0f, 0.1f, 565f), new Vector3(8f, 3f, 12f), "Fallback spawn before the bridge funnel.", new Color(0.45f, 0.95f, 0.9f));
            CreateHookMarker(spawnPoints, "FinalStretchStart", MarkerKind.SpawnPoint, new Vector3(0f, 0.1f, 805f), new Vector3(8f, 3f, 12f), "Late-run spawn near the crash corridor.", new Color(0.45f, 0.95f, 0.9f));

            CreateHookMarker(fuelStops, "LastLightFuel", MarkerKind.FuelStop, new Vector3(-16f, 0.1f, 238f), new Vector3(16f, 4f, 18f), "Main gas forecourt with room for later refuel interaction.", new Color(0.95f, 0.8f, 0.25f));
            CreateHookMarker(fuelStops, "CheckpointReserveFuel", MarkerKind.FuelStop, new Vector3(17f, 0.1f, 705f), new Vector3(10f, 4f, 12f), "Optional emergency fuel cache beside the checkpoint.", new Color(0.95f, 0.8f, 0.25f));

            CreateHookMarker(shopStops, "HighwayMart", MarkerKind.ShopStop, new Vector3(18f, 0.1f, 280f), new Vector3(14f, 4f, 16f), "Food and supply stop with storefront and loading area.", new Color(0.4f, 0.95f, 0.45f));
            CreateHookMarker(shopStops, "RestShelterKiosk", MarkerKind.ShopStop, new Vector3(20f, 0.1f, 418f), new Vector3(10f, 4f, 12f), "Small kiosk-sized stop at the rest area.", new Color(0.4f, 0.95f, 0.45f));

            CreateHookMarker(lootAreas, "HouseClusterLoot", MarkerKind.LootArea, new Vector3(-22f, 0.1f, 92f), new Vector3(24f, 4f, 26f), "Abandoned houses suited for later scavenging beats.", new Color(0.3f, 0.7f, 1f));
            CreateHookMarker(lootAreas, "GasStationBackroomLoot", MarkerKind.LootArea, new Vector3(-24f, 0.1f, 240f), new Vector3(10f, 4f, 10f), "Backroom and service lane behind the gas station.", new Color(0.3f, 0.7f, 1f));
            CreateHookMarker(lootAreas, "RestStopLoot", MarkerKind.LootArea, new Vector3(21f, 0.1f, 420f), new Vector3(14f, 4f, 14f), "Rastplatz with vending, benches and later supply pickups.", new Color(0.3f, 0.7f, 1f));
            CreateHookMarker(lootAreas, "CheckpointDepotLoot", MarkerKind.LootArea, new Vector3(-17f, 0.1f, 696f), new Vector3(16f, 4f, 18f), "Checkpoint storage cluster ready for loot containers.", new Color(0.3f, 0.7f, 1f));
            CreateHookMarker(lootAreas, "CrashSiteLoot", MarkerKind.LootArea, new Vector3(4f, 0.1f, 846f), new Vector3(26f, 4f, 22f), "Dense wreck zone with multiple future loot nodes.", new Color(0.3f, 0.7f, 1f));

            CreateHookMarker(zombieSpawnAreas, "SuburbanTreeLineSpawn", MarkerKind.ZombieSpawnArea, new Vector3(29f, 0.1f, 112f), new Vector3(24f, 4f, 30f), "Tree line along the early road section.", new Color(1f, 0.45f, 0.35f));
            CreateHookMarker(zombieSpawnAreas, "BridgeDitchSpawn", MarkerKind.ZombieSpawnArea, new Vector3(-18f, -3.2f, 588f), new Vector3(18f, 6f, 26f), "Dry river channel beneath the bridge.", new Color(1f, 0.45f, 0.35f));
            CreateHookMarker(zombieSpawnAreas, "CheckpointTreeSpawn", MarkerKind.ZombieSpawnArea, new Vector3(29f, 0.1f, 708f), new Vector3(26f, 4f, 28f), "Brush and road edge near the checkpoint.", new Color(1f, 0.45f, 0.35f));
            CreateHookMarker(zombieSpawnAreas, "BurnoutShoulderSpawn", MarkerKind.ZombieSpawnArea, new Vector3(-30f, 0.1f, 858f), new Vector3(28f, 4f, 30f), "Wreckage field shoulder for late-run pressure.", new Color(1f, 0.45f, 0.35f));

            CreateHookMarker(eventTriggers, "EZ_SuburbanSilence", MarkerKind.EventTrigger, new Vector3(0f, 0.1f, 120f), new Vector3(18f, 4f, 22f), "Quiet suburb event lane with space for road-side discoveries.", new Color(0.95f, 0.3f, 0.8f));
            CreateHookMarker(eventTriggers, "EZ_FuelForecourt", MarkerKind.EventTrigger, new Vector3(-4f, 0.1f, 240f), new Vector3(30f, 4f, 24f), "Gas station forecourt set up for interruption events.", new Color(0.95f, 0.3f, 0.8f));
            CreateHookMarker(eventTriggers, "EZ_MarketScramble", MarkerKind.EventTrigger, new Vector3(8f, 0.1f, 280f), new Vector3(28f, 4f, 22f), "Shop stop encounter band spanning both lanes.", new Color(0.95f, 0.3f, 0.8f));
            CreateHookMarker(eventTriggers, "EZ_RestStopEcho", MarkerKind.EventTrigger, new Vector3(0f, 0.1f, 420f), new Vector3(22f, 4f, 24f), "Rest stop pocket suited to ambient or threat events.", new Color(0.95f, 0.3f, 0.8f));
            CreateHookMarker(eventTriggers, "EZ_BridgeFunnel", MarkerKind.EventTrigger, new Vector3(0f, 0.1f, 588f), new Vector3(14f, 5f, 22f), "Bridge choke point with strong line-of-sight control.", new Color(0.95f, 0.3f, 0.8f));
            CreateHookMarker(eventTriggers, "EZ_CheckpointSweep", MarkerKind.EventTrigger, new Vector3(0f, 0.1f, 706f), new Vector3(24f, 5f, 26f), "Checkpoint event zone covering barricades and flanks.", new Color(0.95f, 0.3f, 0.8f));
            CreateHookMarker(eventTriggers, "EZ_BurnoutChoke", MarkerKind.EventTrigger, new Vector3(0f, 0.1f, 845f), new Vector3(24f, 5f, 24f), "Crash corridor built for later scripted stop-and-go beats.", new Color(0.95f, 0.3f, 0.8f));

            BuildEventZoneVisuals(eventZones);

            if (spawnPreviewVehicle)
            {
                BuildPreviewVehicle(vehicles, new Vector3(0f, 0.55f, 12f));
            }

            BuildPrototypeGameplaySystems(prototypeGameplay);
        }

        private void ConfigureAtmosphere(Transform lightingRoot)
        {
            var skybox = GetSkyboxMaterial(
                "skybox_open_road",
                new Color(0.43f, 0.52f, 0.58f),
                new Color(0.34f, 0.37f, 0.36f),
                1.18f,
                0.82f);
            RenderSettings.skybox = skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 245f;
            RenderSettings.fogColor = new Color(0.42f, 0.48f, 0.52f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.43f, 0.5f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.36f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.19f, 0.18f, 0.16f);
            RenderSettings.ambientIntensity = 0.62f;

            var sunObject = CreateGroup(lightingRoot, "SunLight");
            var sun = sunObject.gameObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.62f, 0.72f, 0.86f);
            sun.intensity = 0.72f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.86f;
            sun.shadowBias = 0.02f;
            sun.bounceIntensity = 0.7f;
            sunObject.localRotation = Quaternion.Euler(12f, -42f, 0f);

            CreatePointLight(lightingRoot, "GasStationPractical", new Vector3(-16f, 5.5f, 238f), new Color(0.86f, 0.72f, 0.48f), 0.52f, 14f);
            CreatePointLight(lightingRoot, "CheckpointBeacon", new Vector3(0f, 6.2f, 706f), new Color(0.48f, 0.67f, 0.82f), 0.65f, 16f);
        }

        private void EnsureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = EnsureSceneRoot("Main Camera").gameObject;
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
                if (camera == null)
                {
                    camera = cameraObject.AddComponent<Camera>();
                }
            }

            camera.fieldOfView = 61f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 420f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.41f, 0.47f, 0.5f);
        }

        private void ConfigureStreamingSystem(Transform streamingRoot, bool enableRuntimeStreaming)
        {
            CreateGroup(streamingRoot, "ChunkSceneRegistry");
            CreateGroup(streamingRoot, "LoadedSceneHandles");
            CreateGroup(streamingRoot, "StreamingVolumes");

            var performanceConfigurator = EnsureComponent<WorldPerformanceConfigurator>(streamingRoot.gameObject);
            performanceConfigurator.enabled = true;

            var streamingManager = EnsureComponent<WorldStreamingManager>(streamingRoot.gameObject);
            streamingManager.enabled = enableRuntimeStreaming;
        }

        private void BuildRegionBlueprint(Transform regionsRoot)
        {
            CreateRegionBlueprint(
                regionsRoot,
                "region_01",
                "RedMesaApproach",
                "Outer foothills with a suburban edge, early derelict housing and the first long read on the highway.",
                0f,
                240f,
                new Color(0.84f, 0.53f, 0.29f, 0.3f),
                new ChunkBlueprint("region_01", "chunk_01_suburb_gate", "Chunk_01_SuburbGate", 0f, 120f, 150f, 210f, "Calm opening stretch with player spawn, utility line and house cluster.", "PlayerStart", "HouseClusterLoot", "EZ_SuburbanSilence"),
                new ChunkBlueprint("region_01", "chunk_02_service_edge", "Chunk_02_ServiceEdge", 120f, 240f, 150f, 210f, "Transition into the service corridor with first fuel stop hooks.", "LastLightFuel", "GasStationBackroomLoot", "EZ_FuelForecourt"));

            CreateRegionBlueprint(
                regionsRoot,
                "region_02",
                "DustServiceCorridor",
                "Commercial ruins, food access and rest infrastructure with strong road-side readability.",
                240f,
                480f,
                new Color(0.92f, 0.75f, 0.31f, 0.3f),
                new ChunkBlueprint("region_02", "chunk_03_mart_line", "Chunk_03_MartLine", 240f, 360f, 150f, 220f, "Gas station, food mart and roadside clutter for supply-stop gameplay.", "HighwayMart", "EZ_MarketScramble"),
                new ChunkBlueprint("region_02", "chunk_04_rest_basin", "Chunk_04_RestBasin", 360f, 480f, 150f, 220f, "Rest stop pocket with kiosk, loot nodes and broad sightlines.", "RestShelterKiosk", "RestStopLoot", "EZ_RestStopEcho"));

            CreateRegionBlueprint(
                regionsRoot,
                "region_03",
                "RavineFrontier",
                "The road compresses into a controlled funnel with a bridge crossing and quarantine perimeter.",
                480f,
                720f,
                new Color(0.34f, 0.78f, 0.83f, 0.3f),
                new ChunkBlueprint("region_03", "chunk_05_bridge_approach", "Chunk_05_BridgeApproach", 480f, 600f, 160f, 230f, "Bridge setup with terrain drop, wrecks and event funnel geometry.", "BridgeDitchSpawn", "EZ_BridgeFunnel"),
                new ChunkBlueprint("region_03", "chunk_06_quarantine_breach", "Chunk_06_QuarantineBreach", 600f, 720f, 160f, 230f, "Checkpoint perimeter with storage, reserve fuel and multi-lane control.", "CheckpointReserveFuel", "CheckpointDepotLoot", "EZ_CheckpointSweep"));

            CreateRegionBlueprint(
                regionsRoot,
                "region_04",
                "BlackglassExpanse",
                "The late-game corridor widens into heavier destruction, crash fields and a final lookout line.",
                720f,
                RoadLength,
                new Color(0.9f, 0.36f, 0.29f, 0.3f),
                new ChunkBlueprint("region_04", "chunk_07_crash_fields", "Chunk_07_CrashFields", 720f, 840f, 170f, 240f, "A burnt staging corridor leading into dense accident geometry.", "FinalStretchStart", "BurnoutShoulderSpawn"),
                new ChunkBlueprint("region_04", "chunk_08_final_mile", "Chunk_08_FinalMile", 840f, RoadLength, 170f, 240f, "Final wreck zone with vista, observation tower and high-pressure event hooks.", "CrashSiteLoot", "EZ_BurnoutChoke"));
        }

        private void CreateRegionBlueprint(Transform regionsRoot, string regionId, string displayName, string summary, float startZ, float endZ, Color color, params ChunkBlueprint[] chunks)
        {
            var regionRoot = CreateGroup(regionsRoot, displayName);
            regionRoot.localPosition = new Vector3(0f, 0f, startZ);

            var region = EnsureComponent<WorldRegionAuthoring>(regionRoot.gameObject);
            region.regionId = regionId;
            region.displayName = displayName;
            region.summary = summary;
            region.startRoadZ = startZ;
            region.endRoadZ = endZ;
            region.debugColor = color;

            foreach (var chunk in chunks)
            {
                var chunkRoot = CreateGroup(regionRoot, chunk.DisplayName);
                chunkRoot.localPosition = new Vector3(0f, 0f, chunk.StartZ - startZ);

                CreateGroup(chunkRoot, "Terrain");
                CreateGroup(chunkRoot, "Road");
                CreateGroup(chunkRoot, "Environment");
                CreateGroup(chunkRoot, "Buildings");
                CreateGroup(chunkRoot, "Props");
                CreateGroup(chunkRoot, "Vehicles");
                CreateGroup(chunkRoot, "Lighting");
                CreateGroup(chunkRoot, "FutureGameplayHooks");

                var chunkAuthoring = EnsureComponent<WorldChunkAuthoring>(chunkRoot.gameObject);
                chunkAuthoring.regionId = chunk.RegionId;
                chunkAuthoring.chunkId = chunk.ChunkId;
                chunkAuthoring.scenePath = $"Assets/Scenes/Regions/{displayName}/{chunk.DisplayName}.unity";
                chunkAuthoring.summary = chunk.Summary;
                chunkAuthoring.localCenter = new Vector3(0f, 4f, (chunk.EndZ - chunk.StartZ) * 0.5f);
                chunkAuthoring.size = new Vector3(84f, 16f, chunk.EndZ - chunk.StartZ);
                chunkAuthoring.loadDistance = chunk.LoadDistance;
                chunkAuthoring.unloadDistance = chunk.UnloadDistance;
                chunkAuthoring.poiIds = chunk.PoiIds;
            }
        }

        private void BuildRoad(Transform roadRoot, Transform propsRoot)
        {
            var suburbanAsphalt = GetMaterial("asphalt_suburban", new Color(0.33f, 0.35f, 0.35f), 0.02f, 0.22f);
            var serviceAsphalt = GetMaterial("asphalt_service", new Color(0.34f, 0.36f, 0.36f), 0.02f, 0.24f);
            var bridgeAsphalt = GetMaterial("asphalt_bridge", new Color(0.31f, 0.33f, 0.34f), 0.03f, 0.26f);
            var burntAsphalt = GetMaterial("asphalt_burnt", new Color(0.28f, 0.29f, 0.3f), 0.02f, 0.18f);
            var lineMaterial = GetMaterial("lane_line", new Color(0.92f, 0.81f, 0.47f), 0f, 0.18f);
            var edgeLineMaterial = GetMaterial("lane_edge", new Color(0.9f, 0.88f, 0.8f), 0f, 0.16f);
            var shoulderMaterial = GetMaterial("road_shoulder", new Color(0.55f, 0.48f, 0.35f), 0.01f, 0.06f);
            var concreteBarrier = GetMaterial("concrete_barrier", new Color(0.72f, 0.72f, 0.68f), 0.02f, 0.18f);

            BuildRoadSlice(roadRoot, 0f, 180f, suburbanAsphalt, shoulderMaterial, lineMaterial, edgeLineMaterial, 0.08f);
            BuildRoadSlice(roadRoot, 180f, 500f, serviceAsphalt, shoulderMaterial, lineMaterial, edgeLineMaterial, 0.12f);
            BuildRoadSlice(roadRoot, 500f, 650f, bridgeAsphalt, shoulderMaterial, lineMaterial, edgeLineMaterial, 0.05f);
            BuildRoadSlice(roadRoot, 650f, 790f, serviceAsphalt, shoulderMaterial, lineMaterial, edgeLineMaterial, 0.1f);
            BuildRoadSlice(roadRoot, 790f, RoadLength, burntAsphalt, shoulderMaterial, lineMaterial, edgeLineMaterial, 0.15f);
            BuildRoadTail(roadRoot, shoulderMaterial, burntAsphalt, edgeLineMaterial);

            CreatePrimitive(PrimitiveType.Cube, roadRoot, "BridgeDeck", new Vector3(0f, -0.15f, 586f), new Vector3(9.2f, 0.2f, 58f), Quaternion.identity, bridgeAsphalt);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "BridgeRailLeft", new Vector3(-4.95f, 0.65f, 586f), new Vector3(0.28f, 0.9f, 58f), Quaternion.identity, concreteBarrier);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "BridgeRailRight", new Vector3(4.95f, 0.65f, 586f), new Vector3(0.28f, 0.9f, 58f), Quaternion.identity, concreteBarrier);

            for (var z = 35f; z < RoadLength - 20f; z += 70f)
            {
                CreateBarrier(propsRoot, new Vector3(Random.Range(-5.5f, 5.5f), 0.45f, z), Quaternion.Euler(0f, Random.Range(-12f, 12f), 0f));
            }
        }

        private void BuildRoadSlice(Transform roadRoot, float startZ, float endZ, Material roadMaterial, Material shoulderMaterial, Material lineMaterial, Material edgeLineMaterial, float damageScale)
        {
            var length = endZ - startZ;
            var midZ = startZ + length * 0.5f;

            CreatePrimitive(PrimitiveType.Cube, roadRoot, $"Road_{startZ:000}_{endZ:000}", new Vector3(0f, 0.02f, midZ), new Vector3(RoadWidth, 0.14f, length), Quaternion.identity, roadMaterial);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, $"ShoulderLeft_{startZ:000}_{endZ:000}", new Vector3(-(RoadWidth * 0.5f + ShoulderWidth * 0.5f), -0.02f, midZ), new Vector3(ShoulderWidth, 0.08f, length), Quaternion.identity, shoulderMaterial);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, $"ShoulderRight_{startZ:000}_{endZ:000}", new Vector3(RoadWidth * 0.5f + ShoulderWidth * 0.5f, -0.02f, midZ), new Vector3(ShoulderWidth, 0.08f, length), Quaternion.identity, shoulderMaterial);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, $"EdgeLineLeft_{startZ:000}_{endZ:000}", new Vector3(-(RoadWidth * 0.5f - 0.32f), 0.1f, midZ), new Vector3(0.09f, 0.02f, length - 4f), Quaternion.identity, edgeLineMaterial);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, $"EdgeLineRight_{startZ:000}_{endZ:000}", new Vector3(RoadWidth * 0.5f - 0.32f, 0.1f, midZ), new Vector3(0.09f, 0.02f, length - 4f), Quaternion.identity, edgeLineMaterial);

            for (var z = startZ + 10f; z < endZ - 8f; z += 12f)
            {
                var dashLength = Random.Range(3.6f, 5.2f);
                CreatePrimitive(PrimitiveType.Cube, roadRoot, $"LaneDash_{z:000}", new Vector3(0f, 0.1f, z), new Vector3(0.25f, 0.03f, dashLength), Quaternion.identity, lineMaterial);
            }

            for (var z = startZ + 15f; z < endZ - 15f; z += 28f)
            {
                var patchWidth = Random.Range(0.7f, 2.2f);
                var patchLength = Random.Range(2f, 6.5f);
                var xOffset = Random.Range(-2.8f, 2.8f);
                var patchMaterial = GetMaterial("road_patch", new Color(0.27f, 0.29f, 0.31f), 0.03f, 0.14f);
                CreatePrimitive(PrimitiveType.Cube, roadRoot, $"Patch_{z:000}", new Vector3(xOffset, 0.09f, z), new Vector3(patchWidth, 0.02f, patchLength), Quaternion.Euler(0f, Random.Range(-14f, 14f), 0f), patchMaterial);
            }

            for (var z = startZ + 8f; z < endZ - 8f; z += 34f)
            {
                var crack = CreatePrimitive(PrimitiveType.Cube, roadRoot, $"Crack_{z:000}", new Vector3(Random.Range(-3f, 3f), 0.1f, z), new Vector3(0.12f, 0.015f, Random.Range(4f, 10f) * damageScale * 4f), Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f), GetMaterial("crack", new Color(0.17f, 0.16f, 0.16f), 0f, 0f));
                crack.transform.localScale = new Vector3(Mathf.Max(0.08f, crack.transform.localScale.x), crack.transform.localScale.y, crack.transform.localScale.z);
            }
        }

        private void BuildTerrain(Transform terrainRoot)
        {
            BuildTerrainSection(terrainRoot, 0f, 180f, new Color(0.72f, 0.73f, 0.47f), new Color(0.63f, 0.72f, 0.48f), 0f);
            BuildTerrainSection(terrainRoot, 180f, 340f, new Color(0.76f, 0.72f, 0.45f), new Color(0.69f, 0.71f, 0.44f), 0.04f);
            BuildTerrainSection(terrainRoot, 340f, 500f, new Color(0.66f, 0.71f, 0.47f), new Color(0.56f, 0.66f, 0.46f), -0.02f);
            BuildTerrainSection(terrainRoot, 500f, 650f, new Color(0.69f, 0.69f, 0.54f), new Color(0.62f, 0.65f, 0.5f), 0.08f);
            BuildTerrainSection(terrainRoot, 650f, 790f, new Color(0.61f, 0.73f, 0.5f), new Color(0.54f, 0.69f, 0.47f), 0.03f);
            BuildTerrainSection(terrainRoot, 790f, RoadLength, new Color(0.56f, 0.6f, 0.42f), new Color(0.49f, 0.55f, 0.39f), 0f);
            BuildTerrainSection(terrainRoot, RoadLength, RoadLength + VisualTailLength, new Color(0.6f, 0.64f, 0.46f), new Color(0.54f, 0.59f, 0.42f), -0.04f);

            var ditchMaterial = GetMaterial("ditch", new Color(0.53f, 0.57f, 0.5f), 0.01f, 0.04f);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, "BridgeDitchCenter", new Vector3(0f, -4.1f, 586f), new Vector3(13f, 0.4f, 74f), Quaternion.identity, ditchMaterial);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, "BridgeDitchLeftWall", new Vector3(-9.8f, -2.1f, 586f), new Vector3(8f, 4.5f, 74f), Quaternion.Euler(0f, 0f, -18f), ditchMaterial);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, "BridgeDitchRightWall", new Vector3(9.8f, -2.1f, 586f), new Vector3(8f, 4.5f, 74f), Quaternion.Euler(0f, 0f, 18f), ditchMaterial);
        }

        private void BuildTerrainSection(Transform terrainRoot, float startZ, float endZ, Color leftColor, Color rightColor, float yOffset)
        {
            var length = endZ - startZ;
            var midZ = startZ + length * 0.5f;
            var leftTerrain = GetMaterial($"terrain_left_{startZ:000}", leftColor, 0f, 0.03f);
            var rightTerrain = GetMaterial($"terrain_right_{startZ:000}", rightColor, 0f, 0.03f);
            var roadsideDust = GetMaterial($"roadside_dust_{startZ:000}", Color.Lerp(leftColor, new Color(0.64f, 0.57f, 0.42f), 0.46f), 0f, 0.05f);
            var swale = GetMaterial($"swale_{startZ:000}", Color.Lerp(leftColor, new Color(0.47f, 0.52f, 0.39f), 0.35f), 0f, 0.03f);
            var moundLeft = GetMaterial($"mound_left_{startZ:000}", Color.Lerp(leftColor, new Color(0.66f, 0.67f, 0.48f), 0.24f), 0f, 0.03f);
            var moundRight = GetMaterial($"mound_right_{startZ:000}", Color.Lerp(rightColor, new Color(0.62f, 0.66f, 0.47f), 0.24f), 0f, 0.03f);
            var farShelfLeft = GetMaterial($"shelf_left_{startZ:000}", Color.Lerp(leftColor, RenderSettings.fogColor, 0.2f), 0f, 0.02f);
            var farShelfRight = GetMaterial($"shelf_right_{startZ:000}", Color.Lerp(rightColor, RenderSettings.fogColor, 0.2f), 0f, 0.02f);
            var farRidgeLeft = GetMaterial($"ridge_left_{startZ:000}", Color.Lerp(leftColor, RenderSettings.fogColor, 0.42f), 0f, 0.02f);
            var farRidgeRight = GetMaterial($"ridge_right_{startZ:000}", Color.Lerp(rightColor, RenderSettings.fogColor, 0.42f), 0f, 0.02f);

            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"TerrainLeft_{startZ:000}", new Vector3(-30f, -0.22f + yOffset, midZ), new Vector3(39f, 0.42f, length), Quaternion.identity, leftTerrain);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"TerrainRight_{startZ:000}", new Vector3(30f, -0.22f + yOffset, midZ), new Vector3(39f, 0.42f, length), Quaternion.identity, rightTerrain);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"RoadsideDustLeft_{startZ:000}", new Vector3(-10.8f, -0.13f + yOffset, midZ), new Vector3(8.4f, 0.16f, length), Quaternion.identity, roadsideDust);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"RoadsideDustRight_{startZ:000}", new Vector3(10.8f, -0.13f + yOffset, midZ), new Vector3(8.4f, 0.16f, length), Quaternion.identity, roadsideDust);

            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"SwaleLeft_{startZ:000}", new Vector3(-14.2f, -0.34f + yOffset, midZ), new Vector3(5.2f, 0.24f, length), Quaternion.Euler(0f, 0f, -5f), swale);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"SwaleRight_{startZ:000}", new Vector3(14.2f, -0.34f + yOffset, midZ), new Vector3(5.2f, 0.24f, length), Quaternion.Euler(0f, 0f, 5f), swale);

            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"InnerMoundLeft_{startZ:000}", new Vector3(-21.5f, 0.72f + yOffset, midZ), new Vector3(12f, 1.55f, length), Quaternion.Euler(0f, 0f, -8f), moundLeft);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"InnerMoundRight_{startZ:000}", new Vector3(21.5f, 0.72f + yOffset, midZ), new Vector3(12f, 1.55f, length), Quaternion.Euler(0f, 0f, 8f), moundRight);

            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"OuterShelfLeft_{startZ:000}", new Vector3(-58f, 0.64f + yOffset, midZ), new Vector3(34f, 1.8f, length), Quaternion.Euler(0f, 0f, -4f), farShelfLeft);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"OuterShelfRight_{startZ:000}", new Vector3(58f, 0.64f + yOffset, midZ), new Vector3(34f, 1.8f, length), Quaternion.Euler(0f, 0f, 4f), farShelfRight);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"FarRidgeLeft_{startZ:000}", new Vector3(-103f, 4.6f + yOffset, midZ), new Vector3(38f, 9.2f, length), Quaternion.Euler(0f, 0f, -7f), farRidgeLeft);
            CreatePrimitive(PrimitiveType.Cube, terrainRoot, $"FarRidgeRight_{startZ:000}", new Vector3(103f, 4.4f + yOffset, midZ), new Vector3(38f, 8.8f, length), Quaternion.Euler(0f, 0f, 7f), farRidgeRight);

            CreatePrimitive(PrimitiveType.Sphere, terrainRoot, $"TerrainKnuckleLeftA_{startZ:000}", new Vector3(-43f, 1.2f + yOffset, midZ - length * 0.28f), new Vector3(24f, 3.6f, 44f), Quaternion.identity, moundLeft);
            CreatePrimitive(PrimitiveType.Sphere, terrainRoot, $"TerrainKnuckleLeftB_{startZ:000}", new Vector3(-50f, 1.8f + yOffset, midZ + length * 0.22f), new Vector3(30f, 4.8f, 54f), Quaternion.identity, farShelfLeft);
            CreatePrimitive(PrimitiveType.Sphere, terrainRoot, $"TerrainKnuckleRightA_{startZ:000}", new Vector3(43f, 1.1f + yOffset, midZ - length * 0.18f), new Vector3(24f, 3.4f, 42f), Quaternion.identity, moundRight);
            CreatePrimitive(PrimitiveType.Sphere, terrainRoot, $"TerrainKnuckleRightB_{startZ:000}", new Vector3(51f, 1.9f + yOffset, midZ + length * 0.26f), new Vector3(32f, 4.6f, 58f), Quaternion.identity, farShelfRight);
        }

        private void BuildRoadTail(Transform roadRoot, Material shoulderMaterial, Material roadMaterial, Material edgeLineMaterial)
        {
            var tailLength = VisualTailLength;
            var tailMidZ = RoadLength + tailLength * 0.5f;
            var fadedShoulder = GetMaterial("road_tail_shoulder", new Color(0.58f, 0.56f, 0.46f), 0f, 0.03f);
            var fadedRoad = GetMaterial("road_tail", new Color(0.37f, 0.39f, 0.39f), 0.01f, 0.1f);
            var fadedEdge = GetMaterial("road_tail_edge", new Color(0.75f, 0.75f, 0.69f), 0f, 0.08f);

            CreatePrimitive(PrimitiveType.Cube, roadRoot, "RoadTail", new Vector3(0f, 0.01f, tailMidZ), new Vector3(RoadWidth, 0.08f, tailLength), Quaternion.identity, fadedRoad);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, "RoadTailShoulderLeft", new Vector3(-(RoadWidth * 0.5f + ShoulderWidth * 0.55f), -0.03f, tailMidZ), new Vector3(ShoulderWidth * 1.1f, 0.06f, tailLength), Quaternion.identity, fadedShoulder);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, "RoadTailShoulderRight", new Vector3(RoadWidth * 0.5f + ShoulderWidth * 0.55f, -0.03f, tailMidZ), new Vector3(ShoulderWidth * 1.1f, 0.06f, tailLength), Quaternion.identity, fadedShoulder);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, "RoadTailEdgeLeft", new Vector3(-(RoadWidth * 0.5f - 0.28f), 0.06f, tailMidZ), new Vector3(0.06f, 0.015f, tailLength * 0.72f), Quaternion.identity, fadedEdge);
            CreatePrimitive(PrimitiveType.Cube, roadRoot, "RoadTailEdgeRight", new Vector3(RoadWidth * 0.5f - 0.28f, 0.06f, tailMidZ), new Vector3(0.06f, 0.015f, tailLength * 0.72f), Quaternion.identity, fadedEdge);
        }

        private void BuildSuburbanApproach(Transform environmentRoot, Transform buildingsRoot, Transform vehiclesRoot, Transform propsRoot)
        {
            CreateSectionTitle(environmentRoot, "Section_01_SuburbanApproach", 90f);

            for (var z = 26f; z <= 150f; z += 28f)
            {
                CreateUtilityPole(environmentRoot, new Vector3(-15f, 0f, z));
                if (z < 142f)
                {
                    CreateUtilityPole(environmentRoot, new Vector3(15f, 0f, z + 12f));
                }
            }

            CreateAbandonedHouse(buildingsRoot, new Vector3(-24f, 0f, 48f), Quaternion.Euler(0f, 14f, 0f), 1.05f, new Color(0.71f, 0.68f, 0.58f));
            CreateAbandonedHouse(buildingsRoot, new Vector3(-23f, 0f, 82f), Quaternion.Euler(0f, -8f, 0f), 1.15f, new Color(0.63f, 0.7f, 0.63f));
            CreateAbandonedHouse(buildingsRoot, new Vector3(-21f, 0f, 118f), Quaternion.Euler(0f, 9f, 0f), 0.95f, new Color(0.69f, 0.64f, 0.56f));
            CreateAbandonedHouse(buildingsRoot, new Vector3(24f, 0f, 62f), Quaternion.Euler(0f, -12f, 0f), 0.92f, new Color(0.67f, 0.72f, 0.66f));
            CreateAbandonedHouse(buildingsRoot, new Vector3(25f, 0f, 132f), Quaternion.Euler(0f, 11f, 0f), 1.08f, new Color(0.74f, 0.69f, 0.59f));

            CreateVehicleWreck(vehiclesRoot, "SuburbanSedan", new Vector3(-4.2f, 0.36f, 74f), Quaternion.Euler(0f, 6f, 0f), new Color(0.42f, 0.56f, 0.62f), false);
            CreateVehicleWreck(vehiclesRoot, "SideRoadPickup", new Vector3(17f, 0.35f, 106f), Quaternion.Euler(0f, -26f, 0f), new Color(0.67f, 0.43f, 0.29f), true);

            CreateBillboard(propsRoot, new Vector3(18f, 5.4f, 155f), Quaternion.Euler(0f, -18f, 0f), new Color(0.28f, 0.35f, 0.38f), "Distance Billboard");
            CreateRoadSign(propsRoot, new Vector3(-8f, 1.6f, 18f), "Main Street");

            ScatterStylizedTrees(environmentRoot, -34f, -16f, 18f, 170f, 16);
            ScatterStylizedTrees(environmentRoot, 17f, 36f, 20f, 170f, 12);
            ScatterShrubs(environmentRoot, -28f, 32f, 24f, 170f, 18, 1f);
            ScatterScrap(propsRoot, -10f, 10f, 30f, 168f, 10, 0.6f);
        }

        private void BuildServiceStrip(Transform environmentRoot, Transform buildingsRoot, Transform vehiclesRoot, Transform propsRoot)
        {
            CreateSectionTitle(environmentRoot, "Section_02_ServiceStrip", 260f);

            CreateGasStation(buildingsRoot, propsRoot, new Vector3(-20f, 0f, 238f));
            CreateMarketStop(buildingsRoot, propsRoot, new Vector3(19f, 0f, 280f));
            CreateChargingPylon(propsRoot, new Vector3(14f, 0f, 250f));
            CreateBillboard(propsRoot, new Vector3(-23f, 5.8f, 326f), Quaternion.Euler(0f, 22f, 0f), new Color(0.42f, 0.28f, 0.24f), "Service District Billboard");

            CreateVehicleWreck(vehiclesRoot, "ForecourtVan", new Vector3(-10f, 0.35f, 242f), Quaternion.Euler(0f, -10f, 0f), new Color(0.63f, 0.58f, 0.43f), true);
            CreateVehicleWreck(vehiclesRoot, "DeliveryTruck", new Vector3(13.5f, 0.45f, 292f), Quaternion.Euler(0f, 88f, 0f), new Color(0.7f, 0.54f, 0.3f), true, 1.15f);

            ScatterStylizedTrees(environmentRoot, -35f, -18f, 190f, 336f, 10);
            ScatterStylizedTrees(environmentRoot, 18f, 38f, 182f, 334f, 8);
            ScatterShrubs(environmentRoot, -28f, 34f, 186f, 334f, 14, 1.1f);
            ScatterDeadTrees(environmentRoot, -30f, -22f, 214f, 318f, 4);
            ScatterScrap(propsRoot, -12f, 12f, 188f, 336f, 10, 0.6f);
        }

        private void BuildRestBasin(Transform environmentRoot, Transform buildingsRoot, Transform vehiclesRoot, Transform propsRoot)
        {
            CreateSectionTitle(environmentRoot, "Section_03_RestBasin", 420f);

            CreateRestStop(buildingsRoot, propsRoot, new Vector3(22f, 0f, 420f));
            CreateVistaPoint(buildingsRoot, propsRoot, new Vector3(-20f, 0f, 452f), "Vista_RestBasin", new Color(0.28f, 0.6f, 0.74f));
            CreateBillboard(propsRoot, new Vector3(-18f, 5.2f, 390f), Quaternion.Euler(0f, 18f, 0f), new Color(0.25f, 0.31f, 0.26f), "Evacuation Billboard");
            CreateVehicleWreck(vehiclesRoot, "ShoulderHatchback", new Vector3(-7f, 0.34f, 402f), Quaternion.Euler(0f, 18f, 0f), new Color(0.48f, 0.63f, 0.49f), false);
            CreateVehicleWreck(vehiclesRoot, "RestStopSUV", new Vector3(16f, 0.34f, 433f), Quaternion.Euler(0f, -34f, 0f), new Color(0.59f, 0.51f, 0.44f), true);

            ScatterStylizedTrees(environmentRoot, -38f, -18f, 344f, 494f, 14);
            ScatterStylizedTrees(environmentRoot, 20f, 38f, 350f, 492f, 12);
            ScatterShrubs(environmentRoot, -32f, 34f, 350f, 492f, 20, 1.2f);
            ScatterScrap(propsRoot, -12f, 12f, 350f, 492f, 10, 0.7f);
        }

        private void BuildBridgeNarrows(Transform environmentRoot, Transform buildingsRoot, Transform vehiclesRoot, Transform propsRoot)
        {
            CreateSectionTitle(environmentRoot, "Section_04_BridgeNarrows", 586f);

            for (var z = 546f; z <= 624f; z += 13f)
            {
                CreateBarrier(propsRoot, new Vector3(-7.8f, 0.42f, z), Quaternion.Euler(0f, 90f, 0f));
                CreateBarrier(propsRoot, new Vector3(7.8f, 0.42f, z + 5f), Quaternion.Euler(0f, 90f, 0f));
            }

            CreateVehicleWreck(vehiclesRoot, "BridgeBus", new Vector3(0.8f, 0.55f, 610f), Quaternion.Euler(0f, 4f, 0f), new Color(0.71f, 0.56f, 0.35f), true, 1.35f);
            CreateVehicleWreck(vehiclesRoot, "DitchCar", new Vector3(-13.5f, -3.2f, 578f), Quaternion.Euler(18f, 42f, 24f), new Color(0.47f, 0.44f, 0.39f), true);

            CreateRoadSign(propsRoot, new Vector3(9.5f, 2.1f, 546f), "Bridge Out");
            CreateVistaPoint(buildingsRoot, propsRoot, new Vector3(21f, 0f, 618f), "Vista_Bridge", new Color(0.19f, 0.63f, 0.7f));
            ScatterStylizedTrees(environmentRoot, -32f, -18f, 520f, 646f, 8);
            ScatterStylizedTrees(environmentRoot, 19f, 34f, 524f, 644f, 7);
            ScatterDeadTrees(environmentRoot, -30f, -18f, 548f, 618f, 5);
        }

        private void BuildCheckpoint(Transform environmentRoot, Transform buildingsRoot, Transform vehiclesRoot, Transform propsRoot)
        {
            CreateSectionTitle(environmentRoot, "Section_05_Checkpoint", 706f);

            CreateMilitaryCheckpoint(buildingsRoot, propsRoot, new Vector3(0f, 0f, 706f));
            CreateVehicleWreck(vehiclesRoot, "CheckpointAPC", new Vector3(-11f, 0.46f, 701f), Quaternion.Euler(0f, 96f, 0f), new Color(0.45f, 0.53f, 0.36f), true, 1.2f);
            CreateVehicleWreck(vehiclesRoot, "CheckpointSedan", new Vector3(8f, 0.34f, 725f), Quaternion.Euler(0f, -18f, 0f), new Color(0.52f, 0.6f, 0.66f), false);

            ScatterStylizedTrees(environmentRoot, -36f, -19f, 654f, 784f, 8);
            ScatterStylizedTrees(environmentRoot, 18f, 38f, 656f, 788f, 12);
            ScatterDeadTrees(environmentRoot, 24f, 38f, 674f, 768f, 6);
            ScatterShrubs(environmentRoot, -28f, 34f, 660f, 782f, 14, 0.95f);
            ScatterScrap(propsRoot, -10f, 10f, 660f, 782f, 10, 0.55f);
        }

        private void BuildBurnoutMile(Transform environmentRoot, Transform buildingsRoot, Transform vehiclesRoot, Transform propsRoot)
        {
            CreateSectionTitle(environmentRoot, "Section_06_BurnoutMile", 860f);

            CreateCrashSite(propsRoot, vehiclesRoot, new Vector3(0f, 0f, 845f));
            CreateAbandonedHouse(buildingsRoot, new Vector3(-26f, 0f, 898f), Quaternion.Euler(0f, -12f, 0f), 0.9f, new Color(0.62f, 0.58f, 0.5f));
            CreateObservationTower(environmentRoot, new Vector3(28f, 0f, 930f));
            CreateVistaPoint(buildingsRoot, propsRoot, new Vector3(-22f, 0f, 916f), "Vista_FinalStretch", new Color(0.71f, 0.53f, 0.24f));

            ScatterDeadTrees(environmentRoot, -37f, -18f, 796f, 954f, 14);
            ScatterDeadTrees(environmentRoot, 18f, 38f, 798f, 956f, 14);
            ScatterStylizedTrees(environmentRoot, -34f, -22f, 812f, 952f, 5);
            ScatterStylizedTrees(environmentRoot, 22f, 36f, 814f, 952f, 5);
            ScatterScrap(propsRoot, -12f, 12f, 800f, 948f, 16, 0.85f);
        }

        private void BuildBackdrop(Transform environmentRoot, Transform propsRoot)
        {
            var horizonRoot = CreateGroup(environmentRoot, "HorizonSystem");
            horizonRoot.position = new Vector3(0f, 0f, 360f);
            SetStaticRecursively(horizonRoot, false);

            var foregroundBand = CreateGroup(horizonRoot, "ForegroundBand");
            var midgroundBand = CreateGroup(horizonRoot, "MidgroundBand");
            var backgroundBand = CreateGroup(horizonRoot, "BackgroundBand");
            var skylineBand = CreateGroup(horizonRoot, "SkylineBand");

            ConfigureHorizonBand(foregroundBand, 0.08f, 0.62f, new Vector3(0f, 0f, 220f));
            ConfigureHorizonBand(midgroundBand, 0.04f, 0.44f, new Vector3(0f, 0f, 280f));
            ConfigureHorizonBand(backgroundBand, 0.02f, 0.24f, new Vector3(0f, 0f, 340f));
            ConfigureHorizonBand(skylineBand, 0.01f, 0.14f, new Vector3(0f, 0f, 420f));

            BuildHorizonForeground(foregroundBand);
            BuildHorizonMidground(midgroundBand);
            BuildHorizonBackground(backgroundBand);
            BuildHorizonSkyline(skylineBand);
        }

        private void ConfigureHorizonBand(Transform bandRoot, float followXMultiplier, float followZMultiplier, Vector3 offset)
        {
            SetStaticRecursively(bandRoot, false);
            var follower = EnsureComponent<HorizonBandFollower>(bandRoot.gameObject);
            follower.followXMultiplier = followXMultiplier;
            follower.followZMultiplier = followZMultiplier;
            follower.positionOffset = offset;
        }

        private void BuildHorizonForeground(Transform bandRoot)
        {
            var ridgeLeft = GetMaterial("horizon_fg_left", new Color(0.47f, 0.58f, 0.43f), 0f, 0.02f);
            var ridgeRight = GetMaterial("horizon_fg_right", new Color(0.45f, 0.56f, 0.42f), 0f, 0.02f);
            var brushMass = GetMaterial("horizon_fg_brush", new Color(0.35f, 0.47f, 0.34f), 0f, 0.05f);

            CreateSilhouetteRidge(bandRoot, "FGRidgeLeft_A", new Vector3(-108f, 14f, 110f), new Vector3(42f, 20f, 180f), -6f, ridgeLeft);
            CreateSilhouetteRidge(bandRoot, "FGRidgeLeft_B", new Vector3(-92f, 9f, 370f), new Vector3(26f, 12f, 120f), -3f, ridgeLeft);
            CreateSilhouetteRidge(bandRoot, "FGRidgeRight_A", new Vector3(118f, 13f, 160f), new Vector3(48f, 22f, 210f), 7f, ridgeRight);
            CreateSilhouetteRidge(bandRoot, "FGRidgeRight_B", new Vector3(86f, 8f, 445f), new Vector3(24f, 11f, 110f), 4f, ridgeRight);

            CreateSilhouetteTreeMass(bandRoot, "FGBrushLeftA", new Vector3(-72f, 6f, 190f), new Vector3(18f, 10f, 58f), brushMass);
            CreateSilhouetteTreeMass(bandRoot, "FGBrushLeftB", new Vector3(-60f, 4.6f, 430f), new Vector3(11f, 7f, 34f), brushMass);
            CreateSilhouetteTreeMass(bandRoot, "FGBrushRightA", new Vector3(78f, 6.2f, 248f), new Vector3(20f, 11f, 64f), brushMass);
            CreateSilhouetteTreeMass(bandRoot, "FGBrushRightB", new Vector3(88f, 4.8f, 472f), new Vector3(10f, 6.2f, 30f), brushMass);
        }

        private void BuildHorizonMidground(Transform bandRoot)
        {
            var leftMesa = GetMaterial("horizon_mg_left", new Color(0.56f, 0.63f, 0.58f), 0f, 0.02f);
            var rightMesa = GetMaterial("horizon_mg_right", new Color(0.54f, 0.61f, 0.56f), 0f, 0.02f);
            var dryShelf = GetMaterial("horizon_mg_shelf", new Color(0.6f, 0.62f, 0.53f), 0f, 0.03f);

            CreateSilhouetteRidge(bandRoot, "MGRidgeLeft_A", new Vector3(-162f, 22f, 150f), new Vector3(70f, 36f, 230f), -5f, leftMesa);
            CreateSilhouetteRidge(bandRoot, "MGRidgeLeft_B", new Vector3(-226f, 28f, 510f), new Vector3(92f, 42f, 320f), -7f, leftMesa);
            CreateSilhouetteRidge(bandRoot, "MGRidgeRight_A", new Vector3(164f, 24f, 240f), new Vector3(74f, 34f, 240f), 5f, rightMesa);
            CreateSilhouetteRidge(bandRoot, "MGRidgeRight_B", new Vector3(228f, 20f, 620f), new Vector3(86f, 32f, 240f), 6f, rightMesa);

            CreateSilhouetteRidge(bandRoot, "MGShelfLeft", new Vector3(-116f, 8f, 330f), new Vector3(38f, 14f, 120f), -2f, dryShelf);
            CreateSilhouetteRidge(bandRoot, "MGShelfRight", new Vector3(110f, 9f, 458f), new Vector3(34f, 12f, 116f), 3f, dryShelf);
        }

        private void BuildHorizonBackground(Transform bandRoot)
        {
            var bgLeft = GetMaterial("horizon_bg_left", new Color(0.62f, 0.68f, 0.71f), 0f, 0.02f);
            var bgRight = GetMaterial("horizon_bg_right", new Color(0.61f, 0.67f, 0.7f), 0f, 0.02f);

            CreateSilhouetteRidge(bandRoot, "BGRidgeLeft_A", new Vector3(-274f, 34f, 110f), new Vector3(150f, 54f, 300f), -3f, bgLeft);
            CreateSilhouetteRidge(bandRoot, "BGRidgeLeft_B", new Vector3(-338f, 46f, 710f), new Vector3(166f, 72f, 410f), -4f, bgLeft);
            CreateSilhouetteRidge(bandRoot, "BGRidgeRight_A", new Vector3(248f, 30f, 180f), new Vector3(132f, 46f, 280f), 4f, bgRight);
            CreateSilhouetteRidge(bandRoot, "BGRidgeRight_B", new Vector3(332f, 42f, 640f), new Vector3(172f, 66f, 390f), 5f, bgRight);
        }

        private void BuildHorizonSkyline(Transform bandRoot)
        {
            var skyline = GetMaterial("horizon_skyline", new Color(0.67f, 0.73f, 0.78f), 0f, 0.02f);
            var silhouettes = new[]
            {
                new Vector3(-186f, 60f, 300f),
                new Vector3(-54f, 72f, 512f),
                new Vector3(98f, 66f, 614f),
                new Vector3(228f, 58f, 392f),
            };

            for (var i = 0; i < silhouettes.Length; i++)
            {
                var position = silhouettes[i];
                CreateSilhouetteRidge(bandRoot, $"Skyline_{i:00}", position, new Vector3(124f + i * 16f, 78f - i * 3f, 210f + i * 40f), i % 2 == 0 ? -2f : 3f, skyline);
            }
        }

        private void BuildCatastropheSetDressing(Transform environmentRoot, Transform propsRoot)
        {
            var soot = GetMaterial("apocalypse_soot", new Color(0.12f, 0.12f, 0.11f), 0f, 0.04f);
            var stainedConcrete = GetMaterial("apocalypse_stained_concrete", new Color(0.43f, 0.42f, 0.37f), 0.01f, 0.08f);
            var rust = GetMaterial("apocalypse_rust", new Color(0.42f, 0.23f, 0.15f), 0.02f, 0.08f);
            var bentMetal = GetMaterial("apocalypse_bent_metal", new Color(0.25f, 0.27f, 0.27f), 0.08f, 0.12f);
            var warningDim = GetMaterial("apocalypse_warning_dim", new Color(0.55f, 0.42f, 0.16f), 0.02f, 0.12f);

            CreateRoadScars(propsRoot, soot);

            CreateLeaningPole(environmentRoot, new Vector3(-15f, 0f, 188f), -18f);
            CreateLeaningPole(environmentRoot, new Vector3(15f, 0f, 346f), 14f);
            CreateLeaningPole(environmentRoot, new Vector3(-17f, 0f, 672f), -22f);
            CreateCollapsedPole(environmentRoot, new Vector3(10f, 0.25f, 804f), 64f);

            CreateBrokenSignCluster(propsRoot, new Vector3(-8.2f, 0f, 214f), Quaternion.Euler(0f, -18f, 8f), warningDim, bentMetal);
            CreateBrokenSignCluster(propsRoot, new Vector3(9.6f, 0f, 515f), Quaternion.Euler(0f, 22f, -10f), warningDim, bentMetal);
            CreateBrokenSignCluster(propsRoot, new Vector3(-9.8f, 0f, 766f), Quaternion.Euler(0f, -26f, 12f), warningDim, bentMetal);

            CreateAbandonedCamp(propsRoot, new Vector3(26f, 0f, 374f), stainedConcrete, rust);
            CreateAbandonedCamp(propsRoot, new Vector3(-28f, 0f, 742f), stainedConcrete, rust);

            CreateDebrisField(propsRoot, new Vector3(-12f, 0f, 318f), 12, rust, bentMetal);
            CreateDebrisField(propsRoot, new Vector3(13f, 0f, 692f), 14, rust, bentMetal);
            CreateDebrisField(propsRoot, new Vector3(-8f, 0f, 876f), 18, rust, bentMetal);

            CreatePrimitive(PrimitiveType.Cube, propsRoot, "CollapsedFence_Service", new Vector3(25f, 0.34f, 300f), new Vector3(0.16f, 0.68f, 18f), Quaternion.Euler(0f, 0f, -68f), bentMetal);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "CollapsedFence_Checkpoint", new Vector3(-22f, 0.36f, 716f), new Vector3(0.18f, 0.72f, 22f), Quaternion.Euler(0f, 8f, 64f), bentMetal);
        }

        private void BuildZombiePresence(Transform environmentRoot)
        {
            var zombieRoot = CreateGroup(environmentRoot, "ZombieEnemies_Waves");
            zombieRoot.gameObject.isStatic = false;

            CreateZombieCluster(zombieRoot, "SuburbanPorches", new[]
            {
                new Vector3(-18.5f, 0f, 66f),
                new Vector3(22f, 0f, 118f),
                new Vector3(-28f, 0f, 142f),
                new Vector3(8.5f, 0f, 74f),
                new Vector3(-10.5f, 0f, 104f),
                new Vector3(30f, 0f, 164f),
                new Vector3(-32f, 0f, 184f),
            }, 0.92f);

            CreateZombieCluster(zombieRoot, "FuelStop", new[]
            {
                new Vector3(-12.8f, 0f, 232f),
                new Vector3(-23.4f, 0f, 250f),
                new Vector3(-5.6f, 0f, 264f),
                new Vector3(8.8f, 0f, 218f),
                new Vector3(18.5f, 0f, 246f),
                new Vector3(-28.5f, 0f, 278f),
                new Vector3(28f, 0f, 294f),
            }, 1f);

            CreateZombieCluster(zombieRoot, "MarketAndRest", new[]
            {
                new Vector3(16.8f, 0f, 286f),
                new Vector3(25f, 0f, 406f),
                new Vector3(14.5f, 0f, 446f),
                new Vector3(-14.5f, 0f, 326f),
                new Vector3(7.5f, 0f, 368f),
                new Vector3(-26f, 0f, 418f),
                new Vector3(30f, 0f, 462f),
                new Vector3(-8f, 0f, 486f),
            }, 0.96f);

            CreateZombieCluster(zombieRoot, "BridgeAndCheckpoint", new[]
            {
                new Vector3(-9.2f, 0f, 548f),
                new Vector3(11.6f, 0f, 618f),
                new Vector3(-15.4f, 0f, 704f),
                new Vector3(18.8f, 0f, 730f),
                new Vector3(24f, 0f, 566f),
                new Vector3(-28f, 0f, 604f),
                new Vector3(5.5f, 0f, 668f),
                new Vector3(-24f, 0f, 742f),
                new Vector3(30f, 0f, 760f),
            }, 1.04f);

            CreateZombieCluster(zombieRoot, "BurnoutMile", new[]
            {
                new Vector3(-6.5f, 0f, 824f),
                new Vector3(8.4f, 0f, 852f),
                new Vector3(-18.8f, 0f, 884f),
                new Vector3(20.5f, 0f, 922f),
                new Vector3(-28f, 0f, 804f),
                new Vector3(28f, 0f, 836f),
                new Vector3(2f, 0f, 892f),
                new Vector3(-32f, 0f, 936f),
                new Vector3(12f, 0f, 956f),
            }, 1.08f);

            CreateZombieCluster(zombieRoot, "RoadSwarmReserve", new[]
            {
                new Vector3(-22f, 0f, 205f),
                new Vector3(24f, 0f, 350f),
                new Vector3(-24f, 0f, 515f),
                new Vector3(23f, 0f, 642f),
                new Vector3(-30f, 0f, 790f),
                new Vector3(31f, 0f, 905f),
                new Vector3(-8f, 0f, 965f),
                new Vector3(8f, 0f, 985f),
            }, 1.1f);
        }

        private void CreateZombieCluster(Transform parent, string clusterName, Vector3[] positions, float scaleMultiplier)
        {
            var cluster = CreateGroup(parent, clusterName);
            cluster.gameObject.isStatic = false;

            for (var i = 0; i < positions.Length; i++)
            {
                var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-3f, 3f));
                var scale = Random.Range(0.88f, 1.12f) * scaleMultiplier;
                CreateZombieFigure(cluster, $"{clusterName}_Zombie_{i:00}", positions[i], rotation, scale, i);
            }
        }

        private void CreateZombieFigure(Transform parent, string name, Vector3 position, Quaternion rotation, float scaleMultiplier, int variant)
        {
            var root = CreateGroup(parent, name);
            root.position = position;
            root.rotation = rotation;
            root.localScale = Vector3.one * scaleMultiplier;
            root.gameObject.isStatic = false;

            var skin = GetMaterial($"zombie_skin_{variant % 3}", variant % 3 == 0 ? new Color(0.39f, 0.49f, 0.43f) : variant % 3 == 1 ? new Color(0.34f, 0.42f, 0.38f) : new Color(0.43f, 0.46f, 0.4f), 0f, 0.08f);
            var shirt = GetMaterial($"zombie_shirt_{variant % 4}", variant % 4 == 0 ? new Color(0.25f, 0.31f, 0.34f) : variant % 4 == 1 ? new Color(0.38f, 0.28f, 0.23f) : variant % 4 == 2 ? new Color(0.28f, 0.34f, 0.25f) : new Color(0.31f, 0.29f, 0.27f), 0f, 0.06f);
            var pants = GetMaterial("zombie_pants", new Color(0.18f, 0.2f, 0.21f), 0f, 0.05f);
            var dark = GetMaterial("zombie_dark_detail", new Color(0.07f, 0.07f, 0.065f), 0f, 0.02f);

            var lean = variant % 2 == 0 ? -7f : 6f;
            var armPitchA = variant % 2 == 0 ? 18f : -8f;
            var armPitchB = variant % 2 == 0 ? -12f : 22f;

            CreatePrimitive(PrimitiveType.Capsule, root, "Torso", new Vector3(0f, 1.18f, 0f), new Vector3(0.44f, 0.62f, 0.32f), Quaternion.Euler(lean, 0f, 0f), shirt);
            CreatePrimitive(PrimitiveType.Sphere, root, "Head", new Vector3(0f, 1.94f, 0.04f), new Vector3(0.34f, 0.38f, 0.32f), Quaternion.Euler(lean * 0.5f, 0f, 0f), skin);
            CreatePrimitive(PrimitiveType.Cube, root, "JawShadow", new Vector3(0f, 1.81f, 0.2f), new Vector3(0.2f, 0.05f, 0.06f), Quaternion.identity, dark);

            CreatePrimitive(PrimitiveType.Capsule, root, "LeftArm", new Vector3(-0.38f, 1.2f, 0.12f), new Vector3(0.14f, 0.48f, 0.14f), Quaternion.Euler(armPitchA, 0f, -28f), skin);
            CreatePrimitive(PrimitiveType.Capsule, root, "RightArm", new Vector3(0.38f, 1.18f, 0.12f), new Vector3(0.14f, 0.5f, 0.14f), Quaternion.Euler(armPitchB, 0f, 28f), skin);
            CreatePrimitive(PrimitiveType.Capsule, root, "LeftLeg", new Vector3(-0.15f, 0.48f, 0f), new Vector3(0.15f, 0.52f, 0.15f), Quaternion.Euler(variant % 2 == 0 ? -4f : 6f, 0f, -3f), pants);
            CreatePrimitive(PrimitiveType.Capsule, root, "RightLeg", new Vector3(0.15f, 0.48f, 0f), new Vector3(0.15f, 0.52f, 0.15f), Quaternion.Euler(variant % 2 == 0 ? 7f : -5f, 0f, 3f), pants);
            CreatePrimitive(PrimitiveType.Cube, root, "TornCloth", new Vector3(0.18f, 0.88f, 0.2f), new Vector3(0.22f, 0.34f, 0.05f), Quaternion.Euler(0f, 0f, -12f), shirt);

            SetStaticRecursively(root, false);
            var enemy = EnsureComponent<ZombieEnemy>(root.gameObject);
            enemy.moveSpeed = 2.1f + (variant % 3) * 0.25f;
            enemy.shootRange = 19f + (variant % 2) * 4f;
            enemy.shotDamage = 4f + (variant % 3);
            enemy.fireInterval = 1.35f + (variant % 2) * 0.28f;
        }

        private void BuildCombatCoverAndVerticality(Transform propsRoot)
        {
            var concrete = GetMaterial("cover_cold_concrete", new Color(0.39f, 0.42f, 0.41f), 0.02f, 0.36f);
            var darkConcrete = GetMaterial("cover_soot_concrete", new Color(0.2f, 0.22f, 0.22f), 0.01f, 0.42f);
            var containerBlue = GetMaterial("cover_faded_container_blue", new Color(0.2f, 0.31f, 0.36f), 0.02f, 0.38f);
            var containerRust = GetMaterial("cover_faded_container_rust", new Color(0.43f, 0.23f, 0.15f), 0.02f, 0.46f);
            var asphalt = GetMaterial("elevated_cold_asphalt", new Color(0.2f, 0.23f, 0.23f), 0.01f, 0.52f);
            var barrierYellow = GetMaterial("hazard_faded_yellow", new Color(0.62f, 0.5f, 0.24f), 0.02f, 0.32f);

            CreateInvisibleCollider(PrimitiveType.Cube, propsRoot, "GroundCollision_OpenWorld", new Vector3(0f, -0.08f, 500f), new Vector3(130f, 0.16f, 1320f), Quaternion.identity);
            CreateTallCover(propsRoot, "HighCover_Suburb_BusShell", new Vector3(-6.4f, 1.25f, 156f), new Vector3(2.4f, 2.5f, 7.5f), Quaternion.Euler(0f, -10f, 1.5f), containerBlue);
            CreateTallCover(propsRoot, "HighCover_Gas_CollapsedContainer", new Vector3(7.6f, 1.35f, 258f), new Vector3(2.6f, 2.7f, 8.2f), Quaternion.Euler(0f, 16f, -3f), containerRust);
            CreateTallCover(propsRoot, "HighCover_RestStop_ConcreteStack", new Vector3(-8.6f, 1.15f, 432f), new Vector3(2.2f, 2.3f, 6.8f), Quaternion.Euler(0f, 5f, 0f), concrete);
            CreateTallCover(propsRoot, "HighCover_Checkpoint_BlastWallA", new Vector3(-5.2f, 1.45f, 690f), new Vector3(1.15f, 2.9f, 9.4f), Quaternion.Euler(0f, -18f, 0f), darkConcrete);
            CreateTallCover(propsRoot, "HighCover_Checkpoint_BlastWallB", new Vector3(6.8f, 1.45f, 716f), new Vector3(1.15f, 2.9f, 8.4f), Quaternion.Euler(0f, 14f, 0f), darkConcrete);
            CreateTallCover(propsRoot, "HighCover_Burnout_OverturnedTruck", new Vector3(-6.5f, 1.55f, 836f), new Vector3(2.8f, 3.1f, 9.2f), Quaternion.Euler(0f, -26f, 7f), containerRust);

            CreatePrimitive(PrimitiveType.Cube, propsRoot, "DriveRamp_ServiceUp", new Vector3(0f, 1.35f, 342f), new Vector3(8.2f, 0.38f, 34f), Quaternion.Euler(-7f, 0f, 0f), asphalt);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "ElevatedDrive_ServiceDeck", new Vector3(0f, 3.05f, 386f), new Vector3(8.2f, 0.42f, 52f), Quaternion.identity, asphalt);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "DriveRamp_ServiceDown", new Vector3(0f, 1.35f, 430f), new Vector3(8.2f, 0.38f, 34f), Quaternion.Euler(7f, 0f, 0f), asphalt);

            CreatePrimitive(PrimitiveType.Cube, propsRoot, "UpperRoad_LeftGuard", new Vector3(-4.35f, 3.55f, 386f), new Vector3(0.24f, 0.8f, 56f), Quaternion.identity, barrierYellow);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "UpperRoad_RightGuard", new Vector3(4.35f, 3.55f, 386f), new Vector3(0.24f, 0.8f, 56f), Quaternion.identity, barrierYellow);

            for (var i = 0; i < 9; i++)
            {
                var z = 188f + i * 72f;
                var side = i % 2 == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, propsRoot, $"Cover_JerseyBarrier_{i:00}", new Vector3(side * Random.Range(5.2f, 7.6f), 0.55f, z), new Vector3(2.2f, 1.1f, 0.7f), Quaternion.Euler(0f, Random.Range(-12f, 12f), 0f), concrete);
            }
        }

        private void CreateTallCover(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            CreatePrimitive(PrimitiveType.Cube, parent, name, position, scale, rotation, material);
            CreatePrimitive(PrimitiveType.Cube, parent, $"{name}_SootBand", position + Vector3.up * (scale.y * 0.18f), new Vector3(scale.x * 1.02f, 0.14f, scale.z * 1.03f), rotation, GetMaterial("cover_soot_band", new Color(0.08f, 0.09f, 0.085f), 0f, 0.54f));
        }

        private void CreateInvisibleCollider(PrimitiveType primitiveType, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            var colliderObject = CreatePrimitive(primitiveType, parent, name, localPosition, localScale, localRotation, GetMaterial("invisible_collision_material", new Color(0.1f, 0.1f, 0.1f), 0f, 0f));
            var renderer = colliderObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private void BuildPrototypeGameplaySystems(Transform prototypeRoot)
        {
            prototypeRoot.gameObject.isStatic = false;
            EnsureComponent<ZombieWaveManager>(prototypeRoot.gameObject);
            EnsureComponent<RoadtripCombatHud>(prototypeRoot.gameObject);
        }

        private void CreateRoadScars(Transform parent, Material soot)
        {
            var scarData = new[]
            {
                new Vector3(-1.8f, 0.115f, 166f),
                new Vector3(2.4f, 0.115f, 272f),
                new Vector3(-2.6f, 0.115f, 438f),
                new Vector3(1.2f, 0.115f, 638f),
                new Vector3(0.4f, 0.115f, 816f),
                new Vector3(-1.1f, 0.115f, 888f),
            };

            for (var i = 0; i < scarData.Length; i++)
            {
                var position = scarData[i];
                CreatePrimitive(PrimitiveType.Cube, parent, $"RoadSoot_{i:00}", position, new Vector3(Random.Range(2.4f, 5.2f), 0.018f, Random.Range(12f, 26f)), Quaternion.Euler(0f, Random.Range(-12f, 12f), 0f), soot);
                CreatePrimitive(PrimitiveType.Cube, parent, $"RoadSkid_{i:00}_A", position + new Vector3(-1.3f, 0.02f, Random.Range(-5f, 4f)), new Vector3(0.16f, 0.014f, Random.Range(10f, 24f)), Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f), soot);
                CreatePrimitive(PrimitiveType.Cube, parent, $"RoadSkid_{i:00}_B", position + new Vector3(1.3f, 0.02f, Random.Range(-5f, 4f)), new Vector3(0.14f, 0.014f, Random.Range(9f, 22f)), Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f), soot);
            }
        }

        private void CreateLeaningPole(Transform parent, Vector3 position, float tiltDegrees)
        {
            var root = CreateGroup(parent, $"LeaningPole_{position.z:000}");
            root.position = position;
            root.rotation = Quaternion.Euler(0f, Random.Range(-10f, 10f), tiltDegrees);

            var pole = GetMaterial("utility_pole", new Color(0.33f, 0.28f, 0.22f), 0.02f, 0.12f);
            var wire = GetMaterial("utility_wire_dark", new Color(0.08f, 0.08f, 0.08f), 0.04f, 0.08f);
            CreatePrimitive(PrimitiveType.Cube, root, "Pole", new Vector3(0f, 4.1f, 0f), new Vector3(0.34f, 8.2f, 0.34f), Quaternion.identity, pole);
            CreatePrimitive(PrimitiveType.Cube, root, "CrossArm", new Vector3(0f, 7.4f, 0f), new Vector3(2.5f, 0.14f, 0.2f), Quaternion.identity, pole);
            CreatePrimitive(PrimitiveType.Cube, root, "HangingWire", new Vector3(1.1f, 6.4f, 0.6f), new Vector3(0.06f, 0.06f, 5.4f), Quaternion.Euler(18f, 0f, 9f), wire);
        }

        private void CreateCollapsedPole(Transform parent, Vector3 position, float yaw)
        {
            var root = CreateGroup(parent, $"CollapsedPole_{position.z:000}");
            root.position = position;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            var pole = GetMaterial("utility_pole", new Color(0.33f, 0.28f, 0.22f), 0.02f, 0.12f);
            var wire = GetMaterial("utility_wire_dark", new Color(0.08f, 0.08f, 0.08f), 0.04f, 0.08f);
            CreatePrimitive(PrimitiveType.Cube, root, "FallenPole", new Vector3(0f, 0.34f, 0f), new Vector3(0.34f, 0.34f, 9.4f), Quaternion.Euler(0f, 0f, 5f), pole);
            CreatePrimitive(PrimitiveType.Cube, root, "CrossArm", new Vector3(0f, 0.58f, 2.8f), new Vector3(2.5f, 0.14f, 0.2f), Quaternion.identity, pole);
            CreatePrimitive(PrimitiveType.Cube, root, "SnappedWire", new Vector3(1.3f, 0.22f, -1.2f), new Vector3(0.05f, 0.05f, 12f), Quaternion.Euler(0f, 11f, 0f), wire);
        }

        private void CreateBrokenSignCluster(Transform parent, Vector3 position, Quaternion rotation, Material panel, Material metal)
        {
            var root = CreateGroup(parent, $"BrokenSign_{position.z:000}");
            root.position = position;
            root.rotation = rotation;
            CreatePrimitive(PrimitiveType.Cube, root, "BentPost", new Vector3(0f, 0.95f, 0f), new Vector3(0.16f, 1.9f, 0.16f), Quaternion.Euler(0f, 0f, 14f), metal);
            CreatePrimitive(PrimitiveType.Cube, root, "MainPanel", new Vector3(0.26f, 1.86f, 0f), new Vector3(2.2f, 0.82f, 0.1f), Quaternion.Euler(0f, 0f, -10f), panel);
            CreatePrimitive(PrimitiveType.Cube, root, "PanelShard", new Vector3(-0.72f, 0.42f, 0.34f), new Vector3(0.86f, 0.18f, 0.08f), Quaternion.Euler(0f, 18f, 31f), panel);
        }

        private void CreateAbandonedCamp(Transform parent, Vector3 origin, Material fabric, Material rust)
        {
            var root = CreateGroup(parent, $"AbandonedCamp_{origin.z:000}");
            root.position = origin;
            root.rotation = Quaternion.Euler(0f, Random.Range(-18f, 18f), 0f);

            CreatePrimitive(PrimitiveType.Cube, root, "TarpSag", new Vector3(0f, 0.82f, 0f), new Vector3(4.8f, 0.12f, 3.4f), Quaternion.Euler(0f, 0f, -9f), fabric);
            CreatePrimitive(PrimitiveType.Cube, root, "CrateA", new Vector3(-1.9f, 0.38f, 1.3f), new Vector3(1.1f, 0.76f, 1.1f), Quaternion.Euler(0f, 21f, 0f), rust);
            CreatePrimitive(PrimitiveType.Cube, root, "CrateB", new Vector3(1.7f, 0.3f, -1.1f), new Vector3(0.9f, 0.6f, 1.2f), Quaternion.Euler(0f, -16f, 0f), rust);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Barrel", new Vector3(2.6f, 0.52f, 1.4f), new Vector3(0.42f, 0.52f, 0.42f), Quaternion.identity, rust);
        }

        private void CreateDebrisField(Transform parent, Vector3 origin, int count, Material rust, Material metal)
        {
            for (var i = 0; i < count; i++)
            {
                var material = i % 3 == 0 ? rust : metal;
                var size = new Vector3(Random.Range(0.25f, 1.5f), Random.Range(0.08f, 0.48f), Random.Range(0.4f, 2.1f));
                var position = origin + new Vector3(Random.Range(-7f, 7f), size.y * 0.5f + 0.02f, Random.Range(-12f, 12f));
                CreatePrimitive(PrimitiveType.Cube, parent, $"DebrisField_{origin.z:000}_{i:00}", position, size, Quaternion.Euler(Random.Range(-18f, 18f), Random.Range(0f, 180f), Random.Range(-20f, 20f)), material);
            }
        }

        private void CreateSilhouetteRidge(Transform parent, string name, Vector3 localPosition, Vector3 localScale, float rollDegrees, Material material)
        {
            var root = CreateGroup(parent, name);
            root.localPosition = localPosition;
            root.localRotation = Quaternion.Euler(0f, 0f, rollDegrees);
            root.localScale = Vector3.one;
            root.gameObject.isStatic = false;

            CreatePrimitive(PrimitiveType.Cube, root, "BaseShelf", new Vector3(0f, localScale.y * 0.18f, 0f), new Vector3(localScale.x * 0.82f, localScale.y * 0.32f, localScale.z * 0.9f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Sphere, root, "MassA", new Vector3(-localScale.x * 0.28f, localScale.y * 0.34f, -localScale.z * 0.18f), new Vector3(localScale.x * 0.54f, localScale.y * 0.62f, localScale.z * 0.44f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Sphere, root, "MassB", new Vector3(localScale.x * 0.04f, localScale.y * 0.42f, localScale.z * 0.08f), new Vector3(localScale.x * 0.66f, localScale.y * 0.76f, localScale.z * 0.52f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Sphere, root, "MassC", new Vector3(localScale.x * 0.34f, localScale.y * 0.28f, localScale.z * 0.26f), new Vector3(localScale.x * 0.44f, localScale.y * 0.54f, localScale.z * 0.34f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Cylinder, root, "MesaCap", new Vector3(-localScale.x * 0.04f, localScale.y * 0.66f, localScale.z * 0.06f), new Vector3(localScale.x * 0.12f, localScale.y * 0.08f, localScale.z * 0.16f), Quaternion.Euler(90f, 0f, 0f), material);
        }

        private void CreateSilhouetteTreeMass(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var root = CreateGroup(parent, name);
            root.localPosition = localPosition;
            root.localScale = Vector3.one;
            root.gameObject.isStatic = false;

            CreatePrimitive(PrimitiveType.Cube, root, "TrunkBand", new Vector3(0f, localScale.y * 0.18f, 0f), new Vector3(localScale.x * 0.28f, localScale.y * 0.32f, localScale.z * 0.72f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyA", new Vector3(-localScale.x * 0.26f, localScale.y * 0.48f, -localScale.z * 0.16f), new Vector3(localScale.x * 0.56f, localScale.y * 0.62f, localScale.z * 0.42f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyB", new Vector3(localScale.x * 0.08f, localScale.y * 0.54f, localScale.z * 0.04f), new Vector3(localScale.x * 0.72f, localScale.y * 0.72f, localScale.z * 0.5f), Quaternion.identity, material);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyC", new Vector3(localScale.x * 0.3f, localScale.y * 0.44f, localScale.z * 0.2f), new Vector3(localScale.x * 0.38f, localScale.y * 0.46f, localScale.z * 0.3f), Quaternion.identity, material);
        }

        private void BuildEventZoneVisuals(Transform eventZonesRoot)
        {
            CreateEventZoneVisual(eventZonesRoot, "SuburbanSilenceZone", new Vector3(0f, 0f, 120f), new Vector3(18f, 4f, 22f), new Color(0.72f, 0.39f, 0.95f));
            CreateEventZoneVisual(eventZonesRoot, "FuelForecourtZone", new Vector3(-4f, 0f, 240f), new Vector3(30f, 4f, 24f), new Color(0.42f, 0.86f, 1f));
            CreateEventZoneVisual(eventZonesRoot, "MarketScrambleZone", new Vector3(8f, 0f, 280f), new Vector3(28f, 4f, 22f), new Color(0.4f, 1f, 0.78f));
            CreateEventZoneVisual(eventZonesRoot, "RestStopEchoZone", new Vector3(0f, 0f, 420f), new Vector3(22f, 4f, 24f), new Color(0.98f, 0.8f, 0.3f));
            CreateEventZoneVisual(eventZonesRoot, "BridgeFunnelZone", new Vector3(0f, 0f, 588f), new Vector3(14f, 5f, 22f), new Color(1f, 0.54f, 0.35f));
            CreateEventZoneVisual(eventZonesRoot, "CheckpointSweepZone", new Vector3(0f, 0f, 706f), new Vector3(24f, 5f, 26f), new Color(0.55f, 0.92f, 0.95f));
            CreateEventZoneVisual(eventZonesRoot, "BurnoutChokeZone", new Vector3(0f, 0f, 845f), new Vector3(24f, 5f, 24f), new Color(0.98f, 0.44f, 0.44f));
        }

        private void BuildPreviewVehicle(Transform vehiclesRoot, Vector3 startPosition)
        {
            var previewCar = CreateGroup(vehiclesRoot, "PreviewPlayerCar");
            previewCar.position = startPosition;
            previewCar.gameObject.isStatic = false;

            var rigidbody = previewCar.gameObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = previewCar.gameObject.AddComponent<Rigidbody>();
            }

            var collider = previewCar.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = previewCar.gameObject.AddComponent<BoxCollider>();
            }

            collider.center = new Vector3(0f, 0.65f, 0f);
            collider.size = new Vector3(2.2f, 1.35f, 4.6f);

            var controller = previewCar.gameObject.GetComponent<PreviewCarController>();
            if (controller == null)
            {
                controller = previewCar.gameObject.AddComponent<PreviewCarController>();
            }

            var shellMaterial = GetMaterial("player_shell", new Color(0.82f, 0.81f, 0.72f), 0.06f, 0.28f);
            var accentMaterial = GetMaterial("player_accent", new Color(0.23f, 0.55f, 0.57f), 0.04f, 0.28f, new Color(0.03f, 0.08f, 0.09f));
            var glassMaterial = GetMaterial("glass", new Color(0.16f, 0.2f, 0.24f), 0.05f, 0.8f);
            var wheelMaterial = GetMaterial("wheel", new Color(0.08f, 0.08f, 0.08f), 0.02f, 0.18f);
            var trimMaterial = GetMaterial("player_trim", new Color(0.18f, 0.19f, 0.18f), 0.03f, 0.16f);
            var roofMaterial = GetMaterial("player_roofgear", new Color(0.66f, 0.64f, 0.56f), 0.04f, 0.18f);

            CreatePrimitive(PrimitiveType.Cube, previewCar, "RearBody", new Vector3(0f, 0.95f, -0.35f), new Vector3(2.24f, 1.06f, 3.9f), Quaternion.identity, shellMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "CabBase", new Vector3(0f, 0.78f, 1.66f), new Vector3(2f, 0.72f, 1.4f), Quaternion.identity, shellMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "CabTop", new Vector3(0f, 1.2f, 1.18f), new Vector3(1.88f, 0.42f, 1.3f), Quaternion.identity, glassMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "OverCab", new Vector3(0f, 1.45f, 1.18f), new Vector3(1.98f, 0.34f, 1.78f), Quaternion.identity, shellMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, previewCar, "RoofCap", new Vector3(0f, 1.52f, -0.58f), new Vector3(0.78f, 1.22f, 0.78f), Quaternion.Euler(90f, 0f, 0f), shellMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "Windshield", new Vector3(0f, 1.12f, 1.82f), new Vector3(1.7f, 0.34f, 0.12f), Quaternion.Euler(-18f, 0f, 0f), glassMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "FrontGrille", new Vector3(0f, 0.7f, 2.28f), new Vector3(1.62f, 0.42f, 0.14f), Quaternion.identity, trimMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "FrontBumper", new Vector3(0f, 0.4f, 2.28f), new Vector3(1.88f, 0.16f, 0.22f), Quaternion.identity, trimMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "SideStripe", new Vector3(0f, 0.88f, -0.22f), new Vector3(2.26f, 0.16f, 3.78f), Quaternion.identity, accentMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "RoofUnitA", new Vector3(0f, 1.82f, -0.65f), new Vector3(1.08f, 0.24f, 0.72f), Quaternion.identity, roofMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "RoofUnitB", new Vector3(-0.42f, 1.8f, 0.74f), new Vector3(0.56f, 0.2f, 0.82f), Quaternion.identity, roofMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "RoofRack", new Vector3(0.42f, 1.84f, 0.58f), new Vector3(0.78f, 0.1f, 1.02f), Quaternion.identity, trimMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "SideAwning", new Vector3(1.18f, 1.26f, -0.18f), new Vector3(0.08f, 0.18f, 2.82f), Quaternion.identity, trimMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "RearLadder", new Vector3(-0.98f, 1.24f, -2.08f), new Vector3(0.08f, 1.22f, 0.42f), Quaternion.identity, trimMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "SideWindowA", new Vector3(-0.92f, 1.2f, -0.2f), new Vector3(0.08f, 0.5f, 1.22f), Quaternion.identity, glassMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "SideWindowB", new Vector3(0.92f, 1.16f, -0.58f), new Vector3(0.08f, 0.42f, 0.92f), Quaternion.identity, glassMaterial);
            CreatePrimitive(PrimitiveType.Cube, previewCar, "SideWindowC", new Vector3(0.92f, 1.14f, 0.74f), new Vector3(0.08f, 0.38f, 0.62f), Quaternion.identity, glassMaterial);

            CreateWheel(previewCar, new Vector3(-0.95f, 0.35f, 1.25f), wheelMaterial);
            CreateWheel(previewCar, new Vector3(0.95f, 0.35f, 1.25f), wheelMaterial);
            CreateWheel(previewCar, new Vector3(-0.95f, 0.35f, -1.2f), wheelMaterial);
            CreateWheel(previewCar, new Vector3(0.95f, 0.35f, -1.2f), wheelMaterial);

            var cameraPivot = CreateGroup(previewCar, "CameraPivot");
            cameraPivot.localPosition = new Vector3(0f, 1.8f, -0.6f);
            cameraPivot.gameObject.isStatic = false;
            controller.cameraPivot = cameraPivot;
            var health = EnsureComponent<VehicleHealth>(previewCar.gameObject);
            health.maxHealth = 100f;
            health.currentHealth = 100f;
            health.holdRepairRate = 16f;
            EnsureComponent<VehicleExitController>(previewCar.gameObject);
            SetStaticRecursively(previewCar, false);
        }

        private void CreateGasStation(Transform buildingsRoot, Transform propsRoot, Vector3 origin)
        {
            var root = CreateGroup(buildingsRoot, "GasStation_LastLight");
            root.position = origin;

            var facade = GetMaterial("station_facade", new Color(0.84f, 0.82f, 0.73f), 0.02f, 0.28f);
            var canopy = GetMaterial("station_canopy", new Color(0.67f, 0.73f, 0.75f), 0.04f, 0.34f);
            var accent = GetMaterial("station_accent", new Color(0.28f, 0.72f, 0.75f), 0.03f, 0.44f, new Color(0.06f, 0.14f, 0.16f));
            var concrete = GetMaterial("service_concrete", new Color(0.8f, 0.76f, 0.65f), 0.01f, 0.16f);
            var steel = GetMaterial("station_steel", new Color(0.53f, 0.55f, 0.53f), 0.08f, 0.24f);
            var trim = GetMaterial("station_trim", new Color(0.94f, 0.9f, 0.82f), 0.01f, 0.28f);
            var glass = GetMaterial("store_glass", new Color(0.17f, 0.22f, 0.26f), 0.05f, 0.78f);

            CreatePrimitive(PrimitiveType.Cube, root, "Forecourt", new Vector3(0f, 0f, 0f), new Vector3(16f, 0.08f, 18f), Quaternion.identity, concrete);
            CreatePrimitive(PrimitiveType.Cube, root, "StoreBody", new Vector3(-3.2f, 2.4f, -5.3f), new Vector3(7.6f, 4.8f, 7.8f), Quaternion.identity, facade);
            CreatePrimitive(PrimitiveType.Cube, root, "StoreParapet", new Vector3(-3.2f, 4.95f, -5.3f), new Vector3(8f, 0.34f, 8.1f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "StoreGlass", new Vector3(-3.2f, 2.3f, -1.54f), new Vector3(4.6f, 2.35f, 0.16f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "Door", new Vector3(-5.6f, 1.5f, -1.5f), new Vector3(1.25f, 3f, 0.14f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "SignBand", new Vector3(-3.2f, 4.1f, -1.46f), new Vector3(5.2f, 0.52f, 0.2f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, root, "CanopyDeck", new Vector3(1.7f, 4f, 1.7f), new Vector3(11.4f, 0.32f, 8.8f), Quaternion.identity, canopy);
            CreatePrimitive(PrimitiveType.Cube, root, "CanopyAccent", new Vector3(1.7f, 3.74f, 5.82f), new Vector3(11.4f, 0.18f, 0.32f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cylinder, root, "CanopyLipLeft", new Vector3(-4f, 3.84f, 1.7f), new Vector3(0.18f, 4.4f, 0.18f), Quaternion.Euler(0f, 0f, 90f), trim);
            CreatePrimitive(PrimitiveType.Cylinder, root, "CanopyLipRight", new Vector3(7.4f, 3.84f, 1.7f), new Vector3(0.18f, 4.4f, 0.18f), Quaternion.Euler(0f, 0f, 90f), trim);
            CreatePrimitive(PrimitiveType.Cube, root, "GarageBay", new Vector3(-8.35f, 2.05f, -5.4f), new Vector3(2.4f, 4.1f, 5.8f), Quaternion.identity, facade);
            CreatePrimitive(PrimitiveType.Cube, root, "GarageDoor", new Vector3(-8.35f, 1.58f, -2.78f), new Vector3(1.92f, 2.7f, 0.14f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cube, root, "RoofVentA", new Vector3(-5.8f, 5.26f, -6f), new Vector3(1.2f, 0.28f, 1f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cube, root, "RoofVentB", new Vector3(-1.2f, 5.22f, -6.3f), new Vector3(0.9f, 0.22f, 0.9f), Quaternion.identity, steel);

            CreatePrimitive(PrimitiveType.Cylinder, root, "SupportA", new Vector3(-2f, 2f, 1.3f), new Vector3(0.28f, 2f, 0.28f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cylinder, root, "SupportB", new Vector3(5.4f, 2f, 1.3f), new Vector3(0.28f, 2f, 0.28f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cylinder, root, "SupportC", new Vector3(-2f, 2f, 4.7f), new Vector3(0.28f, 2f, 0.28f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cylinder, root, "SupportD", new Vector3(5.4f, 2f, 4.7f), new Vector3(0.28f, 2f, 0.28f), Quaternion.identity, steel);

            CreateFuelPump(root, new Vector3(0f, 0.62f, 2.1f), accent);
            CreateFuelPump(root, new Vector3(3.2f, 0.62f, 2.1f), accent);
            CreateFuelPump(root, new Vector3(0f, 0.62f, 3.8f), accent);
            CreateFuelPump(root, new Vector3(3.2f, 0.62f, 3.8f), accent);
            CreatePrimitive(PrimitiveType.Cube, root, "PumpIslandA", new Vector3(0f, 0.12f, 2.1f), new Vector3(1.8f, 0.18f, 1.1f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "PumpIslandB", new Vector3(3.2f, 0.12f, 2.1f), new Vector3(1.8f, 0.18f, 1.1f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "PumpIslandC", new Vector3(0f, 0.12f, 3.8f), new Vector3(1.8f, 0.18f, 1.1f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "PumpIslandD", new Vector3(3.2f, 0.12f, 3.8f), new Vector3(1.8f, 0.18f, 1.1f), Quaternion.identity, trim);

            CreatePrimitive(PrimitiveType.Cylinder, propsRoot, "StationSignPylon", origin + new Vector3(-7.5f, 3.3f, 7.5f), new Vector3(0.32f, 3.3f, 0.32f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "StationSignPanel", origin + new Vector3(-7.5f, 6.8f, 7.5f), new Vector3(2.8f, 2.2f, 0.4f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "AirPump", origin + new Vector3(-7.2f, 1.1f, 1f), new Vector3(0.9f, 2.2f, 0.9f), Quaternion.identity, steel);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "TrashCluster", origin + new Vector3(7.5f, 0.52f, -6.4f), new Vector3(1.6f, 1.04f, 1.2f), Quaternion.identity, concrete);
        }

        private void CreateMarketStop(Transform buildingsRoot, Transform propsRoot, Vector3 origin)
        {
            var root = CreateGroup(buildingsRoot, "FoodMart_Crossing");
            root.position = origin;

            var wall = GetMaterial("market_wall", new Color(0.82f, 0.78f, 0.66f), 0.02f, 0.22f);
            var roof = GetMaterial("market_roof", new Color(0.42f, 0.46f, 0.48f), 0.03f, 0.14f);
            var accent = GetMaterial("market_accent", new Color(0.36f, 0.77f, 0.58f), 0.04f, 0.36f, new Color(0.08f, 0.16f, 0.12f));
            var concrete = GetMaterial("market_pad", new Color(0.79f, 0.75f, 0.64f), 0.02f, 0.12f);
            var trim = GetMaterial("market_trim", new Color(0.93f, 0.89f, 0.8f), 0.01f, 0.24f);
            var glass = GetMaterial("market_glass", new Color(0.14f, 0.18f, 0.2f), 0.05f, 0.74f);

            CreatePrimitive(PrimitiveType.Cube, root, "ParkingPad", new Vector3(0f, 0f, 0f), new Vector3(14f, 0.08f, 16f), Quaternion.identity, concrete);
            CreatePrimitive(PrimitiveType.Cube, root, "StoreBody", new Vector3(0f, 2.55f, -2f), new Vector3(10.5f, 5.1f, 7.8f), Quaternion.identity, wall);
            CreatePrimitive(PrimitiveType.Cube, root, "Roof", new Vector3(0f, 5.35f, -2f), new Vector3(11.1f, 0.42f, 8.4f), Quaternion.identity, roof);
            CreatePrimitive(PrimitiveType.Cube, root, "Parapet", new Vector3(0f, 5.04f, 2.08f), new Vector3(11.1f, 0.72f, 0.34f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, root, "FrontAwning", new Vector3(0f, 4.2f, 2.1f), new Vector3(11.1f, 0.3f, 0.45f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, root, "FrontGlass", new Vector3(0f, 2.2f, 1.7f), new Vector3(6.1f, 2.3f, 0.16f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "EntryDoor", new Vector3(-3.55f, 1.35f, 1.68f), new Vector3(1.2f, 2.7f, 0.16f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cylinder, root, "AwningTube", new Vector3(0f, 4.08f, 2.34f), new Vector3(0.14f, 5.2f, 0.14f), Quaternion.Euler(0f, 0f, 90f), trim);
            CreatePrimitive(PrimitiveType.Cube, root, "BackStorage", new Vector3(-3.9f, 1.8f, -5.8f), new Vector3(2.9f, 3.6f, 3.3f), Quaternion.identity, wall);
            CreatePrimitive(PrimitiveType.Cube, root, "LoadingCanopy", new Vector3(-4.1f, 3.3f, -5.35f), new Vector3(3.6f, 0.22f, 4.1f), Quaternion.identity, roof);
            CreatePrimitive(PrimitiveType.Cube, root, "RooftopUnitA", new Vector3(-2.8f, 5.72f, -2.5f), new Vector3(1.42f, 0.34f, 1.04f), Quaternion.identity, roof);
            CreatePrimitive(PrimitiveType.Cube, root, "RooftopUnitB", new Vector3(2.2f, 5.66f, -1.8f), new Vector3(1.1f, 0.28f, 0.92f), Quaternion.identity, roof);

            CreatePrimitive(PrimitiveType.Cube, propsRoot, "MarketLoadingDock", origin + new Vector3(-6.2f, 0.5f, -4.6f), new Vector3(2.4f, 1f, 2.8f), Quaternion.identity, concrete);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "MarketCrateA", origin + new Vector3(-5.8f, 0.4f, 4.8f), new Vector3(1.3f, 0.8f, 1.1f), Quaternion.identity, roof);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "MarketCrateB", origin + new Vector3(-4.4f, 0.55f, 5.2f), new Vector3(1.1f, 1.1f, 1.2f), Quaternion.identity, roof);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "MarketCooler", origin + new Vector3(5.5f, 1.1f, 5.4f), new Vector3(1.1f, 2.2f, 1.1f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "MarketCartCorral", origin + new Vector3(6.3f, 0.38f, 1.8f), new Vector3(1.8f, 0.76f, 1.3f), Quaternion.identity, trim);
        }

        private void CreateRestStop(Transform buildingsRoot, Transform propsRoot, Vector3 origin)
        {
            var root = CreateGroup(buildingsRoot, "DryBasinRestStop");
            root.position = origin;

            var shelter = GetMaterial("rest_shelter", new Color(0.84f, 0.8f, 0.66f), 0.02f, 0.22f);
            var roof = GetMaterial("rest_roof", new Color(0.42f, 0.48f, 0.5f), 0.05f, 0.18f);
            var wood = GetMaterial("rest_wood", new Color(0.64f, 0.5f, 0.32f), 0.02f, 0.16f);
            var accent = GetMaterial("rest_vending", new Color(0.24f, 0.55f, 0.63f), 0.05f, 0.34f);

            CreatePrimitive(PrimitiveType.Cube, root, "ShelterPad", new Vector3(0f, 0f, 0f), new Vector3(10f, 0.08f, 12f), Quaternion.identity, shelter);
            CreatePrimitive(PrimitiveType.Cube, root, "ShelterRoof", new Vector3(0f, 3.25f, 0f), new Vector3(9.4f, 0.35f, 11.4f), Quaternion.identity, roof);
            CreatePrimitive(PrimitiveType.Cylinder, root, "RoofTubeFront", new Vector3(0f, 3.02f, 5.72f), new Vector3(0.16f, 4.7f, 0.16f), Quaternion.Euler(0f, 0f, 90f), wood);
            CreatePrimitive(PrimitiveType.Cylinder, root, "RoofTubeBack", new Vector3(0f, 3.02f, -5.72f), new Vector3(0.16f, 4.7f, 0.16f), Quaternion.Euler(0f, 0f, 90f), wood);
            CreatePrimitive(PrimitiveType.Cylinder, root, "PostA", new Vector3(-3.8f, 1.6f, -4.5f), new Vector3(0.18f, 1.6f, 0.18f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cylinder, root, "PostB", new Vector3(3.8f, 1.6f, -4.5f), new Vector3(0.18f, 1.6f, 0.18f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cylinder, root, "PostC", new Vector3(-3.8f, 1.6f, 4.5f), new Vector3(0.18f, 1.6f, 0.18f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cylinder, root, "PostD", new Vector3(3.8f, 1.6f, 4.5f), new Vector3(0.18f, 1.6f, 0.18f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "BenchA", new Vector3(-2.5f, 0.5f, 0f), new Vector3(2.1f, 0.32f, 0.6f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "BenchB", new Vector3(2.5f, 0.5f, 0f), new Vector3(2.1f, 0.32f, 0.6f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "RestKiosk", origin + new Vector3(-6.2f, 1.6f, -2.8f), new Vector3(1.8f, 3.2f, 1.8f), Quaternion.identity, shelter);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "RestVending", origin + new Vector3(5.5f, 1.2f, -2.8f), new Vector3(1f, 2.4f, 0.8f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "PlanterA", origin + new Vector3(-3.6f, 0.32f, 5.2f), new Vector3(1.8f, 0.64f, 0.9f), Quaternion.identity, wood);
            CreateShrubCluster(propsRoot, origin + new Vector3(-3.6f, 0.15f, 5.2f), 0.55f);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "PlanterB", origin + new Vector3(3.2f, 0.32f, 5.2f), new Vector3(1.8f, 0.64f, 0.9f), Quaternion.identity, wood);
            CreateShrubCluster(propsRoot, origin + new Vector3(3.2f, 0.15f, 5.2f), 0.55f);
        }

        private void CreateMilitaryCheckpoint(Transform buildingsRoot, Transform propsRoot, Vector3 origin)
        {
            var root = CreateGroup(buildingsRoot, "Checkpoint_ShatteredLine");
            root.position = origin;

            var barrier = GetMaterial("checkpoint_barrier", new Color(0.74f, 0.72f, 0.61f), 0.02f, 0.18f);
            var metal = GetMaterial("checkpoint_metal", new Color(0.41f, 0.46f, 0.45f), 0.06f, 0.22f);
            var tarp = GetMaterial("checkpoint_tarp", new Color(0.58f, 0.67f, 0.49f), 0.01f, 0.12f);
            var warning = GetMaterial("warning", new Color(0.88f, 0.72f, 0.28f), 0.04f, 0.26f);
            var accent = GetMaterial("checkpoint_accent", new Color(0.33f, 0.7f, 0.74f), 0.04f, 0.28f, new Color(0.05f, 0.11f, 0.12f));

            CreatePrimitive(PrimitiveType.Cube, root, "BarricadeCore", new Vector3(0f, 0.6f, 0f), new Vector3(12f, 1.2f, 1.2f), Quaternion.identity, barrier);
            CreatePrimitive(PrimitiveType.Cube, root, "BarricadeWingLeft", new Vector3(-7.6f, 0.6f, -2.2f), new Vector3(3.6f, 1.2f, 5f), Quaternion.Euler(0f, -22f, 0f), barrier);
            CreatePrimitive(PrimitiveType.Cube, root, "BarricadeWingRight", new Vector3(7.6f, 0.6f, 2.2f), new Vector3(3.6f, 1.2f, 5f), Quaternion.Euler(0f, 22f, 0f), barrier);
            CreatePrimitive(PrimitiveType.Cube, root, "ObservationTower", new Vector3(-13f, 3.6f, -6f), new Vector3(2.5f, 7.2f, 2.5f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, root, "TowerCap", new Vector3(-13f, 7.6f, -6f), new Vector3(3.2f, 0.3f, 3.2f), Quaternion.identity, barrier);
            CreatePrimitive(PrimitiveType.Cube, root, "GateArm", new Vector3(4.4f, 2.4f, -4f), new Vector3(0.3f, 0.3f, 8f), Quaternion.Euler(0f, 0f, 78f), warning);
            CreatePrimitive(PrimitiveType.Cube, root, "GuidePanel", new Vector3(0f, 2.1f, -5.6f), new Vector3(4.2f, 1.1f, 0.2f), Quaternion.identity, accent);
            CreateTent(root, new Vector3(-14f, 0f, 4.5f), tarp);
            CreateTent(root, new Vector3(15f, 0f, -4.5f), tarp);

            CreatePrimitive(PrimitiveType.Cube, propsRoot, "CheckpointFenceLeft", origin + new Vector3(-18f, 0.85f, 0f), new Vector3(0.18f, 1.7f, 20f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "CheckpointFenceRight", origin + new Vector3(18f, 0.85f, 0f), new Vector3(0.18f, 1.7f, 20f), Quaternion.identity, metal);
        }

        private void CreateCrashSite(Transform propsRoot, Transform vehiclesRoot, Vector3 origin)
        {
            var scorch = GetMaterial("scorch", new Color(0.24f, 0.21f, 0.19f), 0.03f, 0.08f);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "CrashScorchA", origin + new Vector3(0f, 0.05f, 0f), new Vector3(8f, 0.03f, 22f), Quaternion.identity, scorch);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, "CrashScorchB", origin + new Vector3(3f, 0.05f, 8f), new Vector3(5f, 0.03f, 12f), Quaternion.Euler(0f, 12f, 0f), scorch);

            CreateVehicleWreck(vehiclesRoot, "CrashTanker", origin + new Vector3(-1.8f, 0.68f, 0f), Quaternion.Euler(0f, 12f, 0f), new Color(0.35f, 0.27f, 0.2f), true, 1.6f);
            CreateVehicleWreck(vehiclesRoot, "CrashSedanA", origin + new Vector3(3.5f, 0.34f, -8f), Quaternion.Euler(0f, -28f, 0f), new Color(0.24f, 0.2f, 0.19f), true);
            CreateVehicleWreck(vehiclesRoot, "CrashSedanB", origin + new Vector3(-4.6f, 0.34f, 9f), Quaternion.Euler(0f, 36f, 0f), new Color(0.22f, 0.24f, 0.26f), true);
            CreateVehicleWreck(vehiclesRoot, "CrashVan", origin + new Vector3(7.5f, 0.45f, 4f), Quaternion.Euler(0f, 84f, 0f), new Color(0.34f, 0.32f, 0.28f), true, 1.12f);

            for (var i = 0; i < 8; i++)
            {
                var piece = CreatePrimitive(PrimitiveType.Cube, propsRoot, $"CrashDebris_{i:00}", origin + new Vector3(Random.Range(-9f, 9f), 0.32f, Random.Range(-14f, 14f)), new Vector3(Random.Range(0.5f, 1.6f), Random.Range(0.2f, 0.7f), Random.Range(0.6f, 1.7f)), Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 180f), Random.Range(-18f, 18f)), GetMaterial("crash_debris", new Color(0.41f, 0.35f, 0.28f), 0.04f, 0.1f));
                piece.isStatic = true;
            }
        }

        private void CreateObservationTower(Transform parent, Vector3 position)
        {
            var root = CreateGroup(parent, "ObservationTower_Final");
            root.position = position;
            var metal = GetMaterial("tower_metal", new Color(0.45f, 0.52f, 0.48f), 0.04f, 0.14f);
            var deck = GetMaterial("tower_deck", new Color(0.69f, 0.57f, 0.39f), 0.01f, 0.12f);
            var lightMaterial = GetMaterial("tower_light", new Color(0.72f, 0.87f, 0.84f), 0.08f, 0.44f, new Color(0.08f, 0.14f, 0.13f));

            CreatePrimitive(PrimitiveType.Cube, root, "TowerStem", new Vector3(0f, 5f, 0f), new Vector3(1.4f, 10f, 1.4f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, root, "TowerHead", new Vector3(0f, 10.8f, 0f), new Vector3(3.8f, 1.6f, 3.8f), Quaternion.identity, deck);
            CreatePrimitive(PrimitiveType.Cube, root, "TowerLamp", new Vector3(0f, 10.8f, 2f), new Vector3(1.2f, 0.4f, 0.3f), Quaternion.identity, lightMaterial);
        }

        private void CreateAbandonedHouse(Transform parent, Vector3 position, Quaternion rotation, float scaleMultiplier, Color wallColor)
        {
            var root = CreateGroup(parent, $"House_{position.z:000}");
            root.position = position;
            root.rotation = rotation;
            root.localScale = Vector3.one * scaleMultiplier;

            var wall = GetMaterial($"house_wall_{wallColor.r:0.00}", wallColor, 0.02f, 0.18f);
            var roof = GetMaterial("house_roof", new Color(0.54f, 0.41f, 0.28f), 0.03f, 0.14f);
            var wood = GetMaterial("house_board", new Color(0.61f, 0.48f, 0.31f), 0.02f, 0.16f);
            var trim = GetMaterial("house_trim", new Color(0.91f, 0.85f, 0.75f), 0.01f, 0.18f);
            var glass = GetMaterial("house_glass", new Color(0.19f, 0.24f, 0.28f), 0.05f, 0.7f);

            CreatePrimitive(PrimitiveType.Cube, root, "LotPad", new Vector3(0f, 0.02f, 0.4f), new Vector3(10.2f, 0.08f, 11.4f), Quaternion.identity, GetMaterial("house_lot", new Color(0.66f, 0.61f, 0.46f), 0f, 0.04f));
            CreatePrimitive(PrimitiveType.Cube, root, "Driveway", new Vector3(-2.8f, 0.06f, 4.9f), new Vector3(2.4f, 0.1f, 2.7f), Quaternion.identity, GetMaterial("house_driveway", new Color(0.58f, 0.55f, 0.49f), 0f, 0.03f));
            CreatePrimitive(PrimitiveType.Cube, root, "Body", new Vector3(0f, 2.1f, 0f), new Vector3(7.2f, 4.2f, 6.8f), Quaternion.identity, wall);
            CreatePrimitive(PrimitiveType.Cube, root, "RearVolume", new Vector3(-1.6f, 1.5f, -3.3f), new Vector3(2.5f, 3f, 2.4f), Quaternion.identity, wall);
            CreatePrimitive(PrimitiveType.Cube, root, "RoofA", new Vector3(0f, 4.9f, 0f), new Vector3(7.5f, 0.22f, 7.4f), Quaternion.Euler(0f, 0f, 10f), roof);
            CreatePrimitive(PrimitiveType.Cube, root, "RoofB", new Vector3(0f, 4.9f, 0f), new Vector3(7.5f, 0.22f, 7.4f), Quaternion.Euler(0f, 0f, -10f), roof);
            CreatePrimitive(PrimitiveType.Cube, root, "Porch", new Vector3(0f, 0.45f, 3.8f), new Vector3(3.2f, 0.22f, 1.5f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cylinder, root, "PorchPostA", new Vector3(-1.25f, 1.15f, 4.18f), new Vector3(0.08f, 1.1f, 0.08f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cylinder, root, "PorchPostB", new Vector3(1.25f, 1.15f, 4.18f), new Vector3(0.08f, 1.1f, 0.08f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "WindowTrimA", new Vector3(-2f, 2.2f, 3.42f), new Vector3(1.8f, 1.6f, 0.12f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "WindowTrimB", new Vector3(2f, 2.2f, 3.42f), new Vector3(1.6f, 1.6f, 0.12f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "WindowGlassA", new Vector3(-2f, 2.2f, 3.36f), new Vector3(1.45f, 1.25f, 0.08f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "WindowGlassB", new Vector3(2f, 2.2f, 3.36f), new Vector3(1.25f, 1.25f, 0.08f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "SideWindow", new Vector3(3.56f, 2.15f, -0.8f), new Vector3(0.08f, 1.1f, 1.35f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "WindowBoardA", new Vector3(-2f, 2.2f, 3.5f), new Vector3(1.5f, 0.18f, 0.18f), Quaternion.Euler(0f, 0f, 10f), wood);
            CreatePrimitive(PrimitiveType.Cube, root, "WindowBoardB", new Vector3(2f, 2.2f, 3.5f), new Vector3(1.35f, 0.18f, 0.18f), Quaternion.Euler(0f, 0f, -8f), wood);
            CreatePrimitive(PrimitiveType.Cube, root, "Door", new Vector3(0f, 1.1f, 3.42f), new Vector3(1.2f, 2.2f, 0.12f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "Chimney", new Vector3(2.15f, 5.6f, -1.8f), new Vector3(0.8f, 1.8f, 0.8f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "FenceStubA", new Vector3(-4.3f, 0.38f, 4.8f), new Vector3(0.16f, 0.76f, 2.4f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "FenceStubB", new Vector3(4.2f, 0.38f, 4.1f), new Vector3(0.16f, 0.76f, 1.8f), Quaternion.identity, wood);
            CreateShrubCluster(root, new Vector3(-3.9f, 0f, -2.2f), 0.72f);
            CreateShrubCluster(root, new Vector3(3.8f, 0f, -3.1f), 0.66f);
        }

        private void CreateUtilityPole(Transform parent, Vector3 position)
        {
            var poleMaterial = GetMaterial("utility_pole", new Color(0.33f, 0.28f, 0.22f), 0.02f, 0.12f);
            var metal = GetMaterial("utility_metal", new Color(0.21f, 0.22f, 0.23f), 0.08f, 0.18f);

            CreatePrimitive(PrimitiveType.Cube, parent, $"Pole_{position.z:000}", position + new Vector3(0f, 4.2f, 0f), new Vector3(0.38f, 8.4f, 0.38f), Quaternion.identity, poleMaterial);
            CreatePrimitive(PrimitiveType.Cube, parent, $"CrossArm_{position.z:000}", position + new Vector3(0f, 7.7f, 0f), new Vector3(2.4f, 0.15f, 0.22f), Quaternion.identity, poleMaterial);
            CreatePrimitive(PrimitiveType.Sphere, parent, $"Junction_{position.z:000}", position + new Vector3(0f, 8.05f, 0f), new Vector3(0.3f, 0.3f, 0.3f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, parent, $"BraceLeft_{position.z:000}", position + new Vector3(-0.42f, 6.8f, 0f), new Vector3(0.12f, 1.9f, 0.12f), Quaternion.Euler(0f, 0f, 24f), poleMaterial);
            CreatePrimitive(PrimitiveType.Cube, parent, $"BraceRight_{position.z:000}", position + new Vector3(0.42f, 6.8f, 0f), new Vector3(0.12f, 1.9f, 0.12f), Quaternion.Euler(0f, 0f, -24f), poleMaterial);
        }

        private void CreateBillboard(Transform parent, Vector3 position, Quaternion rotation, Color panelColor, string rootName)
        {
            var root = CreateGroup(parent, rootName);
            root.position = position;
            root.rotation = rotation;

            var support = GetMaterial("billboard_support", new Color(0.23f, 0.22f, 0.21f), 0.05f, 0.12f);
            var panel = GetMaterial($"billboard_panel_{panelColor.r:0.00}", panelColor, 0.03f, 0.18f);

            CreatePrimitive(PrimitiveType.Cube, root, "SupportA", new Vector3(-1.2f, -2.2f, 0f), new Vector3(0.4f, 5.8f, 0.4f), Quaternion.identity, support);
            CreatePrimitive(PrimitiveType.Cube, root, "SupportB", new Vector3(1.2f, -2.2f, 0f), new Vector3(0.4f, 5.8f, 0.4f), Quaternion.identity, support);
            CreatePrimitive(PrimitiveType.Cube, root, "Panel", new Vector3(0f, 0f, 0f), new Vector3(6.8f, 3.4f, 0.3f), Quaternion.identity, panel);
        }

        private void CreateRoadSign(Transform parent, Vector3 position, string rootName)
        {
            var root = CreateGroup(parent, rootName.Replace(" ", "_"));
            root.position = position;
            var support = GetMaterial("sign_support", new Color(0.22f, 0.22f, 0.22f), 0.08f, 0.16f);
            var panel = GetMaterial("sign_panel", new Color(0.26f, 0.42f, 0.48f), 0.04f, 0.26f);
            CreatePrimitive(PrimitiveType.Cube, root, "Pole", new Vector3(0f, 0f, 0f), new Vector3(0.18f, 3.1f, 0.18f), Quaternion.identity, support);
            CreatePrimitive(PrimitiveType.Cube, root, "Panel", new Vector3(0f, 1.2f, 0f), new Vector3(2.6f, 1.2f, 0.12f), Quaternion.identity, panel);
        }

        private void CreateChargingPylon(Transform parent, Vector3 position)
        {
            var shell = GetMaterial("charger_shell", new Color(0.24f, 0.28f, 0.31f), 0.08f, 0.3f);
            var glow = GetMaterial("charger_glow", new Color(0.18f, 0.62f, 0.8f), 0.04f, 0.56f, new Color(0.08f, 0.18f, 0.24f));
            CreatePrimitive(PrimitiveType.Cube, parent, "BrokenChargingPylon", position + new Vector3(0f, 1.4f, 0f), new Vector3(1.4f, 2.8f, 1.1f), Quaternion.identity, shell);
            CreatePrimitive(PrimitiveType.Cube, parent, "BrokenChargingScreen", position + new Vector3(0f, 1.7f, 0.58f), new Vector3(0.7f, 1f, 0.08f), Quaternion.identity, glow);
        }

        private void CreateFuelPump(Transform parent, Vector3 position, Material accent)
        {
            var body = GetMaterial("pump_body", new Color(0.42f, 0.43f, 0.43f), 0.08f, 0.2f);
            var root = CreateGroup(parent, $"FuelPump_{position.x:0.0}_{position.z:0.0}");
            root.localPosition = position;
            CreatePrimitive(PrimitiveType.Cube, root, "Body", new Vector3(0f, 0f, 0f), new Vector3(0.9f, 1.25f, 0.8f), Quaternion.identity, body);
            CreatePrimitive(PrimitiveType.Cube, root, "Display", new Vector3(0f, 0.2f, 0.42f), new Vector3(0.5f, 0.42f, 0.08f), Quaternion.identity, accent);
        }

        private void CreateTent(Transform parent, Vector3 position, Material tarp)
        {
            var root = CreateGroup(parent, $"Tent_{position.x:0.0}_{position.z:0.0}");
            root.localPosition = position;
            var metal = GetMaterial("tent_frame", new Color(0.24f, 0.24f, 0.22f), 0.08f, 0.16f);
            CreatePrimitive(PrimitiveType.Cube, root, "FrameA", new Vector3(-1.8f, 1.2f, -1.8f), new Vector3(0.12f, 2.4f, 0.12f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, root, "FrameB", new Vector3(1.8f, 1.2f, -1.8f), new Vector3(0.12f, 2.4f, 0.12f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, root, "FrameC", new Vector3(-1.8f, 1.2f, 1.8f), new Vector3(0.12f, 2.4f, 0.12f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, root, "FrameD", new Vector3(1.8f, 1.2f, 1.8f), new Vector3(0.12f, 2.4f, 0.12f), Quaternion.identity, metal);
            CreatePrimitive(PrimitiveType.Cube, root, "Tarp", new Vector3(0f, 2.45f, 0f), new Vector3(4f, 0.22f, 4f), Quaternion.identity, tarp);
        }

        private void CreateVehicleWreck(Transform parent, string name, Vector3 position, Quaternion rotation, Color bodyColor, bool damaged, float scaleMultiplier = 1f)
        {
            var root = CreateGroup(parent, name);
            root.position = position;
            root.rotation = rotation;
            root.localScale = Vector3.one * scaleMultiplier;

            var body = GetMaterial($"vehicle_body_{bodyColor.r:0.00}_{bodyColor.g:0.00}", bodyColor, 0.08f, 0.34f);
            var glass = GetMaterial("vehicle_glass", new Color(0.14f, 0.16f, 0.18f), 0.06f, 0.68f);
            var wheel = GetMaterial("vehicle_wheel", new Color(0.06f, 0.06f, 0.07f), 0.02f, 0.16f);
            var trim = GetMaterial("vehicle_trim", new Color(0.82f, 0.79f, 0.72f), 0.03f, 0.24f);

            CreatePrimitive(PrimitiveType.Cube, root, "Chassis", new Vector3(0f, 0.48f, 0f), new Vector3(2f, 0.65f, 4.1f), Quaternion.identity, body);
            CreatePrimitive(PrimitiveType.Cylinder, root, "RoofCap", new Vector3(0f, 0.98f, -0.1f), new Vector3(0.42f, 0.96f, 0.42f), Quaternion.Euler(90f, 0f, 0f), body);
            CreatePrimitive(PrimitiveType.Cube, root, "Cabin", new Vector3(0f, 0.9f, -0.2f), new Vector3(1.72f, 0.44f, 2.04f), Quaternion.identity, glass);
            CreatePrimitive(PrimitiveType.Cube, root, "Bumper", new Vector3(0f, 0.43f, 1.95f), new Vector3(1.85f, 0.16f, 0.2f), Quaternion.identity, trim);
            CreatePrimitive(PrimitiveType.Cube, root, "RearBox", new Vector3(0f, 0.78f, -1.32f), new Vector3(1.72f, 0.34f, 1.32f), Quaternion.identity, body);

            var dentScale = damaged ? new Vector3(1.68f, 0.22f, 1.22f) : new Vector3(1.8f, 0.18f, 1.5f);
            CreatePrimitive(PrimitiveType.Cube, root, "Hood", new Vector3(0f, 0.74f, 1.52f), dentScale, Quaternion.Euler(damaged ? -8f : 0f, 0f, damaged ? 3f : 0f), body);
            if (damaged)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "DoorDent", new Vector3(0.86f, 0.68f, -0.24f), new Vector3(0.12f, 0.58f, 1.12f), Quaternion.Euler(0f, 0f, 9f), trim);
            }

            CreateWheel(root, new Vector3(-0.94f, 0.22f, 1.18f), wheel);
            CreateWheel(root, new Vector3(0.94f, 0.22f, 1.18f), wheel);
            CreateWheel(root, new Vector3(-0.94f, 0.22f, -1.22f), wheel);
            CreateWheel(root, new Vector3(0.94f, 0.22f, -1.22f), wheel);
        }

        private void CreateWheel(Transform parent, Vector3 position, Material material)
        {
            var wheel = CreatePrimitive(PrimitiveType.Cylinder, parent, $"Wheel_{position.x:0.0}_{position.z:0.0}", position, new Vector3(0.32f, 0.18f, 0.32f), Quaternion.Euler(90f, 0f, 0f), material);
            wheel.isStatic = true;
        }

        private void CreateBarrier(Transform parent, Vector3 position, Quaternion rotation)
        {
            var material = GetMaterial("barrier", new Color(0.87f, 0.54f, 0.19f), 0.03f, 0.28f);
            CreatePrimitive(PrimitiveType.Cube, parent, $"Barrier_{position.z:000}", position, new Vector3(1.4f, 0.74f, 0.45f), rotation, material);
            CreatePrimitive(PrimitiveType.Cylinder, parent, $"BarrierCapA_{position.z:000}", position + rotation * new Vector3(-0.62f, 0f, 0f), new Vector3(0.18f, 0.22f, 0.18f), rotation * Quaternion.Euler(0f, 0f, 90f), material);
            CreatePrimitive(PrimitiveType.Cylinder, parent, $"BarrierCapB_{position.z:000}", position + rotation * new Vector3(0.62f, 0f, 0f), new Vector3(0.18f, 0.22f, 0.18f), rotation * Quaternion.Euler(0f, 0f, 90f), material);
        }

        private void ScatterDeadTrees(Transform parent, float minX, float maxX, float minZ, float maxZ, int count)
        {
            ScatterClustered(parent, count, minX, maxX, minZ, maxZ, 2, 4, (position) => CreateDeadTree(parent, position, Random.Range(2.8f, 5.8f)));
        }

        private void ScatterStylizedTrees(Transform parent, float minX, float maxX, float minZ, float maxZ, int count)
        {
            ScatterClustered(parent, count, minX, maxX, minZ, maxZ, 2, 5, (position) => CreateStylizedTree(parent, position, Random.Range(2.9f, 5.1f)));
        }

        private void ScatterShrubs(Transform parent, float minX, float maxX, float minZ, float maxZ, int count, float scaleMultiplier)
        {
            ScatterClustered(parent, count, minX, maxX, minZ, maxZ, 3, 6, (position) => CreateShrubCluster(parent, position, Random.Range(0.8f, 1.4f) * scaleMultiplier));
        }

        private void CreateDeadTree(Transform parent, Vector3 position, float height)
        {
            var root = CreateGroup(parent, $"Tree_{position.x:0}_{position.z:0}");
            root.position = position;
            root.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), Random.Range(-4f, 4f));

            var trunk = GetMaterial("dead_trunk", new Color(0.24f, 0.2f, 0.16f), 0.02f, 0.08f);
            var branch = GetMaterial("dead_branch", new Color(0.17f, 0.2f, 0.16f), 0.02f, 0.06f);

            CreatePrimitive(PrimitiveType.Cylinder, root, "Trunk", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.18f, height * 0.5f, 0.18f), Quaternion.identity, trunk);
            CreatePrimitive(PrimitiveType.Cube, root, "BranchA", new Vector3(0.35f, height * 0.72f, 0f), new Vector3(0.12f, 0.12f, 1.2f), Quaternion.Euler(-12f, 0f, 26f), branch);
            CreatePrimitive(PrimitiveType.Cube, root, "BranchB", new Vector3(-0.28f, height * 0.62f, 0.1f), new Vector3(0.12f, 0.12f, 1f), Quaternion.Euler(8f, 0f, -28f), branch);
        }

        private void CreateStylizedTree(Transform parent, Vector3 position, float height)
        {
            var root = CreateGroup(parent, $"StylizedTree_{position.x:0}_{position.z:0}");
            root.position = position;
            root.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);

            var trunk = GetMaterial("stylized_trunk", new Color(0.48f, 0.35f, 0.21f), 0.02f, 0.12f);
            var foliagePrimary = GetMaterial("stylized_foliage_primary", new Color(0.36f, 0.73f, 0.5f), 0f, 0.16f);
            var foliageSecondary = GetMaterial("stylized_foliage_secondary", new Color(0.55f, 0.84f, 0.58f), 0f, 0.18f);

            CreatePrimitive(PrimitiveType.Cylinder, root, "TrunkBase", new Vector3(0f, height * 0.26f, 0f), new Vector3(0.2f, height * 0.26f, 0.2f), Quaternion.identity, trunk);
            CreatePrimitive(PrimitiveType.Cylinder, root, "TrunkUpper", new Vector3(0f, height * 0.58f, 0f), new Vector3(0.14f, height * 0.18f, 0.14f), Quaternion.identity, trunk);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyCore", new Vector3(0f, height * 0.94f, 0f), new Vector3(1.58f, 1.06f, 1.48f), Quaternion.identity, foliagePrimary);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyLeft", new Vector3(-0.62f, height * 0.88f, 0.18f), new Vector3(0.98f, 0.76f, 0.98f), Quaternion.identity, foliageSecondary);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyRight", new Vector3(0.68f, height * 0.84f, -0.12f), new Vector3(0.92f, 0.72f, 0.9f), Quaternion.identity, foliageSecondary);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyRear", new Vector3(0.12f, height * 0.9f, -0.54f), new Vector3(0.86f, 0.66f, 0.82f), Quaternion.identity, foliagePrimary);
            CreatePrimitive(PrimitiveType.Sphere, root, "CanopyLow", new Vector3(-0.16f, height * 0.72f, 0.42f), new Vector3(0.74f, 0.48f, 0.7f), Quaternion.identity, foliageSecondary);
        }

        private void CreateShrubCluster(Transform parent, Vector3 position, float scaleMultiplier)
        {
            var root = CreateGroup(parent, $"Shrub_{position.x:0}_{position.z:0}");
            root.position = position;
            root.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);

            var shrubPrimary = GetMaterial("shrub_primary", new Color(0.5f, 0.76f, 0.41f), 0f, 0.12f);
            var shrubSecondary = GetMaterial("shrub_secondary", new Color(0.69f, 0.84f, 0.48f), 0f, 0.14f);

            CreatePrimitive(PrimitiveType.Sphere, root, "ShrubA", new Vector3(0f, 0.45f * scaleMultiplier, 0f), new Vector3(1f, 0.65f, 1f) * scaleMultiplier, Quaternion.identity, shrubPrimary);
            CreatePrimitive(PrimitiveType.Sphere, root, "ShrubB", new Vector3(0.42f * scaleMultiplier, 0.38f * scaleMultiplier, 0.18f * scaleMultiplier), new Vector3(0.7f, 0.45f, 0.7f) * scaleMultiplier, Quaternion.identity, shrubSecondary);
            CreatePrimitive(PrimitiveType.Sphere, root, "ShrubC", new Vector3(-0.38f * scaleMultiplier, 0.36f * scaleMultiplier, -0.22f * scaleMultiplier), new Vector3(0.62f, 0.42f, 0.62f) * scaleMultiplier, Quaternion.identity, shrubSecondary);
            CreatePrimitive(PrimitiveType.Sphere, root, "ShrubD", new Vector3(0.08f * scaleMultiplier, 0.34f * scaleMultiplier, -0.44f * scaleMultiplier), new Vector3(0.62f, 0.4f, 0.62f) * scaleMultiplier, Quaternion.identity, shrubPrimary);
        }

        private void CreateVistaPoint(Transform buildingsRoot, Transform propsRoot, Vector3 origin, string name, Color accentColor)
        {
            var root = CreateGroup(buildingsRoot, name);
            root.position = origin;

            var deck = GetMaterial($"vista_deck_{name}", new Color(0.73f, 0.67f, 0.52f), 0.01f, 0.14f);
            var wood = GetMaterial($"vista_wood_{name}", new Color(0.58f, 0.44f, 0.28f), 0.01f, 0.12f);
            var accent = GetMaterial($"vista_accent_{name}", accentColor, 0.03f, 0.28f, accentColor * 0.08f);

            CreatePrimitive(PrimitiveType.Cube, root, "Platform", new Vector3(0f, 0f, 0f), new Vector3(8f, 0.16f, 6f), Quaternion.identity, deck);
            CreatePrimitive(PrimitiveType.Cube, root, "BenchA", new Vector3(-2f, 0.52f, 1.5f), new Vector3(1.8f, 0.3f, 0.55f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "BenchB", new Vector3(2f, 0.52f, 1.5f), new Vector3(1.8f, 0.3f, 0.55f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "SignPole", new Vector3(3.1f, 1.35f, -1.8f), new Vector3(0.18f, 2.7f, 0.18f), Quaternion.identity, wood);
            CreatePrimitive(PrimitiveType.Cube, root, "SignPanel", new Vector3(3.1f, 2.45f, -1.8f), new Vector3(1.6f, 0.85f, 0.12f), Quaternion.identity, accent);
            CreatePrimitive(PrimitiveType.Cube, propsRoot, $"{name}_Fence", origin + new Vector3(-4.1f, 0.7f, 0f), new Vector3(0.14f, 1.4f, 6.2f), Quaternion.identity, wood);
        }

        private void ScatterScrap(Transform parent, float minX, float maxX, float minZ, float maxZ, int count, float scaleMultiplier)
        {
            var scrap = GetMaterial("scrap", new Color(0.26f, 0.24f, 0.2f), 0.06f, 0.1f);
            var index = 0;
            ScatterClustered(parent, count, minX, maxX, minZ, maxZ, 2, 5, (position) =>
            {
                var size = new Vector3(Random.Range(0.35f, 1.4f), Random.Range(0.15f, 0.7f), Random.Range(0.35f, 1.7f)) * scaleMultiplier;
                CreatePrimitive(PrimitiveType.Cube, parent, $"Scrap_{index:00}", new Vector3(position.x, size.y * 0.5f - 0.02f, position.z), size, Quaternion.Euler(Random.Range(-16f, 16f), Random.Range(0f, 180f), Random.Range(-14f, 14f)), scrap);
                index++;
            });
        }

        private void ScatterClustered(Transform parent, int count, float minX, float maxX, float minZ, float maxZ, int minClusters, int maxClusters, System.Action<Vector3> spawn)
        {
            var clusterCount = Mathf.Clamp(Random.Range(minClusters, maxClusters + 1), 1, Mathf.Max(1, count));
            var centers = new Vector3[clusterCount];

            for (var i = 0; i < clusterCount; i++)
            {
                centers[i] = new Vector3(Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));
            }

            for (var i = 0; i < count; i++)
            {
                var center = centers[i % clusterCount];
                var radiusX = Mathf.Lerp(2.8f, 9.5f, Random.value);
                var radiusZ = Mathf.Lerp(5.5f, 18f, Random.value);
                var position = new Vector3(
                    Mathf.Clamp(center.x + Random.Range(-radiusX, radiusX), minX, maxX),
                    0f,
                    Mathf.Clamp(center.z + Random.Range(-radiusZ, radiusZ), minZ, maxZ));
                spawn(position);
            }
        }

        private void CreateSectionTitle(Transform parent, string name, float zPosition)
        {
            var group = CreateGroup(parent, name);
            group.localPosition = new Vector3(0f, 0f, zPosition);
        }

        private void CreateHookMarker(Transform parent, string name, MarkerKind kind, Vector3 position, Vector3 size, string description, Color color)
        {
            var markerTransform = CreateGroup(parent, name);
            markerTransform.position = position;

            var marker = markerTransform.gameObject.GetComponent<AreaMarker>();
            if (marker == null)
            {
                marker = markerTransform.gameObject.AddComponent<AreaMarker>();
            }

            marker.kind = kind;
            marker.markerId = name;
            marker.description = description;
            marker.triggerSize = size;
            marker.gizmoColor = color;
        }

        private void CreateEventZoneVisual(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var zone = CreateGroup(parent, name);
            zone.position = position;

            var marker = zone.gameObject.GetComponent<AreaMarker>();
            if (marker == null)
            {
                marker = zone.gameObject.AddComponent<AreaMarker>();
            }

            marker.kind = MarkerKind.EventTrigger;
            marker.markerId = name;
            marker.description = "Visible placeholder zone for later encounter beats and readable roadside events.";
            marker.triggerSize = size;
            marker.gizmoColor = color;

            var beaconMaterial = GetMaterial($"zone_{name}", Color.Lerp(color, Color.white, 0.1f), 0.04f, 0.38f, color * 0.08f);
            CreatePrimitive(PrimitiveType.Cylinder, zone, "BeaconLeft", new Vector3(-(size.x * 0.5f + 1.2f), 1.15f, 0f), new Vector3(0.18f, 1.15f, 0.18f), Quaternion.identity, beaconMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, zone, "BeaconRight", new Vector3(size.x * 0.5f + 1.2f, 1.15f, 0f), new Vector3(0.18f, 1.15f, 0.18f), Quaternion.identity, beaconMaterial);
            CreatePrimitive(PrimitiveType.Cube, zone, "GateTop", new Vector3(0f, 2.5f, 0f), new Vector3(size.x + 1.2f, 0.12f, 0.18f), Quaternion.identity, beaconMaterial);
        }

        private void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightRoot = CreateGroup(parent, name);
            lightRoot.localPosition = position;
            var point = lightRoot.gameObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = color;
            point.intensity = intensity;
            point.range = range;
            point.shadows = LightShadows.None;
        }

        private Material GetMaterial(string key, Color color, float metallic, float smoothness, Color? emission = null)
        {
            if (materials.TryGetValue(key, out var existing))
            {
                return existing;
            }

            color = GradeApocalypseColor(color);

            var useUrp = GraphicsSettings.currentRenderPipeline != null ||
                         GraphicsSettings.defaultRenderPipeline != null ||
                         QualitySettings.renderPipeline != null;

            var shader = useUrp ? Shader.Find("Universal Render Pipeline/Lit") : null;
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
            }

            var material = new Material(shader);
            material.name = $"Generated_{key}";

            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", GradeApocalypseEmission(emission.Value));
                }
            }

            material.enableInstancing = true;

            materials[key] = material;
            return material;
        }

        private static Color GradeApocalypseColor(Color color)
        {
            var grayscale = color.grayscale;
            var desaturated = Color.Lerp(color, new Color(grayscale, grayscale, grayscale, color.a), 0.42f);
            var cold = new Color(0.78f, 0.86f, 0.95f, 1f);
            var graded = Color.Lerp(desaturated, new Color(desaturated.r * cold.r, desaturated.g * cold.g, desaturated.b * cold.b, desaturated.a), 0.38f);
            graded *= 0.78f;
            graded.a = color.a;
            return graded;
        }

        private static Color GradeApocalypseEmission(Color color)
        {
            var grayscale = color.grayscale;
            var desaturated = Color.Lerp(color, new Color(grayscale, grayscale, grayscale, color.a), 0.22f);
            desaturated *= 0.52f;
            desaturated.a = color.a;
            return desaturated;
        }

        private Material GetSkyboxMaterial(string key, Color skyTint, Color groundTint, float atmosphereThickness, float exposure)
        {
            if (materials.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                return GetMaterial(key, skyTint, 0f, 0f);
            }

            var material = new Material(shader);
            material.name = $"Generated_{key}";
            material.SetColor("_SkyTint", skyTint);
            material.SetColor("_GroundColor", groundTint);
            material.SetFloat("_AtmosphereThickness", atmosphereThickness);
            material.SetFloat("_Exposure", exposure);
            if (material.HasProperty("_SunSize"))
            {
                material.SetFloat("_SunSize", 0.035f);
            }
            if (material.HasProperty("_SunSizeConvergence"))
            {
                material.SetFloat("_SunSizeConvergence", 4f);
            }

            materials[key] = material;
            return material;
        }

        private Transform EnsureSceneRoot(string name)
        {
            foreach (var rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootObject.name == name)
                {
                    return rootObject.transform;
                }
            }

            return new GameObject(name).transform;
        }

        private Transform CreateGroup(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;
            group.localScale = Vector3.one;
            group.gameObject.isStatic = true;
            return group;
        }

        private T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private GameObject CreatePrimitive(PrimitiveType primitiveType, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;
            primitive.isStatic = true;

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            var collider = primitive.GetComponent<Collider>();
            if (collider != null && !ShouldRetainCollider(parent))
            {
                DestroyComponent(collider, !Application.isPlaying);
            }

            return primitive;
        }

        private bool ShouldRetainCollider(Transform parent)
        {
            for (var current = parent; current != null; current = current.parent)
            {
                if (current.name == "Road" ||
                    current.name == "Terrain" ||
                    current.name == "HorizonSystem" ||
                    current.name == "EventZones" ||
                    current.name == "PreviewPlayerCar")
                {
                    return false;
                }

                if (current.name == "Buildings" ||
                    current.name == "Props" ||
                    current.name == "Vehicles" ||
                    current.name == "ZombieVisuals_NoAI" ||
                    current.name == "ZombieEnemies_Waves")
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearChildren(Transform parent, bool immediate)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyComponent(parent.GetChild(i).gameObject, immediate);
            }
        }

        private void SetStaticRecursively(Transform root, bool isStatic)
        {
            root.gameObject.isStatic = isStatic;
            for (var i = 0; i < root.childCount; i++)
            {
                SetStaticRecursively(root.GetChild(i), isStatic);
            }
        }

        private static void DestroyComponent(Object target, bool immediate)
        {
            if (target == null)
            {
                return;
            }

            if (immediate)
            {
                DestroyImmediate(target);
                return;
            }

            Destroy(target);
        }
    }
}
