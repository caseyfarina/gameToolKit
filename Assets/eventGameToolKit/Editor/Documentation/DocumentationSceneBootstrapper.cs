using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hooks into scene-open events so that opening EGTK_Documentation.unity
/// automatically runs the Script Documentation Generator against the EGTK
/// Runtime folder — no Tools menu, no Play button required.
///
/// One-time setup: run Tools > Documentation > Create Documentation Scene
/// to create the scene file in ExampleScenes/.  After that, just open the
/// scene and documentation regenerates automatically.
/// </summary>
[InitializeOnLoad]
public static class DocumentationSceneBootstrapper
{
    private const string DocSceneName  = "EGTK_Documentation";
    private const string RuntimePath   = "Assets/eventGameToolKit/Runtime";
    private const string SceneSavePath = "Assets/eventGameToolKit/ExampleScenes/EGTK_Documentation.unity";

    static DocumentationSceneBootstrapper()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.name != DocSceneName) return;

        // CreateInstance does NOT show the window — it just allocates the object.
        var gen = ScriptableObject.CreateInstance<ScriptDocumentationGenerator>();
        gen.rootFolderPath = RuntimePath;
        gen.GenerateDocumentation();
        Object.DestroyImmediate(gen);

        Debug.Log("[EGTK] Documentation generated. Save the scene (Ctrl+S) to persist it.");
    }

    // ──────────────────────────────────────────────
    // One-time scene creation
    // ──────────────────────────────────────────────

    [MenuItem("Tools/Documentation/Create Documentation Scene")]
    public static void CreateDocumentationScene()
    {
        if (System.IO.File.Exists(SceneSavePath))
        {
            EditorUtility.DisplayDialog(
                "Scene Already Exists",
                $"The documentation scene already exists at:\n\n{SceneSavePath}\n\nJust open it to regenerate documentation.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, SceneSavePath);
        AssetDatabase.Refresh();

        Debug.Log($"[EGTK] Documentation scene created at: {SceneSavePath}  —  open it to generate documentation.");
    }
}
