using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(InputHandler))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField, Min(0.25f)] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    public IInteractable CurrentInteractable { get; private set; }

    private FirstPersonController playerController;

    private void Reset()
    {
        inputHandler = GetComponent<InputHandler>();
    }

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
        if (inputHandler == null)
        {
            inputHandler = GetComponent<InputHandler>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        FindInteractable();

        if (CurrentInteractable == null || !inputHandler.InteractPressed)
        {
            return;
        }

        if (CurrentInteractable.CanInteract(playerController))
        {
            CurrentInteractable.Interact(playerController);
        }
    }

    private void FindInteractable()
    {
        CurrentInteractable = null;
        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            return;
        }

        CurrentInteractable = hit.collider.GetComponentInParent<IInteractable>();
    }
}
