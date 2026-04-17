using UnityEngine;

[DisallowMultipleComponent]
public class VehicleStats : MonoBehaviour
{
    [Header("Core Stats")]
    [SerializeField, Min(5f)] private float maxSpeed = 90f;
    [SerializeField, Min(25f)] private float durability = 200f;
    [SerializeField, Min(10f)] private float fuelCapacity = 120f;

    [Header("Runtime")]
    [SerializeField] private float currentDurability = 200f;
    [SerializeField] private float currentFuel = 120f;

    public float MaxSpeed => maxSpeed;
    public float Durability => durability;
    public float FuelCapacity => fuelCapacity;
    public float CurrentDurability => currentDurability;
    public float Fuel => currentFuel;

    private void Awake()
    {
        currentDurability = Mathf.Clamp(currentDurability, 0f, durability);
        currentFuel = Mathf.Clamp(currentFuel, 0f, fuelCapacity);
    }

    public void ConsumeFuel(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentFuel = Mathf.Max(0f, currentFuel - amount);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentDurability = Mathf.Max(0f, currentDurability - amount);
    }

    public void Refuel(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentFuel = Mathf.Min(fuelCapacity, currentFuel + amount);
    }

    public void Repair(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentDurability = Mathf.Min(durability, currentDurability + amount);
    }

    public void RepairAndUpgrade(float speedBonus, float durabilityBonus, float fuelCapacityBonus)
    {
        maxSpeed = Mathf.Max(5f, maxSpeed + speedBonus);
        durability = Mathf.Max(25f, durability + durabilityBonus);
        fuelCapacity = Mathf.Max(10f, fuelCapacity + fuelCapacityBonus);

        // Repair/refill after upgrades so scavenged parts feel impactful.
        currentDurability = durability;
        currentFuel = fuelCapacity;
    }
}
