using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Loads every shipped example scene, runs it for a moment, and fails on any error or
/// exception logged while it runs.
///
/// This is deliberately assertion-free about gameplay. Its whole job is to catch the
/// class of bug that compiles perfectly and only misbehaves once a scene actually runs:
/// a null dereference on a component whose RequireComponent was removed, a validation
/// routine that reports a valid setup as broken, a missing reference in a scene. Several
/// bugs of exactly that shape reached students before this existed.
///
/// A scene is added here by adding it to the Build Settings scene list; nothing needs
/// editing in this file.
/// </summary>
// Named to sort last. The runner executes fixtures alphabetically, and loading real scenes
// mutates global state — Input System device pairing from PlayerInput components in
// particular — which corrupts the virtual devices that InputTestFixture-based tests rely on.
// "SceneSmokeTests" sorts after CharacterController*, EGTK* and Input*, so that
// contamination stays one-way instead of failing unrelated tests by run order. NUnit's
// [Order] attribute cannot express this: it is valid on methods only, and orders within a
// fixture rather than between them.
public class SceneSmokeTests
{
    // Seconds each scene is left running. Long enough for Awake/Start/OnEnable, several
    // physics steps, and any first-frame coroutines to surface a problem.
    private const float RunSeconds = 1.5f;

    private readonly List<string> _errors = new List<string>();

    [SetUp]
    public void SetUp()
    {
        _errors.Clear();
        Application.logMessageReceived += Capture;
    }

    [TearDown]
    public void TearDown()
    {
        Application.logMessageReceived -= Capture;
    }

    /// <summary>
    /// Leaves a clean, empty scene behind.
    ///
    /// Example scenes contain PlayerInput components, which pair real input devices. Left
    /// loaded, they corrupt the virtual devices that InputTestFixture-based tests create,
    /// making unrelated tests fail depending on run order. Unloading here keeps this suite
    /// from affecting anything that runs after it.
    /// </summary>
    [UnityTearDown]
    public IEnumerator UnloadScene()
    {
        Scene loaded = SceneManager.GetActiveScene();

        Scene blank = SceneManager.CreateScene("__SmokeTestBlank__");
        SceneManager.SetActiveScene(blank);

        if (loaded.IsValid() && loaded.isLoaded && loaded != blank)
        {
            yield return SceneManager.UnloadSceneAsync(loaded);
        }

        // Unloading a scene does not remove DontDestroyOnLoad objects. GameCheckpointManager
        // and any PlayerInput promoted alongside it survive, and a stale PlayerInput reading
        // a device that a later test removes throws deep inside the Input System. Clear them
        // so each scene starts, and leaves, from nothing.
        foreach (GameObject survivor in GetDontDestroyOnLoadObjects())
        {
            Object.DestroyImmediate(survivor);
        }

        yield return null;
    }

    // Unity exposes no direct accessor for the DontDestroyOnLoad scene, so a throwaway
    // object is placed in it and its scene queried.
    private static List<GameObject> GetDontDestroyOnLoadObjects()
    {
        GameObject probe = new GameObject("__probe__");
        Object.DontDestroyOnLoad(probe);
        Scene ddol = probe.scene;
        Object.DestroyImmediate(probe);

        return ddol.IsValid()
            ? new List<GameObject>(ddol.GetRootGameObjects())
            : new List<GameObject>();
    }

    private void Capture(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _errors.Add($"[{type}] {condition}");
        }
    }

    /// <summary>
    /// Every scene in the Build Settings list. Using the build list rather than a
    /// hard-coded array means a new example scene is covered as soon as it is added
    /// there, instead of being silently untested.
    /// </summary>
    private static IEnumerable<string> ScenePaths()
    {
        int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        if (count == 0)
        {
            // Yield a sentinel so the test reports a clear reason rather than silently
            // passing with zero cases.
            yield return "__NO_SCENES_IN_BUILD_SETTINGS__";
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            yield return SceneUtility.GetScenePathByBuildIndex(i);
        }
    }

    [UnityTest]
    public IEnumerator SceneRunsWithoutErrors([ValueSource(nameof(ScenePaths))] string scenePath)
    {
        if (scenePath == "__NO_SCENES_IN_BUILD_SETTINGS__")
        {
            Assert.Fail("No scenes in Build Settings, so no example scene is being smoke " +
                        "tested. Add the ExampleScenes to File > Build Settings.");
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
        yield return new WaitForSeconds(RunSeconds);

        Assert.IsEmpty(_errors,
            $"'{System.IO.Path.GetFileNameWithoutExtension(scenePath)}' logged " +
            $"{_errors.Count} error(s) while running:\n  " + string.Join("\n  ", _errors));
    }
}
