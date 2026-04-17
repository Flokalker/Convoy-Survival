using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class ZombieHealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private Vector3 canvasOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float visibleDurationAfterHit = 1.2f;

    private Camera mainCamera;
    private Coroutine hideRoutine;

    private void Reset()
    {
        health = GetComponent<Health>();
    }

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        mainCamera = Camera.main;
        EnsureUi();
        SetVisible(!hideWhenFull);
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.HealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        if (worldSpaceCanvas == null || mainCamera == null)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            return;
        }

        worldSpaceCanvas.transform.position = transform.position + canvasOffset;
        Vector3 lookForward = worldSpaceCanvas.transform.position - mainCamera.transform.position;
        lookForward.y = 0f;
        if (lookForward.sqrMagnitude > 0.001f)
        {
            worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(lookForward.normalized, Vector3.up);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = current / Mathf.Max(1f, max);
        }

        if (hideWhenFull && current >= max)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleDurationAfterHit);

        if (health != null && hideWhenFull && !health.IsDead && health.CurrentHealth >= health.MaxHealth)
        {
            SetVisible(false);
        }
    }

    private void EnsureUi()
    {
        if (worldSpaceCanvas != null && fillImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("ZombieHealthCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = canvasOffset;

        worldSpaceCanvas = canvasObject.GetComponent<Canvas>();
        worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
        worldSpaceCanvas.worldCamera = Camera.main;
        worldSpaceCanvas.sortingOrder = 5;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1.4f, 0.28f);
        canvasRect.localScale = Vector3.one * 0.02f;

        GameObject bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasObject.transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(6f, 6f);
        fillRect.offsetMax = new Vector2(-6f, -6f);

        fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = health != null ? health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) : 1f;
    }

    private void SetVisible(bool visible)
    {
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.enabled = visible;
        }
    }
}
