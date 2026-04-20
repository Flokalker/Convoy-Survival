using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BrowserPrototypeInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField, Min(0.5f)] private float interactionDistance = 4f;
    [SerializeField, Min(0.01f)] private float interactionRadius = 0.75f;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private Key interactionKey = Key.E;
    [SerializeField] private bool requireCursorLock = true;
    [SerializeField, Range(-1f, 1f)] private float minViewDot = 0.2f;

    private readonly Collider[] overlapResults = new Collider[32];
    private BrowserPrototypeInteractable currentTarget;

    public event Action<BrowserPrototypeInteractable> TargetChanged;
    public event Action<BrowserPrototypeInteractable> Interacted;

    public BrowserPrototypeInteractable CurrentTarget => currentTarget;
    public string CurrentPrompt => currentTarget != null ? currentTarget.InteractionText : string.Empty;

    public void SetInteractionOrigin(Transform originTransform)
    {
        interactionOrigin = originTransform;
    }

    private void Reset()
    {
        interactionOrigin = transform;
    }

    private void Awake()
    {
        if (interactionOrigin == null)
        {
            interactionOrigin = transform;
        }
    }

    private void OnDisable()
    {
        SetCurrentTarget(null);
    }

    private void Update()
    {
        UpdateCurrentTarget();
        TryInteract();
    }

    private void UpdateCurrentTarget()
    {
        Vector3 originPosition = interactionOrigin.position;
        Vector3 originForward = interactionOrigin.forward;

        int hitCount = Physics.OverlapSphereNonAlloc(
            originPosition,
            interactionRadius,
            overlapResults,
            interactionMask,
            QueryTriggerInteraction.Collide);

        BrowserPrototypeInteractable bestTarget = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = overlapResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            BrowserPrototypeInteractable interactable = hitCollider.GetComponentInParent<BrowserPrototypeInteractable>();
            if (interactable == null || !interactable.CanInteract(gameObject))
            {
                continue;
            }

            Vector3 toTarget = interactable.transform.position - originPosition;
            float distance = toTarget.magnitude;
            if (distance > interactionDistance)
            {
                continue;
            }

            float normalizedDot = 1f;
            if (distance > 0.001f)
            {
                normalizedDot = Vector3.Dot(originForward, toTarget / distance);
            }

            if (normalizedDot < minViewDot)
            {
                continue;
            }

            float score = distance + (1f - normalizedDot);
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = interactable;
            }
        }

        SetCurrentTarget(bestTarget);
    }

    private void TryInteract()
    {
        if (currentTarget == null)
        {
            return;
        }

        if (requireCursorLock && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        var keyControl = keyboard[interactionKey];
        if (keyControl == null || !keyControl.wasPressedThisFrame)
        {
            return;
        }

        BrowserPrototypeInteractable targetBeforeInteraction = currentTarget;
        if (targetBeforeInteraction.Interact(gameObject))
        {
            Interacted?.Invoke(targetBeforeInteraction);
        }
    }

    private void SetCurrentTarget(BrowserPrototypeInteractable nextTarget)
    {
        if (ReferenceEquals(currentTarget, nextTarget))
        {
            return;
        }

        currentTarget = nextTarget;
        TargetChanged?.Invoke(currentTarget);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = interactionOrigin != null ? interactionOrigin : transform;
        Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
        Gizmos.DrawWireSphere(origin.position, interactionRadius);
        Gizmos.DrawLine(origin.position, origin.position + origin.forward * interactionDistance);
    }
}
