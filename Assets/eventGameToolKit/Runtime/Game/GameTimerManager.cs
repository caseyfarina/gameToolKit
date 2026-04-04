using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Flexible timer system supporting countdown and count-up modes with threshold events and optional self-contained UI.
/// Enable Show UI for a text clock display and/or Show Bar for a fill bar — both support gradient color mapping over time.
/// Common use: Level time limits, speedrun timers, cooldown indicators, wave spawn timers, or challenge countdowns.
/// </summary>
public class GameTimerManager : MonoBehaviour
{
    /// <summary>
    /// Text format options for the timer display
    /// </summary>
    public enum DisplayFormat { MM_SS, Seconds, Seconds_Decimal, HH_MM_SS }

    [System.Serializable]
    public class TimeThreshold
    {
        [Tooltip("Time value (in seconds) at which this threshold triggers")]
        public float thresholdTime;

        [Tooltip("Descriptive name for this threshold")]
        public string thresholdName = "Threshold";

        [Tooltip("Event fired when this threshold is reached")]
        public UnityEvent onThresholdReached;

        [HideInInspector]
        public bool hasTriggered = false;
    }

    [Header("Timer Settings")]
    [SerializeField] private bool countUp = true;
    [SerializeField] private float startTime = 0f;

    [Tooltip("For count-up mode: the time at which the bar/gradient reaches 100%. Not used for countdown (startTime is used instead).")]
    [SerializeField] private float totalTime = 60f;

    [SerializeField] private bool startAutomatically = true;
    [Tooltip("Automatically pause/resume with GameStateManager")]
    [SerializeField] private bool respondToGamePause = true;

    [Header("Threshold Settings")]
    [Tooltip("List of time thresholds — each fires its own event when the timer crosses it")]
    [SerializeField] private TimeThreshold[] thresholds;

    [Header("Periodic Events")]
    [Tooltip("Fire an event every X seconds (e.g. 10 = every 10 seconds)")]
    [SerializeField] private float periodicInterval = 10f;
    [SerializeField] private bool enablePeriodicEvents = false;

    [Header("UI Text (Optional)")]
    [Tooltip("Enable to create a self-contained text clock display")]
    [SerializeField] private bool showUI = false;

    [Tooltip("Text shown before the time value (e.g. 'Time: ', 'Remaining: ')")]
    [SerializeField] private string labelPrefix = "";

    [Tooltip("How the time value is formatted")]
    [SerializeField] private DisplayFormat displayFormat = DisplayFormat.MM_SS;

    [Tooltip("Position of the text on screen (anchor position, top-left origin)")]
    [SerializeField] private Vector2 textPosition = new Vector2(60, -40);

    [Tooltip("Font size for the timer text")]
    [SerializeField] private float fontSize = 40f;

    [Tooltip("Text alignment")]
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Left;

    [Tooltip("When enabled, text color is driven by the gradient below instead of Text Color")]
    [SerializeField] private bool useTextGradient = false;

    [Tooltip("Static text color (used when Use Text Gradient is off)")]
    [SerializeField] private Color textColor = new Color(0.6f, 0.85f, 0.92f, 1f);

    [Tooltip("Text color gradient mapped across the timer's total duration (left = start, right = end/done)")]
    [SerializeField] private Gradient textGradient = DefaultTimerGradient();

    [Tooltip("Custom font (leave empty for TMP default)")]
    [SerializeField] private TMP_FontAsset customFont;

    [Header("UI Bar (Optional)")]
    [Tooltip("Enable to create a fill bar display. Count-up requires Total Time > 0.")]
    [SerializeField] private bool showBar = false;

    [Tooltip("Position of the bar on screen (anchor position, top-left origin)")]
    [SerializeField] private Vector2 barPosition = new Vector2(60, -80);

    [Tooltip("Size of the bar in pixels")]
    [SerializeField] private Vector2 barSize = new Vector2(200, 20);

    [Tooltip("Background color of the bar")]
    [SerializeField] private Color barBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [Tooltip("Bar fill color gradient mapped across the timer's total duration (left = start, right = end/done)")]
    [SerializeField] private Gradient barGradient = DefaultTimerGradient();

    [Tooltip("Enable smooth animation when bar value changes")]
    [SerializeField] private bool animateBar = true;

    [Tooltip("Duration of the bar fill animation")]
    [SerializeField] private float barAnimationDuration = 0.1f;

