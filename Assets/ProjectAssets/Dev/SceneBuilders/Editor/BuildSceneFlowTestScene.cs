using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds the scene-flow test scene: flags, spawn points, respawn and scene loading.
///
/// These are the components students hit when a game needs to remember anything, and none
/// of them had scene coverage. The scene is playable so each can be exercised, and the
/// smoke test now watches them.
///
/// Harness-only tooling; only the scene it produces is shipped.
/// </summary>
public static class BuildSceneFlowTestScene
{
    private const string Art = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example2D_SceneFlowTest.unity";

    private static TMP_FontAsset _font;

    private static Sprite Tile(string f) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{f}");

    private static GameObject Sprite(string name, Sprite sprite, Vector2 pos, int order = 5)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    private static TextMeshPro Label(string text, Vector2 pos, float width = 4.8f, float size = 0.62f)
        => SceneLabel.Create(_font, text, pos, width, size, Color.white);

    private static void Wire(UnityEngine.Events.UnityEvent evt, UnityEngine.Events.UnityAction call)
        => UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(evt, call);

    private static void SetProp(Object target, string name, System.Action<SerializedProperty> set)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static InputMouseInteraction Clickable(GameObject go)
    {
        go.AddComponent<BoxCollider2D>();
        var mi = go.AddComponent<InputMouseInteraction>();
        mi.onMouseClick ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseEnter ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseExit ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseDown ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseUp ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseHover ??= new UnityEngine.Events.UnityEvent();
        return mi;
    }

    [MenuItem("EGTK/Rebuild Scene Flow Test Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int groundLayer = LayerMask.NameToLayer("Ground");
        Sprite ground = Tile("tile_0000.png");
        Sprite crate = Tile("tile_0130.png");
        Sprite flagSprite = Tile("tile_0111.png");
        Sprite tree = Tile("tile_0126.png");
        Sprite coin = Tile("tile_0151.png");
        Sprite playerSprite = Tile("Characters/tile_0000.png");
        if (ground == null || playerSprite == null) { Debug.LogError("Art2D sprites missing"); return; }

        // ---- Camera ---------------------------------------------------------------
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.0f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.16f, 0.20f);
        camGo.transform.position = new Vector3(10.7f, 4f, -10f);

        Label("EGTK SCENE FLOW TEST  —  flags, spawn points, respawn, scene loading",
              new Vector2(10.7f, 10.6f), 34f, 0.72f).alignment = TextAlignmentOptions.Center;

        var level = new GameObject("Level");
        for (int x = -3; x <= 26; x++)
        {
            var t = Sprite($"Ground_{x}", ground, new Vector2(x, -1f), 0);
            t.transform.SetParent(level.transform);
            t.layer = groundLayer;
            t.AddComponent<BoxCollider2D>();
        }

        // ---- Managers -------------------------------------------------------------
        var managers = new GameObject("Managers");
        var checkpointManager = managers.AddComponent<GameCheckpointManager>();
        var flagManager = managers.AddComponent<GameFlagManager>();
        var sceneManager = managers.AddComponent<GameSceneManager>();

        // ---- Player ---------------------------------------------------------------
        var player = new GameObject("Player") { tag = "Player" };
        player.transform.position = new Vector2(0f, 1f);
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = playerSprite;
        psr.sortingOrder = 10;

        var body = player.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        var cap = player.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(0.7f, 0.95f);

        var pi = player.AddComponent<PlayerInput>();
        pi.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;

        var controller = player.AddComponent<CharacterController2D>();
        SetProp(controller, "groundLayer", p => p.intValue = 1 << groundLayer);

        Label("WASD to move\nSpace to jump", new Vector2(0f, 3.6f), 6f, 0.5f);

        // ---- 1. SpawnPoint --------------------------------------------------------
        var spawnGo = Sprite("1_SpawnPoint", flagSprite, new Vector2(4f, 0f));
        var spawnPoint = spawnGo.AddComponent<SpawnPoint>();
        SetProp(spawnPoint, "spawnId", p => p.stringValue = "start");
        SetProp(spawnPoint, "isDefaultSpawnPoint", p => p.boolValue = true);
        Label("1. SpawnPoint\nWhere respawn sends you", new Vector2(4f, 3.6f));

        // ---- 2. GameFlagManager + 3. GameFlagListener -----------------------------
        // Clicking the crate sets a named flag. The listener reacts to it, and because
        // flags live in GameData the state survives a scene restart.
        var flagSetter = Sprite("2_GameFlagManager", crate, new Vector2(9f, 0f));
        var setterClick = Clickable(flagSetter);
        UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
            setterClick.onMouseClick,
            new UnityEngine.Events.UnityAction<string>(flagManager.SetFlag), "door_opened");
        Label("2. GameFlagManager\nClick: sets \"door_opened\"", new Vector2(9f, 3.6f));

        var listenerGo = Sprite("3_GameFlagListener", tree, new Vector2(13.5f, 0.5f));
        var listener = listenerGo.AddComponent<GameFlagListener>();
        SetProp(listener, "flagManager", p => p.objectReferenceValue = flagManager);
        SetProp(listener, "flagName", p => p.stringValue = "door_opened");

        // The tree hides once the flag is set, and stays hidden across a restart.
        var listenerToggle = listenerGo.AddComponent<ActionToggle>();
        SetProp(listenerToggle, "targets", p =>
        {
            p.arraySize = 1;
            p.GetArrayElementAtIndex(0).objectReferenceValue = listenerGo;
        });
        listener.onFlagBecameSet ??= new UnityEngine.Events.UnityEvent();
        listener.onFlagAlreadySet ??= new UnityEngine.Events.UnityEvent();
        Wire(listener.onFlagBecameSet, listenerToggle.Toggle);
        Wire(listener.onFlagAlreadySet, listenerToggle.Toggle);
        Label("3. GameFlagListener\nTree hides when the flag\nis set, and stays hidden",
              new Vector2(13.5f, 3.6f));

        // ---- 4. ActionRespawnPlayer -----------------------------------------------
        var respawnGo = Sprite("4_ActionRespawnPlayer", coin, new Vector2(18f, 0f));
        var respawn = respawnGo.AddComponent<ActionRespawnPlayer>();
        SetProp(respawn, "playerObject", p => p.objectReferenceValue = player);
        SetProp(respawn, "fallbackSpawnPoint", p => p.objectReferenceValue = spawnGo.transform);
        SetProp(respawn, "checkpointPersistence", p => p.objectReferenceValue = checkpointManager);
        SetProp(respawn, "respawnDelay", p => p.floatValue = 0.2f);
        Wire(Clickable(respawnGo).onMouseClick, respawn.RespawnImmediate);
        Label("4. ActionRespawnPlayer\nClick: sends the player\nback to the spawn point",
              new Vector2(18f, 3.6f));

        // ---- 5. GameSceneManager --------------------------------------------------
        // Restarting this same scene is the clearest demonstration: the flag survives,
        // proving GameData persistence, while everything else resets.
        var restartGo = Sprite("5_GameSceneManager", crate, new Vector2(22.5f, 0f));
        restartGo.GetComponent<SpriteRenderer>().color = new Color(1f, 0.6f, 0.6f);
        Wire(Clickable(restartGo).onMouseClick, sceneManager.RestartCurrentScene);
        Label("5. GameSceneManager\nClick: restarts the scene.\nThe flag survives.",
              new Vector2(22.5f, 3.6f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EGTKFLOW built {ScenePath}");
    }
}
