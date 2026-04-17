using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SurvivalWeaponManager : MonoBehaviour
{
    [Serializable]
    private class WeaponRuntime
    {
        public SurvivalWeaponDefinition definition;
        public int ammoInMagazine;
        public int reserveAmmo;
    }

    [Header("Setup")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private LayerMask meleeHitMask = ~0;
    [SerializeField] private List<SurvivalWeaponDefinition> startingWeapons = new List<SurvivalWeaponDefinition>();

    [Header("Input")]
    [SerializeField] private Key reloadKey = Key.R;
    [SerializeField] private Key nextWeaponKey = Key.E;
    [SerializeField] private Key previousWeaponKey = Key.Q;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs;

    private readonly List<WeaponRuntime> weapons = new List<WeaponRuntime>();
    private readonly Collider[] meleeResults = new Collider[16];

    private SurvivalWeaponView activeWeaponView;
    private int activeWeaponIndex = -1;
    private float nextAttackTime;
    private bool isReloading;
    private bool started;

    public event Action<SurvivalWeaponDefinition, int, int> WeaponChanged;
    public event Action<int, int, bool> AmmoChanged;

    public SurvivalWeaponDefinition CurrentWeapon => IsValidIndex(activeWeaponIndex) ? weapons[activeWeaponIndex].definition : null;
    public int CurrentAmmo => IsValidIndex(activeWeaponIndex) ? weapons[activeWeaponIndex].ammoInMagazine : 0;
    public int ReserveAmmo => IsValidIndex(activeWeaponIndex) ? weapons[activeWeaponIndex].reserveAmmo : 0;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (weaponHoldPoint == null && aimCamera != null)
        {
            GameObject hold = new GameObject("WeaponHoldPoint");
            hold.transform.SetParent(aimCamera.transform, false);
            hold.transform.localPosition = new Vector3(0.2f, -0.2f, 0.45f);
            weaponHoldPoint = hold.transform;
        }
    }

    private void Start()
    {
        started = true;
        InitializeFromStartingWeapons();
    }

    public void ConfigureRuntime(Camera cameraReference, Transform holdPoint, List<SurvivalWeaponDefinition> runtimeWeapons)
    {
        aimCamera = cameraReference;
        weaponHoldPoint = holdPoint;
        startingWeapons = runtimeWeapons ?? new List<SurvivalWeaponDefinition>();

        if (started)
        {
            InitializeFromStartingWeapons();
        }
    }

    private void InitializeFromStartingWeapons()
    {
        weapons.Clear();
        activeWeaponIndex = -1;
        isReloading = false;
        nextAttackTime = 0f;

        if (activeWeaponView != null)
        {
            Destroy(activeWeaponView.gameObject);
            activeWeaponView = null;
        }

        for (int i = 0; i < startingWeapons.Count; i++)
        {
            AddWeapon(startingWeapons[i], autoEquipIfFirst: i == 0);
        }

        if (weapons.Count > 0 && activeWeaponIndex < 0)
        {
            EquipWeapon(0);
        }
    }

    private void Update()
    {
        if (!IsValidIndex(activeWeaponIndex))
        {
            return;
        }

        HandleSwitchInput();
        HandleReloadInput();
        HandleAttackInput();
    }

    public bool AddWeapon(SurvivalWeaponDefinition definition, bool autoEquipIfFirst = false)
    {
        if (definition == null)
        {
            return false;
        }

        int existing = FindWeaponIndex(definition);
        if (existing >= 0)
        {
            if (definition.UsesAmmo)
            {
                weapons[existing].reserveAmmo += Mathf.Max(0, definition.StartingReserveAmmo);
                RaiseAmmoChanged();
            }

            return true;
        }

        WeaponRuntime runtime = new WeaponRuntime
        {
            definition = definition,
            ammoInMagazine = definition.UsesAmmo ? definition.MagazineSize : 0,
            reserveAmmo = definition.UsesAmmo ? definition.StartingReserveAmmo : 0
        };

        weapons.Add(runtime);

        if (autoEquipIfFirst && activeWeaponIndex < 0)
        {
            EquipWeapon(weapons.Count - 1);
        }
        else
        {
            RaiseWeaponChanged();
        }

        return true;
    }

    public void EquipWeapon(int index)
    {
        if (!IsValidIndex(index))
        {
            return;
        }

        activeWeaponIndex = index;
        isReloading = false;
        BuildWeaponView(weapons[index].definition);
        RaiseWeaponChanged();
        RaiseAmmoChanged();
    }

    public void EquipNextWeapon()
    {
        if (weapons.Count <= 1)
        {
            return;
        }

        int next = (activeWeaponIndex + 1) % weapons.Count;
        EquipWeapon(next);
    }

    public void EquipPreviousWeapon()
    {
        if (weapons.Count <= 1)
        {
            return;
        }

        int previous = activeWeaponIndex - 1;
        if (previous < 0)
        {
            previous = weapons.Count - 1;
        }

        EquipWeapon(previous);
    }

    private void HandleAttackInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Time.time < nextAttackTime || isReloading)
        {
            return;
        }

        SurvivalWeaponDefinition def = weapons[activeWeaponIndex].definition;
        bool attackPressed = def.AutomaticFire ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
        if (!attackPressed)
        {
            return;
        }

        if (def.UsesAmmo)
        {
            if (weapons[activeWeaponIndex].ammoInMagazine <= 0)
            {
                TryStartReload();
                return;
            }

            weapons[activeWeaponIndex].ammoInMagazine--;
            RaiseAmmoChanged();
        }

        nextAttackTime = Time.time + 1f / Mathf.Max(0.1f, def.AttacksPerSecond);

        if (def.Type == SurvivalWeaponDefinition.WeaponType.Ranged)
        {
            PerformRangedAttack(def);
            if (activeWeaponView != null)
            {
                activeWeaponView.PlayRangedAttack(def.AttackSound);
            }
        }
        else
        {
            PerformMeleeAttack(def);
            if (activeWeaponView != null)
            {
                activeWeaponView.PlayMeleeAttack(def.AttackSound);
            }
        }
    }

    private void HandleReloadInput()
    {
        if (!IsValidIndex(activeWeaponIndex) || isReloading)
        {
            return;
        }

        SurvivalWeaponDefinition def = weapons[activeWeaponIndex].definition;
        if (!def.UsesAmmo)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[reloadKey] != null && keyboard[reloadKey].wasPressedThisFrame)
        {
            TryStartReload();
        }
    }

    private void TryStartReload()
    {
        if (!IsValidIndex(activeWeaponIndex) || isReloading)
        {
            return;
        }

        WeaponRuntime runtime = weapons[activeWeaponIndex];
        SurvivalWeaponDefinition def = runtime.definition;
        if (!def.UsesAmmo || runtime.ammoInMagazine >= def.MagazineSize || runtime.reserveAmmo <= 0)
        {
            return;
        }

        StartCoroutine(ReloadRoutine(runtime, def));
    }

    private IEnumerator ReloadRoutine(WeaponRuntime runtime, SurvivalWeaponDefinition def)
    {
        isReloading = true;
        if (activeWeaponView != null)
        {
            activeWeaponView.PlayReload();
        }

        yield return new WaitForSeconds(def.ReloadDuration);

        int needed = def.MagazineSize - runtime.ammoInMagazine;
        int toLoad = Mathf.Min(needed, runtime.reserveAmmo);
        runtime.ammoInMagazine += toLoad;
        runtime.reserveAmmo -= toLoad;
        isReloading = false;
        RaiseAmmoChanged();
    }

    private void HandleSwitchInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null)
        {
            return;
        }

        if (keyboard[nextWeaponKey] != null && keyboard[nextWeaponKey].wasPressedThisFrame)
        {
            EquipNextWeapon();
            return;
        }

        if (keyboard[previousWeaponKey] != null && keyboard[previousWeaponKey].wasPressedThisFrame)
        {
            EquipPreviousWeapon();
            return;
        }

        float wheel = mouse.scroll.ReadValue().y;
        if (wheel > 0f)
        {
            EquipNextWeapon();
        }
        else if (wheel < 0f)
        {
            EquipPreviousWeapon();
        }
    }

    private void PerformRangedAttack(SurvivalWeaponDefinition def)
    {
        if (aimCamera == null)
        {
            return;
        }

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, def.Range, hitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        TryApplyDamage(hit.collider, def.Damage, def);
        SpawnHitFeedback(hit, def);
    }

    private void PerformMeleeAttack(SurvivalWeaponDefinition def)
    {
        Vector3 origin = transform.TransformPoint(def.MeleeOffset);
        int count = Physics.OverlapSphereNonAlloc(origin, def.MeleeRadius, meleeResults, meleeHitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = meleeResults[i];
            if (c == null || c.transform.IsChildOf(transform))
            {
                continue;
            }

            TryApplyDamage(c, def.Damage, def);
        }
    }

    private bool TryApplyDamage(Collider targetCollider, float damage, SurvivalWeaponDefinition def)
    {
        if (targetCollider == null)
        {
            return false;
        }

        IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            damageable.TakeDamage(damage, gameObject);
            return true;
        }

        Health health = targetCollider.GetComponentInParent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage, gameObject);
            return true;
        }

        if (showDebugLogs)
        {
            Debug.Log($"No IDamageable/Health on {targetCollider.name}");
        }

        return false;
    }

    private void SpawnHitFeedback(RaycastHit hit, SurvivalWeaponDefinition def)
    {
        if (def.HitEffectPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            Instantiate(def.HitEffectPrefab, hit.point, rot);
        }

        if (activeWeaponView != null)
        {
            activeWeaponView.PlaySound(def.HitSound);
        }
    }

    private void BuildWeaponView(SurvivalWeaponDefinition def)
    {
        if (activeWeaponView != null)
        {
            Destroy(activeWeaponView.gameObject);
            activeWeaponView = null;
        }

        if (def == null || def.WeaponViewPrefab == null || weaponHoldPoint == null)
        {
            return;
        }

        activeWeaponView = Instantiate(def.WeaponViewPrefab, weaponHoldPoint);
        activeWeaponView.transform.localPosition = Vector3.zero;
        activeWeaponView.transform.localRotation = Quaternion.identity;
        activeWeaponView.transform.localScale = Vector3.one;
    }

    private int FindWeaponIndex(SurvivalWeaponDefinition def)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].definition == def || weapons[i].definition.WeaponId == def.WeaponId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < weapons.Count;
    }

    private void RaiseWeaponChanged()
    {
        if (!IsValidIndex(activeWeaponIndex))
        {
            return;
        }

        WeaponRuntime runtime = weapons[activeWeaponIndex];
        WeaponChanged?.Invoke(runtime.definition, runtime.ammoInMagazine, runtime.reserveAmmo);
    }

    private void RaiseAmmoChanged()
    {
        if (!IsValidIndex(activeWeaponIndex))
        {
            return;
        }

        WeaponRuntime runtime = weapons[activeWeaponIndex];
        AmmoChanged?.Invoke(runtime.ammoInMagazine, runtime.reserveAmmo, runtime.definition.UsesAmmo);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !IsValidIndex(activeWeaponIndex))
        {
            return;
        }

        SurvivalWeaponDefinition def = weapons[activeWeaponIndex].definition;
        if (def.Type != SurvivalWeaponDefinition.WeaponType.Melee)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(def.MeleeOffset), def.MeleeRadius);
    }
}