    [Header("Timer Events")]
    /// <summary>
    /// Fires when the timer starts running
    /// </summary>
    public UnityEvent onTimerStarted;
    /// <summary>
    /// Fires when the timer stops completely (countdown reaches zero or manually stopped)
    /// </summary>
    public UnityEvent onTimerStopped;
    /// <summary>
    /// Fires when the timer is paused
    /// </summary>
    public UnityEvent onTimerPaused;
    /// <summary>
    /// Fires when the timer is resumed from pause
    /// </summary>
    public UnityEvent onTimerResumed;
    /// <summary>
    /// Fires when the timer is reset to start time
    /// </summary>
    public UnityEvent onTimerRestarted;
    /// <summary>
    /// Fires at regular intervals based on the periodic interval setting
    /// </summary>
    public UnityEvent onPeriodicEvent;
    /// <summary>
    /// Fires every frame while the timer is running, passing the current time as a float parameter
    /// </summary>
    public UnityEvent<float> onTimerUpdate;

    // Runtime state
    private float currentTime;
    private bool isRunning = false;
    private bool isPaused = false;
    private float lastPeriodicTime = 0f;

    // UI runtime references
    private Canvas uiCanvas;
    private TextMeshProUGUI uiText;
    private Slider barSlider;
    private Image barFillImage;

    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning && !isPaused;
    public bool IsPaused => isPaused;

    private static Gradient DefaultTimerGradient()
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

    private void Start()
    {
        currentTime = startTime;

        if (showUI || (showBar && GetMaxTime() > 0f))
        {
            CreateCanvas();
        }

        if (showUI)
        {
            CreateTextUI();
            UpdateUIText();
        }

        if (showBar && GetMaxTime() > 0f)
        {
            CreateBarUI();
            UpdateBar();
        }

        if (startAutomatically)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (isRunning && !isPaused)
        {
            UpdateTimerTick();
        }
    }

