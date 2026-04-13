using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SurvivalAdminPanel : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "BrowserPrototype";

    private CombatAIManager combatManager;
    private PlayerStats playerStats;
    private Text godModeLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        if (FindObjectOfType<SurvivalAdminPanel>() != null)
        {
            return;
        }

        GameObject panelObject = new GameObject("SurvivalAdminPanel");
        panelObject.AddComponent<SurvivalAdminPanel>();
    }

    private void Awake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && scene.name != targetSceneName)
        {
            Destroy(gameObject);
            return;
        }

        combatManager = FindObjectOfType<CombatAIManager>();
        playerStats = FindObjectOfType<PlayerStats>();

        BuildUi();
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("AdminCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("AdminPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);
        panelRect.sizeDelta = new Vector2(300f, 360f);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        CreateButton(panel.transform, font, "Spawn Runner Wave (8)", 0, () => SpawnVariant(ZombieAI.ZombieVariant.Runner, 8));
        CreateButton(panel.transform, font, "Spawn Tank Wave (4)", 1, () => SpawnVariant(ZombieAI.ZombieVariant.Tank, 4));
        CreateButton(panel.transform, font, "Spawn Spitter Wave (6)", 2, () => SpawnVariant(ZombieAI.ZombieVariant.Spitter, 6));
        CreateButton(panel.transform, font, "Spawn Mixed Wave (12)", 3, () => SpawnMixed(12));
        CreateButton(panel.transform, font, "Toggle Infinite Health", 4, ToggleGodMode);
        godModeLabel = CreateLabel(panel.transform, font, 5, "GodModeStatus");
        UpdateGodModeLabel();
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void CreateButton(Transform parent, Font font, string text, int row, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f - row * 40f);
        rect.sizeDelta = new Vector2(-14f, 34f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.16f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 16;
    }

    private static Text CreateLabel(Transform parent, Font font, int row, string name)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -14f - row * 40f);
        rect.sizeDelta = new Vector2(-14f, 26f);

        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.color = new Color(0.5f, 1f, 0.5f, 1f);
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 14;
        return label;
    }

    private void SpawnVariant(ZombieAI.ZombieVariant variant, int count)
    {
        if (combatManager == null)
        {
            combatManager = FindObjectOfType<CombatAIManager>();
        }

        if (combatManager != null)
        {
            combatManager.SpawnVariantWaveNow(variant, count);
        }
    }

    private void SpawnMixed(int count)
    {
        if (combatManager == null)
        {
            combatManager = FindObjectOfType<CombatAIManager>();
        }

        if (combatManager != null)
        {
            combatManager.SpawnMixedWaveNow(count);
        }
    }

    private void ToggleGodMode()
    {
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }

        if (playerStats == null)
        {
            return;
        }

        playerStats.SetInfiniteHealth(!playerStats.InfiniteHealth);
        UpdateGodModeLabel();
    }

    private void UpdateGodModeLabel()
    {
        if (godModeLabel == null)
        {
            return;
        }

        bool on = playerStats != null && playerStats.InfiniteHealth;
        godModeLabel.text = on ? "Infinite Health: ON" : "Infinite Health: OFF";
    }

}
