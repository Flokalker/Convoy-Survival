using System;
using UnityEngine;

[DisallowMultipleComponent]
public class BrowserPrototypeInteractionState : MonoBehaviour
{
    [SerializeField] private BrowserPrototypeInteractor interactor;

    public int CollectedCount { get; private set; }

    public event Action<int> CollectedCountChanged;
    public event Action<BrowserPrototypeCollectible> CollectibleCollected;

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
}
