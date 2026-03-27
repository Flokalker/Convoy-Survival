using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class BrowserPrototypeWaterVolume : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrowserPrototypeWaterSurface waterSurface;
    [SerializeField] private BoxCollider triggerCollider;

    [Header("Splash")]
    [SerializeField, Min(0f)] private float splashVelocityThreshold = 1f;
    [SerializeField, Min(0f)] private float splashForceScale = 0.3f;
    [SerializeField, Min(0.05f)] private float splashRadius = 0.8f;

    public BrowserPrototypeWaterSurface Surface => waterSurface;

    public void Configure(BrowserPrototypeWaterSurface surface, BoxCollider volumeCollider)
    {
        waterSurface = surface;
        triggerCollider = volumeCollider;

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Reset()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        waterSurface = GetComponentInChildren<BrowserPrototypeWaterSurface>();
    }

    private void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider>();
        }

        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleExit(other);
    }

    public bool TryGetWaterData(Vector3 worldPosition, out float surfaceHeight, out float surfaceVelocity)
    {
        surfaceHeight = 0f;
        surfaceVelocity = 0f;

        if (waterSurface == null || triggerCollider == null)
        {
            return false;
        }

        Vector3 closest = triggerCollider.ClosestPoint(worldPosition);
        bool isInsideVolume = (closest - worldPosition).sqrMagnitude < 0.0001f;
        if (!isInsideVolume)
        {
            return false;
        }

        return waterSurface.TryGetSurfaceData(worldPosition, out surfaceHeight, out surfaceVelocity);
    }

    public void AddDisturbance(Vector3 worldPosition, float force, float radius)
    {
        if (waterSurface == null)
        {
            return;
        }

        waterSurface.AddDisturbance(worldPosition, force, radius);
    }

    private void HandleEnter(Collider other)
    {
        if (!TryGetBuoyancyBody(other, out BrowserPrototypeBuoyancyBody body))
        {
            return;
        }

        body.RegisterVolume(this);
        TryCreateEntrySplash(body.Rigidbody);
    }

    private void HandleExit(Collider other)
    {
        if (!TryGetBuoyancyBody(other, out BrowserPrototypeBuoyancyBody body))
        {
            return;
        }

        body.UnregisterVolume(this);
    }

    private bool TryGetBuoyancyBody(Collider other, out BrowserPrototypeBuoyancyBody body)
    {
        body = null;
        Rigidbody attachedRigidbody = other.attachedRigidbody;
        if (attachedRigidbody == null)
        {
            return false;
        }

        body = attachedRigidbody.GetComponent<BrowserPrototypeBuoyancyBody>();
        return body != null;
    }

    private void TryCreateEntrySplash(Rigidbody body)
    {
        if (waterSurface == null || body == null)
        {
            return;
        }

        float verticalSpeed = body.linearVelocity.y;
        if (Mathf.Abs(verticalSpeed) < splashVelocityThreshold)
        {
            return;
        }

        Vector3 splashPosition = body.worldCenterOfMass;
        float force = Mathf.Clamp(-verticalSpeed * splashForceScale, -maxForce, maxForce);
        waterSurface.AddDisturbance(splashPosition, force, splashRadius);
    }

    private const float maxForce = 4f;
}
