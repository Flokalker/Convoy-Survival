using UnityEngine;

namespace PostApocRoadtrip.World
{
    [DisallowMultipleComponent]
    public class WorldChunkAuthoring : MonoBehaviour
    {
        public string regionId = "region_01";
        public string chunkId = "chunk_01";
        public string scenePath = "Assets/Scenes/Regions/Region_01/Chunk_01.unity";
        [TextArea(2, 5)] public string summary = string.Empty;
        public Vector3 localCenter = new Vector3(0f, 4f, 60f);
        public Vector3 size = new Vector3(84f, 16f, 120f);
        public float loadDistance = 150f;
        public float unloadDistance = 210f;
        public bool usesOcclusionCulling = true;
        public bool staticBatchEligible = true;
        public bool gpuInstancingRecommended = true;
        public string[] poiIds = new string[0];

        public Bounds WorldBounds => new Bounds(transform.TransformPoint(localCenter), size);

        private void OnDrawGizmos()
        {
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(localCenter, size);
            Gizmos.matrix = previous;
        }
    }
}
