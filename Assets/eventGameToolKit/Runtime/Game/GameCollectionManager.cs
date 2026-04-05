using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Threshold entry with a target value and events that fire when the value crosses it
/// </summary>
[System.Serializable]
public class CollectionThreshold
{
    [Tooltip("The value at which this threshold triggers")]
    public int value = 10;

    [Tooltip("Fires ONCE when value crosses this threshold going UP (below → at/above)")]
    /// <summary>
    /// Fires when the collection value crosses this threshold going UP
    /// </summary>
    public UnityEvent onCrossedUp;

    [Tooltip("Fires ONCE when value crosses this threshold going DOWN (at/above → below)")]
    /// <summary>
    /// Fires when the collection value crosses this threshold going DOWN
    /// </summary>
    public UnityEvent onCrossedDown;

    [System.NonSerialized]
    public bool wasAbove;
}

/// <summary>
/// Tracks a numeric value (score, coins, items) with threshold-based event triggers.
/// Optionally creates its own UI display - enable Show UI for text and/or Show Bar for a fill bar.
///
/// MULTI-SCENE SUPPORT: Enable Persist Across Scenes to save the value when loading a new scene.
/// The manager is recreated per scene but the value carries over automatically.
///
/// Common use: Score systems, collectible counters, resource tracking, or objective progress meters.
/// </summary>
public class GameCollectionManager : MonoBehaviour
{
    /// <summary>
    /// Animation style for value change feedback
    /// </summary>
    public enum ValueAnimation { None, PunchScale, FadeFlash }

    [Header("Scene Persistence")]
    [Tooltip("Save the current value when loading a new scene. Each scene can have its own manager — only the value carries over.")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Header("Value Settings")]
    [Tooltip("Current value (score, coins, items, etc.)")]
    [SerializeField] private int currentValue = 0;

    [Tooltip("Minimum allowed value (0 = no minimum)")]
    [SerializeField] private int minValue = 0;

    [Tooltip("Maximum allowed value (0 = no maximum)")]
    [SerializeField] private int maxValue = 0;

    [Header("Thresholds")]
    [Tooltip("List of thresholds - each fires its own events when the value crosses it")]
    [SerializeField] private List<CollectionThreshold> thresholds = new List<CollectionThreshold> { new CollectionThreshold() };

    [Header("UI Text (Optional)")]
    [Tooltip("Enable to create a self-contained UI text display for this value")]
    [SerializeField] private bool showUI = false;

    [Tooltip("Text shown before the value (e.g. 'Score: ', 'Gold: ')")]
    [SerializeField] private string labelPrefix = "Score: ";

    [Tooltip("Position of the text on screen (anchor position, top-left origin)")]
    [SerializeField] private Vector2 textPosition = new Vector2(60, -40);

    [Tooltip("Font size for the display text")]
    [SerializeField] private float fontSize = 40f;

    [Tooltip("Text alignment")]
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Left;

    [Tooltip("Text color")]
    [SerializeField] private Color textColor = new Color(0.6f, 0.85f, 0.92f, 1f);

    [Tooltip("Custom font (leave empty for TMP default)")]
    [SerializeField] private TMP_FontAsset customFont;

    [Header("Text Animation")]
    [Tooltip("Animation played when the value changes")]
    [SerializeField] private ValueAnimation valueAnimation = ValueAnimation.PunchScale;

    [Tooltip("Duration of the value change animation")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("Strength of the value change animation")]
    [SerializeField] private float animationStrength = 0.2f;

    [Header("UI Bar (Optional)")]
    [Tooltip("Enable to create a fill bar display for this value (requires Max Value > 0)")]
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

    [Header("Events")]
    [Tooltip("Fires every time the value changes, passes new value as parameter")]
    /// <summary>
    /// Fires whenever the collection value changes, passing the new value as an int parameter
    /// </summary>
    public UnityEvent<int> onValueChanged;

    [Header("Limit Events")]
    [Tooltip("Fires when value reaches maximum limit")]
    /// <summary>
    /// Fires when the value reaches the maximum limit
    /// </summary>
    public UnityEvent onMaxReached;

    [Tooltip("Fires when value reaches minimum limit")]
    /// <summary>
    /// Fires when the value reaches the minimum limit
    /// </summary>
    public UnityEvent onMinReached;

