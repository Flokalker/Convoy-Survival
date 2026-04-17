using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ApocalypseHudController : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private Color panelColor = new Color(0.13f, 0.16f, 0.14f, 0.68f);
    [SerializeField] private Color panelEdge = new Color(0.43f, 0.39f, 0.28f, 0.85f);
    [SerializeField] private Color textPrimary = new Color(0.89f, 0.91f, 0.84f, 1f);
    [SerializeField] private Color textMuted = new Color(0.62f, 0.68f, 0.61f, 1f);
    [SerializeField] private Color healthColor = new Color(0.72f, 0.16f, 0.17f, 1f);
    [SerializeField] private Color staminaColor = new Color(0.73f, 0.64f, 0.27f, 1f);
    [SerializeField] private Color accentWarning = new Color(0.86f, 0.36f, 0.17f, 1f);
    [SerializeField] private Color accentSafe = new Color(0.39f, 0.64f, 0.49f, 1f);

    [Header("Stamina")]
    [SerializeField, Min(10f)] private float maxStamina = 100f;
    [SerializeField, Min(1f)] private float sprintDrainPerSecond = 22f;
    [SerializeField, Min(1f)] private float recoverPerSecond = 18f;

    [Header("Combat Detection")]
    [SerializeField, Min(4f)] private float combatDistance = 14f;
    [SerializeField, Min(0.1f)] private float combatCheckInterval = 0.4f;

    [Header("Animation")]
    [SerializeField] private bool useAnimatedScanline = true;
    [SerializeField, Min(0.1f)] private float panelPulseSpeed = 1.35f;
    [SerializeField, Min(0f)] private float panelPulseAmount = 0.08f;

    private Canvas rootCanvas;
    private Font uiFont;
    private PlayerStats playerStats;
    private InputHandler inputHandler;
    private SurvivalWeaponManager weaponManager;

    private Image healthFill;
    private Image staminaFill;
    private Text weaponText;
    private Text ammoText;
    private Text objectiveText;
    private Text alertText;
    private Text modeText;
    private Image mapFog;
    private Image scanline;
    private CanvasGroup hudGroup;
    private RectTransform inventoryRoot;

    private readonly Queue<string> notificationQueue = new Queue<string>();
    private readonly List<Text> notificationLines = new List<Text>();
    private float stamina;
    private float nextCombatCheck;
    private bool combatMode;
    private float notificationTimer;
    private float nextBindTry;
    private bool hudBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<ApocalypseHudController>() != null)
        {
            return;
        }

        GameObject host = new GameObject("ApocalypseHudController");
        host.AddComponent<ApocalypseHudController>();
    }

    public void SetWeaponManager(SurvivalWeaponManager manager)
    {
        EnsureHudBuilt();

        if (weaponManager == manager)
        {
            return;
        }

        UnsubscribeWeaponEvents();
        weaponManager = manager;
        SubscribeWeaponEvents();
        RefreshWeaponUi();
    }

    private void Awake()
    {
        stamina = maxStamina;
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureHudBuilt();
        HideLegacyHud();
        BindReferences();
    }

    private void Update()
    {
        if (Time.time >= nextBindTry)
        {
            nextBindTry = Time.time + 0.8f;
            if (playerStats == null || inputHandler == null || weaponManager == null)
            {
                BindReferences();
            }
        }

        UpdateVitals();
        UpdateCombatMode();
        UpdateAtmosphere();
        UpdateNotifications();
    }

    private void OnDestroy()
    {
        UnsubscribeWeaponEvents();
    }

    private void BindReferences()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (inputHandler == null)
        {
            inputHandler = FindAnyObjectByType<InputHandler>();
        }

        if (weaponManager == null)
        {
            SetWeaponManager(FindAnyObjectByType<SurvivalWeaponManager>());
        }
    }

    private void BuildHud()
    {
        if (hudBuilt)
        {
            return;
        }

        GameObject canvasObj = new GameObject("ApocalypseHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        rootCanvas = canvasObj.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        hudGroup = canvasObj.GetComponent<CanvasGroup>();
        hudGroup.alpha = 1f;

        RectTransform statusPanel = CreatePanel(canvasObj.transform, "StatusPanel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(410f, 190f), panelColor);
        CreateLabel(statusPanel, "VitalsLabel", "VITAL MODULE", new Vector2(14f, -12f), 16, textMuted);
        modeText = CreateLabel(statusPanel, "ModeLabel", "MODE: EXPLORATION", new Vector2(250f, -12f), 14, accentSafe);
        healthFill = CreateFramedBar(statusPanel, "Health", "HEALTH", new Vector2(14f, -56f), new Vector2(380f, 24f), healthColor);
        staminaFill = CreateFramedBar(statusPanel, "Stamina", "STAMINA", new Vector2(14f, -106f), new Vector2(380f, 22f), staminaColor);

        RectTransform weaponPanel = CreatePanel(canvasObj.transform, "WeaponPanel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(390f, 140f), panelColor);
        CreateLabel(weaponPanel, "WeaponLabel", "CURRENT WEAPON", new Vector2(14f, -12f), 14, textMuted);
        weaponText = CreateLabel(weaponPanel, "WeaponText", "NO WEAPON", new Vector2(14f, -44f), 24, textPrimary);
        ammoText = CreateLabel(weaponPanel, "AmmoText", "--", new Vector2(14f, -82f), 20, accentWarning);

        RectTransform mapPanel = CreatePanel(canvasObj.transform, "MapPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(320f, 220f), panelColor);
        CreateLabel(mapPanel, "MapLabel", "SCAVENGER MAP [FOG]", new Vector2(14f, -12f), 14, textMuted);
        Image mapBg = CreateRectImage(mapPanel, "MapSurface", new Vector2(12f, -38f), new Vector2(296f, 168f), new Color(0.2f, 0.22f, 0.2f, 0.9f));
        CreateMapGrid(mapBg.rectTransform);
        mapFog = CreateRectImage(mapBg.transform, "MapFog", Vector2.zero, mapBg.rectTransform.sizeDelta, new Color(0.08f, 0.09f, 0.08f, 0.52f));

        RectTransform objPanel = CreatePanel(canvasObj.transform, "ObjectivePanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(520f, 118f), panelColor);
        CreateLabel(objPanel, "ObjTitle", "OBJECTIVE", new Vector2(14f, -12f), 14, textMuted);
        objectiveText = CreateLabel(objPanel, "ObjBody", "Reach the relay tower and scavenge 3 supply crates.", new Vector2(14f, -40f), 18, textPrimary);
        objectiveText.alignment = TextAnchor.UpperLeft;
        objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
        objectiveText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform alertsPanel = CreatePanel(canvasObj.transform, "AlertsPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(420f, 150f), new Color(0.1f, 0.11f, 0.1f, 0.56f));
        alertText = CreateLabel(alertsPanel, "AlertText", "NO IMMEDIATE THREATS", new Vector2(14f, -14f), 16, textMuted);
        alertText.alignment = TextAnchor.UpperLeft;

        RectTransform notificationsPanel = CreatePanel(canvasObj.transform, "NotifyPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(520f, 120f), new Color(0.09f, 0.1f, 0.09f, 0.4f));
        for (int i = 0; i < 3; i++)
        {
            Text line = CreateLabel(notificationsPanel, $"Notify_{i}", "", new Vector2(14f, -14f - i * 30f), 16, textPrimary);
            notificationLines.Add(line);
        }

        inventoryRoot = CreatePanel(canvasObj.transform, "InventoryStrip", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(460f, 84f), new Color(0.1f, 0.1f, 0.09f, 0.52f));
        for (int i = 0; i < 5; i++)
        {
            RectTransform slot = CreatePanel(inventoryRoot, $"Slot_{i}", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f + i * 88f, 12f), new Vector2(76f, 60f), new Color(0.18f, 0.18f, 0.16f, 0.9f));
            CreateLabel(slot, "SlotKey", (i + 1).ToString(), new Vector2(6f, -4f), 12, textMuted);
        }

        CreateCrosshair(canvasObj.transform);
        scanline = CreateRectImage(canvasObj.transform, "Scanline", new Vector2(0f, 0f), new Vector2(1920f, 10f), new Color(0.78f, 0.84f, 0.78f, 0.08f));
        scanline.rectTransform.anchorMin = new Vector2(0f, 0f);
        scanline.rectTransform.anchorMax = new Vector2(1f, 0f);
        scanline.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        PushNotification("SYSTEM ONLINE");
        PushNotification("SCAVENGER LINK ESTABLISHED");
        hudBuilt = true;
    }

    private void EnsureHudBuilt()
    {
        if (hudBuilt && rootCanvas != null)
        {
            return;
        }

        BuildHud();
    }

    private void HideLegacyHud()
    {
        SurvivalWeaponHud[] legacyHuds = FindObjectsByType<SurvivalWeaponHud>(FindObjectsSortMode.None);
        for (int i = 0; i < legacyHuds.Length; i++)
        {
            if (legacyHuds[i] != null)
            {
                legacyHuds[i].enabled = false;
                legacyHuds[i].gameObject.SetActive(false);
            }
        }

        HideObjectByName("WeaponHUD");
        HideObjectByName("PlayerHealthBar");
        HideObjectByName("Crosshair");
    }

    private static void HideObjectByName(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private void UpdateVitals()
    {
        if (playerStats != null && healthFill != null)
        {
            float hp = Mathf.Clamp01(playerStats.CurrentHealth / Mathf.Max(1f, playerStats.MaxHealth));
            healthFill.fillAmount = hp;
            healthFill.color = Color.Lerp(healthColor, accentWarning, 1f - hp);
        }

        bool sprinting = inputHandler != null && inputHandler.SprintHeld && inputHandler.MoveInput.sqrMagnitude > 0.1f;
        float delta = sprinting ? -sprintDrainPerSecond : recoverPerSecond;
        stamina = Mathf.Clamp(stamina + delta * Time.deltaTime, 0f, maxStamina);

        if (staminaFill != null)
        {
            float s = stamina / Mathf.Max(1f, maxStamina);
            staminaFill.fillAmount = s;
            staminaFill.color = Color.Lerp(accentWarning, staminaColor, s);
        }
    }

    private void UpdateCombatMode()
    {
        if (Time.time < nextCombatCheck)
        {
            return;
        }

        nextCombatCheck = Time.time + combatCheckInterval;
        bool threatNear = false;

        ZombieAgent[] zombies = FindObjectsByType<ZombieAgent>(FindObjectsSortMode.None);
        if (playerStats != null)
        {
            for (int i = 0; i < zombies.Length; i++)
            {
                ZombieAgent z = zombies[i];
                if (z == null || z.IsDead)
                {
                    continue;
                }

                float d = Vector3.Distance(z.transform.position, playerStats.transform.position);
                if (d <= combatDistance)
                {
                    threatNear = true;
                    break;
                }
            }
        }

        if (combatMode == threatNear)
        {
            return;
        }

        combatMode = threatNear;
        modeText.text = combatMode ? "MODE: COMBAT" : "MODE: EXPLORATION";
        modeText.color = combatMode ? accentWarning : accentSafe;
        alertText.text = combatMode ? "HOSTILE CONTACT NEARBY" : "NO IMMEDIATE THREATS";
        alertText.color = combatMode ? accentWarning : textMuted;
        PushNotification(combatMode ? "ALERT: ZOMBIES DETECTED" : "THREAT LEVEL LOWERED");
    }

    private void UpdateAtmosphere()
    {
        float pulse = 1f + Mathf.Sin(Time.time * panelPulseSpeed) * panelPulseAmount;
        if (hudGroup != null)
        {
            hudGroup.alpha = combatMode ? 0.96f : 0.9f + 0.05f * pulse;
        }

        if (mapFog != null)
        {
            float fogAlpha = combatMode ? 0.42f : 0.55f + Mathf.Sin(Time.time * 0.8f) * 0.04f;
            Color c = mapFog.color;
            c.a = Mathf.Clamp01(fogAlpha);
            mapFog.color = c;
        }

        if (useAnimatedScanline && scanline != null)
        {
            float y = Mathf.Repeat(Time.time * 240f, 1080f);
            scanline.rectTransform.anchoredPosition = new Vector2(0f, y);
            Color c = scanline.color;
            c.a = combatMode ? 0.14f : 0.08f;
            scanline.color = c;
        }
    }

    private void UpdateNotifications()
    {
        notificationTimer -= Time.deltaTime;
        if (notificationTimer > 0f)
        {
            return;
        }

        notificationTimer = 2.1f;
        while (notificationQueue.Count > 3)
        {
            notificationQueue.Dequeue();
        }

        int index = 0;
        foreach (string msg in notificationQueue)
        {
            if (index >= notificationLines.Count)
            {
                break;
            }

            notificationLines[index].text = msg;
            notificationLines[index].color = index == notificationQueue.Count - 1 ? textPrimary : textMuted;
            index++;
        }

        for (int i = index; i < notificationLines.Count; i++)
        {
            notificationLines[i].text = string.Empty;
        }
    }

    private void SubscribeWeaponEvents()
    {
        if (weaponManager == null)
        {
            return;
        }

        weaponManager.WeaponChanged += OnWeaponChanged;
        weaponManager.AmmoChanged += OnAmmoChanged;
    }

    private void UnsubscribeWeaponEvents()
    {
        if (weaponManager == null)
        {
            return;
        }

        weaponManager.WeaponChanged -= OnWeaponChanged;
        weaponManager.AmmoChanged -= OnAmmoChanged;
    }

    private void OnWeaponChanged(SurvivalWeaponDefinition def, int ammo, int reserve)
    {
        if (!hudBuilt)
        {
            EnsureHudBuilt();
        }

        if (weaponText == null || ammoText == null)
        {
            return;
        }

        weaponText.text = def != null ? def.DisplayName.ToUpperInvariant() : "NO WEAPON";
        ammoText.text = def != null && def.UsesAmmo ? $"{ammo} / {reserve}" : "MELEE";
        PushNotification(def != null ? $"EQUIPPED: {def.DisplayName}" : "NO WEAPON EQUIPPED");
    }

    private void OnAmmoChanged(int ammo, int reserve, bool usesAmmo)
    {
        if (!hudBuilt)
        {
            EnsureHudBuilt();
        }

        if (ammoText == null)
        {
            return;
        }

        ammoText.text = usesAmmo ? $"{ammo} / {reserve}" : "MELEE";
        if (usesAmmo && ammo <= 3 && alertText != null)
        {
            alertText.text = "LOW AMMO";
            alertText.color = accentWarning;
        }
    }

    private void RefreshWeaponUi()
    {
        if (!hudBuilt)
        {
            EnsureHudBuilt();
        }

        if (weaponText == null || ammoText == null)
        {
            return;
        }

        if (weaponManager == null)
        {
            weaponText.text = "NO WEAPON";
            ammoText.text = "--";
            return;
        }

        SurvivalWeaponDefinition def = weaponManager.CurrentWeapon;
        if (def == null)
        {
            weaponText.text = "NO WEAPON";
            ammoText.text = "--";
            return;
        }

        weaponText.text = def.DisplayName.ToUpperInvariant();
        ammoText.text = def.UsesAmmo ? $"{weaponManager.CurrentAmmo} / {weaponManager.ReserveAmmo}" : "MELEE";
    }

    private void PushNotification(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        notificationQueue.Enqueue(message.ToUpperInvariant());
        while (notificationQueue.Count > 3)
        {
            notificationQueue.Dequeue();
        }
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panelObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(parent, false);
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = panelObj.GetComponent<Image>();
        img.color = color;

        GameObject edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));
        edge.transform.SetParent(panelObj.transform, false);
        RectTransform edgeRect = edge.GetComponent<RectTransform>();
        edgeRect.anchorMin = Vector2.zero;
        edgeRect.anchorMax = Vector2.one;
        edgeRect.offsetMin = Vector2.zero;
        edgeRect.offsetMax = Vector2.zero;
        Image edgeImg = edge.GetComponent<Image>();
        edgeImg.color = new Color(panelEdge.r, panelEdge.g, panelEdge.b, 0.22f);

        return rect;
    }

    private Text CreateLabel(Transform parent, string name, string content, Vector2 anchoredPos, int fontSize, Color color)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(parent, false);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(1000f, 100f);

        Text text = textObj.GetComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        return text;
    }

    private Image CreateFramedBar(Transform parent, string rootName, string label, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        RectTransform panel = CreatePanel(parent, rootName, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPos, size, new Color(0.08f, 0.09f, 0.08f, 0.92f));
        CreateLabel(panel, "Label", label, new Vector2(6f, -3f), 11, textMuted);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(panel, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -14f);

        Image fill = fillObj.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        fill.color = fillColor;
        return fill;
    }

    private static Image CreateRectImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject imageObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObj.transform.SetParent(parent, false);
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = imageObj.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private void CreateMapGrid(RectTransform mapSurface)
    {
        for (int i = 0; i < 6; i++)
        {
            Image h = CreateRectImage(mapSurface, $"GridH_{i}", new Vector2(0f, -i * 28f), new Vector2(mapSurface.sizeDelta.x, 1f), new Color(0.7f, 0.75f, 0.7f, 0.08f));
            h.raycastTarget = false;
        }

        for (int i = 0; i < 9; i++)
        {
            GameObject vObj = new GameObject($"GridV_{i}", typeof(RectTransform), typeof(Image));
            vObj.transform.SetParent(mapSurface, false);
            RectTransform rect = vObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(i * 34f, 0f);
            rect.sizeDelta = new Vector2(1f, 0f);
            vObj.GetComponent<Image>().color = new Color(0.7f, 0.75f, 0.7f, 0.08f);
        }
    }

    private void CreateCrosshair(Transform parent)
    {
        RectTransform root = CreatePanel(parent, "CrosshairRoot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 42f), new Color(0f, 0f, 0f, 0f));
        CreateRectImage(root, "Top", new Vector2(20f, -6f), new Vector2(2f, 10f), textPrimary);
        CreateRectImage(root, "Bottom", new Vector2(20f, -26f), new Vector2(2f, 10f), textPrimary);
        CreateRectImage(root, "Left", new Vector2(6f, -20f), new Vector2(10f, 2f), textPrimary);
        CreateRectImage(root, "Right", new Vector2(26f, -20f), new Vector2(10f, 2f), textPrimary);
    }
}
