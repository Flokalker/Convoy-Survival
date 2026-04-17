using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BrowserPrototypeWaterSurface : MonoBehaviour
{
    [Header("Surface Size")]
    [SerializeField, Min(1f)] private float sizeX = 14f;
    [SerializeField, Min(1f)] private float sizeZ = 10f;
    [SerializeField, Range(4, 128)] private int resolutionX = 28;
    [SerializeField, Range(4, 128)] private int resolutionZ = 20;

    [Header("Spring Simulation")]
    [SerializeField, Min(0f)] private float springStrength = 28f;
    [SerializeField, Min(0f)] private float damping = 5.5f;
    [SerializeField, Min(0f)] private float spread = 8f;
    [SerializeField, Range(1, 8)] private int spreadPasses = 2;
    [SerializeField, Min(0f)] private float maxDisplacement = 1.5f;
    [SerializeField, Min(0f)] private float maxVelocity = 8f;

    private MeshFilter meshFilter;
    private Mesh surfaceMesh;
    private Vector3[] baseVertices;
    private Vector3[] workingVertices;
    private float[] displacements;
    private float[] velocities;
    private bool isInitialized;

    public float SizeX => sizeX;
    public float SizeZ => sizeZ;

    public void Configure(float width, float length, int nodesX, int nodesZ)
    {
        sizeX = Mathf.Max(1f, width);
        sizeZ = Mathf.Max(1f, length);
        resolutionX = Mathf.Clamp(nodesX, 4, 128);
        resolutionZ = Mathf.Clamp(nodesZ, 4, 128);
        InitializeMeshData();
    }

    private void Awake()
    {
        InitializeMeshData();
    }

    private void Reset()
    {
        InitializeMeshData();
    }

    private void OnValidate()
    {
        resolutionX = Mathf.Clamp(resolutionX, 4, 128);
        resolutionZ = Mathf.Clamp(resolutionZ, 4, 128);
        sizeX = Mathf.Max(1f, sizeX);
        sizeZ = Mathf.Max(1f, sizeZ);

        if (!Application.isPlaying)
        {
            InitializeMeshData();
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized || displacements == null || velocities == null)
        {
            return;
        }

        float deltaTime = Time.fixedDeltaTime;

        for (int i = 0; i < displacements.Length; i++)
        {
            float acceleration = (-springStrength * displacements[i]) - (damping * velocities[i]);
            velocities[i] += acceleration * deltaTime;
        }

        for (int pass = 0; pass < spreadPasses; pass++)
        {
            float spreadScale = spread * deltaTime / spreadPasses;

            for (int z = 0; z <= resolutionZ; z++)
            {
                for (int x = 0; x <= resolutionX; x++)
                {
                    int index = ToIndex(x, z);
                    float current = displacements[index];
                    float neighbourDelta = 0f;

                    if (x > 0)
                    {
                        neighbourDelta += displacements[ToIndex(x - 1, z)] - current;
                    }

                    if (x < resolutionX)
                    {
                        neighbourDelta += displacements[ToIndex(x + 1, z)] - current;
                    }

                    if (z > 0)
                    {
                        neighbourDelta += displacements[ToIndex(x, z - 1)] - current;
                    }

                    if (z < resolutionZ)
                    {
                        neighbourDelta += displacements[ToIndex(x, z + 1)] - current;
                    }

                    velocities[index] += neighbourDelta * spreadScale;
                }
            }
        }

        for (int i = 0; i < displacements.Length; i++)
        {
            velocities[i] = Mathf.Clamp(velocities[i], -maxVelocity, maxVelocity);
            displacements[i] += velocities[i] * deltaTime;
            displacements[i] = Mathf.Clamp(displacements[i], -maxDisplacement, maxDisplacement);
        }

        UpdateMesh();
    }

    public void AddDisturbance(Vector3 worldPosition, float force, float radius)
    {
        if (!isInitialized)
        {
            return;
        }

        radius = Mathf.Max(0.05f, radius);
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;

        if (localPosition.x < -halfX - radius || localPosition.x > halfX + radius ||
            localPosition.z < -halfZ - radius || localPosition.z > halfZ + radius)
        {
            return;
        }

        int minX = Mathf.Max(0, Mathf.FloorToInt(((localPosition.x - radius) + halfX) / sizeX * resolutionX));
        int maxX = Mathf.Min(resolutionX, Mathf.CeilToInt(((localPosition.x + radius) + halfX) / sizeX * resolutionX));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(((localPosition.z - radius) + halfZ) / sizeZ * resolutionZ));
        int maxZ = Mathf.Min(resolutionZ, Mathf.CeilToInt(((localPosition.z + radius) + halfZ) / sizeZ * resolutionZ));

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int index = ToIndex(x, z);
                Vector3 nodePosition = baseVertices[index];
                float distance = Vector2.Distance(
                    new Vector2(nodePosition.x, nodePosition.z),
                    new Vector2(localPosition.x, localPosition.z));

                if (distance > radius)
                {
                    continue;
                }

                float falloff = 1f - (distance / radius);
                velocities[index] = Mathf.Clamp(velocities[index] + force * falloff, -maxVelocity, maxVelocity);
            }
        }
    }

    public bool TryGetSurfaceData(Vector3 worldPosition, out float surfaceHeight, out float surfaceVelocity)
    {
        surfaceHeight = 0f;
        surfaceVelocity = 0f;

        if (!isInitialized)
        {
            return false;
        }

        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;

        if (localPosition.x < -halfX || localPosition.x > halfX || localPosition.z < -halfZ || localPosition.z > halfZ)
        {
            return false;
        }

        float normalizedX = Mathf.InverseLerp(-halfX, halfX, localPosition.x);
        float normalizedZ = Mathf.InverseLerp(-halfZ, halfZ, localPosition.z);
        float gridX = normalizedX * resolutionX;
        float gridZ = normalizedZ * resolutionZ;

        int x0 = Mathf.Clamp(Mathf.FloorToInt(gridX), 0, resolutionX);
        int x1 = Mathf.Clamp(x0 + 1, 0, resolutionX);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(gridZ), 0, resolutionZ);
        int z1 = Mathf.Clamp(z0 + 1, 0, resolutionZ);
        float tx = Mathf.Clamp01(gridX - x0);
        float tz = Mathf.Clamp01(gridZ - z0);

        float d00 = displacements[ToIndex(x0, z0)];
        float d10 = displacements[ToIndex(x1, z0)];
        float d01 = displacements[ToIndex(x0, z1)];
        float d11 = displacements[ToIndex(x1, z1)];
        float v00 = velocities[ToIndex(x0, z0)];
        float v10 = velocities[ToIndex(x1, z0)];
        float v01 = velocities[ToIndex(x0, z1)];
        float v11 = velocities[ToIndex(x1, z1)];

        float localHeight = Bilinear(d00, d10, d01, d11, tx, tz);
        float localVelocity = Bilinear(v00, v10, v01, v11, tx, tz);

        Vector3 worldSurfacePoint = transform.TransformPoint(new Vector3(localPosition.x, localHeight, localPosition.z));
        surfaceHeight = worldSurfacePoint.y;
        surfaceVelocity = transform.TransformVector(Vector3.up * localVelocity).y;
        return true;
    }

    private void InitializeMeshData()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        int nodeCount = (resolutionX + 1) * (resolutionZ + 1);
        if (baseVertices == null || baseVertices.Length != nodeCount)
        {
            baseVertices = new Vector3[nodeCount];
            workingVertices = new Vector3[nodeCount];
            displacements = new float[nodeCount];
            velocities = new float[nodeCount];
        }

        if (surfaceMesh == null)
        {
            surfaceMesh = new Mesh
            {
                name = "BrowserPrototypeWaterSurface"
            };
            surfaceMesh.MarkDynamic();
            meshFilter.sharedMesh = surfaceMesh;
        }

        BuildVertices();
        BuildTriangles();
        UpdateMesh();
        isInitialized = true;
    }

    private void BuildVertices()
    {
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;

        for (int z = 0; z <= resolutionZ; z++)
        {
            float normalizedZ = z / (float)resolutionZ;
            float positionZ = Mathf.Lerp(-halfZ, halfZ, normalizedZ);

            for (int x = 0; x <= resolutionX; x++)
            {
                float normalizedX = x / (float)resolutionX;
                float positionX = Mathf.Lerp(-halfX, halfX, normalizedX);
                int index = ToIndex(x, z);
                baseVertices[index] = new Vector3(positionX, 0f, positionZ);
                workingVertices[index] = baseVertices[index];
            }
        }
    }

    private void BuildTriangles()
    {
        int quadCount = resolutionX * resolutionZ;
        int[] triangles = new int[quadCount * 6];
        int triangleIndex = 0;

        for (int z = 0; z < resolutionZ; z++)
        {
            for (int x = 0; x < resolutionX; x++)
            {
                int i00 = ToIndex(x, z);
                int i10 = ToIndex(x + 1, z);
                int i01 = ToIndex(x, z + 1);
                int i11 = ToIndex(x + 1, z + 1);

                triangles[triangleIndex++] = i00;
                triangles[triangleIndex++] = i01;
                triangles[triangleIndex++] = i10;
                triangles[triangleIndex++] = i10;
                triangles[triangleIndex++] = i01;
                triangles[triangleIndex++] = i11;
            }
        }

        Vector2[] uvs = new Vector2[baseVertices.Length];
        for (int z = 0; z <= resolutionZ; z++)
        {
            for (int x = 0; x <= resolutionX; x++)
            {
                uvs[ToIndex(x, z)] = new Vector2(x / (float)resolutionX, z / (float)resolutionZ);
            }
        }

        surfaceMesh.Clear();
        surfaceMesh.vertices = workingVertices;
        surfaceMesh.triangles = triangles;
        surfaceMesh.uv = uvs;
    }

    private void UpdateMesh()
    {
        for (int i = 0; i < workingVertices.Length; i++)
        {
            workingVertices[i] = baseVertices[i];
            workingVertices[i].y += displacements[i];
        }

        surfaceMesh.vertices = workingVertices;
        surfaceMesh.RecalculateNormals();
        surfaceMesh.RecalculateBounds();
    }

    private int ToIndex(int x, int z)
    {
        return (z * (resolutionX + 1)) + x;
    }

    private static float Bilinear(float a, float b, float c, float d, float tx, float tz)
    {
        float top = Mathf.Lerp(a, b, tx);
        float bottom = Mathf.Lerp(c, d, tx);
        return Mathf.Lerp(top, bottom, tz);
    }
}
