using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Manages health with damage and healing mechanics, firing events at critical thresholds.
/// Optionally creates its own UI display - enable Show UI for text and/or Show Bar for a health bar.
///
/// MULTI-SCENE SUPPORT: Enable Persist Across Scenes to save health when loading a new scene.
/// The manager is recreated per scene but the health value carries over automatically.
///
/// Common use: Player or enemy health systems, destructible objects, shield mechanics, or boss health bars.
/// </summary>
public class GameHealthManager : MonoBehaviour
{
    /// <summary>
    /// Animation style for value change feedback
    /// </summary>
    public enum ValueAnimation { None, PunchScale, FadeFlash }

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int lowHealthThreshold = 25;

    [Header("Scene Persistence")]
    [Tooltip("Save health when loading a new scene. Each scene can have its own manager — only the value carries over.")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Header("UI Text (Optional)")]
    [Tooltip("Enable to create a self-contained UI text display for health")]
    [SerializeField] private bool showUI = false;

    [Tooltip("Text shown before the value (e.g. 'HP: ', 'Health: ')")]
    [SerializeField] private string labelPrefix = "HP: ";

    [Tooltip("Show health as 'current / max' format (e.g. HP: 75 / 100)")]
    [SerializeField] private bool showMaxInText = true;

    [Tooltip("Position of the text on screen (anchor position, top-left origin)")]
    [SerializeField] private Vector2 textPosition = new Vector2(60, -40);

    [Tooltip("Font size for the display text")]
    [SerializeField] private float fontSize = 40f;

    [Tooltip("Text alignment")]
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Left;

    [Tooltip("Text color")]
    [SerializeField] private Color textColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    [Tooltip("Custom font (leave empty for TMP default)")]
    [SerializeField] private TMP_FontAsset customFont;

    [Header("Text Animation")]
    [Tooltip("Animation played when health changes")]
    [SerializeField] private ValueAnimation valueAnimation = ValueAnimation.PunchScale;

    [Tooltip("Duration of the value change animation")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("Strength of the value change animation")]
    [SerializeField] private float animationStrength = 0.2f;

    [Header("UI Bar (Optional)")]
    [Tooltip("Enable to create a fill bar display for health")]
    [SerializeField] private bool showBar = false;

    [Tooltip("Position of the bar on screen (anchor position, top-left origin)")]
    [SerializeField] private Vector2 barPosition = new Vector2(60, -80);

    [Tooltip("Size of the bar in pixels")]
    [SerializeField] private Vector2 barSize = new Vector2(200, 20);

    [Tooltip("Background color of the bar")]
    [SerializeField] private Color barBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [Tooltip("Color gradient for the bar fill (left = empty, right = full). Use Blend for smooth transitions or Fixed for hard color bands")]
    [SerializeField] private Gradient barGradient = DefaultBarGradient();

    [Tooltip("Enable smooth animation when bar value changes")]
    [SerializeField] private bool animateBar = true;

    [Tooltip("Duration of the bar fill animation")]
    [SerializeField] private float barAnimationDuration = 0.2f;

    [Header("Health Events")]
    /// <summary>
    /// Fires whenever health value changes (both damage and healing), passing current and max health as parameters
    /// </summary>
    public UnityEvent<int, int> onHealthChanged;
    /// <summary>
    /// Fires when damage is taken
    /// </summary>
    public UnityEvent onDamageReceived;
    /// <summary>
    /// Fires when health is gained through healing
    /// </summary>
    public UnityEvent onHealthGained;
    /// <summary>
    /// Fires when health drops to or below the low health threshold
    /// </summary>
    public UnityEvent onLowHealthReached;
    /// <summary>
    /// Fires when health recovers above the low health threshold
    /// </summary>
    public UnityEvent onLowHealthRecovered;
    /// <summary>
    /// Fires when health reaches zero
    /// </summary>
    public UnityEvent onDeath;
    /// <summary>
    /// Fires when health is restored above zero after death
    /// </summary>
    public UnityEvent onRevived;

    private bool isLowHealth = false;
    private bool isDead = false;

