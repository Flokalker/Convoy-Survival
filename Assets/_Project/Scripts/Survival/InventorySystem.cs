using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventorySystem : MonoBehaviour
{
    public enum ItemType
    {
        UpgradePart,
        Ammo,
        Weapon,
        RepairKit,
        FuelCan
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public ItemType itemType;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    public class WeaponData
    {
        public string weaponId = "Rifle";
        public float damage = 25f;
        public float range = 150f;
        public float fireRate = 5f;
        public int magazineSize = 30;
        public int reserveAmmo = 90;
    }

    [SerializeField] private List<InventoryItem> startingItems = new List<InventoryItem>();
    [SerializeField] private WeaponData startingWeapon = new WeaponData();

    private readonly Dictionary<string, InventoryItem> itemLookup = new Dictionary<string, InventoryItem>();

    public WeaponData EquippedWeapon { get; private set; }

    private void Awake()
    {
        itemLookup.Clear();
        for (int i = 0; i < startingItems.Count; i++)
        {
            AddItem(startingItems[i].itemId, startingItems[i].itemType, startingItems[i].amount);
        }

        EquippedWeapon = startingWeapon;
    }

    public void AddItem(string itemId, ItemType itemType, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        if (itemLookup.TryGetValue(itemId, out InventoryItem existing))
        {
            existing.amount += amount;
            return;
        }

        InventoryItem item = new InventoryItem
        {
            itemId = itemId,
            itemType = itemType,
            amount = amount
        };

        itemLookup.Add(itemId, item);
    }

    public bool ConsumeItem(string itemId, int amount = 1)
    {
        if (amount <= 0 || !itemLookup.TryGetValue(itemId, out InventoryItem existing) || existing.amount < amount)
        {
            return false;
        }

        existing.amount -= amount;
        if (existing.amount <= 0)
        {
            itemLookup.Remove(itemId);
        }

        return true;
    }

    public bool TryUseUpgradeOnVehicle(VehicleStats vehicleStats, float speedBonus, float durabilityBonus, float fuelBonus, int requiredParts = 1)
    {
        if (vehicleStats == null)
        {
            return false;
        }

        if (!ConsumeItem("truck_part", requiredParts))
        {
            return false;
        }

        vehicleStats.RepairAndUpgrade(speedBonus, durabilityBonus, fuelBonus);
        return true;
    }
}
