using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LootChest : BrowserPrototypeCollectible
{
    [Serializable]
    public struct LootDrop
    {
        public string itemId;
        public string displayName;
        public int minAmount;
        public int maxAmount;
        public int weight;
    }

    [SerializeField] private LootDrop[] lootTable;
    [SerializeField, Min(1)] private int minRolls = 2;
    [SerializeField, Min(1)] private int maxRolls = 4;

    protected override void Awake()
    {
        base.Awake();

        if (lootTable == null || lootTable.Length == 0)
        {
            lootTable = new[]
            {
                CreateDrop("ammo_light", "Light Ammo", 12, 30, 24),
                CreateDrop("ammo_shells", "Shells", 4, 10, 14),
                CreateDrop("shield_small", "Mini Shields", 1, 3, 16),
                CreateDrop("medkit", "Medkit", 1, 2, 10),
                CreateDrop("bandages", "Bandages", 2, 5, 14),
                CreateDrop("scrap", "Scrap", 8, 20, 22),
                CreateDrop("fuel", "Fuel", 1, 2, 12),
                CreateDrop("gold", "Gold Bars", 10, 40, 8),
            };
        }
    }

    public override bool Interact(GameObject interactor)
    {
        if (!base.Interact(interactor))
        {
            return false;
        }

        BrowserPrototypeInteractionState interactionState = interactor.GetComponent<BrowserPrototypeInteractionState>();
        if (interactionState == null)
        {
            return true;
        }

        int rolls = UnityEngine.Random.Range(minRolls, maxRolls + 1);
        for (int index = 0; index < rolls; index++)
        {
            LootDrop selectedDrop = PickDrop();
            int amount = UnityEngine.Random.Range(
                Mathf.Max(1, selectedDrop.minAmount),
                Mathf.Max(selectedDrop.minAmount, selectedDrop.maxAmount) + 1);
            interactionState.AddLoot(selectedDrop.itemId, selectedDrop.displayName, amount);
        }

        return true;
    }

    private LootDrop PickDrop()
    {
        int totalWeight = 0;
        for (int index = 0; index < lootTable.Length; index++)
        {
            totalWeight += Mathf.Max(1, lootTable[index].weight);
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int runningWeight = 0;
        for (int index = 0; index < lootTable.Length; index++)
        {
            runningWeight += Mathf.Max(1, lootTable[index].weight);
            if (randomValue < runningWeight)
            {
                return lootTable[index];
            }
        }

        return lootTable[lootTable.Length - 1];
    }

    private static LootDrop CreateDrop(string itemId, string displayName, int minAmount, int maxAmount, int weight)
    {
        return new LootDrop
        {
            itemId = itemId,
            displayName = displayName,
            minAmount = minAmount,
            maxAmount = maxAmount,
            weight = weight,
        };
    }
}
