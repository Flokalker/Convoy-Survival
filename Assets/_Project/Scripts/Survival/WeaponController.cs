using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private Camera fireCamera;
    [SerializeField] private Transform muzzleTransform;
    [SerializeField] private Transform recoilTarget;
    [SerializeField] private Transform weaponHoldPoint;

    [Header("Weapon Stats")]
    [SerializeField, Min(1f)] private float damage = 24f;
    [SerializeField, Min(1f)] private float range = 150f;
    [SerializeField, Min(0.1f)] private float fireRate = 7f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject impactSparkPrefab;
    [SerializeField, Min(0f)] private float recoilKick = 0.08f;
    [SerializeField, Min(0.01f)] private float recoilRecoverySpeed = 12f;

    [Header("Runtime")]
    [SerializeField] private bool hasEquippedWeapon;
    [SerializeField] private string equippedWeaponName = "None";

    private float nextFireTime;
    private Vector3 recoilStartLocalPos;
    private Coroutine recoilRoutine;
    private GameObject equippedWeaponVisualInstance;

    private void Reset()
    {
        inputHandler = GetComponent<InputHandler>();
    }

    private void Awake()
    {
        if (inputHandler == null)
        {
            inputHandler = GetComponent<InputHandler>();
        }

        if (fireCamera == null)
        {
            fireCamera = Camera.main;
        }

        if (recoilTarget == null && fireCamera != null)
        {
            recoilTarget = fireCamera.transform;
        }

        if (recoilTarget != null)
        {
            recoilStartLocalPos = recoilTarget.localPosition;
        }
    }

    private void Update()
    {
        if (ReadFirePressed())
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (!hasEquippedWeapon || fireCamera == null || Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        ApplyRecoil();

        Ray ray = new Ray(fireCamera.transform.position, fireCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (impactSparkPrefab != null)
        {
            Quaternion lookRotation = Quaternion.LookRotation(hit.normal);
            Instantiate(impactSparkPrefab, hit.point, lookRotation);
        }

        // Interface-based damage keeps the weapon reusable for zombies,
        // props, and future breakable environment objects.
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, gameObject);
        }
    }

    public void Configure(Camera cameraReference, Transform holdPoint)
    {
        fireCamera = cameraReference;
        weaponHoldPoint = holdPoint;
        if (recoilTarget == null && fireCamera != null)
        {
            recoilTarget = fireCamera.transform;
            recoilStartLocalPos = recoilTarget.localPosition;
        }
    }

    public void EquipWeapon(GameObject weaponVisualPrefab, string weaponName, float newDamage, float newFireRate, float newRange)
    {
        equippedWeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Weapon" : weaponName;
        damage = Mathf.Max(1f, newDamage);
        fireRate = Mathf.Max(0.2f, newFireRate);
        range = Mathf.Max(5f, newRange);
        hasEquippedWeapon = true;

        if (weaponVisualPrefab == null || weaponHoldPoint == null)
        {
            return;
        }

        if (equippedWeaponVisualInstance != null)
        {
            Destroy(equippedWeaponVisualInstance);
        }

        equippedWeaponVisualInstance = Instantiate(weaponVisualPrefab, weaponHoldPoint);
        equippedWeaponVisualInstance.name = string.Concat("Equipped_", equippedWeaponName);
        equippedWeaponVisualInstance.transform.localPosition = Vector3.zero;
        equippedWeaponVisualInstance.transform.localRotation = Quaternion.identity;
        equippedWeaponVisualInstance.transform.localScale = Vector3.one;

        Collider[] colliders = equippedWeaponVisualInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }
    }

    private bool ReadFirePressed()
    {
        if (inputHandler != null)
        {
            return inputHandler.FirePressed;
        }

        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        return Input.GetMouseButtonDown(0);
    }

    private void ApplyRecoil()
    {
        if (recoilTarget == null)
        {
            return;
        }

        if (recoilRoutine != null)
        {
            StopCoroutine(recoilRoutine);
        }

        recoilRoutine = StartCoroutine(RecoilRoutine());
    }

    private IEnumerator RecoilRoutine()
    {
        Vector3 kickedPosition = recoilStartLocalPos + Vector3.back * recoilKick;
        recoilTarget.localPosition = kickedPosition;

        while ((recoilTarget.localPosition - recoilStartLocalPos).sqrMagnitude > 0.0001f)
        {
            recoilTarget.localPosition = Vector3.Lerp(
                recoilTarget.localPosition,
                recoilStartLocalPos,
                Time.deltaTime * recoilRecoverySpeed);
            yield return null;
        }

        recoilTarget.localPosition = recoilStartLocalPos;
        recoilRoutine = null;
    }
}
