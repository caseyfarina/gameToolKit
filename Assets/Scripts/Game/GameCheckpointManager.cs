using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent checkpoint system that survives scene reloads using DontDestroyOnLoad.
/// Stores player position and optionally game state (score, health, inventory).
/// Common use: Platformer checkpoints, racing game lap markers, save points in adventure games.
/// </summary>
public class GameCheckpointManager : MonoBehaviour
{
    private static GameCheckpointManager instance;

    [Header("Player Reference")]
    [Tooltip("The player GameObject to respawn. If null, searches for tag 'Player'")]
    [SerializeField] private GameObject playerObject;

    [Header("Checkpoint Settings")]
    [Tooltip("Should checkpoint data persist across scene reloads?")]
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Optional Data Persistence")]
    [Tooltip("Automatically save and restore score from GameCollectionManager")]
    [SerializeField] private bool saveScore = false;
    [SerializeField] private GameCollectionManager scoreManager;

    [Tooltip("Automatically save and restore health from GameHealthManager")]
    [SerializeField] private bool saveHealth = false;
    [SerializeField] private GameHealthManager healthManager;

    [Header("Events")]
    /// <summary>
    /// Fires when a checkpoint is saved
    /// </summary>
    public UnityEvent onCheckpointSaved;
    /// <summary>
    /// Fires when the player is restored to a checkpoint position
    /// </summary>
    public UnityEvent onCheckpointRestored;
    /// <summary>
    /// Fires when checkpoint position is saved, passing the saved position as a Vector3 parameter
    /// </summary>
    public UnityEvent<Vector3> onPositionSaved;  // Passes saved position

    // Saved checkpoint data
    private bool hasCheckpoint = false;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private int savedScore = 0;
    private int savedHealth = 100;

