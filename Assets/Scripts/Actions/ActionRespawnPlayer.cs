using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Respawns player to last checkpoint without reloading the scene.
/// Works with GameCheckpointManager to restore position and optionally game state.
/// Common use: Death respawn, fall zone recovery, manual respawn button, puzzle reset.
/// </summary>
public class ActionRespawnPlayer : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Delay before respawning (allows death animations to play)")]
    [SerializeField] private float respawnDelay = 1f;

    [Tooltip("Restore full game state (score, health) when respawning?")]
    [SerializeField] private bool restoreFullState = false;

    [Tooltip("If no checkpoint exists, respawn at this transform")]
    [SerializeField] private Transform fallbackSpawnPoint;

    [Header("References")]
    [Tooltip("GameCheckpointManager reference. If null, searches automatically.")]
    [SerializeField] private GameCheckpointManager checkpointPersistence;

    [Tooltip("Player object to respawn. If null, searches for 'Player' tag.")]
    [SerializeField] private GameObject playerObject;

    [Header("Visual Effects")]
    [Tooltip("Prefab to spawn at respawn location (e.g., particle effect)")]
    [SerializeField] private GameObject respawnEffect;

    [Tooltip("Duration to keep respawn effect alive")]
    [SerializeField] private float effectDuration = 2f;

    [Header("Events")]
    /// <summary>
    /// Fires when the respawn process begins
    /// </summary>
    public UnityEvent onRespawnStarted;
    /// <summary>
    /// Fires when the respawn process completes successfully
    /// </summary>
    public UnityEvent onRespawnCompleted;
    /// <summary>
    /// Fires when respawn is attempted but no checkpoint or fallback spawn point is available
    /// </summary>
    public UnityEvent onNoCheckpointAvailable;

    private bool isRespawning = false;

    private void Start()
    {
        // Find checkpoint persistence if not assigned
        if (checkpointPersistence == null)
        {
            checkpointPersistence = FindFirstObjectByType<GameCheckpointManager>();
        }

        // Find player if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
    }

    /// <summary>
    /// Respawn player to last checkpoint (call via UnityEvent)
    /// </summary>
    public void RespawnToCheckpoint()
    {
        if (isRespawning)
        {
            return;
        }

        StartCoroutine(RespawnCoroutine());
    }

    /// <summary>
    /// Respawn immediately without delay
    /// </summary>
    public void RespawnImmediate()
    {
        if (isRespawning)
        {
            return;
        }

        StartCoroutine(RespawnCoroutine(0f));
    }

    private IEnumerator RespawnCoroutine(float? customDelay = null)
    {
        isRespawning = true;
        onRespawnStarted.Invoke();

        // Wait for delay
        float delay = customDelay ?? respawnDelay;
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Check if checkpoint exists
        if (checkpointPersistence != null && checkpointPersistence.HasCheckpoint)
        {
            // Restore checkpoint
            if (restoreFullState)
            {
                checkpointPersistence.RestoreAll();
            }
            else
            {
                checkpointPersistence.RestoreCheckpoint();
            }

            // Spawn visual effect
            if (respawnEffect != null)
            {
                GameObject effect = Instantiate(respawnEffect, checkpointPersistence.SavedPosition, Quaternion.identity);
                Destroy(effect, effectDuration);
            }

            Debug.Log("Player respawned to checkpoint");
        }
        else if (fallbackSpawnPoint != null)
        {
            // Use fallback spawn point
            RespawnAtPosition(fallbackSpawnPoint.position, fallbackSpawnPoint.rotation);
            Debug.Log("No checkpoint available, using fallback spawn point");
        }
        else
        {
            // No checkpoint and no fallback
            Debug.LogWarning("ActionRespawnPlayer: No checkpoint or fallback spawn point available!");
            onNoCheckpointAvailable.Invoke();
            isRespawning = false;
            yield break;
        }

        onRespawnCompleted.Invoke();
        isRespawning = false;
    }

    /// <summary>
    /// Respawn player at specific position
    /// </summary>
    public void RespawnAtPosition(Vector3 position, Quaternion rotation)
    {
        GameObject player = GetPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("ActionRespawnPlayer: No player found!");
            return;
        }

        // Disable physics momentarily
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Move player
        player.transform.position = position;
        player.transform.rotation = rotation;

        // Spawn effect
        if (respawnEffect != null)
        {
            GameObject effect = Instantiate(respawnEffect, position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }

    /// <summary>
    /// Set the fallback spawn point at runtime
    /// </summary>
    public void SetFallbackSpawnPoint(Transform newSpawnPoint)
    {
        fallbackSpawnPoint = newSpawnPoint;
    }

    private GameObject GetPlayerObject()
    {
        if (playerObject != null)
        {
            return playerObject;
        }

        // Try to find by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerObject = player;
        }

        return playerObject;
    }

    public bool IsRespawning => isRespawning;
}
