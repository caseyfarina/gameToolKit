using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent checkpoint system that survives scene reloads using DontDestroyOnLoad.
/// Stores player position and optionally game state (score, health, inventory).
/// 
/// ARCHITECTURE: This manager implements ISpawnPointProvider, making it a passive data holder.
/// Instead of actively teleporting players (which causes race conditions), it answers
/// "where should I spawn?" when players initialize. This eliminates timing issues entirely.
/// 
/// The player's character controller checks for ISpawnPointProvider during Start() and
/// spawns at the correct position before physics ever runs.
/// 
/// Common use: Platformer checkpoints, racing game lap markers, save points in adventure games.
/// </summary>
public class GameCheckpointManager : MonoBehaviour, ISpawnPointProvider
{
    private static GameCheckpointManager instance;

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
    /// Fires when the player spawns at a checkpoint (called by player, not by this manager)
    /// </summary>
    public UnityEvent onCheckpointRestored;
    
    /// <summary>
    /// Fires when checkpoint position is saved, passing the saved position as a Vector3 parameter
    /// </summary>
    public UnityEvent<Vector3> onPositionSaved;

    // Saved checkpoint data
    private bool hasCheckpoint = false;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private string savedSceneName;
    private int savedScore = 0;
    private int savedHealth = 100;

    #region ISpawnPointProvider Implementation

    /// <summary>
    /// Returns true if a checkpoint has been saved and is available for spawning.
    /// </summary>
    public bool HasSpawnPoint => hasCheckpoint;

    /// <summary>
    /// The saved checkpoint position. Only valid if HasSpawnPoint is true.
    /// </summary>
    public Vector3 SpawnPosition => savedPosition;

    /// <summary>
    /// The saved checkpoint rotation. Only valid if HasSpawnPoint is true.
    /// </summary>
    public Quaternion SpawnRotation => savedRotation;

    /// <summary>
    /// Called by the player after spawning at the checkpoint.
    /// Fires the onCheckpointRestored event for UI/audio feedback.
    /// </summary>
    public void OnSpawnPointUsed()
    {
        Debug.Log($"GameCheckpointManager: Player spawned at checkpoint {savedPosition}");
        onCheckpointRestored.Invoke();
        
        // Restore game data if configured
        RestoreScore();
        RestoreHealth();
    }

    #endregion

    #region Public Properties (Legacy Compatibility)

    public bool HasCheckpoint => hasCheckpoint;
    public Vector3 SavedPosition => savedPosition;
    public Quaternion SavedRotation => savedRotation;

    #endregion

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
            // DontDestroyOnLoad only works on root GameObjects
            if (transform.parent != null)
            {
                Debug.LogWarning($"GameCheckpointManager: Moving to root hierarchy for DontDestroyOnLoad.", this);
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    #region Checkpoint Saving Methods

    /// <summary>
    /// Save current player position as checkpoint.
    /// Finds player by tag.
    /// </summary>
    public void SaveCheckpointPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("GameCheckpointManager: No player found to save checkpoint!");
            return;
        }