    // UI runtime references
    private Canvas uiCanvas;
    private TextMeshProUGUI uiText;
    private Tween textAnimationTween;
    private Color baseTextColor;
    private Slider barSlider;
    private Image barFillImage;
    private Tween barAnimationTween;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int LowHealthThreshold => lowHealthThreshold;
    public bool IsLowHealth => isLowHealth;
    public bool IsDead => isDead;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private static Gradient DefaultBarGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.9f, 0.15f, 0.1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.95f, 0.2f, 1f), 0.5f),
                new GradientColorKey(new Color(0.25f, 0.9f, 0.1f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    private void SyncToGameData()
    {
        if (persistAcrossScenes)
            GameData.Instance.SetInt(GameData.HEALTH_SLOT, currentHealth);
    }

    private void Start()
    {
        // If persistence is enabled, read the carried-over value (or use Inspector default on first load)
        if (persistAcrossScenes)
            currentHealth = GameData.Instance.GetInt(GameData.HEALTH_SLOT, currentHealth);

        // Ensure health starts within valid range
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        SyncToGameData();
        CheckHealthStates();

        // Create Canvas if any UI is enabled
        if (showUI || showBar)
        {
            CreateCanvas();
        }

        if (showUI)
        {
            CreateTextUI();
            UpdateUIText();
        }

        if (showBar)
        {
            CreateBarUI();
            UpdateBar();
        }

        // Fire initial health event
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Take damage and reduce health
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead || damageAmount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        SyncToGameData();

        onDamageReceived.Invoke();
        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateUIText();
        PlayTextAnimation();
        UpdateBar();

        // Check if we crossed the low health threshold going down (but not if we're dying)
        if (previousHealth > lowHealthThreshold && currentHealth <= lowHealthThreshold && currentHealth > 0)
        {
            isLowHealth = true;
            onLowHealthReached.Invoke();
        }

        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            onDeath.Invoke();
        }
    }

    /// <summary>
    /// Heal and increase health
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isDead || healAmount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        SyncToGameData();

        onHealthGained.Invoke();
        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateUIText();
        PlayTextAnimation();
        UpdateBar();

        CheckHealthStates();

        // Check if we recovered from low health
        if (previousHealth <= lowHealthThreshold && currentHealth > lowHealthThreshold)
        {
            isLowHealth = false;
            onLowHealthRecovered.Invoke();
        }
    }

    /// <summary>
    /// Set health to specific value
    /// </summary>
    public void SetHealth(int newHealth)
    {
        int previousHealth = currentHealth;
        bool wasDead = isDead;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        SyncToGameData();

        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateUIText();
        PlayTextAnimation();
        UpdateBar();

        // Determine if this was damage or healing
        if (currentHealth < previousHealth)
        {
            onDamageReceived.Invoke();
        }
        else if (currentHealth > previousHealth)
        {
            onHealthGained.Invoke();
        }

        // Check threshold crossings (before updating flags)
        if (previousHealth > lowHealthThreshold && currentHealth <= lowHealthThreshold && currentHealth > 0)
        {
            isLowHealth = true;
            onLowHealthReached.Invoke();
        }
        else if (previousHealth <= lowHealthThreshold && currentHealth > lowHealthThreshold)
        {
            isLowHealth = false;
            onLowHealthRecovered.Invoke();
        }

        // Check for death/revival (using saved flag)
        if (currentHealth <= 0 && !wasDead)
        {
            isDead = true;
            isLowHealth = false;
            onDeath.Invoke();
        }
        else if (currentHealth > 0 && wasDead)
        {
            isDead = false;
            onRevived.Invoke();
        }

        CheckHealthStates();
    }

    /// <summary>
    /// Restore to full health
    /// </summary>
    public void FullHeal()
    {
        SetHealth(maxHealth);
    }

    /// <summary>
    /// Set maximum health and adjust current health if needed
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        if (newMaxHealth <= 0) return;

        maxHealth = newMaxHealth;

        // Don't let current health exceed new max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
            SyncToGameData();
        }

        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateUIText();
        UpdateBar();
    }

    /// <summary>
    /// Set the low health threshold
    /// </summary>
    public void SetLowHealthThreshold(int newThreshold)
    {
        lowHealthThreshold = Mathf.Clamp(newThreshold, 0, maxHealth);
        CheckHealthStates();
    }

    private void CheckHealthStates()
    {
        isLowHealth = currentHealth <= lowHealthThreshold && currentHealth > 0;
        isDead = currentHealth <= 0;
    }

    /// <summary>
    /// For testing - add damage over time
    /// </summary>
    public void StartDamageOverTime(int damagePerSecond, float duration)
    {
        StartCoroutine(DamageOverTimeCoroutine(damagePerSecond, duration));
    }

    private System.Collections.IEnumerator DamageOverTimeCoroutine(int damagePerSecond, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !isDead)
        {
            yield return new WaitForSeconds(1f);
            TakeDamage(damagePerSecond);
            elapsed += 1f;
        }
    }

    #region UI Creation

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("HealthUI_Canvas");
        canvasObj.transform.SetParent(transform);

        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateTextUI()
    {
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = textPosition;
        rectTransform.sizeDelta = new Vector2(400, 50);

        uiText = textObj.AddComponent<TextMeshProUGUI>();
        uiText.fontSize = fontSize;
        uiText.alignment = textAlignment;
        uiText.color = textColor;
        uiText.overflowMode = TextOverflowModes.Overflow;
        uiText.enableWordWrapping = false;

        if (customFont != null)
        {
            uiText.font = customFont;
        }

        baseTextColor = textColor;
    }

    private void CreateBarUI()
    {
        // Bar root with Slider
        GameObject barObj = new GameObject("HealthBar");
        barObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 1);
        barRect.anchoredPosition = barPosition;
        barRect.sizeDelta = barSize;

        barSlider = barObj.AddComponent<Slider>();
        barSlider.minValue = 0;
        barSlider.maxValue = 1;
        barSlider.value = 1;
        barSlider.interactable = false;
        barSlider.transition = Selectable.Transition.None;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = barBackgroundColor;

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(barObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        barFillImage = fillObj.AddComponent<Image>();

        // Wire up slider
        barSlider.fillRect = fillRect;
    }

    #endregion

    #region UI Updates

    private void UpdateUIText()
    {
        if (uiText != null)
        {
            if (showMaxInText)
                uiText.text = labelPrefix + currentHealth + " / " + maxHealth;
            else
                uiText.text = labelPrefix + currentHealth.ToString();
        }
    }

    private void UpdateBar()
    {
        if (barSlider == null || maxHealth <= 0) return;

        float fillPercent = Mathf.Clamp01((float)currentHealth / maxHealth);

        if (animateBar)
        {
            barAnimationTween?.Kill();
            barAnimationTween = DOTween.To(
                () => barSlider.value,
                x =>
                {
                    barSlider.value = x;
                    if (barFillImage != null)
                        barFillImage.color = barGradient.Evaluate(x);
                },
                fillPercent,
                barAnimationDuration
            ).SetUpdate(true);
        }
        else
        {
            barSlider.value = fillPercent;
            if (barFillImage != null)
                barFillImage.color = barGradient.Evaluate(fillPercent);
        }
    }

    private void PlayTextAnimation()
    {
        if (uiText == null || valueAnimation == ValueAnimation.None) return;

        textAnimationTween?.Kill();

        switch (valueAnimation)
        {
            case ValueAnimation.PunchScale:
                uiText.transform.localScale = Vector3.one;
                textAnimationTween = uiText.transform.DOPunchScale(
                    Vector3.one * animationStrength,
                    animationDuration,
                    10,
                    1
                ).SetUpdate(true);
                break;

            case ValueAnimation.FadeFlash:
                uiText.color = baseTextColor;
                float minAlpha = Mathf.Clamp01(baseTextColor.a * (1f - Mathf.Clamp01(animationStrength) * 3f));
                Color fadeColor = new Color(baseTextColor.r, baseTextColor.g, baseTextColor.b, minAlpha);
                textAnimationTween = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => uiText.color,
                        x => uiText.color = x,
                        fadeColor,
                        animationDuration * 0.5f
                    ))
                    .Append(DOTween.To(
                        () => uiText.color,
                        x => uiText.color = x,
                        baseTextColor,
                        animationDuration * 0.5f
                    ))
                    .SetUpdate(true);
                break;
        }
    }

    private void OnDestroy()
    {
        textAnimationTween?.Kill();
        barAnimationTween?.Kill();
    }

    #endregion

    #region Editor Preview

