using UnityEngine;

namespace PostApocRoadtrip.World
{
    public enum MarkerKind
    {
        SpawnPoint,
        FuelStop,
        ShopStop,
        LootArea,
        ZombieSpawnArea,
        EventTrigger
    }

    [ExecuteAlways]
    public class AreaMarker : MonoBehaviour
    {
        public MarkerKind kind = MarkerKind.EventTrigger;
        public string markerId = "marker";
        [TextArea(2, 4)] public string description = string.Empty;
        public Vector3 triggerSize = new Vector3(10f, 4f, 12f);
        public Color gizmoColor = new Color(0.3f, 0.8f, 1f, 1f);

        private void Reset()
        {
            SyncCollider();
        }

        private void OnValidate()
        {
            SyncCollider();
        }

        private void SyncCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            box.center = new Vector3(0f, triggerSize.y * 0.5f, 0f);
            box.size = triggerSize;
        }

        private void OnDrawGizmos()
        {
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(new Vector3(0f, triggerSize.y * 0.5f, 0f), triggerSize);
            Gizmos.matrix = previousMatrix;
        }
    }
}
