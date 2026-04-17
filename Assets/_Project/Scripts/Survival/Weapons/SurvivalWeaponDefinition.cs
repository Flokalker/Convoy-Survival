using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Survival/Weapons/Weapon Definition")]
public class SurvivalWeaponDefinition : ScriptableObject
{
    public enum WeaponType
    {
        Ranged,
        Melee
    }

    [Header("Identity")]
    [SerializeField] private string weaponId = "weapon_rifle";
    [SerializeField] private string displayName = "Rifle";
    [SerializeField] private WeaponType weaponType = WeaponType.Ranged;

    [Header("Core Combat")]
    [SerializeField, Min(1f)] private float damage = 20f;
    [SerializeField, Min(0.1f)] private float attacksPerSecond = 5f;
    [SerializeField, Min(1f)] private float range = 120f;
    [SerializeField] private bool automaticFire = false;

    [Header("Ammo (Ranged)")]
    [SerializeField] private bool usesAmmo = true;
    [SerializeField, Min(1)] private int magazineSize = 30;
    [SerializeField, Min(0)] private int startingReserveAmmo = 90;
    [SerializeField, Min(0.1f)] private float reloadDuration = 1.5f;

    [Header("Melee")]
    [SerializeField, Min(0.1f)] private float meleeRadius = 1.6f;
    [SerializeField] private Vector3 meleeOffset = new Vector3(0f, 1f, 1.2f);

    [Header("Prefab/Effects")]
    [SerializeField] private SurvivalWeaponView weaponViewPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip attackSound;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public WeaponType Type => weaponType;
    public float Damage => damage;
    public float AttacksPerSecond => attacksPerSecond;
    public float Range => range;
    public bool AutomaticFire => automaticFire;
    public bool UsesAmmo => weaponType == WeaponType.Ranged && usesAmmo;
    public int MagazineSize => magazineSize;
    public int StartingReserveAmmo => startingReserveAmmo;
    public float ReloadDuration => reloadDuration;
    public float MeleeRadius => meleeRadius;
    public Vector3 MeleeOffset => meleeOffset;
    public SurvivalWeaponView WeaponViewPrefab => weaponViewPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public AudioClip HitSound => hitSound;
    public AudioClip AttackSound => attackSound;

    public void ConfigureRuntime(
        string id,
        string name,
        WeaponType type,
        float runtimeDamage,
        float runtimeAps,
        float runtimeRange,
        bool runtimeAutomatic,
        bool runtimeUsesAmmo,
        int runtimeMagazineSize,
        int runtimeReserveAmmo,
        float runtimeReloadDuration,
        SurvivalWeaponView runtimeViewPrefab)
    {
        weaponId = id;
        displayName = name;
        weaponType = type;
        damage = Mathf.Max(1f, runtimeDamage);
        attacksPerSecond = Mathf.Max(0.1f, runtimeAps);
        range = Mathf.Max(1f, runtimeRange);
        automaticFire = runtimeAutomatic;
        usesAmmo = runtimeUsesAmmo;
        magazineSize = Mathf.Max(1, runtimeMagazineSize);
        startingReserveAmmo = Mathf.Max(0, runtimeReserveAmmo);
        reloadDuration = Mathf.Max(0.1f, runtimeReloadDuration);
        weaponViewPrefab = runtimeViewPrefab;
    }
}