    // UI runtime references
    private Canvas uiCanvas;
    private TextMeshProUGUI uiText;
    private Tween textAnimationTween;
    private Color baseTextColor;
    private Slider barSlider;
    private Image barFillImage;
    private Tween barAnimationTween;

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
            GameData.Instance.SetInt(GameData.COLLECTION_SLOT, currentValue);
    }

    private void Start()
    {
        // If persistence is enabled, read the carried-over value (or use Inspector default on first load)
        if (persistAcrossScenes)
            currentValue = GameData.Instance.GetInt(GameData.COLLECTION_SLOT, currentValue);

        // Initialize threshold states based on starting value
        foreach (var t in thresholds)
        {
            t.wasAbove = currentValue >= t.value;
        }

        // Create Canvas if any UI is enabled
        if (showUI || (showBar && maxValue > 0))
        {
            CreateCanvas();
        }

        if (showUI)
        {
            CreateTextUI();
            UpdateUIText();
        }

        if (showBar && maxValue > 0)
        {
            CreateBarUI();
            UpdateBar();
        }

        // Fire initial value event to update UI on start
        onValueChanged.Invoke(currentValue);
    }

    /// <summary>
    /// Increases the collection value by the specified amount
    /// </summary>
    public void Increment(int amount = 1)
    {
        int previousValue = currentValue;
        currentValue += amount;

        // Enforce maximum limit
        if (maxValue > 0 && currentValue > maxValue)
        {
            currentValue = maxValue;
        }

        // Check if we hit the max
        if (maxValue > 0 && currentValue >= maxValue && previousValue < maxValue)
        {
            onMaxReached.Invoke();
        }

        SyncToGameData();
        onValueChanged.Invoke(currentValue);
        UpdateUIText();
        PlayTextAnimation();
        UpdateBar();
        CheckThresholds();
    }

    /// <summary>
    /// Decreases the collection value by the specified amount
    /// </summary>
    public void Decrement(int amount = 1)
    {
        int previousValue = currentValue;
        currentValue -= amount;

        // Enforce minimum limit
        if (currentValue < minValue)
        {
            currentValue = minValue;
        }

        // Check if we hit the min
        if (currentValue <= minValue && previousValue > minValue)
        {
            onMinReached.Invoke();
        }

        SyncToGameData();
        onValueChanged.Invoke(currentValue);
        UpdateUIText();
        PlayTextAnimation();
        UpdateBar();
        CheckThresholds();
    }

    /// <summary>
    /// Get current collection value for saving/loading
    /// </summary>
    public int GetCurrentValue()
    {
        return currentValue;
    }

    /// <summary>
    /// Set collection value directly (for checkpoint restoration)
    /// </summary>
    public void SetValue(int newValue)
    {
        currentValue = newValue;
        SyncToGameData();
        onValueChanged.Invoke(currentValue);
        UpdateUIText();
        UpdateBar();
        CheckThresholds();
    }

    private void CheckThresholds()
    {
        foreach (var t in thresholds)
        {
            bool isAtOrAbove = currentValue >= t.value;

            // Crossed upward: was below, now at or above
            if (isAtOrAbove && !t.wasAbove)
            {
                t.onCrossedUp.Invoke();
            }
            // Crossed downward: was above, now below
            else if (!isAtOrAbove && t.wasAbove)
            {
                t.onCrossedDown.Invoke();
            }

            t.wasAbove = isAtOrAbove;
        }
    }

    #region UI Creation

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("CollectionUI_Canvas");
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
        GameObject textObj = new GameObject("ValueText");
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
        GameObject barObj = new GameObject("ValueBar");
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
            uiText.text = labelPrefix + currentValue.ToString();
        }
    }

    private void UpdateBar()
    {
        if (barSlider == null || maxValue <= 0) return;

        float fillPercent = Mathf.Clamp01((float)currentValue / maxValue);

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
        GameObject canvasObj = new GameObject("CollectionUI_PREVIEW");
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
            GameObject textObj = new GameObject("ValueText_PREVIEW");
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
            uiText.text = labelPrefix + "999";

            if (customFont != null)
            {
                uiText.font = customFont;
            }
        }

        // Create bar preview
        if (showBar)
        {
            GameObject barObj = new GameObject("ValueBar_PREVIEW");
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

            // Show at 50% fill for preview
            barSlider.value = 0.5f;
            barFillImage.color = barGradient.Evaluate(0.5f);
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
            uiText.text = labelPrefix + "999";

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

            // Update fill color at 50%
            barSlider.value = 0.5f;
            if (barFillImage != null)
                barFillImage.color = barGradient.Evaluate(0.5f);
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
