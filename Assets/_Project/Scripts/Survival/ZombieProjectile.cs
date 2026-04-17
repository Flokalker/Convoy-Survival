using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class ZombieProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float lifetime = 6f;

    private Vector3 moveDirection = Vector3.forward;
    private float speed = 10f;
    private float damage = 10f;
    private GameObject owner;

    private void Awake()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        collider.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    public void Launch(Vector3 direction, float projectileSpeed, float projectileDamage, GameObject ownerObject)
    {
        moveDirection = direction.normalized;
        speed = Mathf.Max(0.1f, projectileSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        owner = ownerObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, owner);
            Destroy(gameObject);
            return;
        }

        VehicleController vehicle = other.GetComponentInParent<VehicleController>();
        if (vehicle != null && vehicle.Stats != null)
        {
            vehicle.Stats.ApplyDamage(damage);
        }

        Destroy(gameObject);
    }
}
