using UnityEngine;

[DisallowMultipleComponent]
public class RoadChunk : MonoBehaviour
{
    [SerializeField, Min(5f)] private float chunkLength = 60f;
    [SerializeField] private ScavengePoint[] scavengePoints;

    public float ChunkLength => chunkLength;

    private void Awake()
    {
        if (scavengePoints == null || scavengePoints.Length == 0)
        {
            scavengePoints = GetComponentsInChildren<ScavengePoint>(true);
        }
    }

    private void OnEnable()
    {
        if (scavengePoints == null || scavengePoints.Length == 0)
        {
            Debug.LogWarning($"Road chunk '{name}' has no ScavengePoint. Add at least one for exploration rewards.", this);
        }
    }
}
