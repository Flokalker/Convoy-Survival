using UnityEngine;

namespace ConvoySurvival.Hub
{
    public abstract class HubInteractable : MonoBehaviour
    {
        [SerializeField] private string prompt = "E = Interact";

        public virtual bool CanInteract()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        public virtual string GetPrompt()
        {
            return prompt;
        }

        public abstract void Interact(HubInteractionController interactor);
    }
}
