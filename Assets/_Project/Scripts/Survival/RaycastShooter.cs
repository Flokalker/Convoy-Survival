using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InputHandler))]
[RequireComponent(typeof(InventorySystem))]
public class RaycastShooter : MonoBehaviour
{
    [SerializeField] private Camera shooterCamera;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private string zombieTag = "Zombie";
    [SerializeField] private int currentMagazine = 30;

    private float nextShotTime;

    private void Reset()
    {
        inputHandler = GetComponent<InputHandler>();
        inventory = GetComponent<InventorySystem>();
    }

    private void Awake()
    {
        if (inputHandler == null)
        {
            inputHandler = GetComponent<InputHandler>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<InventorySystem>();
        }

        if (shooterCamera == null)
        {
            shooterCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (inventory.EquippedWeapon != null)
        {
            currentMagazine = Mathf.Clamp(currentMagazine, 0, inventory.EquippedWeapon.magazineSize);
        }
    }

    private void Update()
    {
        if (inputHandler.FirePressed)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        InventorySystem.WeaponData weapon = inventory.EquippedWeapon;
        if (weapon == null || shooterCamera == null || Time.time < nextShotTime)
        {
            return;
        }

        if (currentMagazine <= 0)
        {
            TryReload(weapon);
            return;
        }

        nextShotTime = Time.time + (1f / Mathf.Max(0.1f, weapon.fireRate));
        currentMagazine--;

        Ray ray = new Ray(shooterCamera.transform.position, shooterCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, weapon.range, hitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (!hit.collider.CompareTag(zombieTag))
        {
            return;
        }

        Health targetHealth = hit.collider.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(weapon.damage);
        }
    }

    private void TryReload(InventorySystem.WeaponData weapon)
    {
        int needed = weapon.magazineSize - currentMagazine;
        if (needed <= 0 || weapon.reserveAmmo <= 0)
        {
            return;
        }

        int toLoad = Mathf.Min(needed, weapon.reserveAmmo);
        weapon.reserveAmmo -= toLoad;
        currentMagazine += toLoad;
    }
}
