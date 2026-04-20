using System;
using UnityEngine;

[DisallowMultipleComponent]
public class BrowserPrototypeCollectible : BrowserPrototypeInteractable
{
    [Header("Collectible Data")]
    [SerializeField] private string itemId = "collectible_item";
    [SerializeField] private string displayName = "Data Core";
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField] private string pickupPrompt = "E = Collect";

    [Header("Collect Behaviour")]
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool disableRendererOnCollect = true;
    [SerializeField] private bool disableColliderOnCollect = true;

    private bool isCollected;
    private Collider ownCollider;
    private Renderer[] cachedRenderers;

    public event Action<BrowserPrototypeCollectible> Collected;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public int Value => Mathf.Max(1, value);
    public bool IsCollected => isCollected;

    public override string InteractionText => isCollected ? string.Empty : string.Concat(pickupPrompt, " (", displayName, ")");

    public void Configure(string newItemId, string newDisplayName, int newValue, string newPickupPrompt = null)
    {
        if (!string.IsNullOrWhiteSpace(newItemId))
        {
            itemId = newItemId;
        }

        if (!string.IsNullOrWhiteSpace(newDisplayName))
        {
            displayName = newDisplayName;
        }

        value = Mathf.Max(1, newValue);

        if (!string.IsNullOrWhiteSpace(newPickupPrompt))
        {
            pickupPrompt = newPickupPrompt;
        }
    }

    private void Reset()
    {
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            SphereCollider triggerCollider = gameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
        }
    }

    protected virtual void Awake()
    {
        ownCollider = GetComponent<Collider>();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public override bool CanInteract(GameObject interactor)
    {
        return !isCollected && base.CanInteract(interactor);
    }

    public override bool Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return false;
        }

        isCollected = true;
        Collected?.Invoke(this);
        ApplyCollectState();
        return true;
    }

    private void ApplyCollectState()
    {
        if (disableColliderOnCollect && ownCollider != null)
        {
            ownCollider.enabled = false;
        }

        if (disableRendererOnCollect && cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled = false;
                }
            }
        }

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }
}
