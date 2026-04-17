using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SurvivalWeaponHud : MonoBehaviour
{
    [SerializeField] private SurvivalWeaponManager weaponManager;
    [SerializeField] private Text weaponNameText;
    [SerializeField] private Text ammoText;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private string noWeaponText = "No Weapon";

    public void ConfigureRuntime(SurvivalWeaponManager manager, Text weaponText, Text ammo, Image crosshair)
    {
        weaponManager = manager;
        weaponNameText = weaponText;
        ammoText = ammo;
        crosshairImage = crosshair;
    }

    private void Awake()
    {
        if (weaponManager == null)
        {
            weaponManager = FindAnyObjectByType<SurvivalWeaponManager>();
        }
    }

    private void OnEnable()
    {
        if (weaponManager == null)
        {
            return;
        }

        weaponManager.WeaponChanged += HandleWeaponChanged;
        weaponManager.AmmoChanged += HandleAmmoChanged;
        RefreshFromCurrent();
    }

    private void OnDisable()
    {
        if (weaponManager == null)
        {
            return;
        }

        weaponManager.WeaponChanged -= HandleWeaponChanged;
        weaponManager.AmmoChanged -= HandleAmmoChanged;
    }

    private void HandleWeaponChanged(SurvivalWeaponDefinition def, int ammo, int reserve)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = def != null ? def.DisplayName : noWeaponText;
        }

        HandleAmmoChanged(ammo, reserve, def != null && def.UsesAmmo);
    }

    private void HandleAmmoChanged(int ammo, int reserve, bool usesAmmo)
    {
        if (ammoText == null)
        {
            return;
        }

        ammoText.text = usesAmmo ? $"{ammo} / {reserve}" : "MELEE";
    }

    private void RefreshFromCurrent()
    {
        if (weaponManager == null)
        {
            return;
        }

        SurvivalWeaponDefinition current = weaponManager.CurrentWeapon;
        if (current == null)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = noWeaponText;
            }

            if (ammoText != null)
            {
                ammoText.text = "--";
            }
        }
        else
        {
            HandleWeaponChanged(current, weaponManager.CurrentAmmo, weaponManager.ReserveAmmo);
        }

        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(true);
        }
    }
}
