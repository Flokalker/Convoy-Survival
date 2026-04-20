using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BrowserPrototypeInteractionState : MonoBehaviour
{
    [SerializeField] private BrowserPrototypeInteractor interactor;

    [Serializable]
    public struct LootEntry
    {
        public string itemId;
        public string displayName;
        public int amount;
    }

    public int CollectedCount { get; private set; }
    public IReadOnlyDictionary<string, int> LootInventory => lootInventory;
    public LootEntry? LastLootEntry => lastLootEntry;

    public event Action<int> CollectedCountChanged;
    public event Action<BrowserPrototypeCollectible> CollectibleCollected;
    public event Action<LootEntry> LootAdded;

    private readonly Dictionary<string, int> lootInventory = new();
    private LootEntry? lastLootEntry;

    public void SetInteractor(BrowserPrototypeInteractor newInteractor)
    {
        if (ReferenceEquals(interactor, newInteractor))
        {
            return;
        }

        UnsubscribeFromInteractor();
        interactor = newInteractor;
        SubscribeToInteractor();
    }

    private void Reset()
    {
        interactor = GetComponent<BrowserPrototypeInteractor>();
    }

    private void Awake()
    {
        if (interactor == null)
        {
            interactor = GetComponent<BrowserPrototypeInteractor>();
        }
    }

    private void OnEnable()
    {
        SubscribeToInteractor();
    }

    private void OnDisable()
    {
        UnsubscribeFromInteractor();
    }

    private void SubscribeToInteractor()
    {
        if (interactor != null)
        {
            interactor.Interacted += HandleInteracted;
        }
    }

    private void UnsubscribeFromInteractor()
    {
        if (interactor != null)
        {
            interactor.Interacted -= HandleInteracted;
        }
    }

    private void HandleInteracted(BrowserPrototypeInteractable interactedTarget)
    {
        BrowserPrototypeCollectible collectible = interactedTarget as BrowserPrototypeCollectible;
        if (collectible == null)
        {
            return;
        }

        CollectedCount += collectible.Value;
        CollectedCountChanged?.Invoke(CollectedCount);
        CollectibleCollected?.Invoke(collectible);
    }

    public void AddLoot(string itemId, string displayName, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        if (lootInventory.ContainsKey(itemId))
        {
            lootInventory[itemId] += amount;
        }
        else
        {
            lootInventory.Add(itemId, amount);
        }

        lastLootEntry = new LootEntry
        {
            itemId = itemId,
            displayName = displayName,
            amount = amount,
        };
        LootAdded?.Invoke(lastLootEntry.Value);
    }
}
