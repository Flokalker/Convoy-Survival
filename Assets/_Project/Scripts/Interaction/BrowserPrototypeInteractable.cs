using UnityEngine;

public abstract class BrowserPrototypeInteractable : MonoBehaviour
{
    [SerializeField] private string interactionText = "E = Interact";

    public virtual string InteractionText => interactionText;

    public virtual bool CanInteract(GameObject interactor)
    {
        return enabled && gameObject.activeInHierarchy;
    }

    public abstract bool Interact(GameObject interactor);
}