#if UNITY_EDITOR
    /// <summary>
    /// Creates a preview Canvas in the editor for positioning the UI display
    /// </summary>
    public void CreatePreviewUI()
    {
        DestroyPreviewUI();

        if (!showUI && !showBar) return;

        // Create preview Canvas
        GameObject canvasObj = new GameObject("HealthUI_PREVIEW");
        canvasObj.transform.SetParent(transform);
        canvasObj.hideFlags = HideFlags.DontSave;

        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        // Create text preview
        if (showUI)
        {
            GameObject textObj = new GameObject("HealthText_PREVIEW");
            textObj.transform.SetParent(canvasObj.transform, false);
            textObj.hideFlags = HideFlags.DontSave;

            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = textPosition;
            rectTransform.sizeDelta = new Vector2(400, 50);

            uiText = textObj.AddComponent<TextMeshProUGUI>();
            uiText.fontSize = fontSize;
            uiText.alignment = textAlignment;
            uiText.color = textColor;
            uiText.overflowMode = TextOverflowModes.Overflow;
            uiText.enableWordWrapping = false;

            if (showMaxInText)
                uiText.text = labelPrefix + "75 / 100";
            else
                uiText.text = labelPrefix + "75";

            if (customFont != null)
            {
                uiText.font = customFont;
            }
        }

        // Create bar preview
        if (showBar)
        {
            GameObject barObj = new GameObject("HealthBar_PREVIEW");
            barObj.transform.SetParent(canvasObj.transform, false);
            barObj.hideFlags = HideFlags.DontSave;

            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 1);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 1);
            barRect.anchoredPosition = barPosition;
            barRect.sizeDelta = barSize;

            barSlider = barObj.AddComponent<Slider>();
            barSlider.minValue = 0;
            barSlider.maxValue = 1;
            barSlider.interactable = false;
            barSlider.transition = Selectable.Transition.None;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(barObj.transform, false);
            bgObj.hideFlags = HideFlags.DontSave;
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = barBackgroundColor;

            // Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(barObj.transform, false);
            fillAreaObj.hideFlags = HideFlags.DontSave;
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            fillObj.hideFlags = HideFlags.DontSave;
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            barFillImage = fillObj.AddComponent<Image>();

            barSlider.fillRect = fillRect;

            // Show at 75% fill for preview
            barSlider.value = 0.75f;
            barFillImage.color = barGradient.Evaluate(0.75f);
        }
    }

    /// <summary>
    /// Updates the preview UI to reflect current Inspector values
    /// </summary>
    public void UpdatePreviewUI()
    {
        // Update text preview
        if (uiText != null)
        {
            RectTransform rectTransform = uiText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = textPosition;

            uiText.fontSize = fontSize;
            uiText.alignment = textAlignment;
            uiText.color = textColor;

            if (showMaxInText)
                uiText.text = labelPrefix + "75 / 100";
            else
                uiText.text = labelPrefix + "75";

            if (customFont != null)
                uiText.font = customFont;
        }

        // Update bar preview
        if (barSlider != null)
        {
            RectTransform barRect = barSlider.GetComponent<RectTransform>();
            barRect.anchoredPosition = barPosition;
            barRect.sizeDelta = barSize;

            // Update background color
            Transform bgTransform = barSlider.transform.Find("Background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                    bgImage.color = barBackgroundColor;
            }

            // Update fill color at 75%
            barSlider.value = 0.75f;
            if (barFillImage != null)
                barFillImage.color = barGradient.Evaluate(0.75f);
        }
    }

    /// <summary>
    /// Destroys the preview Canvas
    /// </summary>
    public void DestroyPreviewUI()
    {
        if (uiCanvas != null && uiCanvas.gameObject.name.Contains("PREVIEW"))
        {
            DestroyImmediate(uiCanvas.gameObject);
        }

        uiCanvas = null;
        uiText = null;
        barSlider = null;
        barFillImage = null;
    }
#endif

    #endregion
}
