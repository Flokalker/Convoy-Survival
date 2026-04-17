using UnityEngine;

[DisallowMultipleComponent]
public class SurvivalWeaponDropManager : MonoBehaviour
{
    [SerializeField] private Transform centerTarget;
    [SerializeField] private SurvivalWeaponDefinition[] weaponPool;
    [SerializeField, Min(1)] private int initialDrops = 5;
    [SerializeField, Min(2f)] private float spawnRadius = 35f;
    [SerializeField, Min(0.5f)] private float checkInterval = 3f;
    [SerializeField, Range(0f, 1f)] private float chancePerCheck = 0.45f;
    [SerializeField, Min(1)] private int maxActiveDrops = 10;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private bool verboseLogs = true;

    private float nextCheckTime;

    public void ConfigureRuntime(
        Transform center,
        SurvivalWeaponDefinition[] pool,
        int startDrops,
        float radius,
        float interval,
        float chance,
        int maxDrops)
    {
        centerTarget = center;
        weaponPool = pool;
        initialDrops = Mathf.Max(0, startDrops);
        spawnRadius = Mathf.Max(2f, radius);
        checkInterval = Mathf.Max(0.5f, interval);
        chancePerCheck = Mathf.Clamp01(chance);
        maxActiveDrops = Mathf.Max(1, maxDrops);
    }

    private void Start()
    {
        if (centerTarget == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                centerTarget = player.transform;
            }
        }

        for (int i = 0; i < initialDrops; i++)
        {
            SpawnOne();
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;
        if (CountActiveDrops() >= maxActiveDrops || Random.value > chancePerCheck)
        {
            return;
        }

        SpawnOne();
    }

    private void SpawnOne()
    {
        if (centerTarget == null || weaponPool == null || weaponPool.Length == 0)
        {
            if (verboseLogs)
            {
                Debug.LogWarning("SurvivalWeaponDropManager: Spawn skipped (missing centerTarget or weaponPool).", this);
            }
            return;
        }

        SurvivalWeaponDefinition selected = weaponPool[Random.Range(0, weaponPool.Length)];
        if (selected == null)
        {
            if (verboseLogs)
            {
                Debug.LogWarning("SurvivalWeaponDropManager: Spawn skipped (selected weapon definition was null).", this);
            }
            return;
        }

        Vector3 point = centerTarget.position + new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            12f,
            Random.Range(-spawnRadius, spawnRadius));

        if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, 80f, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point + Vector3.up * 0.8f;
        }
        else
        {
            point = centerTarget.position + Vector3.up * 1.1f;
        }

        GameObject pickupObject = new GameObject($"Pickup_{selected.DisplayName}");
        pickupObject.transform.position = point;

        SphereCollider trigger = pickupObject.AddComponent<SphereCollider>();
        trigger.radius = 1.1f;
        trigger.isTrigger = true;

        Rigidbody rb = pickupObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        SurvivalWeaponPickup pickup = pickupObject.AddComponent<SurvivalWeaponPickup>();
        pickup.Configure(selected);

        if (selected.WeaponViewPrefab != null)
        {
            SurvivalWeaponView view = Instantiate(selected.WeaponViewPrefab, pickupObject.transform);
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            view.transform.localScale = Vector3.one;
            DisableVisualColliders(view.gameObject);
        }

        CreatePickupMarker(pickupObject.transform);

        if (verboseLogs)
        {
            Debug.Log($"SurvivalWeaponDropManager: Spawned pickup '{selected.DisplayName}' at {pickupObject.transform.position}.", this);
        }
    }

    public void ForceSpawnAt(Vector3 worldPosition)
    {
        if (weaponPool == null || weaponPool.Length == 0)
        {
            if (verboseLogs)
            {
                Debug.LogWarning("SurvivalWeaponDropManager: ForceSpawnAt failed (weaponPool empty).", this);
            }

            return;
        }

        SurvivalWeaponDefinition selected = weaponPool[Random.Range(0, weaponPool.Length)];
        if (selected == null)
        {
            return;
        }

        GameObject pickupObject = new GameObject($"Pickup_{selected.DisplayName}");
        pickupObject.transform.position = worldPosition;

        SphereCollider trigger = pickupObject.AddComponent<SphereCollider>();
        trigger.radius = 1.1f;
        trigger.isTrigger = true;

        Rigidbody rb = pickupObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        SurvivalWeaponPickup pickup = pickupObject.AddComponent<SurvivalWeaponPickup>();
        pickup.Configure(selected);

        if (selected.WeaponViewPrefab != null)
        {
            SurvivalWeaponView view = Instantiate(selected.WeaponViewPrefab, pickupObject.transform);
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            view.transform.localScale = Vector3.one;
            DisableVisualColliders(view.gameObject);
        }

        CreatePickupMarker(pickupObject.transform);

        if (verboseLogs)
        {
            Debug.Log($"SurvivalWeaponDropManager: Force spawned pickup '{selected.DisplayName}' at {pickupObject.transform.position}.", this);
        }
    }

    private static void DisableVisualColliders(GameObject visualRoot)
    {
        Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }
    }

    private int CountActiveDrops()
    {
        return FindObjectsByType<SurvivalWeaponPickup>(FindObjectsSortMode.None).Length;
    }

    private static void CreatePickupMarker(Transform pickupRoot)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "PickupMarker";
        marker.transform.SetParent(pickupRoot, false);
        marker.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        marker.transform.localScale = Vector3.one * 0.18f;
        Renderer r = marker.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = new Color(1f, 0.85f, 0.1f, 1f);
        }

        Collider c = marker.GetComponent<Collider>();
        if (c != null)
        {
            Destroy(c);
        }
    }
}
