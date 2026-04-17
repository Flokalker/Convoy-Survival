using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PostApocRoadtrip.World
{
    [DisallowMultipleComponent]
    public class WorldStreamingManager : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private bool scanChunksOnStart = true;
        [SerializeField] private bool loadClosestChunksOnStart = true;
        [SerializeField] private float scanInterval = 0.4f;
        [SerializeField] private int maxConcurrentOperations = 2;

        private readonly List<WorldChunkAuthoring> chunks = new List<WorldChunkAuthoring>();
        private readonly Dictionary<string, AsyncOperation> inFlightLoads = new Dictionary<string, AsyncOperation>();
        private readonly HashSet<string> loadedScenes = new HashSet<string>();
        private Coroutine streamingLoop;

        private void Awake()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                enabled = false;
                return;
            }

            if (scanChunksOnStart)
            {
                RefreshChunkCache();
            }

            if (player == null)
            {
                ResolvePlayerReference();
            }
        }

        private void OnEnable()
        {
            streamingLoop = StartCoroutine(StreamingLoop());
        }

        private void OnDisable()
        {
            if (streamingLoop != null)
            {
                StopCoroutine(streamingLoop);
                streamingLoop = null;
            }
        }

        [ContextMenu("Refresh Chunk Cache")]
        public void RefreshChunkCache()
        {
            chunks.Clear();
            chunks.AddRange(FindObjectsOfType<WorldChunkAuthoring>(true));
        }

        private IEnumerator StreamingLoop()
        {
            if (loadClosestChunksOnStart)
            {
                EvaluateStreaming();
            }

            var wait = new WaitForSeconds(scanInterval);
            while (enabled)
            {
                EvaluateStreaming();
                yield return wait;
            }
        }

        private void EvaluateStreaming()
        {
            if (player == null || chunks.Count == 0)
            {
                ResolvePlayerReference();
                return;
            }

            var playerPosition = player.position;
            var activeOperations = inFlightLoads.Count;

            foreach (var chunk in chunks)
            {
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.scenePath))
                {
                    continue;
                }

                var distance = DistanceToChunk(chunk, playerPosition);
                var sceneName = chunk.scenePath;
                var isLoaded = loadedScenes.Contains(sceneName) || SceneByPathLoaded(sceneName);

                if (distance <= chunk.loadDistance && !isLoaded && activeOperations < maxConcurrentOperations)
                {
                    StartCoroutine(LoadChunk(sceneName));
                    activeOperations++;
                    continue;
                }

                if (distance >= chunk.unloadDistance && isLoaded && !inFlightLoads.ContainsKey(sceneName))
                {
                    StartCoroutine(UnloadChunk(sceneName));
                }
            }
        }

        private float DistanceToChunk(WorldChunkAuthoring chunk, Vector3 playerPosition)
        {
            var bounds = chunk.WorldBounds;
            var closestPoint = bounds.ClosestPoint(playerPosition);
            return Vector3.Distance(playerPosition, closestPoint);
        }

        private IEnumerator LoadChunk(string scenePath)
        {
            if (inFlightLoads.ContainsKey(scenePath) || SceneByPathLoaded(scenePath))
            {
                yield break;
            }

            var asyncLoad = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            if (asyncLoad == null)
            {
                yield break;
            }

            inFlightLoads[scenePath] = asyncLoad;
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            loadedScenes.Add(scenePath);
            inFlightLoads.Remove(scenePath);
        }

        private IEnumerator UnloadChunk(string scenePath)
        {
            if (!SceneByPathLoaded(scenePath))
            {
                loadedScenes.Remove(scenePath);
                yield break;
            }

            var asyncUnload = SceneManager.UnloadSceneAsync(scenePath);
            if (asyncUnload == null)
            {
                yield break;
            }

            inFlightLoads[scenePath] = asyncUnload;
            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            loadedScenes.Remove(scenePath);
            inFlightLoads.Remove(scenePath);
        }

        private bool SceneByPathLoaded(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        private void ResolvePlayerReference()
        {
            if (Camera.main != null)
            {
                player = Camera.main.transform;
                return;
            }

            var previewCar = FindObjectOfType<PreviewCarController>();
            if (previewCar != null)
            {
                player = previewCar.transform;
            }
        }
    }
}
