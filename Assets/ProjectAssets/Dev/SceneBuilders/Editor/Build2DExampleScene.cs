using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the 2D platformer example scene from Kenney sprites.
///
/// Harness-only tooling: this lives outside Assets/eventGameToolKit/, so the robocopy
/// sync never ships it to students. It exists so the example scene can be rebuilt
/// deterministically instead of being hand-placed and lost.
/// </summary>
public static class Build2DExampleScene
{
    private const string ArtRoot = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example2D_Platformer.unity";

    private static Sprite LoadTile(string file)
    {
        string path = $"{ArtRoot}/{file}";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError($"Missing sprite: {path}");
        return s;
    }

    private static GameObject MakeSprite(string name, Sprite sprite, Vector2 pos,
                                         Transform parent, int order = 0)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    [MenuItem("EGTK/Rebuild 2D Example Scene")]
    public static void Build()
    {
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int groundLayer = LayerMask.NameToLayer("Ground");
        Sprite ground = LoadTile("tile_0000.png");
        Sprite coin = LoadTile("tile_0151.png");
        Sprite flag = LoadTile("tile_0111.png");
        Sprite player = LoadTile("Characters/tile_0000.png");
        if (ground == null || player == null) return;

        // ---- Camera -------------------------------------------------------------
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        // Sized so the whole 24-unit level fits at 16:9 without scrolling, since the
        // scene has no camera follow yet.
        cam.orthographicSize = 7.5f;
        cam.backgroundColor = new Color(0.30f, 0.55f, 0.80f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(12f, 5f, -10f);

        // ---- Level geometry -----------------------------------------------------
        // Sprite GameObjects rather than a Tilemap: this keeps the package free of the
        // 2D Tilemap authoring package, and the point of the scene is EGTK wiring.
        GameObject level = new GameObject("Level");

        // Floor
        for (int x = 0; x <= 24; x++)
        {
            GameObject t = MakeSprite($"Ground_{x}", ground, new Vector2(x, 0f), level.transform);
            t.layer = groundLayer;
            t.AddComponent<BoxCollider2D>();
        }

        // Floating platforms: (startX, length, y)
        int[,] platforms = { { 4, 3, 3 }, { 10, 3, 5 }, { 16, 4, 3 } };
        for (int p = 0; p < platforms.GetLength(0); p++)
        {
            int startX = platforms[p, 0], len = platforms[p, 1], y = platforms[p, 2];
            for (int i = 0; i < len; i++)
            {
                GameObject t = MakeSprite($"Platform_{p}_{i}", ground,
                                          new Vector2(startX + i, y), level.transform);
                t.layer = groundLayer;
                t.AddComponent<BoxCollider2D>();
            }
        }

        // ---- Player -------------------------------------------------------------
        GameObject playerGo = new GameObject("Player") { tag = "Player" };
        playerGo.transform.position = new Vector2(1f, 2f);
        SpriteRenderer psr = playerGo.AddComponent<SpriteRenderer>();
        psr.sprite = player;
        psr.sortingOrder = 10;

        Rigidbody2D body = playerGo.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D capsule = playerGo.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.7f, 0.95f);
        capsule.direction = CapsuleDirection2D.Vertical;

        // PlayerInput drives OnMove/OnJump by SendMessage. Without it the controller
        // compiles and runs but never receives input, so the player simply stands there.
        var playerInput = playerGo.AddComponent<UnityEngine.InputSystem.PlayerInput>();
        playerInput.actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");
        playerInput.defaultActionMap = "Player";
        playerInput.notificationBehavior =
            UnityEngine.InputSystem.PlayerNotifications.SendMessages;

        CharacterController2D controller = playerGo.AddComponent<CharacterController2D>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("groundLayer").intValue = 1 << groundLayer;
        so.FindProperty("groundCheckOffset").floatValue = 0.5f;
        so.FindProperty("groundCheckSize").vector2Value = new Vector2(0.6f, 0.12f);
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Score manager ------------------------------------------------------
        GameObject managers = new GameObject("Managers");
        GameCollectionManager collection = managers.AddComponent<GameCollectionManager>();

        // ---- Collectibles -------------------------------------------------------
        GameObject coinParent = new GameObject("Collectibles");
        Vector2[] coinSpots =
        {
            new Vector2(5f, 4f), new Vector2(11f, 6f), new Vector2(17f, 4f),
            new Vector2(8f, 1f), new Vector2(21f, 1f)
        };

        foreach (Vector2 spot in coinSpots)
        {
            GameObject c = MakeSprite("Coin", coin != null ? coin : ground, spot,
                                      coinParent.transform, 5);
            BoxCollider2D trigger = c.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.2f, 1.2f);

            InputTriggerZone zone = c.AddComponent<InputTriggerZone>();
            ActionDestroyObject destroy = c.AddComponent<ActionDestroyObject>();

            // AddComponent leaves serialized UnityEvent fields null; the Inspector would
            // have created them. Persistent listeners need real instances to attach to.
            zone.onTriggerEnterEvent ??= new UnityEngine.Events.UnityEvent();
            zone.onTriggerExitEvent ??= new UnityEngine.Events.UnityEvent();
            zone.onTriggerStayEvent ??= new UnityEngine.Events.UnityEvent();

            // Wired as persistent listeners so they show in the Inspector exactly as a
            // student would have dragged them in, not as invisible runtime hookups.
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                zone.onTriggerEnterEvent,
                new UnityEngine.Events.UnityAction<int>(collection.Increment), 1);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                zone.onTriggerEnterEvent,
                new UnityEngine.Events.UnityAction(destroy.DestroyObject));
        }

        // ---- Goal flag ----------------------------------------------------------
        GameObject goal = MakeSprite("Goal", flag != null ? flag : ground,
                                     new Vector2(23f, 1f), level.transform, 5);
        BoxCollider2D goalTrigger = goal.AddComponent<BoxCollider2D>();
        goalTrigger.isTrigger = true;
        goalTrigger.size = new Vector2(1.2f, 2f);
        InputTriggerZone goalZone = goal.AddComponent<InputTriggerZone>();
        goalZone.onTriggerEnterEvent ??= new UnityEngine.Events.UnityEvent();
        goalZone.onTriggerExitEvent ??= new UnityEngine.Events.UnityEvent();
        goalZone.onTriggerStayEvent ??= new UnityEngine.Events.UnityEvent();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Built 2D example scene at {ScenePath}");
    }
}
