using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BrowserPrototypeUiController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrowserFpsController fpsController;
    [SerializeField] private BrowserPrototypeInteractor interactor;
    [SerializeField] private BrowserPrototypeInteractionState interactionState;
    [SerializeField] private CanvasGroup startOverlayGroup;
    [SerializeField] private Text startOverlayText;
    [SerializeField] private Text interactionPromptText;
    [SerializeField] private Text collectedCounterText;

    [Header("Text Content")]
    [SerializeField, TextArea(5, 12)] private string startOverlayMessage =
        "Browser Prototype\n\n" +
        "WASD = Move\n" +
        "Shift = Sprint\n" +
        "Space = Jump\n" +
        "Left Click = Lock Cursor\n" +
        "Escape = Release Cursor\n\n" +
        "Press Left Click to start";

    [Header("Behaviour")]
    [SerializeField] private bool hideOverlayOnFirstCursorLock = true;

    private bool overlayDismissed;

    public void Configure(
        BrowserFpsController fpsControllerReference,
        BrowserPrototypeInteractor interactorReference,
        BrowserPrototypeInteractionState interactionStateReference,
        CanvasGroup startOverlayGroupReference,
        Text startOverlayTextReference,
        Text interactionPromptTextReference,
        Text collectedCounterTextReference)
    {
        fpsController = fpsControllerReference;
        interactor = interactorReference;
        interactionState = interactionStateReference;
        startOverlayGroup = startOverlayGroupReference;
        startOverlayText = startOverlayTextReference;
        interactionPromptText = interactionPromptTextReference;
        collectedCounterText = collectedCounterTextReference;
    }

    private void Awake()
    {
        if (fpsController == null)
        {
            fpsController = FindAnyObjectByType<BrowserFpsController>();
        }

        if (interactor == null && fpsController != null)
        {
            interactor = fpsController.GetComponent<BrowserPrototypeInteractor>();
        }

        if (interactionState == null && fpsController != null)
        {
            interactionState = fpsController.GetComponent<BrowserPrototypeInteractionState>();
        }

        if (startOverlayText != null && string.IsNullOrWhiteSpace(startOverlayText.text))
        {
            startOverlayText.text = startOverlayMessage;
        }
    }

    private void OnEnable()
    {
        if (fpsController != null)
        {
            fpsController.CursorLockStateChanged += HandleCursorLockStateChanged;
        }

        if (interactor != null)
        {
            interactor.TargetChanged += HandleTargetChanged;
        }

        if (interactionState != null)
        {
            interactionState.CollectedCountChanged += HandleCollectedCountChanged;
        }
    }

    private void Start()
    {
        SetOverlayVisible(!overlayDismissed);
        RefreshInteractionPrompt();
        RefreshCollectedCounter();

        if (hideOverlayOnFirstCursorLock && Cursor.lockState == CursorLockMode.Locked)
        {
            DismissOverlay();
        }
    }

    private void Update()
    {
        if (hideOverlayOnFirstCursorLock && !overlayDismissed && Cursor.lockState == CursorLockMode.Locked)
        {
            DismissOverlay();
        }

        RefreshInteractionPrompt();
    }

    private void OnDisable()
    {
        if (fpsController != null)
        {
            fpsController.CursorLockStateChanged -= HandleCursorLockStateChanged;
        }

        if (interactor != null)
        {
            interactor.TargetChanged -= HandleTargetChanged;
        }

        if (interactionState != null)
        {
            interactionState.CollectedCountChanged -= HandleCollectedCountChanged;
        }
    }

    private void HandleCursorLockStateChanged(bool isLocked)
    {
        if (hideOverlayOnFirstCursorLock && isLocked && !overlayDismissed)
        {
            DismissOverlay();
        }
    }

    private void HandleTargetChanged(BrowserPrototypeInteractable _)
    {
        RefreshInteractionPrompt();
    }

    private void HandleCollectedCountChanged(int _)
    {
        RefreshCollectedCounter();
    }

    private void RefreshInteractionPrompt()
    {
        if (interactionPromptText == null)
        {
            return;
        }

        string prompt = interactor != null ? interactor.CurrentPrompt : string.Empty;
        bool canShowPrompt = !string.IsNullOrWhiteSpace(prompt) && Cursor.lockState == CursorLockMode.Locked;
        interactionPromptText.gameObject.SetActive(canShowPrompt);

        if (canShowPrompt)
        {
            interactionPromptText.text = prompt;
        }
    }

    private void RefreshCollectedCounter()
    {
        if (collectedCounterText == null)
        {
            return;
        }

        int count = interactionState != null ? interactionState.CollectedCount : 0;
        collectedCounterText.text = string.Concat("Collected: ", count);
    }

    private void DismissOverlay()
    {
        overlayDismissed = true;
        SetOverlayVisible(false);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (startOverlayGroup == null)
        {
            return;
        }

        startOverlayGroup.alpha = visible ? 1f : 0f;
        startOverlayGroup.blocksRaycasts = visible;
        startOverlayGroup.interactable = visible;
    }
}
