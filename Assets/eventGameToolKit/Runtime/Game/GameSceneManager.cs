using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene loading for multi-scene games with persistent player and UI.
/// Handles additive scene loading, spawn point selection, and scene transitions.
///
/// USAGE PATTERN - Bootstrap Scene:
/// 1. Create a "Bootstrap" scene with Player, Managers, UI, and this GameSceneManager
/// 2. Create level scenes with SpawnPoint components marking entry locations
/// 3. Use UnityEvents to call LoadScene() or LoadSceneAtSpawnPoint() for transitions
///
/// This manager persists across scenes using DontDestroyOnLoad and coordinates
/// with SpawnPoint components to position the player correctly after scene loads.
///
/// Common use: Multi-level games, hub worlds with connected areas, seamless scene transitions.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    private static GameSceneManager instance;

    [Header("Scene Loading Settings")]
    [Tooltip("Should this manager persist across scene loads?")]
    [SerializeField] private bool persistAcrossScenes = true;

    [Tooltip("Use additive scene loading (recommended for persistent player)")]
    [SerializeField] private bool useAdditiveLoading = true;

    [Tooltip("Automatically unload the previous scene when loading a new one")]
    [SerializeField] private bool unloadPreviousScene = true;

    [Header("Transition Settings")]
    [Tooltip("Delay before starting scene load (for fade out effects)")]
    [SerializeField] private float preLoadDelay = 0f;

    [Tooltip("Delay after scene load before activating (for fade in effects)")]
    [SerializeField] private float postLoadDelay = 0f;

    [Header("Events")]
    /// <summary>
    /// Fires when scene loading begins
    /// </summary>
    public UnityEvent onSceneLoadStarted;

    /// <summary>
    /// Fires when the new scene is fully loaded and active
    /// </summary>
    public UnityEvent onSceneLoadCompleted;

    /// <summary>
    /// Fires when scene loading fails
    /// </summary>
    public UnityEvent onSceneLoadFailed;

    /// <summary>
    /// Fires with the name of the scene being loaded
    /// </summary>
    public UnityEvent<string> onSceneLoading;

    /// <summary>
    /// Fires with load progress (0-1)
    /// </summary>
    public UnityEvent<float> onLoadProgress;

    // Internal state
    private string currentLevelScene = "";
    private bool isLoading = false;
    private string bootstrapSceneName = "";

    #region Singleton & Lifecycle

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
                Debug.LogWarning($"GameSceneManager: Moving to root hierarchy for DontDestroyOnLoad.", this);
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        // Remember the bootstrap scene
        bootstrapSceneName = SceneManager.GetActiveScene().name;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Get the singleton instance.
    /// </summary>
    public static GameSceneManager Instance => instance;

    #endregion

    #region Scene Loading Methods

    /// <summary>
    /// Load a scene by name. Player will spawn at the default SpawnPoint.
    /// Wire this to UnityEvents for no-code scene transitions.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load (must be in Build Settings)</param>
    public void LoadScene(string sceneName)
    {
        LoadSceneAtSpawnPoint(sceneName, "");
    }

    /// <summary>
    /// Load a scene and spawn the player at a specific spawn point.
    /// Use this when entering a scene from different locations (e.g., different doors).
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    /// <param name="spawnPointId">ID of the SpawnPoint to use, or empty for default</param>
    public void LoadSceneAtSpawnPoint(string sceneName, string spawnPointId)
    {
        if (isLoading)
        {
            Debug.LogWarning("GameSceneManager: Already loading a scene, ignoring request.");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, spawnPointId));
    }

    /// <summary>
    /// Reload the current level scene.
    /// </summary>
    public void ReloadCurrentScene()
    {
        if (string.IsNullOrEmpty(currentLevelScene))
        {
            Debug.LogWarning("GameSceneManager: No current level scene to reload.");
            return;
        }

        LoadScene(currentLevelScene);
    }

    /// <summary>
    /// Load a scene non-additively (replaces all scenes).
    /// Use for returning to main menu or complete scene changes.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    public void LoadSceneSingle(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("GameSceneManager: Already loading a scene, ignoring request.");
            return;
        }

        StartCoroutine(LoadSceneSingleCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, string spawnPointId)
    {
        isLoading = true;

        // Pre-load delay for transitions
        if (preLoadDelay > 0)
        {
            yield return new WaitForSeconds(preLoadDelay);
        }

        onSceneLoadStarted.Invoke();
        onSceneLoading.Invoke(sceneName);

        // Request the spawn point before loading
        if (!string.IsNullOrEmpty(spawnPointId))
        {
            SpawnPoint.RequestSpawnId(spawnPointId);
        }
        else
        {
            SpawnPoint.ClearRequestedSpawnId();
        }

        // Unload previous level scene if needed
        if (useAdditiveLoading && unloadPreviousScene && !string.IsNullOrEmpty(currentLevelScene))
        {
            Scene previousScene = SceneManager.GetSceneByName(currentLevelScene);
            if (previousScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(previousScene);
                while (unloadOp != null && !unloadOp.isDone)
                {
                    yield return null;
                }
            }
        }

        // Load the new scene
        LoadSceneMode mode = useAdditiveLoading ? LoadSceneMode.Additive : LoadSceneMode.Single;
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, mode);

        if (loadOp == null)
        {
            Debug.LogError($"GameSceneManager: Failed to load scene '{sceneName}'. Is it in Build Settings?");
            onSceneLoadFailed.Invoke();
            isLoading = false;
            yield break;
        }

        // Track progress
        while (!loadOp.isDone)
        {
            onLoadProgress.Invoke(loadOp.progress);
            yield return null;
        }

        // Set as active scene so new objects spawn there
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        currentLevelScene = sceneName;

        // Post-load delay for transitions
        if (postLoadDelay > 0)
        {
            yield return new WaitForSeconds(postLoadDelay);
        }

        onLoadProgress.Invoke(1f);
        isLoading = false;
        onSceneLoadCompleted.Invoke();

        Debug.Log($"GameSceneManager: Loaded scene '{sceneName}'");
    }

    private IEnumerator LoadSceneSingleCoroutine(string sceneName)
    {
        isLoading = true;
        SpawnPoint.ClearRequestedSpawnId();

        if (preLoadDelay > 0)
        {
            yield return new WaitForSeconds(preLoadDelay);
        }

        onSceneLoadStarted.Invoke();
        onSceneLoading.Invoke(sceneName);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (loadOp == null)
        {
            Debug.LogError($"GameSceneManager: Failed to load scene '{sceneName}'. Is it in Build Settings?");
            onSceneLoadFailed.Invoke();
            isLoading = false;
            yield break;
        }

        while (!loadOp.isDone)
        {
            onLoadProgress.Invoke(loadOp.progress);
            yield return null;
        }

        currentLevelScene = sceneName;

        if (postLoadDelay > 0)
        {
            yield return new WaitForSeconds(postLoadDelay);
        }

        onLoadProgress.Invoke(1f);
        isLoading = false;
        onSceneLoadCompleted.Invoke();

        Debug.Log($"GameSceneManager: Loaded scene '{sceneName}' (single mode)");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Check if a scene is currently loaded.
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.isLoaded;
    }

    /// <summary>
    /// Get the name of the current level scene.
    /// </summary>
    public string CurrentLevelScene => currentLevelScene;

    /// <summary>
    /// Get the name of the bootstrap scene.
    /// </summary>
    public string BootstrapSceneName => bootstrapSceneName;

    /// <summary>
    /// Whether a scene is currently being loaded.
    /// </summary>
    public bool IsLoading => isLoading;

    /// <summary>
    /// Set the pre-load delay at runtime.
    /// </summary>
    public void SetPreLoadDelay(float delay)
    {
        preLoadDelay = Mathf.Max(0, delay);
    }

    /// <summary>
    /// Set the post-load delay at runtime.
    /// </summary>
    public void SetPostLoadDelay(float delay)
    {
        postLoadDelay = Mathf.Max(0, delay);
    }

    #endregion
}