    // Public properties
    public bool HasCheckpoint => hasCheckpoint;
    public Vector3 SavedPosition => savedPosition;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Subscribe to scene loaded event for checkpoint restoration after scene reloads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Restore checkpoint whenever a scene loads
        if (hasCheckpoint)
        {
            StartCoroutine(RestoreCheckpointAfterPlayerSpawns());
        }
    }

    private System.Collections.IEnumerator RestoreCheckpointAfterPlayerSpawns()
    {
        // Try to find player immediately
        GameObject player = GetPlayerObject();

        // If player exists, hide it immediately to prevent camera flash
        Renderer[] renderers = null;
        if (player != null)
        {
            renderers = player.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }
        }

        // Wait for end of frame to ensure all objects are spawned and initialized
        yield return new WaitForEndOfFrame();

        // Try to find player up to 10 times if not found yet (10 frames max)
        int attempts = 0;
        while (player == null && attempts < 10)
        {
            player = GetPlayerObject();
            if (player != null)
            {
                // Hide newly found player
                renderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    r.enabled = false;
                }
                break;
            }
            attempts++;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogError("GameCheckpointManager: Could not find player after 10 attempts! Make sure player has 'Player' tag.");
            yield break;
        }

        // Wait one additional frame to ensure player's Start/Awake has completed
        yield return null;

        // Now restore checkpoint
        RestoreCheckpoint();

        // Wait for physics to process the new position
        yield return new WaitForFixedUpdate();

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = savedPosition;
            rb.rotation = savedRotation;
        }
        else
        {
            player.transform.position = savedPosition;
            player.transform.rotation = savedRotation;
        }

        // Wait one more frame to ensure camera has moved with player
        yield return null;

        // Re-enable renderers to make player visible at checkpoint
        if (renderers != null)
        {
            foreach (Renderer r in renderers)
            {
                r.enabled = true;
            }
        }
    }

    /// <summary>
    /// Save current player position as checkpoint
    /// </summary>
    public void SaveCheckpointPosition()
    {
        GameObject player = GetPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("GameCheckpointManager: No player found to save checkpoint!");
            return;
        }

        savedPosition = player.transform.position;
        savedRotation = player.transform.rotation;
        hasCheckpoint = true;

        Debug.Log($"Checkpoint saved at {savedPosition}");

        onPositionSaved.Invoke(savedPosition);
        onCheckpointSaved.Invoke();
    }

    /// <summary>
    /// Save specific position as checkpoint (for checkpoint zones)
    /// </summary>
    public void SaveCheckpointAtPosition(Vector3 position)
    {
        savedPosition = position;
        savedRotation = Quaternion.identity; // Default rotation
        hasCheckpoint = true;

        Debug.Log($"Checkpoint saved at {savedPosition}");

        onPositionSaved.Invoke(savedPosition);
        onCheckpointSaved.Invoke();
    }

    /// <summary>
    /// Save specific position and rotation as checkpoint
    /// </summary>
    public void SaveCheckpointAtPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        savedPosition = position;
        savedRotation = rotation;
        hasCheckpoint = true;

        Debug.Log($"Checkpoint saved at {savedPosition} with rotation {savedRotation.eulerAngles}");

        onPositionSaved.Invoke(savedPosition);
        onCheckpointSaved.Invoke();
    }

    /// <summary>
    /// Save checkpoint with position and optional game data
    /// </summary>
    public void SaveCheckpointFull()
    {
        SaveCheckpointPosition();

        // Save score if enabled
        if (saveScore && scoreManager != null)
        {
            savedScore = scoreManager.GetCurrentValue();
        }

        // Save health if enabled
        if (saveHealth && healthManager != null)
        {
            savedHealth = healthManager.CurrentHealth;
        }
    }

    /// <summary>
    /// Save checkpoint at specific position with optional game data
    /// </summary>
    public void SaveCheckpointFullAtPosition(Vector3 position)
    {
        SaveCheckpointAtPosition(position);

        // Save score if enabled
        if (saveScore && scoreManager != null)
        {
            savedScore = scoreManager.GetCurrentValue();
        }

        // Save health if enabled
        if (saveHealth && healthManager != null)
        {
            savedHealth = healthManager.CurrentHealth;
        }
    }

    /// <summary>
    /// Save score value manually (for event wiring)
    /// </summary>
    public void SaveScore(int score)
    {
        savedScore = score;
    }

    /// <summary>
    /// Save health value manually (for event wiring)
    /// </summary>
    public void SaveHealth(int health)
    {
        savedHealth = health;
    }

    /// <summary>
    /// Restore player to checkpoint position
    /// </summary>
    public void RestoreCheckpoint()
    {
        if (!hasCheckpoint)
        {
            Debug.Log("No checkpoint to restore");
            return;
        }

        GameObject player = GetPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("GameCheckpointManager: No player found to restore!");
            return;
        }

        // Get Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();

        // Stop all physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // Put rigidbody to sleep to prevent immediate physics updates
        }

        // Restore position and rotation
        player.transform.position = savedPosition;
        player.transform.rotation = savedRotation;

        // Wake rigidbody after position is set
        if (rb != null)
        {
            rb.WakeUp();
        }

        Debug.Log($"Checkpoint restored to {savedPosition}, player is now at {player.transform.position}");
        onCheckpointRestored.Invoke();
    }

    /// <summary>
    /// Restore saved score to the score manager
    /// </summary>
    public void RestoreScore()
    {
        if (saveScore && scoreManager != null)
        {
            scoreManager.SetValue(savedScore);
        }
    }

    /// <summary>
    /// Restore saved health to the health manager
    /// </summary>
    public void RestoreHealth()
    {
        if (saveHealth && healthManager != null)
        {
            healthManager.SetHealth(savedHealth);
        }
    }

    /// <summary>
    /// Restore all saved data (position, score, health)
    /// </summary>
    public void RestoreAll()
    {
        RestoreCheckpoint();
        RestoreScore();
        RestoreHealth();
    }

    /// <summary>
    /// Clear checkpoint data
    /// </summary>
    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        Debug.Log("Checkpoint cleared");
    }

    /// <summary>
    /// Get or find the player object
    /// </summary>
    private GameObject GetPlayerObject()
    {
        // Always search by tag after scene loads - don't rely on cached reference
        // The cached reference becomes invalid when scene reloads (player gets destroyed/recreated)
        try
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerObject = player; // Update cache
                Debug.Log($"GameCheckpointManager: Found player '{player.name}' at {player.transform.position}");
                return player;
            }
        }
        catch (UnityException)
        {
            // Tag doesn't exist
            Debug.LogError("GameCheckpointManager: 'Player' tag doesn't exist! Add it in Tag Manager.");
            return null;
        }

        Debug.LogWarning("GameCheckpointManager: Could not find player! Make sure player is tagged as 'Player'");
        return null;
    }

    /// <summary>
    /// Get the singleton instance
    /// </summary>
    public static GameCheckpointManager Instance => instance;
}
