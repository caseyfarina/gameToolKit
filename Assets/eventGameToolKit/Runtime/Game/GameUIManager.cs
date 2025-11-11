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
    [SerializeField] private Vector2 scorePosition = new Vector2(10, -10);

    [Tooltip("Health text position")]
    [SerializeField] private Vector2 healthTextPosition = new Vector2(10, -50);

    [Tooltip("Health bar position")]
    [SerializeField] private Vector2 healthBarPosition = new Vector2(10, -80);

    [Tooltip("Health bar size")]
    [SerializeField] private Vector2 healthBarSize = new Vector2(200, 20);

    [Tooltip("Timer position")]
    [SerializeField] private Vector2 timerPosition = new Vector2(10, -120);

    [Tooltip("Inventory position")]
    [SerializeField] private Vector2 inventoryPosition = new Vector2(10, -160);

    [Header("UI Styling")]
    [Tooltip("Font size for all text")]
    [SerializeField] private int fontSize = 24;

    [Tooltip("Text color")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Health bar colors")]
    [SerializeField] private Color healthColorHigh = Color.green;
    [SerializeField] private Color healthColorMid = Color.yellow;
    [SerializeField] private Color healthColorLow = Color.red;

    [Header("Animation Settings")]
    [Tooltip("Duration for score punch animation")]
    [SerializeField] private float scoreAnimationDuration = 0.3f;

    [Tooltip("Duration for health bar transition")]
    [SerializeField] private float healthAnimationDuration = 0.2f;

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
            CreateOrUpdateEditorPreview();
            #endif
        }
        else if (!enableEditorPreview && !Application.isPlaying)
        {
            #if UNITY_EDITOR
            DestroyEditorPreview();
            #endif
        }
    }

    void OnDestroy()
    {
        // Clean up DOTween tweens
        scoreAnimationTween?.Kill();
        healthAnimationTween?.Kill();
    }

    #region UI Creation

    /// <summary>
    /// Creates all enabled UI elements at runtime
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
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create score display
        if (showScore && scoreText == null)
        {
            scoreText = CreateTextElement("Score", scorePosition);
        }

        // Create health text
        if (showHealthText && healthText == null)
        {
            healthText = CreateTextElement("HealthText", healthTextPosition);
        }

        // Create health bar
        if (showHealthBar && healthBar == null)
        {
            CreateHealthBar();
        }

        // Create timer
        if (showTimer && timerText == null)
        {
            timerText = CreateTextElement("Timer", timerPosition);
        }

        // Create inventory
        if (showInventory && inventoryText == null)
        {
            inventoryText = CreateTextElement("Inventory", inventoryPosition);
        }
    }

    private TextMeshProUGUI CreateTextElement(string name, Vector2 position)
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
        canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameUI_Canvas_PREVIEW");
            canvasObj.transform.SetParent(transform);
            canvasObj.hideFlags = HideFlags.DontSave; // Don't save to scene
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
        }
        else
        {
            canvas.gameObject.hideFlags = HideFlags.DontSave;
        }

        // Create/update UI elements
        if (showScore)
        {
            if (scoreText == null)
                scoreText = CreateTextElement("Score_PREVIEW", scorePosition);
            else
                UpdateTextElementPosition(scoreText, scorePosition);
            scoreText.text = scorePrefix + "999";
        }
        else if (scoreText != null)
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (scoreText != null) DestroyImmediate(scoreText.gameObject);
            };
            scoreText = null;
        }

        if (showHealthText)
        {
            if (healthText == null)
                healthText = CreateTextElement("HealthText_PREVIEW", healthTextPosition);
            else
                UpdateTextElementPosition(healthText, healthTextPosition);
            healthText.text = healthPrefix + "100/100";
        }
        else if (healthText != null)
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (healthText != null) DestroyImmediate(healthText.gameObject);
            };
            healthText = null;
        }

        if (showHealthBar)
        {
            if (healthBar == null)
                CreateHealthBar();
            else
                UpdateHealthBarPosition();
        }
        else if (healthBar != null)
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (healthBar != null) DestroyImmediate(healthBar.gameObject);
            };
            healthBar = null;
            healthBarFill = null;
        }

        if (showTimer)
        {
            if (timerText == null)
                timerText = CreateTextElement("Timer_PREVIEW", timerPosition);
            else
                UpdateTextElementPosition(timerText, timerPosition);
            timerText.text = timerPrefix + "01:30";
        }
        else if (timerText != null)
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (timerText != null) DestroyImmediate(timerText.gameObject);
            };
            timerText = null;
        }

        if (showInventory)
        {
            if (inventoryText == null)
                inventoryText = CreateTextElement("Inventory_PREVIEW", inventoryPosition);
            else
                UpdateTextElementPosition(inventoryText, inventoryPosition);
            inventoryText.text = inventoryPrefix + "5";
        }
        else if (inventoryText != null)
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (inventoryText != null) DestroyImmediate(inventoryText.gameObject);
            };
            inventoryText = null;
        }
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
        }

        if (healthBar != null && maxHealth > 0)
        {
            float healthPercent = (float)currentHealth / maxHealth;

            // Kill existing animation
            healthAnimationTween?.Kill();

            // Animate health bar
            healthAnimationTween = DOTween.To(
                () => healthBar.value,
                x => healthBar.value = x,
                healthPercent,
                healthAnimationDuration
            ).SetUpdate(true);

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
        }
        onUIUpdated.Invoke();
    }

    #endregion

    #region Animation Methods

    private void AnimateScoreChange()
    {
        if (scoreText == null) return;

        // Kill existing animation
        scoreAnimationTween?.Kill();

        // Punch scale animation
        scoreAnimationTween = scoreText.transform.DOPunchScale(
            Vector3.one * 0.2f,
            scoreAnimationDuration,
            10,
            1
        ).SetUpdate(true);
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