        SaveCheckpointAtPositionAndRotation(player.transform.position, player.transform.rotation);
    }

    /// <summary>
    /// Save specific position as checkpoint (for checkpoint zones).
    /// Uses identity rotation.
    /// </summary>
    public void SaveCheckpointAtPosition(Vector3 position)
    {
        SaveCheckpointAtPositionAndRotation(position, Quaternion.identity);
    }

    /// <summary>
    /// Save specific position and rotation as checkpoint.
    /// </summary>
    public void SaveCheckpointAtPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        savedPosition = position;
        savedRotation = rotation;
        savedSceneName = SceneManager.GetActiveScene().name;
        hasCheckpoint = true;

        Debug.Log($"GameCheckpointManager: Checkpoint saved at {savedPosition}");

        onPositionSaved.Invoke(savedPosition);
        onCheckpointSaved.Invoke();
    }

    /// <summary>
    /// Save checkpoint with position and optional game data (score, health).
    /// </summary>
    public void SaveCheckpointFull()
    {
        SaveCheckpointPosition();
        SaveGameData();
    }

    /// <summary>
    /// Save checkpoint at specific position with optional game data.
    /// </summary>
    public void SaveCheckpointFullAtPosition(Vector3 position)
    {
        SaveCheckpointAtPosition(position);
        SaveGameData();
    }

    private void SaveGameData()
    {
        if (saveScore && scoreManager != null)
        {
            savedScore = scoreManager.GetCurrentValue();
        }

        if (saveHealth && healthManager != null)
        {
            savedHealth = healthManager.CurrentHealth;
        }
    }

    /// <summary>
    /// Save score value manually (for event wiring).
    /// </summary>
    public void SaveScore(int score)
    {
        savedScore = score;
    }

    /// <summary>
    /// Save health value manually (for event wiring).
    /// </summary>
    public void SaveHealth(int health)
    {
        savedHealth = health;
    }

    #endregion

    #region Checkpoint Restoration Methods

    /// <summary>
    /// Restore saved score to the score manager.
    /// Called automatically when player spawns at checkpoint if saveScore is enabled.
    /// </summary>
    public void RestoreScore()
    {
        if (saveScore && scoreManager != null)
        {
            scoreManager.SetValue(savedScore);
        }
    }

    /// <summary>
    /// Called by ActionRespawnPlayer when full state restoration is requested.
    /// (Fixes error CS1061: '...RestoreAll' not found)
    /// </summary>
    public void RestoreAll()
    {
        // A full restore should restore game data AND teleport the player.
        RestoreScore();
        RestoreHealth();
        TeleportPlayerToCheckpoint();
    }

    /// <summary>
    /// Called by ActionRespawnPlayer when only minimal restoration is requested.
    /// (Fixes error CS1061: '...RestoreCheckpoint' not found)
    /// </summary>
    public void RestoreCheckpoint()
    {
        // A minimal restore only teleports the player, leaving score/health as they are.
        TeleportPlayerToCheckpoint();
    }

    /// <summary>
    /// Restore saved health to the health manager.
    /// Called automatically when player spawns at checkpoint if saveHealth is enabled.
    /// </summary>
    public void RestoreHealth()
    {
        if (saveHealth && healthManager != null)
        {
            healthManager.SetHealth(savedHealth);
        }
    }

    /// <summary>
    /// Manual teleport for same-scene respawns (e.g., player death without scene reload).
    /// For scene reloads, the player automatically uses ISpawnPointProvider.
    /// </summary>
    public void TeleportPlayerToCheckpoint()
    {
        if (!hasCheckpoint)
        {
            Debug.Log("GameCheckpointManager: No checkpoint to restore");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("GameCheckpointManager: No player found!");
            return;
        }

        // Try to use player's TeleportTo method if available
        player.SendMessage("TeleportTo", savedPosition, SendMessageOptions.DontRequireReceiver);
        
        // Verify it worked, fallback to direct manipulation if not
        if (Vector3.Distance(player.transform.position, savedPosition) > 0.1f)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = savedPosition;
                player.transform.rotation = savedRotation;
                cc.enabled = true;
            }
            else
            {
                player.transform.position = savedPosition;
                player.transform.rotation = savedRotation;
            }
        }
        else
        {
            player.transform.rotation = savedRotation;
        }
        
        Debug.Log($"GameCheckpointManager: Player teleported to checkpoint {savedPosition}");
        onCheckpointRestored.Invoke();
        
        RestoreScore();
        RestoreHealth();
    }

    /// <summary>
    /// Clear checkpoint data.
    /// </summary>
    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        Debug.Log("GameCheckpointManager: Checkpoint cleared");
    }

    #endregion

    /// <summary>
    /// Get the singleton instance.
    /// </summary>
    public static GameCheckpointManager Instance => instance;
}
