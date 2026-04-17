using UnityEngine;
using UnityEngine.InputSystem;

namespace ConvoySurvival.Hub
{
    [DisallowMultipleComponent]
    public class HubInteractionController : MonoBehaviour
    {
        [SerializeField] private Transform interactionOrigin;
        [SerializeField, Min(1f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private HubInteractable currentTarget;

        public string CurrentPrompt => currentTarget != null ? currentTarget.GetPrompt() : string.Empty;

        public void Configure(Transform origin)
        {
            interactionOrigin = origin;
        }

        private void Awake()
        {
            if (interactionOrigin == null)
            {
                interactionOrigin = transform;
            }
        }

        private void Update()
        {
            RefreshTarget();
            TryInteract();
        }

        private void RefreshTarget()
        {
            currentTarget = null;

            Ray ray = new Ray(interactionOrigin.position, interactionOrigin.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            HubInteractable interactable = hit.collider.GetComponentInParent<HubInteractable>();
            if (interactable == null || !interactable.CanInteract())
            {
                return;
            }

            currentTarget = interactable;
        }

        private void TryInteract()
        {
            if (currentTarget == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
            {
                return;
            }

            currentTarget.Interact(this);
        }
    }
}
