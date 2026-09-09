using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the 2D click-to-toggle example scene.
///
/// Demonstrates the smallest complete interaction in the toolkit: click a sprite, and
/// something else turns on or off. Clicking is handled by InputMouseInteraction, which
/// picks 2D sprites via their Collider2D with no setting to change; the toggling is
/// ActionToggle.
///
/// Harness-only tooling: lives outside Assets/eventGameToolKit/, so it never ships.
/// </summary>
public static class Build2DClickToggleScene
{
    private const string ArtRoot = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example2D_ClickToToggle.unity";

    private static Sprite Tile(string file)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{file}");
        if (s == null) Debug.LogError($"Missing sprite: {ArtRoot}/{file}");
        return s;
    }

    private static GameObject MakeSprite(string name, Sprite sprite, Vector2 pos,
                                         Transform parent, int order = 0)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    [MenuItem("EGTK/Rebuild 2D Click-To-Toggle Scene")]
    public static void Build()
    {
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Sprite ground = Tile("tile_0000.png");
        Sprite crate = Tile("tile_0130.png");
        Sprite lamp = Tile("tile_0109.png");
        if (ground == null || crate == null || lamp == null) return;

        // ---- Camera -------------------------------------------------------------
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.20f, 0.29f);
        camGo.transform.position = new Vector3(4f, 1.5f, -10f);

        // ---- Ground strip, purely for context ------------------------------------
        GameObject level = new GameObject("Level");
        for (int x = -2; x <= 10; x++)
        {
            MakeSprite($"Ground_{x}", ground, new Vector2(x, -1f), level.transform);
        }

        // ---- The lamp that gets toggled ------------------------------------------
        GameObject lampGo = MakeSprite("Lamp", lamp, new Vector2(6f, 0.5f), null, 5);

        // ---- The clickable crate -------------------------------------------------
        GameObject switchGo = MakeSprite("ClickableSwitch", crate, new Vector2(2f, 0f), null, 5);

        // A Collider2D is what makes a sprite clickable. InputMouseInteraction detects
        // it and picks with Physics2D instead of a 3D raycast, automatically.
        BoxCollider2D box = switchGo.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;

        InputMouseInteraction click = switchGo.AddComponent<InputMouseInteraction>();
        ActionToggle toggle = switchGo.AddComponent<ActionToggle>();

        // Point the toggle at the lamp.
        SerializedObject toggleSo = new SerializedObject(toggle);
        SerializedProperty targets = toggleSo.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = lampGo;
        toggleSo.ApplyModifiedPropertiesWithoutUndo();

        // AddComponent leaves serialized UnityEvent fields null; the Inspector would have
        // created them. Persistent listeners need real instances to attach to.
        click.onMouseClick ??= new UnityEngine.Events.UnityEvent();
        click.onMouseEnter ??= new UnityEngine.Events.UnityEvent();
        click.onMouseExit ??= new UnityEngine.Events.UnityEvent();
        click.onMouseDown ??= new UnityEngine.Events.UnityEvent();
        click.onMouseUp ??= new UnityEngine.Events.UnityEvent();
        click.onMouseHover ??= new UnityEngine.Events.UnityEvent();

        // Wired as a persistent listener so it appears in the Inspector exactly as a
        // student would have dragged it in.
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
            click.onMouseClick, new UnityEngine.Events.UnityAction(toggle.Toggle));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Built click-to-toggle scene at {ScenePath}");
    }
}
