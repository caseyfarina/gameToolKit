using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Flexible timer system supporting countdown and count-up modes with threshold and periodic event triggers.
/// Common use: Level time limits, speedrun timers, cooldown indicators, wave spawn timers, or challenge countdowns.
/// </summary>
public class GameTimerManager : MonoBehaviour
{
    public enum TimeFormat
    {
        MinutesSeconds,     // 01:30
        DecimalSeconds,     // 90.5s
        HoursMinutesSeconds // 01:01:30
    }

    [System.Serializable]
    public class TimeThreshold
    {
        [Tooltip("Time value to trigger this threshold (in seconds)")]
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
    [SerializeField] private bool startAutomatically = true;
    [Tooltip("Automatically pause/resume with GameStateManager")]
    [SerializeField] private bool respondToGamePause = true;

    [Header("Threshold Settings")]
    [Tooltip("Array of time thresholds - each with its own event")]
    [SerializeField] private TimeThreshold[] thresholds;

    [Header("Periodic Events")]
    [Tooltip("Send periodic events every X seconds (e.g., 10 for every 10 seconds)")]
    [SerializeField] private float periodicInterval = 10f;
    [SerializeField] private bool enablePeriodicEvents = false;

    [Header("Display Settings")]
    [SerializeField] private TextMeshProUGUI timerDisplay;
    [SerializeField] private string timerPrefix = "Time: ";
    [SerializeField] private TimeFormat displayFormat = TimeFormat.MinutesSeconds;

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
    /// Fires every frame while the timer is running
    /// </summary>
    public UnityEvent onTimerUpdate;

    private float currentTime;
    private bool isRunning = false;
    private bool isPaused = false;
    private float lastPeriodicTime = 0f;

    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning && !isPaused;
    public bool IsPaused => isPaused;

    private void Start()
    {
        currentTime = startTime;
        UpdateDisplay();

        if (startAutomatically)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (isRunning && !isPaused)
        {
            UpdateTimer();
        }
    }

    private void UpdateTimer()
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
                StopTimer();
                return;
            }
        }

        // Check all thresholds
        CheckThresholds(previousTime);

        // Check periodic events
        CheckPeriodicEvents();

        UpdateDisplay();
        onTimerUpdate.Invoke();
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
            // For count up: check if we've passed each interval
            int currentInterval = Mathf.FloorToInt(currentTime / periodicInterval);
            int lastInterval = Mathf.FloorToInt(lastPeriodicTime / periodicInterval);

            if (currentInterval > lastInterval)
            {
                onPeriodicEvent.Invoke();
                Debug.Log($"Periodic event triggered at {currentTime:F1} seconds (interval: {periodicInterval})");
            }
        }
        else
        {
            // For countdown: check if we've passed each interval going down
            int currentInterval = Mathf.FloorToInt(currentTime / periodicInterval);
            int lastInterval = Mathf.FloorToInt(lastPeriodicTime / periodicInterval);

            if (currentInterval < lastInterval)
            {
                onPeriodicEvent.Invoke();
                Debug.Log($"Periodic event triggered at {currentTime:F1} seconds (interval: {periodicInterval})");
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
            // If this is an external pause request, check if timer should respond
            if (forceExternal && !respondToGamePause)
            {
                return; // This timer doesn't respond to external pause requests
            }

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
        if (isPaused)
        {
            ResumeTimer();
        }
        else
        {
            PauseTimer();
        }
    }

    /// <summary>
    /// Restart the timer to start time
    /// </summary>
    public void RestartTimer()
    {
        currentTime = startTime;

        // Reset all thresholds
        if (thresholds != null)
        {
            foreach (var threshold in thresholds)
            {
                threshold.hasTriggered = false;
            }
        }

        lastPeriodicTime = currentTime;
        UpdateDisplay();
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
    /// Set the current time
    /// </summary>
    public void SetTime(float time)
    {
        currentTime = time;

        // Reset all thresholds when time is manually set
        if (thresholds != null)
        {
            foreach (var threshold in thresholds)
            {
                threshold.hasTriggered = false;
            }
        }

        lastPeriodicTime = currentTime;
        UpdateDisplay();
    }

    /// <summary>
    /// Add time to the current timer
    /// </summary>
    public void AddTime(float additionalTime)
    {
        currentTime += additionalTime;
        UpdateDisplay();
    }

    /// <summary>
    /// Add a new threshold at runtime
    /// </summary>
    public void AddThreshold(float thresholdTime, string thresholdName, UnityEvent thresholdEvent)
    {
        // This would require expanding the array at runtime - better to configure in editor
        Debug.LogWarning("Use editor to configure thresholds. Runtime threshold addition not implemented.");
    }

    #endregion

    private void UpdateDisplay()
    {
        if (timerDisplay != null)
        {
            timerDisplay.text = timerPrefix + GetFormattedTime();
        }
    }

    #region Student Helper Methods

    /// <summary>
    /// Get formatted time string based on display format
    /// </summary>
    public string GetFormattedTime()
    {
        switch (displayFormat)
        {
            case TimeFormat.MinutesSeconds:
                int minutes = Mathf.FloorToInt(currentTime / 60f);
                int seconds = Mathf.FloorToInt(currentTime % 60f);
                return $"{minutes:00}:{seconds:00}";

            case TimeFormat.DecimalSeconds:
                return $"{currentTime:F1}s";

            case TimeFormat.HoursMinutesSeconds:
                int hours = Mathf.FloorToInt(currentTime / 3600f);
                int mins = Mathf.FloorToInt((currentTime % 3600f) / 60f);
                int secs = Mathf.FloorToInt(currentTime % 60f);
                return $"{hours:00}:{mins:00}:{secs:00}";

            default:
                return $"{currentTime:F1}s";
        }
    }

    /// <summary>
    /// Check if timer has reached specific time
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

    #region Debug Methods

    [Header("Debug Tools")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUILayout.BeginArea(new Rect(250, 10, 200, 200));
            GUILayout.Label($"Timer: {GetFormattedTime()}");
            GUILayout.Label($"Running: {IsRunning}");
            GUILayout.Label($"Paused: {IsPaused}");
            GUILayout.Label($"Thresholds: {GetTriggeredThresholdCount()}/{thresholds?.Length ?? 0}");

            if (GUILayout.Button("Start"))
                StartTimer();
            if (GUILayout.Button("Stop"))
                StopTimer();
            if (GUILayout.Button("Pause/Resume"))
                TogglePause();
            if (GUILayout.Button("Restart"))
                RestartTimer();

            GUILayout.EndArea();
        }
    }

    #endregion
}