using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponDropSpawner : MonoBehaviour
{
    [System.Serializable]
    public class WeaponDropDefinition
    {
        public GameObject visualPrefab;
        public string weaponName = "Weapon";
        [Min(1f)] public float damage = 24f;
        [Min(0.2f)] public float fireRate = 6f;
        [Min(5f)] public float range = 120f;
    }

    [SerializeField] private WeaponDropDefinition[] weapons;
    [SerializeField, Min(1)] private int dropCount = 8;
    [SerializeField, Min(3f)] private float spawnRadius = 40f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Vector3 pickupScale = Vector3.one;
    [SerializeField, Min(1)] private int maxActivePickups = 20;
    [SerializeField, Min(0.5f)] private float spawnCheckInterval = 1.25f;
    [SerializeField, Range(0f, 1f)] private float spawnChancePerCheck = 0.9f;
    [SerializeField, Min(0.2f)] private float pickupVisualTargetSize = 1.2f;

    private readonly List<WeaponPickup> activePickups = new List<WeaponPickup>();
    private Transform spawnCenter;
    private float nextSpawnCheckTime;

    public void Configure(WeaponDropDefinition[] definitions, int count, float radius, LayerMask groundLayerMask)
    {
        weapons = definitions;
        dropCount = Mathf.Max(1, count);
        spawnRadius = Mathf.Max(3f, radius);
        groundMask = groundLayerMask;
    }

    public void SetSpawnCenter(Transform center)
    {
        spawnCenter = center;
    }

    private void Update()
    {
        CleanupMissingPickups();

        if (weapons == null || weapons.Length == 0 || spawnCenter == null || Time.time < nextSpawnCheckTime)
        {
            return;
        }

        nextSpawnCheckTime = Time.time + spawnCheckInterval;
        if (activePickups.Count >= maxActivePickups || Random.value > spawnChancePerCheck)
        {
            return;
        }

        SpawnSingleDrop(spawnCenter.position);
    }

    public void SpawnDrops(Vector3 center)
    {
        if (weapons == null || weapons.Length == 0)
        {
            return;
        }

        for (int i = 0; i < dropCount; i++)
        {
            WeaponDropDefinition definition = weapons[Random.Range(0, weapons.Length)];
            if (definition == null || definition.visualPrefab == null)
            {
                continue;
            }

            Vector3 point = center + new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                10f,
                Random.Range(-spawnRadius, spawnRadius));

            if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point + Vector3.up * 1.1f;
            }
            else
            {
                point.y = center.y + 1.2f;
            }

            CreatePickup(point, definition);
        }
    }

    private void SpawnSingleDrop(Vector3 center)
    {
        WeaponDropDefinition definition = weapons[Random.Range(0, weapons.Length)];
        if (definition == null || definition.visualPrefab == null)
        {
            return;
        }

        Vector3 point = center + new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            10f,
            Random.Range(-spawnRadius, spawnRadius));

        if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point + Vector3.up * 1.1f;
        }
        else
        {
            point.y = center.y + 1.2f;
        }

        CreatePickup(point, definition);
    }

    private void CreatePickup(Vector3 position, WeaponDropDefinition definition)
    {
        GameObject pickupRoot = new GameObject(string.Concat("Pickup_", definition.weaponName.Replace(" ", string.Empty)));
        pickupRoot.transform.SetParent(transform);
        pickupRoot.transform.position = position;

        SphereCollider trigger = pickupRoot.AddComponent<SphereCollider>();
        trigger.radius = 1.15f;
        trigger.isTrigger = true;

        Rigidbody triggerBody = pickupRoot.AddComponent<Rigidbody>();
        triggerBody.isKinematic = true;
        triggerBody.useGravity = false;

        WeaponPickup pickup = pickupRoot.AddComponent<WeaponPickup>();
        pickup.Configure(definition.visualPrefab, definition.weaponName, definition.damage, definition.fireRate, definition.range);
        activePickups.Add(pickup);

        GameObject visual = Instantiate(definition.visualPrefab, pickupRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = pickupScale;
        NormalizeVisualScale(visual, pickupVisualTargetSize);
        CreateDebugMarker(pickupRoot.transform);

        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Destroy(rigidbodies[i]);
        }

        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }
    }

    private void CleanupMissingPickups()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            if (activePickups[i] == null)
            {
                activePickups.RemoveAt(i);
            }
        }
    }

    private static void NormalizeVisualScale(GameObject root, float targetLongestSide)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (longest <= 0.001f)
        {
            return;
        }

        float scale = Mathf.Max(0.01f, targetLongestSide / longest);
        root.transform.localScale *= scale;
    }

    private static void CreateDebugMarker(Transform parent)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "PickupMarker";
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        marker.transform.localScale = Vector3.one * 0.2f;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }
    }
}
