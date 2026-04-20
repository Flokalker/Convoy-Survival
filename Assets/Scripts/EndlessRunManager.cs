using UnityEngine;

public class EndlessRunManager : MonoBehaviour
{
    public static EndlessRunManager Instance { get; private set; }

    private BrowserFpsController fpsController;
    private BrowserPrototypeInteractor interactor;
    private BrowserPrototypeInteractionState interactionState;
    private Vector3 lastPosition;
    private float traveledDistance;
    private GUIStyle labelStyle;
    private GUIStyle overlayStyle;
    private bool overlayDismissed;
    private string lastLootText = string.Empty;
    private float lastLootTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Configure(
        BrowserFpsController fpsControllerReference,
        BrowserPrototypeInteractor interactorReference,
        BrowserPrototypeInteractionState interactionStateReference)
    {
        fpsController = fpsControllerReference;
        interactor = interactorReference;
        interactionState = interactionStateReference;

        if (fpsController != null)
        {
            lastPosition = fpsController.transform.position;
        }

        SubscribeToInteractionState();
    }

    private void Update()
    {
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<BrowserFpsController>();
            if (fpsController != null)
            {
                lastPosition = fpsController.transform.position;
            }
        }

        if (interactor == null && fpsController != null)
        {
            interactor = fpsController.GetComponent<BrowserPrototypeInteractor>();
        }

        if (interactionState == null && fpsController != null)
        {
            interactionState = fpsController.GetComponent<BrowserPrototypeInteractionState>();
            SubscribeToInteractionState();
        }

        if (fpsController != null)
        {
            Vector3 delta = fpsController.transform.position - lastPosition;
            delta.y = 0f;
            traveledDistance += delta.magnitude;
            lastPosition = fpsController.transform.position;
        }

        if (!overlayDismissed && Cursor.lockState == CursorLockMode.Locked)
        {
            overlayDismissed = true;
        }
    }

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white,
                },
            };

            overlayStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                wordWrap = true,
            };
        }

        if (!overlayDismissed)
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            GUI.Label(
                new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 120f, 480f, 240f),
                "Convoy Survival\n\nWASD / Pfeiltasten = Laufen\nShift = Sprint\nSpace = Springen\nLinksklick oder Bewegung = Kamera aktivieren\nEscape = Cursor lösen\nE = Einsammeln",
                overlayStyle);
        }

        GUI.Label(new Rect(20f, 20f, 460f, 34f), $"Distanz: {traveledDistance:0} m", labelStyle);
        GUI.Label(new Rect(20f, 56f, 460f, 34f), $"Gesammelt: {GetCollectedCount()}", labelStyle);
        GUI.Label(new Rect(20f, 92f, 460f, 34f), $"Loot: {GetLootSummary()}", labelStyle);

        string prompt = interactor != null ? interactor.CurrentPrompt : string.Empty;
        if (!string.IsNullOrWhiteSpace(prompt) && Cursor.lockState == CursorLockMode.Locked)
        {
            GUI.Label(new Rect(20f, Screen.height - 60f, 520f, 34f), prompt, labelStyle);
        }

        if (!string.IsNullOrWhiteSpace(lastLootText) && Time.time - lastLootTime < 3.5f)
        {
            GUI.Label(new Rect(Screen.width - 320f, 20f, 300f, 34f), lastLootText, labelStyle);
        }
    }

    private int GetCollectedCount()
    {
        return interactionState != null ? interactionState.CollectedCount : 0;
    }

    private string GetLootSummary()
    {
        if (interactionState == null)
        {
            return "0";
        }

        int totalLoot = 0;
        foreach (int amount in interactionState.LootInventory.Values)
        {
            totalLoot += amount;
        }

        return totalLoot.ToString();
    }

    private void SubscribeToInteractionState()
    {
        if (interactionState == null)
        {
            return;
        }

        interactionState.LootAdded -= HandleLootAdded;
        interactionState.LootAdded += HandleLootAdded;
    }

    private void HandleLootAdded(BrowserPrototypeInteractionState.LootEntry lootEntry)
    {
        lastLootText = $"+{lootEntry.amount} {lootEntry.displayName}";
        lastLootTime = Time.time;
    }
}
