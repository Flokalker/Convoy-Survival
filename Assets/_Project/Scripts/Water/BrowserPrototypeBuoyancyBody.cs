using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BrowserPrototypeBuoyancyBody : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Header("Buoyancy")]
    [SerializeField] private Vector3[] localSamplePoints =
    {
        new Vector3(-0.35f, 0f, -0.35f),
        new Vector3(0.35f, 0f, -0.35f),
        new Vector3(-0.35f, 0f, 0.35f),
        new Vector3(0.35f, 0f, 0.35f)
    };
    [SerializeField, Min(0.05f)] private float maxSubmersionDepth = 1.2f;
    [SerializeField, Min(0f)] private float buoyancyStrength = 20f;
    [SerializeField, Min(0f)] private float buoyancyDamping = 4f;

    [Header("Water Damping")]
    [SerializeField, Min(0f)] private float waterLinearDamping = 2f;
    [SerializeField, Min(0f)] private float waterAngularDamping = 1.75f;

    [Header("Surface Interaction")]
    [SerializeField, Min(0f)] private float surfaceDisturbanceStrength = 0.12f;
    [SerializeField, Min(0.05f)] private float disturbanceRadius = 0.65f;

    [Header("Optional Stability")]
    [SerializeField] private bool keepUpright = false;
    [SerializeField, Min(0f)] private float uprightTorque = 3f;

    private readonly List<BrowserPrototypeWaterVolume> activeVolumes = new List<BrowserPrototypeWaterVolume>(4);
    private float defaultLinearDamping;
    private float defaultAngularDamping;

    public Rigidbody Rigidbody => targetRigidbody;

    public void AutoConfigureSamplePointsFromCollider()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        Bounds bounds = collider.bounds;
        Vector3 centerLocal = transform.InverseTransformPoint(bounds.center);
        Vector3 extentsLocal = transform.InverseTransformVector(bounds.extents);
        float x = Mathf.Max(0.15f, Mathf.Abs(extentsLocal.x) * 0.8f);
        float y = centerLocal.y - Mathf.Max(0.05f, Mathf.Abs(extentsLocal.y) * 0.5f);
        float z = Mathf.Max(0.15f, Mathf.Abs(extentsLocal.z) * 0.8f);

        localSamplePoints = new[]
        {
            new Vector3(-x, y, -z),
            new Vector3(x, y, -z),
            new Vector3(-x, y, z),
            new Vector3(x, y, z)
        };
    }

    public void RegisterVolume(BrowserPrototypeWaterVolume volume)
    {
        if (volume == null || activeVolumes.Contains(volume))
        {
            return;
        }

        activeVolumes.Add(volume);
    }

    public void UnregisterVolume(BrowserPrototypeWaterVolume volume)
    {
        if (volume == null)
        {
            return;
        }

        activeVolumes.Remove(volume);
    }

    private void Reset()
    {
        targetRigidbody = GetComponent<Rigidbody>();
        AutoConfigureSamplePointsFromCollider();
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }

        defaultLinearDamping = targetRigidbody.linearDamping;
        defaultAngularDamping = targetRigidbody.angularDamping;
    }

    private void OnDisable()
    {
        RestoreDefaultDamping();
        activeVolumes.Clear();
    }

    private void FixedUpdate()
    {
        CleanupNullVolumes();

        if (activeVolumes.Count == 0 || localSamplePoints == null || localSamplePoints.Length == 0)
        {
            RestoreDefaultDamping();
            return;
        }

        int submergedSamples = 0;
        float summedSubmersion = 0f;

        for (int i = 0; i < localSamplePoints.Length; i++)
        {
            Vector3 worldPoint = transform.TransformPoint(localSamplePoints[i]);
            if (!TryGetWaterAtPoint(worldPoint, out BrowserPrototypeWaterVolume volume, out float surfaceHeight, out float surfaceVelocity))
            {
                continue;
            }

            float depth = surfaceHeight - worldPoint.y;
            if (depth <= 0f)
            {
                continue;
            }

            float submersion = Mathf.Clamp01(depth / maxSubmersionDepth);
            float pointVelocityY = targetRigidbody.GetPointVelocity(worldPoint).y;
            float lift = buoyancyStrength * submersion;
            float dampingForce = (surfaceVelocity - pointVelocityY) * buoyancyDamping * submersion;
            float totalForce = lift + dampingForce;

            targetRigidbody.AddForceAtPosition(Vector3.up * totalForce, worldPoint, ForceMode.Acceleration);
            submergedSamples++;
            summedSubmersion += submersion;

            if (surfaceDisturbanceStrength > 0f && volume != null)
            {
                float disturbance = -pointVelocityY * surfaceDisturbanceStrength * submersion;
                if (Mathf.Abs(disturbance) > 0.001f)
                {
                    volume.AddDisturbance(worldPoint, disturbance, disturbanceRadius);
                }
            }
        }

        if (submergedSamples == 0)
        {
            RestoreDefaultDamping();
            return;
        }

        float normalizedSubmersion = summedSubmersion / Mathf.Max(1, localSamplePoints.Length);
        targetRigidbody.linearDamping = Mathf.Lerp(defaultLinearDamping, waterLinearDamping, normalizedSubmersion);
        targetRigidbody.angularDamping = Mathf.Lerp(defaultAngularDamping, waterAngularDamping, normalizedSubmersion);

        if (keepUpright)
        {
            ApplyUprightTorque(normalizedSubmersion);
        }
    }

    private bool TryGetWaterAtPoint(
        Vector3 worldPoint,
        out BrowserPrototypeWaterVolume matchedVolume,
        out float surfaceHeight,
        out float surfaceVelocity)
    {
        matchedVolume = null;
        surfaceHeight = float.MinValue;
        surfaceVelocity = 0f;

        for (int i = 0; i < activeVolumes.Count; i++)
        {
            BrowserPrototypeWaterVolume volume = activeVolumes[i];
            if (volume == null)
            {
                continue;
            }

            if (!volume.TryGetWaterData(worldPoint, out float candidateHeight, out float candidateVelocity))
            {
                continue;
            }

            if (candidateHeight > surfaceHeight)
            {
                surfaceHeight = candidateHeight;
                surfaceVelocity = candidateVelocity;
                matchedVolume = volume;
            }
        }

        return matchedVolume != null;
    }

    private void ApplyUprightTorque(float submersion)
    {
        Vector3 correctionAxis = Vector3.Cross(transform.up, Vector3.up);
        if (correctionAxis.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        targetRigidbody.AddTorque(correctionAxis * (uprightTorque * submersion), ForceMode.Acceleration);
    }

    private void CleanupNullVolumes()
    {
        for (int i = activeVolumes.Count - 1; i >= 0; i--)
        {
            if (activeVolumes[i] == null)
            {
                activeVolumes.RemoveAt(i);
            }
        }
    }

    private void RestoreDefaultDamping()
    {
        targetRigidbody.linearDamping = defaultLinearDamping;
        targetRigidbody.angularDamping = defaultAngularDamping;
    }

    private void OnDrawGizmosSelected()
    {
        if (localSamplePoints == null)
        {
            return;
        }

        Gizmos.color = new Color(0f, 0.65f, 1f, 0.85f);
        for (int i = 0; i < localSamplePoints.Length; i++)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(localSamplePoints[i]), 0.05f);
        }
    }
}
