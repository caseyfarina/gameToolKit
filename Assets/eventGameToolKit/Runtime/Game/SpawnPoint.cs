using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Marks a location in a scene where players should spawn when entering.
/// Implements ISpawnPointProvider with lower priority than GameCheckpointManager.
///
/// PRIORITY SYSTEM: When multiple ISpawnPointProviders exist:
/// 1. GameCheckpointManager (if has checkpoint) - highest priority
/// 2. SpawnPoint with matching spawnId (if GameSceneManager requested specific ID)
/// 3. SpawnPoint marked as defaultSpawnPoint
/// 4. First SpawnPoint found
///
/// Common use: Level entry points, scene transitions, multiplayer spawn locations.
/// </summary>
public class SpawnPoint : MonoBehaviour, ISpawnPointProvider
{
    [Header("Spawn Point Settings")]
    [Tooltip("Unique identifier for this spawn point. Used when loading scenes with a specific entry point.")]
    [SerializeField] private string spawnId = "";

    [Tooltip("If true, this is the default spawn point when no specific ID is requested.")]
    [SerializeField] private bool isDefaultSpawnPoint = true;

    [Tooltip("Offset from this transform's position where the player will spawn.")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Events")]
    /// <summary>
    /// Fires when a player spawns at this spawn point
    /// </summary>
    public UnityEvent onPlayerSpawned;

    // Static reference to track which spawn point should be used
    private static string requestedSpawnId = null;
    private static bool spawnIdRequested = false;

    #region ISpawnPointProvider Implementation

    /// <summary>
    /// Returns true if this spawn point should be used.
    /// Considers priority: checkpoint manager takes precedence, then spawn ID matching.
    /// </summary>
    public bool HasSpawnPoint
    {
        get
        {
            // If a checkpoint manager exists with a valid checkpoint, defer to it
            if (GameCheckpointManager.Instance != null && GameCheckpointManager.Instance.HasCheckpoint)
            {
                return false;
            }

            // If a specific spawn ID was requested, only match if we have that ID
            if (spawnIdRequested && !string.IsNullOrEmpty(requestedSpawnId))
            {
                return spawnId == requestedSpawnId;
            }

            // Otherwise, use default spawn point logic
            return isDefaultSpawnPoint;
        }
    }

    /// <summary>
    /// The world position where the player should spawn.
    /// </summary>
    public Vector3 SpawnPosition => transform.position + transform.TransformDirection(spawnOffset);

    /// <summary>
    /// The world rotation the player should have when spawning.
    /// </summary>
    public Quaternion SpawnRotation => transform.rotation;

    /// <summary>
    /// Called when a player uses this spawn point.
    /// </summary>
    public void OnSpawnPointUsed()
    {
        Debug.Log($"SpawnPoint '{gameObject.name}': Player spawned at {SpawnPosition}");

        // Clear the requested spawn ID after use
        ClearRequestedSpawnId();

        onPlayerSpawned.Invoke();
    }

    #endregion

    #region Static Spawn ID Management

    /// <summary>
    /// Request a specific spawn point by ID for the next scene load.
    /// Called by GameSceneManager before loading a scene.
    /// </summary>
    /// <param name="id">The spawn point ID to use, or empty/null for default.</param>
    public static void RequestSpawnId(string id)
    {
        requestedSpawnId = id;
        spawnIdRequested = !string.IsNullOrEmpty(id);
        Debug.Log($"SpawnPoint: Requested spawn ID '{id}'");
    }

    /// <summary>
    /// Clear the requested spawn ID.
    /// </summary>
    public static void ClearRequestedSpawnId()
    {
        requestedSpawnId = null;
        spawnIdRequested = false;
    }

    /// <summary>
    /// Get the currently requested spawn ID.
    /// </summary>
    public static string RequestedSpawnId => requestedSpawnId;

    #endregion

    #region Public Properties

    /// <summary>
    /// The unique identifier for this spawn point.
    /// </summary>
    public string SpawnId => spawnId;

    /// <summary>
    /// Whether this is the default spawn point for the scene.
    /// </summary>
    public bool IsDefaultSpawnPoint => isDefaultSpawnPoint;

    #endregion

    #region Editor Gizmos

    private void OnDrawGizmos()
    {
        // Draw spawn position
        Vector3 spawnPos = SpawnPosition;

        // Player silhouette
        Gizmos.color = isDefaultSpawnPoint ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(spawnPos + Vector3.up * 0.9f, 0.3f); // Head
        Gizmos.DrawWireCube(spawnPos + Vector3.up * 0.45f, new Vector3(0.5f, 0.9f, 0.3f)); // Body

        // Direction arrow
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward * 1.5f;
        Gizmos.DrawRay(spawnPos + Vector3.up * 0.5f, forward);
        Gizmos.DrawRay(spawnPos + Vector3.up * 0.5f + forward, Quaternion.Euler(0, 150, 0) * -forward * 0.3f);
        Gizmos.DrawRay(spawnPos + Vector3.up * 0.5f + forward, Quaternion.Euler(0, -150, 0) * -forward * 0.3f);

        // Spawn offset indicator
        if (spawnOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, spawnPos);
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn ID label area
        Vector3 spawnPos = SpawnPosition;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(spawnPos + Vector3.up * 2f, new Vector3(1f, 0.3f, 0.1f));
    }

    #endregion
}
