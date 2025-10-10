using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Displays and animates UI elements including score, health bars, timers, and victory screens.
/// Common use: HUD systems, stat displays, game overlays, or end-game result screens.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private int currentScore = 0;

    [Header("Health Display")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private string healthPrefix = "Health: ";
    [SerializeField] private Color healthColorHigh = Color.green;
    [SerializeField] private Color healthColorMid = Color.yellow;
    [SerializeField] private Color healthColorLow = Color.red;

    [Header("Victory Display")]
    [SerializeField] private TextMeshProUGUI victoryText;

    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private string timerPrefix = "Time: ";
    [SerializeField] private bool countUp = true;
    [SerializeField] private float startTime = 0f;

    [Header("Inventory Display")]
    [SerializeField] private TextMeshProUGUI inventoryText;
    [SerializeField] private string inventoryPrefix = "Items: ";

    [Header("Animation Settings")]
    [SerializeField] private float scoreAnimationDuration = 0.3f;
    [SerializeField] private float healthAnimationDuration = 0.2f;
    [SerializeField] private AnimationCurve scorePunchCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    [Header("Events")]
    /// <summary>
    /// Fires when the score value changes
    /// </summary>
    public UnityEvent onScoreChanged;
    /// <summary>
    /// Fires when health values are updated in the UI
    /// </summary>
    public UnityEvent onHealthChanged;
    /// <summary>
    /// Fires every frame while the timer is running
    /// </summary>
    public UnityEvent onTimerUpdated;
    /// <summary>
    /// Fires when any UI element is updated
    /// </summary>
    public UnityEvent onUIUpdated;

    private float gameTime;
    private bool isTimerRunning = false;
    private Coroutine scoreAnimationCoroutine;
    private Coroutine healthAnimationCoroutine;

    public int CurrentScore => currentScore;
    public float GameTime => gameTime;
    public bool IsTimerRunning => isTimerRunning;

    private void Start()
    {
        InitializeUI();
        StartTimer();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            UpdateTimer();
        }
    }

    private void InitializeUI()
    {
        UpdateScoreDisplay();
        UpdateTimerDisplay();

        // Set initial health bar color if available
        if (healthBarFill != null)
        {
            healthBarFill.color = healthColorHigh;
        }
    }

    #region Score Management

    /// <summary>
    /// Add points to the score
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreDisplay();
        AnimateScoreChange();
        onScoreChanged.Invoke();
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Subtract points from the score
    /// </summary>
    public void SubtractScore(int points)
    {
        currentScore = Mathf.Max(0, currentScore - points);
        UpdateScoreDisplay();
        AnimateScoreChange();
        onScoreChanged.Invoke();
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Set score to specific value
    /// </summary>
    public void SetScore(int newScore)
    {
        currentScore = Mathf.Max(0, newScore);
        UpdateScoreDisplay();
        onScoreChanged.Invoke();
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Reset score to zero
    /// </summary>
    public void ResetScore()
    {
        SetScore(0);
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + currentScore.ToString();
        }
    }

    private void AnimateScoreChange()
    {
        if (scoreText != null && scoreAnimationCoroutine == null)
        {
            scoreAnimationCoroutine = StartCoroutine(AnimateScorePunch());
        }
    }

    private IEnumerator AnimateScorePunch()
    {
        Vector3 originalScale = scoreText.transform.localScale;
        float elapsed = 0f;

        while (elapsed < scoreAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / scoreAnimationDuration;
            float scaleMultiplier = scorePunchCurve.Evaluate(progress);

            scoreText.transform.localScale = originalScale * (1f + scaleMultiplier * 0.2f);
            yield return null;
        }

        scoreText.transform.localScale = originalScale;
        scoreAnimationCoroutine = null;
    }

    #endregion

    #region Health Management

    /// <summary>
    /// Update health display from health manager
    /// </summary>
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        UpdateHealthText(currentHealth, maxHealth);
        UpdateHealthBar(currentHealth, maxHealth);
        onHealthChanged.Invoke();
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Update just the health text
    /// </summary>
    public void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = healthPrefix + $"{currentHealth}/{maxHealth}";
        }
    }

    /// <summary>
    /// Update health bar and color
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBar != null && maxHealth > 0)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthBar.value = healthPercent;

            // Animate health bar change
            if (healthAnimationCoroutine != null)
                StopCoroutine(healthAnimationCoroutine);
            healthAnimationCoroutine = StartCoroutine(AnimateHealthBar(healthPercent));

            // Update health bar color
            UpdateHealthBarColor(healthPercent);
        }
    }

    private void UpdateHealthBarColor(float healthPercent)
    {
        if (healthBarFill != null)
        {
            Color targetColor;
            if (healthPercent > 0.6f)
                targetColor = healthColorHigh;
            else if (healthPercent > 0.3f)
                targetColor = healthColorMid;
            else
                targetColor = healthColorLow;

            healthBarFill.color = targetColor;
        }
    }

    private IEnumerator AnimateHealthBar(float targetValue)
    {
        float startValue = healthBar.value;
        float elapsed = 0f;

        while (elapsed < healthAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / healthAnimationDuration;

            healthBar.value = Mathf.Lerp(startValue, targetValue, progress);
            yield return null;
        }

        healthBar.value = targetValue;
        healthAnimationCoroutine = null;
    }

    #endregion

    #region Timer Management

    /// <summary>
    /// Start the game timer
    /// </summary>
    public void StartTimer()
    {
        isTimerRunning = true;
        gameTime = startTime;
    }

    /// <summary>
    /// Stop the game timer
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// Reset timer to start value
    /// </summary>
    public void ResetTimer()
    {
        gameTime = startTime;
        UpdateTimerDisplay();
    }

    /// <summary>
    /// Set timer value
    /// </summary>
    public void SetTimer(float time)
    {
        gameTime = time;
        UpdateTimerDisplay();
    }

    private void UpdateTimer()
    {
        if (countUp)
        {
            gameTime += Time.deltaTime;
        }
        else
        {
            gameTime -= Time.deltaTime;
            if (gameTime <= 0)
            {
                gameTime = 0;
                StopTimer();
                // Could trigger timer expired event here
            }
        }

        UpdateTimerDisplay();
        onTimerUpdated.Invoke();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            timerText.text = timerPrefix + $"{minutes:00}:{seconds:00}";
        }
    }

    #endregion

    #region Victory Display

    /// <summary>
    /// Display victory message with score and time
    /// </summary>
    public void DisplayVictory()
    {
        StopTimer();
        if (victoryText != null)
        {
            victoryText.text = $"Victory!\nScore: {currentScore}\nTime: {FormatTime(gameTime)}";
        }
        onUIUpdated.Invoke();
    }

    #endregion

    #region Inventory Display

    /// <summary>
    /// Update inventory display
    /// </summary>
    public void UpdateInventory(int itemCount)
    {
        if (inventoryText != null)
        {
            inventoryText.text = inventoryPrefix + itemCount.ToString();
        }
        onUIUpdated.Invoke();
    }

    /// <summary>
    /// Update inventory with item type and count
    /// </summary>
    public void UpdateInventory(string itemType, int itemCount)
    {
        if (inventoryText != null)
        {
            inventoryText.text = $"{itemType}: {itemCount}";
        }
        onUIUpdated.Invoke();
    }

    #endregion

    #region Student Helper Methods

    /// <summary>
    /// Simple method for students - update all UI elements
    /// </summary>
    public void RefreshAllUI()
    {
        UpdateScoreDisplay();
        UpdateTimerDisplay();
        onUIUpdated.Invoke();
    }


    /// <summary>
    /// Format time as string
    /// </summary>
    public string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    #endregion
}