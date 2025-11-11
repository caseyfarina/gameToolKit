using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Automatically creates and manages UI elements for displaying game data (score, health, timer, inventory).
/// Receives data from Game Managers via UnityEvents - does NOT manage game state itself.
/// Enable editor preview to see and adjust UI layout before runtime.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI Element Toggles")]
    [Tooltip("Enable score display")]
    [SerializeField] private bool showScore = true;

    [Tooltip("Enable health text display")]
    [SerializeField] private bool showHealthText = true;

    [Tooltip("Enable health bar display")]
    [SerializeField] private bool showHealthBar = true;

    [Tooltip("Enable timer display")]
    [SerializeField] private bool showTimer = true;

    [Tooltip("Enable inventory display")]
    [SerializeField] private bool showInventory = true;

    [Header("UI Layout Settings")]
    [Tooltip("Score position (anchor position)")]
    [SerializeField] private Vector2 scorePosition = new Vector2(60, -40);

    [Tooltip("Health text position")]
    [SerializeField] private Vector2 healthTextPosition = new Vector2(60, -80);

    [Tooltip("Health bar position")]
    [SerializeField] private Vector2 healthBarPosition = new Vector2(60, -134.8f);

    [Tooltip("Health bar size")]
    [SerializeField] private Vector2 healthBarSize = new Vector2(430.8f, 20);

    [Tooltip("Timer position")]
    [SerializeField] private Vector2 timerPosition = new Vector2(60, -160);

    [Tooltip("Inventory position")]
    [SerializeField] private Vector2 inventoryPosition = new Vector2(60, -200);

    [Header("UI Styling")]
    [Tooltip("Font size for score text")]
    [SerializeField] private int scoreFontSize = 40;

    [Tooltip("Font size for health text")]
    [SerializeField] private int healthFontSize = 40;

    [Tooltip("Font size for timer text")]
    [SerializeField] private int timerFontSize = 40;

    [Tooltip("Font size for inventory text")]
    [SerializeField] private int inventoryFontSize = 40;

    [Tooltip("Text color")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Health bar colors")]
    [SerializeField] private Color healthColorHigh = Color.green;
    [SerializeField] private Color healthColorMid = Color.yellow;
    [SerializeField] private Color healthColorLow = Color.red;

    [Header("Score Animation")]
    [Tooltip("Enable punch animation when score changes")]
    [SerializeField] private bool animateScore = true;
    [Tooltip("Duration for score punch animation")]
    [SerializeField] private float scoreAnimationDuration = 0.3f;
    [Tooltip("Punch scale strength")]
    [SerializeField] private float scorePunchStrength = 0.2f;

    [Header("Health Animation")]
    [Tooltip("Enable smooth transitions for health bar")]
    [SerializeField] private bool animateHealth = true;
    [Tooltip("Duration for health bar transition")]
    [SerializeField] private float healthAnimationDuration = 0.2f;
    [Tooltip("Enable fade effect on health text when damaged")]
    [SerializeField] private bool animateHealthText = false;
    [Tooltip("Duration for health text fade")]
    [SerializeField] private float healthTextAnimationDuration = 0.3f;

    [Header("Timer Animation")]
    [Tooltip("Enable pulse animation on timer")]
    [SerializeField] private bool animateTimer = false;
    [Tooltip("Pulse frequency (seconds)")]
    [SerializeField] private float timerPulseInterval = 1f;
    [Tooltip("Pulse scale strength")]
    [SerializeField] private float timerPulseStrength = 0.1f;

    [Header("Inventory Animation")]
    [Tooltip("Enable animation when inventory changes")]
    [SerializeField] private bool animateInventory = true;
    [Tooltip("Animation type for inventory changes")]
    [SerializeField] private InventoryAnimationType inventoryAnimationType = InventoryAnimationType.PunchScale;
    [Tooltip("Duration for inventory animation")]
    [SerializeField] private float inventoryAnimationDuration = 0.3f;
    [Tooltip("Animation strength")]
    [SerializeField] private float inventoryAnimationStrength = 0.2f;

    public enum InventoryAnimationType
    {
        PunchScale,
        Fade,
        Bounce
    }

    [Header("Text Prefixes")]
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private string healthPrefix = "Health: ";
    [SerializeField] private string timerPrefix = "Time: ";
    [SerializeField] private string inventoryPrefix = "Items: ";

    [Header("Editor Preview")]
    [Tooltip("Enable to preview UI layout in editor (updates on value change)")]
    [SerializeField] private bool enableEditorPreview = false;

    [Header("Events")]
    /// <summary>
    /// Fires when any UI element is updated
    /// </summary>
    public UnityEvent onUIUpdated;

    // UI References (created at runtime or in editor preview)
    private Canvas canvas;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI healthText;
    private Slider healthBar;
    private Image healthBarFill;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI inventoryText;

    // Current values for display
    private int currentScore = 0;
    private int currentHealth = 100;
    private int maxHealth = 100;
    private float currentTime = 0f;
    private string inventoryItemType = "";
    private int inventoryCount = 0;

    // Animation tweens
    private Tween scoreAnimationTween;
    private Tween healthAnimationTween;
    private Tween healthTextAnimationTween;
    private Tween timerAnimationTween;
    private Tween inventoryAnimationTween;
    private float lastTimerPulse = 0f;

    void Start()
    {
        CreateUIElements();
        UpdateAllDisplays();
    }

    void OnValidate()
    {
        // Update preview in editor when values change
        if (enableEditorPreview && !Application.isPlaying)
        {
            #if UNITY_EDITOR
            // Use delayCall to avoid "SendMessage cannot be called during OnValidate" warnings
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null && enableEditorPreview && !Application.isPlaying)
                {
                    CreateOrUpdateEditorPreview();
                }
            };
            #endif
        }
        else if (!enableEditorPreview && !Application.isPlaying)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null && !enableEditorPreview && !Application.isPlaying)
                {
                    DestroyEditorPreview();
                }
            };
            #endif
        }
    }

    void OnDestroy()
    {
        // Clean up DOTween tweens
        scoreAnimationTween?.Kill();
        healthAnimationTween?.Kill();
        healthTextAnimationTween?.Kill();
        timerAnimationTween?.Kill();
        inventoryAnimationTween?.Kill();
    }

    #region UI Creation

    /// <summary>
    /// Creates all UI elements at runtime (toggles just enable/disable them)
    /// </summary>
    private void CreateUIElements()
    {
        // Create canvas if it doesn't exist
        canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameUI_Canvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Add and configure CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // Match width
            scaler.referencePixelsPerUnit = 100f;

            // Add GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create ALL UI elements regardless of toggles
        if (scoreText == null)
        {
            scoreText = CreateTextElement("Score", scorePosition, scoreFontSize);
        }

        if (healthText == null)
        {
            healthText = CreateTextElement("HealthText", healthTextPosition, healthFontSize);
        }

        if (healthBar == null)
        {
            CreateHealthBar();
        }

        if (timerText == null)
        {
            timerText = CreateTextElement("Timer", timerPosition, timerFontSize);
        }

        if (inventoryText == null)
        {
            inventoryText = CreateTextElement("Inventory", inventoryPosition, inventoryFontSize);
        }

        // Apply toggle states (enable/disable based on settings)
        ApplyToggleStates();
    }

    /// <summary>
    /// Enable or disable UI elements based on toggle settings
    /// </summary>
    private void ApplyToggleStates()
    {
        if (scoreText != null)
            scoreText.gameObject.SetActive(showScore);

        if (healthText != null)
            healthText.gameObject.SetActive(showHealthText);

        if (healthBar != null)
            healthBar.gameObject.SetActive(showHealthBar);

        if (timerText != null)
            timerText.gameObject.SetActive(showTimer);

        if (inventoryText != null)
            inventoryText.gameObject.SetActive(showInventory);
    }

    private TextMeshProUGUI CreateTextElement(string name, Vector2 position, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1); // Top-left
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = textColor;
        text.text = $"{name}: 0";

        return text;
    }

    private void CreateHealthBar()
    {
        GameObject barObj = new GameObject("HealthBar");
        barObj.transform.SetParent(canvas.transform, false);

        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 1);
        barRect.anchoredPosition = healthBarPosition;
        barRect.sizeDelta = healthBarSize;

        // Add slider component
        healthBar = barObj.AddComponent<Slider>();
        healthBar.minValue = 0;
        healthBar.maxValue = 1;
        healthBar.value = 1;

        // Create background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Create fill area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(barObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Create fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = healthColorHigh;

        // Wire up slider
        healthBar.fillRect = fillRect;
    }

    #endregion

    #region Editor Preview

    #if UNITY_EDITOR
    private void CreateOrUpdateEditorPreview()
    {
        // Find or create canvas
        canvas = GetComponentInChildren<Canvas>(true); // Include inactive objects
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameUI_Canvas_PREVIEW");
            canvasObj.transform.SetParent(transform);
            canvasObj.hideFlags = HideFlags.DontSave; // Don't save to scene
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Add and configure CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // Match width
            scaler.referencePixelsPerUnit = 100f;
        }
        else
        {
            canvas.gameObject.hideFlags = HideFlags.DontSave;
        }

        // Find existing preview elements to reuse
        if (scoreText == null && canvas != null)
        {
            Transform existingScore = canvas.transform.Find("Score_PREVIEW");
            if (existingScore != null)
                scoreText = existingScore.GetComponent<TextMeshProUGUI>();
        }

        if (healthText == null && canvas != null)
        {
            Transform existingHealth = canvas.transform.Find("HealthText_PREVIEW");
            if (existingHealth != null)
                healthText = existingHealth.GetComponent<TextMeshProUGUI>();
        }

        if (healthBar == null && canvas != null)
        {
            Transform existingBar = canvas.transform.Find("HealthBar_PREVIEW");
            if (existingBar != null)
            {
                healthBar = existingBar.GetComponent<Slider>();
                Transform fillTransform = existingBar.Find("Fill Area/Fill");
                if (fillTransform != null)
                    healthBarFill = fillTransform.GetComponent<Image>();
            }
        }

        if (timerText == null && canvas != null)
        {
            Transform existingTimer = canvas.transform.Find("Timer_PREVIEW");
            if (existingTimer != null)
                timerText = existingTimer.GetComponent<TextMeshProUGUI>();
        }

        if (inventoryText == null && canvas != null)
        {
            Transform existingInventory = canvas.transform.Find("Inventory_PREVIEW");
            if (existingInventory != null)
                inventoryText = existingInventory.GetComponent<TextMeshProUGUI>();
        }

        // Create ALL UI elements (don't destroy based on toggles)
        if (scoreText == null)
        {
            scoreText = CreateTextElement("Score_PREVIEW", scorePosition, scoreFontSize);
            scoreText.gameObject.hideFlags = HideFlags.DontSave;
        }
        else
        {
            UpdateTextElementPosition(scoreText, scorePosition);
            scoreText.fontSize = scoreFontSize;
            scoreText.gameObject.hideFlags = HideFlags.DontSave;
        }
        scoreText.text = scorePrefix + "999";

        if (healthText == null)
        {
            healthText = CreateTextElement("HealthText_PREVIEW", healthTextPosition, healthFontSize);
            healthText.gameObject.hideFlags = HideFlags.DontSave;
        }
        else
        {
            UpdateTextElementPosition(healthText, healthTextPosition);
            healthText.fontSize = healthFontSize;
            healthText.gameObject.hideFlags = HideFlags.DontSave;
        }
        healthText.text = healthPrefix + "100/100";

        if (healthBar == null)
        {
            CreateHealthBar();
            if (healthBar != null)
            {
                healthBar.gameObject.name = "HealthBar_PREVIEW"; // Rename for consistency
                healthBar.gameObject.hideFlags = HideFlags.DontSave;
            }
        }
        else
        {
            UpdateHealthBarPosition();
            healthBar.gameObject.hideFlags = HideFlags.DontSave;
        }

        if (timerText == null)
        {
            timerText = CreateTextElement("Timer_PREVIEW", timerPosition, timerFontSize);
            timerText.gameObject.hideFlags = HideFlags.DontSave;
        }
        else
        {
            UpdateTextElementPosition(timerText, timerPosition);
            timerText.fontSize = timerFontSize;
            timerText.gameObject.hideFlags = HideFlags.DontSave;
        }
        timerText.text = timerPrefix + "01:30";

        if (inventoryText == null)
        {
            inventoryText = CreateTextElement("Inventory_PREVIEW", inventoryPosition, inventoryFontSize);
            inventoryText.gameObject.hideFlags = HideFlags.DontSave;
        }
        else
        {
            UpdateTextElementPosition(inventoryText, inventoryPosition);
            inventoryText.fontSize = inventoryFontSize;
            inventoryText.gameObject.hideFlags = HideFlags.DontSave;
        }
        inventoryText.text = inventoryPrefix + "5";

        // Apply toggle states (enable/disable based on settings)
        ApplyToggleStates();
    }

    private void UpdateTextElementPosition(TextMeshProUGUI text, Vector2 position)
    {
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
    }

    private void UpdateHealthBarPosition()
    {
        RectTransform rect = healthBar.GetComponent<RectTransform>();
        rect.anchoredPosition = healthBarPosition;
        rect.sizeDelta = healthBarSize;
    }

    private void DestroyEditorPreview()
    {
        if (canvas != null && canvas.gameObject.name.Contains("PREVIEW"))
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (canvas != null) DestroyImmediate(canvas.gameObject);
            };
            canvas = null;
            scoreText = null;
            healthText = null;
            healthBar = null;
            healthBarFill = null;
            timerText = null;
            inventoryText = null;
        }
    }
    #endif

    #endregion

    #region Public Update Methods (Called from Manager Events)

    /// <summary>
    /// Update score display (wire from GameCollectionManager.onValueChanged)
    /// </summary>
    public void UpdateScore(int newScore)
    {
        currentScore = newScore;
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + currentScore.ToString();
            AnimateScoreChange();
        }
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Update health display (wire from GameHealthManager.onHealthChanged)
    /// </summary>
    public void UpdateHealth(int current, int max)
    {
        currentHealth = current;
        maxHealth = max;

        if (healthText != null)
        {
            healthText.text = healthPrefix + $"{currentHealth}/{maxHealth}";

            // Animate health text if enabled
            if (animateHealthText)
            {
                AnimateHealthText();
            }
        }

        if (healthBar != null && maxHealth > 0)
        {
            float healthPercent = (float)currentHealth / maxHealth;

            if (animateHealth)
            {
                // Kill existing animation
                healthAnimationTween?.Kill();

                // Animate health bar
                healthAnimationTween = DOTween.To(
                    () => healthBar.value,
                    x => healthBar.value = x,
                    healthPercent,
                    healthAnimationDuration
                ).SetUpdate(true);
            }
            else
            {
                // Instant update
                healthBar.value = healthPercent;
            }

            // Update color
            if (healthBarFill != null)
            {
                Color targetColor = healthPercent > 0.6f ? healthColorHigh :
                                   healthPercent > 0.3f ? healthColorMid : healthColorLow;
                healthBarFill.color = targetColor;
            }
        }

        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Update timer display (wire from GameTimerManager.onTimerUpdate)
    /// </summary>
    public void UpdateTimer(float time)
    {
        currentTime = time;
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = timerPrefix + $"{minutes:00}:{seconds:00}";

            // Pulse animation at intervals
            if (animateTimer && Time.time >= lastTimerPulse + timerPulseInterval)
            {
                AnimateTimerPulse();
                lastTimerPulse = Time.time;
            }
        }
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Update inventory display (wire from GameInventorySlot.onValueChanged)
    /// </summary>
    public void UpdateInventory(string itemType, int count)
    {
        inventoryItemType = itemType;
        inventoryCount = count;
        if (inventoryText != null)
        {
            inventoryText.text = $"{itemType}: {count}";

            // Animate inventory change if enabled
            if (animateInventory)
            {
                AnimateInventoryChange();
            }
        }
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Update inventory display with just count (for single item type games)
    /// </summary>
    public void UpdateInventoryCount(int count)
    {
        inventoryCount = count;
        if (inventoryText != null)
        {
            inventoryText.text = inventoryPrefix + count.ToString();

            // Animate inventory change if enabled
            if (animateInventory)
            {
                AnimateInventoryChange();
            }
        }
        onUIUpdated.Invoke();
    }

    #endregion

    #region Animation Methods

    private void AnimateScoreChange()
    {
        if (scoreText == null || !animateScore) return;

        // Kill existing animation and reset scale
        scoreAnimationTween?.Kill();
        scoreText.transform.localScale = Vector3.one;

        // Punch scale animation
        scoreAnimationTween = scoreText.transform.DOPunchScale(
            Vector3.one * scorePunchStrength,
            scoreAnimationDuration,
            10,
            1
        ).SetUpdate(true);
    }

    private void AnimateHealthText()
    {
        if (healthText == null) return;

        // Kill existing animation
        healthTextAnimationTween?.Kill();

        // Fade pulse animation
        Color originalColor = healthText.color;
        Color fadeColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);

        healthTextAnimationTween = DOTween.Sequence()
            .Append(DOTween.To(
                () => healthText.color,
                x => healthText.color = x,
                fadeColor,
                healthTextAnimationDuration * 0.5f
            ))
            .Append(DOTween.To(
                () => healthText.color,
                x => healthText.color = x,
                originalColor,
                healthTextAnimationDuration * 0.5f
            ))
            .SetUpdate(true);
    }

    private void AnimateTimerPulse()
    {
        if (timerText == null) return;

        // Kill existing animation and reset scale
        timerAnimationTween?.Kill();
        timerText.transform.localScale = Vector3.one;

        // Pulse scale animation
        timerAnimationTween = timerText.transform.DOPunchScale(
            Vector3.one * timerPulseStrength,
            0.2f,
            10,
            1
        ).SetUpdate(true);
    }

    private void AnimateInventoryChange()
    {
        if (inventoryText == null) return;

        // Kill existing animation
        inventoryAnimationTween?.Kill();

        switch (inventoryAnimationType)
        {
            case InventoryAnimationType.PunchScale:
                // Reset scale before punch
                inventoryText.transform.localScale = Vector3.one;

                inventoryAnimationTween = inventoryText.transform.DOPunchScale(
                    Vector3.one * inventoryAnimationStrength,
                    inventoryAnimationDuration,
                    10,
                    1
                ).SetUpdate(true);
                break;

            case InventoryAnimationType.Fade:
                Color originalColor = inventoryText.color;
                Color fadeColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);

                inventoryAnimationTween = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => inventoryText.color,
                        x => inventoryText.color = x,
                        fadeColor,
                        inventoryAnimationDuration * 0.5f
                    ))
                    .Append(DOTween.To(
                        () => inventoryText.color,
                        x => inventoryText.color = x,
                        originalColor,
                        inventoryAnimationDuration * 0.5f
                    ))
                    .SetUpdate(true);
                break;

            case InventoryAnimationType.Bounce:
                RectTransform inventoryRect = inventoryText.GetComponent<RectTransform>();
                Vector2 originalPos = inventoryRect.anchoredPosition;
                Vector2 bouncePos = originalPos + Vector2.up * (inventoryAnimationStrength * 100f);

                inventoryAnimationTween = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => inventoryRect.anchoredPosition,
                        x => inventoryRect.anchoredPosition = x,
                        bouncePos,
                        inventoryAnimationDuration * 0.5f
                    ))
                    .Append(DOTween.To(
                        () => inventoryRect.anchoredPosition,
                        x => inventoryRect.anchoredPosition = x,
                        originalPos,
                        inventoryAnimationDuration * 0.5f
                    ))
                    .SetUpdate(true)
                    .SetEase(Ease.OutQuad);
                break;
        }
    }

    #endregion

    #region Helper Methods

    private void UpdateAllDisplays()
    {
        if (scoreText != null)
            scoreText.text = scorePrefix + currentScore.ToString();

        if (healthText != null)
            healthText.text = healthPrefix + $"{currentHealth}/{maxHealth}";

        if (healthBar != null && maxHealth > 0)
            healthBar.value = (float)currentHealth / maxHealth;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = timerPrefix + $"{minutes:00}:{seconds:00}";
        }

        if (inventoryText != null)
        {
            if (!string.IsNullOrEmpty(inventoryItemType))
                inventoryText.text = $"{inventoryItemType}: {inventoryCount}";
            else
                inventoryText.text = inventoryPrefix + inventoryCount.ToString();
        }
    }

    #endregion
}
