using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Automatically spawns prefabs from a list at randomized intervals with optional auto-cleanup.
/// Features: Random timing, positional variance, spawn limits, lifetime tracking, and automatic cleanup.
/// Common use: Enemy spawners, item generators, particle effect triggers, or obstacle creation systems.
/// </summary>
public class ActionAutoSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("List of prefabs to spawn randomly from")]
    [SerializeField] private GameObject[] spawnPrefabs;

    [Tooltip("Minimum time in seconds between spawns")]
    [SerializeField] private float spawnRateMin = 1f;

    [Tooltip("Maximum time in seconds between spawns")]
    [SerializeField] private float spawnRateMax = 5f;

    [Tooltip("Random positional offset range (0 = spawn at exact position)")]
    [SerializeField] private float spawnPositionRange = 0f;

    [Header("Spawn Limits (Optional)")]
    [Tooltip("Maximum number of spawned objects that can exist at once (0 = unlimited)")]
    [SerializeField] private int maxActiveSpawns = 0;

    [Tooltip("Maximum total spawns before stopping (0 = unlimited)")]
    [SerializeField] private int maxTotalSpawns = 0;

    [Header("Auto-Cleanup (Optional)")]
    [Tooltip("Automatically destroy spawned objects after this many seconds (0 = never destroy)")]
    [SerializeField] private float spawnLifetime = 0f;

    [Tooltip("If enabled, spawner starts automatically on Start()")]
    [SerializeField] private bool autoStart = true;

    [Header("Events")]
    /// <summary>
    /// Fires when a new object is spawned
    /// </summary>
    public UnityEvent onSpawn;

    /// <summary>
    /// Fires when a spawned object is automatically destroyed
    /// </summary>
    public UnityEvent onSpawnDestroyed;

    /// <summary>
    /// Fires when max total spawns is reached
    /// </summary>
    public UnityEvent onMaxSpawnsReached;

    // Private state
    private float time = 0f;
    private float nextSpawnTime = 0f;
    private int totalSpawnCount = 0;
    private bool isSpawning = false;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Start()
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0)
        {
            Debug.LogWarning($"ActionAutoSpawner on {gameObject.name}: No spawn prefabs assigned!", this);
            return;
        }

        nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax);

        if (autoStart)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        if (!isSpawning) return;

        time += Time.deltaTime;

        if (time >= nextSpawnTime)
        {
            SpawnObject();
        }
    }

    /// <summary>
    /// Starts the automatic spawning process
    /// </summary>
    public void StartSpawning()
    {
        isSpawning = true;
    }

    /// <summary>
    /// Stops the automatic spawning process
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    /// <summary>
    /// Manually triggers a single spawn
    /// </summary>
    public void SpawnObject()
    {
        // Check max total spawns limit
        if (maxTotalSpawns > 0 && totalSpawnCount >= maxTotalSpawns)
        {
            StopSpawning();
            onMaxSpawnsReached?.Invoke();
            return;
        }

        // Check max active spawns limit
        if (maxActiveSpawns > 0)
        {
            // Clean up null references (objects destroyed externally)
            spawnedObjects.RemoveAll(obj => obj == null);

            if (spawnedObjects.Count >= maxActiveSpawns)
            {
                // Don't spawn if at limit, but keep checking next frame
                nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax) + time;
                return;
            }
        }

        // Choose random prefab from list
        int spawnIndex = Random.Range(0, spawnPrefabs.Length);
        GameObject prefabToSpawn = spawnPrefabs[spawnIndex];

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"ActionAutoSpawner on {gameObject.name}: Spawn prefab at index {spawnIndex} is null!", this);
            nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax) + time;
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = transform.position;
        if (spawnPositionRange > 0f)
        {
            spawnPosition += Random.insideUnitSphere * spawnPositionRange;
        }

        // Instantiate the object
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);

        // Track spawned object
        spawnedObjects.Add(spawnedObject);
        totalSpawnCount++;

        // Set up auto-cleanup if lifetime is configured
        if (spawnLifetime > 0f)
        {
            StartCoroutine(DestroyAfterLifetime(spawnedObject, spawnLifetime));
        }

        // Fire spawn event
        onSpawn?.Invoke();

        // Set next spawn time
        nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax) + time;
    }

    /// <summary>
    /// Destroys all currently spawned objects immediately
    /// </summary>
    public void DestroyAllSpawns()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
    }

    /// <summary>
    /// Resets the spawner to initial state (destroys all spawns and resets counters)
    /// </summary>
    public void ResetSpawner()
    {
        DestroyAllSpawns();
        totalSpawnCount = 0;
        time = 0f;
        nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax);
    }

    private IEnumerator DestroyAfterLifetime(GameObject spawnedObject, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (spawnedObject != null)
        {
            spawnedObjects.Remove(spawnedObject);
            Destroy(spawnedObject);
            onSpawnDestroyed?.Invoke();
        }
    }

    /// <summary>
    /// Sets the minimum time between spawns in seconds
    /// </summary>
    public void SetSpawnTimeMinimum(float spawnTimeMinimum)
    {
        spawnRateMin = Mathf.Max(0f, spawnTimeMinimum);
    }

    /// <summary>
    /// Sets the maximum time between spawns in seconds
    /// </summary>
    public void SetSpawnTimeMaximum(float spawnTimeMaximum)
    {
        spawnRateMax = Mathf.Max(spawnRateMin, spawnTimeMaximum);
    }

    /// <summary>
    /// Sets the spawn lifetime in seconds (0 = never destroy)
    /// </summary>
    public void SetSpawnLifetime(float lifetime)
    {
        spawnLifetime = Mathf.Max(0f, lifetime);
    }

    /// <summary>
    /// Sets the maximum number of active spawns (0 = unlimited)
    /// </summary>
    public void SetMaxActiveSpawns(int maxActive)
    {
        maxActiveSpawns = Mathf.Max(0, maxActive);
    }

    // Public properties for read-only access
    public int TotalSpawnCount => totalSpawnCount;
    public int ActiveSpawnCount => spawnedObjects.Count;
    public bool IsSpawning => isSpawning;

    private void OnDestroy()
    {
        // Clean up all coroutines
        StopAllCoroutines();
    }
}