    private void UpdateTimerTick()
    {
        float previousTime = currentTime;

        if (countUp)
        {
            currentTime += Time.deltaTime;
        }
        else
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;

                CheckThresholds(previousTime);
                CheckPeriodicEvents();
                onTimerUpdate.Invoke(currentTime);
                UpdateUIText();
                UpdateBar();

                StopTimer();
                return;
            }
        }

        CheckThresholds(previousTime);
        CheckPeriodicEvents();

        onTimerUpdate.Invoke(currentTime);
        UpdateUIText();
        UpdateBar();
    }

    // Returns time progress as 0→1 regardless of count direction
    // Countdown: 1.0 = full time remaining, 0.0 = expired
    // Count-up:  0.0 = just started, 1.0 = reached totalTime
    private float GetTimeProgress()
    {
        float max = GetMaxTime();
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(currentTime / max);
    }

    private float GetMaxTime()
    {
        return countUp ? totalTime : startTime;
    }

    private void CheckThresholds(float previousTime)
    {
        if (thresholds == null || thresholds.Length == 0) return;

        foreach (var threshold in thresholds)
        {
            if (threshold.hasTriggered) continue;

            bool thresholdReached = false;

            if (countUp && currentTime >= threshold.thresholdTime && previousTime < threshold.thresholdTime)
            {
                thresholdReached = true;
            }
            else if (!countUp && currentTime <= threshold.thresholdTime && previousTime > threshold.thresholdTime)
            {
                thresholdReached = true;
            }

            if (thresholdReached)
            {
                threshold.hasTriggered = true;
                threshold.onThresholdReached.Invoke();
                Debug.Log($"Timer threshold '{threshold.thresholdName}' reached at {threshold.thresholdTime} seconds");
            }
        }
    }

    private void CheckPeriodicEvents()
    {
        if (!enablePeriodicEvents || periodicInterval <= 0f) return;

        if (countUp)
        {
            int currentInterval = Mathf.FloorToInt(currentTime / periodicInterval);
            int lastInterval = Mathf.FloorToInt(lastPeriodicTime / periodicInterval);

            if (currentInterval > lastInterval)
            {
                onPeriodicEvent.Invoke();
            }
        }
        else
        {
            int currentInterval = Mathf.FloorToInt(currentTime / periodicInterval);
            int lastInterval = Mathf.FloorToInt(lastPeriodicTime / periodicInterval);

            if (currentInterval < lastInterval)
            {
                onPeriodicEvent.Invoke();
            }
        }

        lastPeriodicTime = currentTime;
    }

    #region Timer Control Methods

    /// <summary>
    /// Start the timer
    /// </summary>
    public void StartTimer()
    {
        if (!isRunning)
        {
            isRunning = true;
            isPaused = false;
            lastPeriodicTime = currentTime;
            onTimerStarted.Invoke();
            Debug.Log("Timer started");
        }
    }

    /// <summary>
    /// Stop the timer completely
    /// </summary>
    public void StopTimer()
    {
        if (isRunning)
        {
            isRunning = false;
            isPaused = false;
            onTimerStopped.Invoke();
            Debug.Log("Timer stopped");
        }
    }

    /// <summary>
    /// Pause the timer (can be resumed)
    /// </summary>
    public void PauseTimer()
    {
        PauseTimer(false);
    }

    /// <summary>
    /// Pause the timer with option to force pause regardless of settings
    /// </summary>
    public void PauseTimer(bool forceExternal)
    {
        if (isRunning && !isPaused)
        {
            if (forceExternal && !respondToGamePause)
                return;

            isPaused = true;
            onTimerPaused.Invoke();
            Debug.Log("Timer paused");
        }
    }

    /// <summary>
    /// Resume the timer from pause
    /// </summary>
    public void ResumeTimer()
    {
        if (isRunning && isPaused)
        {
            isPaused = false;
            onTimerResumed.Invoke();
            Debug.Log("Timer resumed");
        }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused) ResumeTimer();
        else PauseTimer();
    }

    /// <summary>
    /// Restart the timer to start time
    /// </summary>
    public void RestartTimer()
    {
        currentTime = startTime;

        if (thresholds != null)
        {
            foreach (var threshold in thresholds)
                threshold.hasTriggered = false;
        }

        lastPeriodicTime = currentTime;
        UpdateUIText();
        UpdateBar();
        onTimerRestarted.Invoke();
        Debug.Log("Timer restarted");
    }

    /// <summary>
    /// Reset and start the timer
    /// </summary>
    public void ResetAndStart()
    {
        RestartTimer();
        StartTimer();
    }

    #endregion

    #region Time Setting Methods

    /// <summary>
    /// Set the current time directly
    /// </summary>
    public void SetTime(float time)
    {
        currentTime = Mathf.Max(0f, time);

        if (thresholds != null)
        {
            foreach (var threshold in thresholds)
                threshold.hasTriggered = false;
        }

        lastPeriodicTime = currentTime;
        UpdateUIText();
        UpdateBar();
    }

    /// <summary>
    /// Add time to the current timer
    /// </summary>
    public void AddTime(float additionalTime)
    {
        currentTime = Mathf.Max(0f, currentTime + additionalTime);
        UpdateUIText();
        UpdateBar();
    }

    #endregion

    #region Student Helper Methods

    /// <summary>
    /// Check if timer has reached a specific time
    /// </summary>
    public bool HasReachedTime(float targetTime)
    {
        if (countUp)
            return currentTime >= targetTime;
        else
            return currentTime <= targetTime;
    }

    /// <summary>
    /// Get count of triggered thresholds
    /// </summary>
    public int GetTriggeredThresholdCount()
    {
        if (thresholds == null) return 0;

        int count = 0;
        foreach (var threshold in thresholds)
        {
            if (threshold.hasTriggered) count++;
        }
        return count;
    }

    #endregion

    #region UI Creation

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("TimerUI_Canvas");
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
        GameObject textObj = new GameObject("TimerText");
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
            uiText.font = customFont;
    }

    private void CreateBarUI()
    {
        GameObject barObj = new GameObject("TimerBar");
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
        barSlider.value = GetTimeProgress();
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

        barSlider.fillRect = fillRect;
    }

    #endregion

    #region UI Updates

    private void UpdateUIText()
    {
        if (uiText == null) return;

        uiText.text = FormatTime(currentTime);

        if (useTextGradient)
            uiText.color = textGradient.Evaluate(GetTimeProgress());
    }

    private void UpdateBar()
    {
        if (barSlider == null || GetMaxTime() <= 0f) return;

        float progress = GetTimeProgress();
        float displayValue;

        if (animateBar && barAnimationDuration > 0f)
        {
            // Lerp toward target each frame — achieves smooth lag without DOTween allocation
            displayValue = Mathf.Lerp(barSlider.value, progress, Time.deltaTime / barAnimationDuration);
        }
        else
        {
            displayValue = progress;
        }

        barSlider.value = displayValue;
        if (barFillImage != null)
            barFillImage.color = barGradient.Evaluate(displayValue);
    }

    private string FormatTime(float time)
    {
        time = Mathf.Max(0f, time);

        switch (displayFormat)
        {
            case DisplayFormat.MM_SS:
                int m = Mathf.FloorToInt(time / 60f);
                int s = Mathf.FloorToInt(time % 60f);
                return labelPrefix + $"{m:00}:{s:00}";

            case DisplayFormat.Seconds:
                return labelPrefix + Mathf.FloorToInt(time).ToString();

            case DisplayFormat.Seconds_Decimal:
                return labelPrefix + time.ToString("F1");

            case DisplayFormat.HH_MM_SS:
                int h = Mathf.FloorToInt(time / 3600f);
                int min = Mathf.FloorToInt((time % 3600f) / 60f);
                int sec = Mathf.FloorToInt(time % 60f);
                return labelPrefix + $"{h:00}:{min:00}:{sec:00}";

            default:
                return labelPrefix + $"{Mathf.FloorToInt(time / 60f):00}:{Mathf.FloorToInt(time % 60f):00}";
        }
    }

    // No tweens to clean up — bar uses Lerp, no DOTween allocation

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

        GameObject canvasObj = new GameObject("TimerUI_PREVIEW");
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

        if (showUI)
        {
            GameObject textObj = new GameObject("TimerText_PREVIEW");
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
            uiText.overflowMode = TextOverflowModes.Overflow;
            uiText.enableWordWrapping = false;

            // Preview at 75% progress
            float previewProgress = 0.75f;
            uiText.color = useTextGradient ? textGradient.Evaluate(previewProgress) : textColor;

            float previewTime = GetMaxTime() > 0f ? GetMaxTime() * previewProgress : 45f;
            uiText.text = FormatTime(previewTime);

            if (customFont != null)
                uiText.font = customFont;
        }

        if (showBar && GetMaxTime() > 0f)
        {
            GameObject barObj = new GameObject("TimerBar_PREVIEW");
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

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(barObj.transform, false);
            bgObj.hideFlags = HideFlags.DontSave;
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = barBackgroundColor;

            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(barObj.transform, false);
            fillAreaObj.hideFlags = HideFlags.DontSave;
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            fillObj.hideFlags = HideFlags.DontSave;
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            barFillImage = fillObj.AddComponent<Image>();

            barSlider.fillRect = fillRect;

            // Preview at 75%
            barSlider.value = 0.75f;
            barFillImage.color = barGradient.Evaluate(0.75f);
        }
    }

    /// <summary>
    /// Updates the preview UI to reflect current Inspector values
    /// </summary>
    public void UpdatePreviewUI()
    {
        if (uiText != null)
        {
            RectTransform rectTransform = uiText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = textPosition;

            uiText.fontSize = fontSize;
            uiText.alignment = textAlignment;

            float previewTime = GetMaxTime() > 0f ? GetMaxTime() * 0.75f : 45f;
            uiText.text = FormatTime(previewTime);
            uiText.color = useTextGradient ? textGradient.Evaluate(0.75f) : textColor;

            if (customFont != null)
                uiText.font = customFont;
        }

        if (barSlider != null)
        {
            RectTransform barRect = barSlider.GetComponent<RectTransform>();
            barRect.anchoredPosition = barPosition;
            barRect.sizeDelta = barSize;

            Transform bgTransform = barSlider.transform.Find("Background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                    bgImage.color = barBackgroundColor;
            }

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
            UnityEngine.Object.DestroyImmediate(uiCanvas.gameObject);
        }

        uiCanvas = null;
        uiText = null;
        barSlider = null;
        barFillImage = null;
    }
#endif

    #endregion

    #region Debug Tools

    [Header("Debug Tools")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUILayout.BeginArea(new Rect(250, 10, 200, 200));
            GUILayout.Label($"Timer: {currentTime:F2}");
            GUILayout.Label($"Running: {IsRunning}");
            GUILayout.Label($"Paused: {IsPaused}");
            GUILayout.Label($"Thresholds: {GetTriggeredThresholdCount()}/{thresholds?.Length ?? 0}");

            if (GUILayout.Button("Start")) StartTimer();
            if (GUILayout.Button("Stop")) StopTimer();
            if (GUILayout.Button("Pause/Resume")) TogglePause();
            if (GUILayout.Button("Restart")) RestartTimer();

            GUILayout.EndArea();
        }
    }

    #endregion
}
