using System.Collections.Generic;
using System.IO;
using PostApocRoadtrip.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PostApocRoadtrip.Editor
{
    public static class WorldStreamingScaffolder
    {
        [MenuItem("Tools/Roadtrip World/Scaffold Additive Chunk Scenes")]
        public static void ScaffoldChunkScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            RoadtripWorldEditorTools.OpenWorldScene();
            var chunks = Object.FindObjectsOfType<WorldChunkAuthoring>(true);
            if (chunks.Length == 0)
            {
                Debug.LogWarning("No WorldChunkAuthoring components found. Rebuild the world first.");
                return;
            }

            foreach (var chunk in chunks)
            {
                ScaffoldChunkScene(chunk);
            }

            UpdateBuildSettings(chunks);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Scaffolded {chunks.Length} additive chunk scenes.");
        }

        private static void ScaffoldChunkScene(WorldChunkAuthoring sourceChunk)
        {
            if (sourceChunk == null || string.IsNullOrWhiteSpace(sourceChunk.scenePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(sourceChunk.scenePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var root = new GameObject("WorldChunkRoot");
            var chunk = root.AddComponent<WorldChunkAuthoring>();
            chunk.regionId = sourceChunk.regionId;
            chunk.chunkId = sourceChunk.chunkId;
            chunk.scenePath = sourceChunk.scenePath;
            chunk.summary = sourceChunk.summary;
            chunk.localCenter = sourceChunk.localCenter;
            chunk.size = sourceChunk.size;
            chunk.loadDistance = sourceChunk.loadDistance;
            chunk.unloadDistance = sourceChunk.unloadDistance;
            chunk.usesOcclusionCulling = sourceChunk.usesOcclusionCulling;
            chunk.staticBatchEligible = sourceChunk.staticBatchEligible;
            chunk.gpuInstancingRecommended = sourceChunk.gpuInstancingRecommended;
            chunk.poiIds = sourceChunk.poiIds;
            SceneManager.MoveGameObjectToScene(root, scene);

            var terrainRoot = CreateGroup(root.transform, "Terrain");
            CreateGroup(root.transform, "Road");
            CreateGroup(root.transform, "Environment");
            CreateGroup(root.transform, "Buildings");
            CreateGroup(root.transform, "Props");
            CreateGroup(root.transform, "Vehicles");
            CreateGroup(root.transform, "Lighting");
            CreateGroup(root.transform, "FutureGameplayHooks");

            CreateTerrainTile(sourceChunk, scene, terrainRoot);
            CreateRoadPlaceholder(sourceChunk, scene, root.transform.Find("Road"));

            EditorSceneManager.SaveScene(scene, sourceChunk.scenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateTerrainTile(WorldChunkAuthoring sourceChunk, Scene scene, Transform terrainRoot)
        {
            var terrainAssetDirectory = "Assets/TerrainData";
            Directory.CreateDirectory(terrainAssetDirectory);

            var safeName = sourceChunk.chunkId.Replace("/", "_");
            var terrainAssetPath = $"{terrainAssetDirectory}/{safeName}_Terrain.asset";
            TerrainData terrainData;

            if (AssetDatabase.LoadAssetAtPath<TerrainData>(terrainAssetPath) is TerrainData existing)
            {
                terrainData = existing;
            }
            else
            {
                terrainData = new TerrainData
                {
                    heightmapResolution = 257
                };
                terrainData.size = new Vector3(sourceChunk.size.x, 80f, sourceChunk.size.z);
                AssetDatabase.CreateAsset(terrainData, terrainAssetPath);
            }

            var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "TerrainTile";
            terrainObject.isStatic = true;
            SceneManager.MoveGameObjectToScene(terrainObject, scene);
            terrainObject.transform.SetParent(terrainRoot, false);
            terrainObject.transform.localPosition = new Vector3(-sourceChunk.size.x * 0.5f, 0f, 0f);
        }

        private static void CreateRoadPlaceholder(WorldChunkAuthoring sourceChunk, Scene scene, Transform roadRoot)
        {
            if (roadRoot == null)
            {
                return;
            }

            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadPlaceholder";
            road.isStatic = true;
            SceneManager.MoveGameObjectToScene(road, scene);
            road.transform.SetParent(roadRoot, false);
            road.transform.localPosition = new Vector3(0f, 0.08f, sourceChunk.size.z * 0.5f);
            road.transform.localScale = new Vector3(10f, 0.16f, sourceChunk.size.z);
        }

        private static void UpdateBuildSettings(WorldChunkAuthoring[] chunks)
        {
            var scenePaths = new List<string>
            {
                "Assets/Scenes/PostApocRoadtrip.unity"
            };

            var chunkPaths = new List<string>();
            foreach (var chunk in chunks)
            {
                if (chunk != null && !string.IsNullOrWhiteSpace(chunk.scenePath))
                {
                    chunkPaths.Add(chunk.scenePath);
                }
            }

            chunkPaths.Sort();
            scenePaths.AddRange(chunkPaths);

            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var scenePath in scenePaths)
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
