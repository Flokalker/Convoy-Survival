using UnityEngine;

namespace PostApocRoadtrip.World
{
    [DisallowMultipleComponent]
    public class WorldRegionAuthoring : MonoBehaviour
    {
        public string regionId = "region_01";
        public string displayName = "Region";
        [TextArea(2, 5)] public string summary = string.Empty;
        public float startRoadZ;
        public float endRoadZ = 120f;
        public Color debugColor = new Color(0.32f, 0.75f, 0.95f, 0.22f);

        public float Length => Mathf.Max(0f, endRoadZ - startRoadZ);

        private void OnDrawGizmos()
        {
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = debugColor;
            Gizmos.DrawWireCube(new Vector3(0f, 6f, Length * 0.5f), new Vector3(84f, 12f, Length));
            Gizmos.matrix = previous;
        }
    }
}
