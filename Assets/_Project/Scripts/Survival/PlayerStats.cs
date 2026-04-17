using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool infiniteHealth;

    [Header("Hit Feedback")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private CanvasGroup bloodSplatterOverlay;
    [SerializeField, Min(0f)] private float bloodFlashAlpha = 0.6f;
    [SerializeField, Min(0.05f)] private float bloodFadeDuration = 0.35f;

    [Header("Death")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private bool resetSceneOnDeath = false;
    [SerializeField, Min(0f)] private float deathResetDelay = 2f;
    
    [Header("Auto UI")]
    [SerializeField] private bool autoCreateUiIfMissing = true;

    private Coroutine bloodFadeCoroutine;
    private RectTransform healthFillRect;
    private float healthFillFullWidth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool InfiniteHealth => infiniteHealth;

    private void Awake()
    {
        if (autoCreateUiIfMissing)
        {
            EnsureUiReferences();
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUi();

        if (bloodSplatterOverlay != null)
        {
            bloodSplatterOverlay.alpha = 0f;
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (infiniteHealth || IsDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UpdateHealthUi();
        TriggerBloodHitEffect();

        if (currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUi();
    }

    public void SetInfiniteHealth(bool enabledState)
    {
        infiniteHealth = enabledState;
        if (infiniteHealth && currentHealth < maxHealth)
        {
            currentHealth = maxHealth;
            UpdateHealthUi();
        }
    }

    public void SetResetSceneOnDeath(bool enabledState)
    {
        resetSceneOnDeath = enabledState;
    }

    private void UpdateHealthUi()
    {
        float normalized = currentHealth / Mathf.Max(1f, maxHealth);

        if (healthFillImage != null)
        {
            if (healthFillImage.type == Image.Type.Filled)
            {
                healthFillImage.fillAmount = normalized;
            }
            else if (healthFillRect != null)
            {
                Vector2 size = healthFillRect.sizeDelta;
                size.x = healthFillFullWidth * normalized;
                healthFillRect.sizeDelta = size;
            }
        }
    }

    private void TriggerBloodHitEffect()
    {
        if (bloodSplatterOverlay == null)
        {
            return;
        }

        if (bloodFadeCoroutine != null)
        {
            StopCoroutine(bloodFadeCoroutine);
        }

        bloodSplatterOverlay.alpha = bloodFlashAlpha;
        bloodFadeCoroutine = StartCoroutine(FadeBloodOverlay());
    }

    private IEnumerator FadeBloodOverlay()
    {
        float elapsed = 0f;
        float startAlpha = bloodSplatterOverlay.alpha;

        while (elapsed < bloodFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bloodFadeDuration);
            bloodSplatterOverlay.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        bloodSplatterOverlay.alpha = 0f;
        bloodFadeCoroutine = null;
    }

    private void HandleDeath()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (resetSceneOnDeath)
        {
            StartCoroutine(ReloadSceneAfterDelay());
        }
    }

    private IEnumerator ReloadSceneAfterDelay()
    {
        if (deathResetDelay > 0f)
        {
            yield return new WaitForSeconds(deathResetDelay);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void EnsureUiReferences()
    {
        if (healthFillImage != null && bloodSplatterOverlay != null)
        {
            return;
        }

        Canvas canvas = GameObject.Find("CombatHUD")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("CombatHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (healthFillImage == null)
        {
            healthFillImage = BuildHealthBar(canvas.transform);
            healthFillRect = healthFillImage.rectTransform;
            healthFillFullWidth = healthFillRect.sizeDelta.x;
        }
        else if (healthFillImage != null)
        {
            healthFillRect = healthFillImage.rectTransform;
            healthFillFullWidth = healthFillRect.sizeDelta.x > 0f ? healthFillRect.sizeDelta.x : 294f;
        }

        if (bloodSplatterOverlay == null)
        {
            bloodSplatterOverlay = BuildBloodOverlay(canvas.transform);
        }

        if (gameOverScreen == null)
        {
            gameOverScreen = BuildGameOverScreen(canvas.transform);
        }
    }

    private static Image BuildHealthBar(Transform canvasRoot)
    {
        GameObject barRoot = new GameObject("PlayerHealthBar", typeof(RectTransform), typeof(Image));
        barRoot.transform.SetParent(canvasRoot, false);
        RectTransform rootRect = barRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(28f, -28f);
        rootRect.sizeDelta = new Vector2(300f, 28f);

        Image bg = barRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(barRoot.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(3f, 0f);
        fillRect.sizeDelta = new Vector2(294f, 0f);

        Image fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        fill.type = Image.Type.Simple;
        fill.fillAmount = 1f;
        return fill;
    }

    private static CanvasGroup BuildBloodOverlay(Transform canvasRoot)
    {
        GameObject overlay = new GameObject("BloodSplatterOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(canvasRoot, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0.55f, 0f, 0f, 0.35f);
        image.raycastTarget = false;

        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        return group;
    }

    private static GameObject BuildGameOverScreen(Transform canvasRoot)
    {
        GameObject screen = new GameObject("GameOverScreen", typeof(RectTransform), typeof(Image));
        screen.transform.SetParent(canvasRoot, false);
        RectTransform rect = screen.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = screen.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(screen.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.2f, 0.4f);
        labelRect.anchorMax = new Vector2(0.8f, 0.6f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = "GAME OVER";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 64;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        screen.SetActive(false);
        return screen;
    }
}
