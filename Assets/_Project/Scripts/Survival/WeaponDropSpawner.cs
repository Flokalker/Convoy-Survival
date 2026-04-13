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

    public void Configure(WeaponDropDefinition[] definitions, int count, float radius, LayerMask groundLayerMask)
    {
        weapons = definitions;
        dropCount = Mathf.Max(1, count);
        spawnRadius = Mathf.Max(3f, radius);
        groundMask = groundLayerMask;
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

        GameObject visual = Instantiate(definition.visualPrefab, pickupRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = pickupScale;

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
}
