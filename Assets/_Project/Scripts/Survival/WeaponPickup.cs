using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private GameObject weaponVisualPrefab;
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField, Min(1f)] private float weaponDamage = 24f;
    [SerializeField, Min(0.2f)] private float weaponFireRate = 6f;
    [SerializeField, Min(5f)] private float weaponRange = 120f;
    [SerializeField, Min(0f)] private float rotateSpeed = 45f;

    private void Awake()
    {
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void Update()
    {
        if (rotateSpeed > 0f)
        {
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        WeaponController weaponController = other.GetComponentInParent<WeaponController>();
        if (weaponController == null)
        {
            return;
        }

        weaponController.EquipWeapon(weaponVisualPrefab, weaponName, weaponDamage, weaponFireRate, weaponRange);
        Destroy(gameObject);
    }

    public void Configure(GameObject visualPrefab, string displayName, float damage, float fireRate, float range)
    {
        weaponVisualPrefab = visualPrefab;
        weaponName = displayName;
        weaponDamage = Mathf.Max(1f, damage);
        weaponFireRate = Mathf.Max(0.2f, fireRate);
        weaponRange = Mathf.Max(5f, range);
    }
}
