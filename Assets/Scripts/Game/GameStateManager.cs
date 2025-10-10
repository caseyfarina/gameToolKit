using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game pause, victory states, and automatically coordinates with timers across the scene.
/// Common use: Pause menu systems, game over screens, victory conditions, or level completion handlers.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Pause Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private bool startPaused = false;
    [SerializeField] private bool autoPauseTimers = true;

    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject restartButton;

    [Header("Events")]
    /// <summary>
    /// Fires when the game is paused
    /// </summary>
    public UnityEvent onGamePaused;
    /// <summary>
    /// Fires when the game is resumed from pause
    /// </summary>
    public UnityEvent onGameResumed;
    /// <summary>
    /// Fires when the victory state is triggered
    /// </summary>
    public UnityEvent onVictory;

    private bool isPaused = false;
    private bool hasWon = false;
    private GameTimerManager[] timers;

    public bool IsPaused => isPaused;
    public bool IsPlaying => !isPaused && !hasWon;
    public bool HasWon => hasWon;

    private void Start()
    {
        // Find all timer managers in the scene if auto-pause is enabled
        if (autoPauseTimers)
        {
            timers = FindObjectsByType<GameTimerManager>(FindObjectsSortMode.None);
            Debug.Log($"Found {timers.Length} timer manager(s) for auto-pause");
        }

        if (startPaused)
        {
            PauseGame();
        }
        else
        {
            UpdateUI();
        }
    }

    private void Update()
    {
        // Check for pause key input
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggle pause state - can be called from UI buttons or other scripts
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // Auto-pause all timers if enabled
        if (autoPauseTimers && timers != null)
        {
            foreach (var timer in timers)
            {
                if (timer != null && timer.IsRunning)
                {
                    timer.PauseTimer(true); // External pause request
                }
            }
        }

        UpdateUI();
        onGamePaused.Invoke();
        Debug.Log("Game Paused");
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // Auto-resume all paused timers if enabled
        if (autoPauseTimers && timers != null)
        {
            foreach (var timer in timers)
            {
                if (timer != null && timer.IsPaused)
                {
                    timer.ResumeTimer();
                }
            }
        }

        UpdateUI();
        onGameResumed.Invoke();
        Debug.Log("Game Resumed");
    }

    /// <summary>
    /// Restart the current scene
    /// </summary>
    public void RestartScene()
    {
        Time.timeScale = 1f; // Ensure scene can load
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Trigger victory state
    /// </summary>
    public void Victory()
    {
        if (!hasWon)
        {
            hasWon = true;
            Time.timeScale = 0f; // Pause the game on victory
            UpdateUI();
            onVictory.Invoke();
            Debug.Log("Victory!");
        }
    }

    private void UpdateUI()
    {
        // Show/hide pause panel (when paused OR when won)
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused || hasWon);
        }

        // Show/hide restart button (when paused OR when won)
        if (restartButton != null)
        {
            restartButton.SetActive(isPaused || hasWon);
        }
    }
}