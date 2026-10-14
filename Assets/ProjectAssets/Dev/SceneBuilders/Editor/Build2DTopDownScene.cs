using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Builds Example2D_TopDown: CharacterController2D in Top-Down mode walking two rooms.
///
/// Room 1 has coins wired to a GameCollectionManager. A door in the dividing wall opens
/// with InputInteractionZone (press E), and room 2 holds a gem worth five.
///
/// Before this scene, Top-Down mode had one PlayMode test and no scene at all — every
/// 2D example used Platformer. Harness-only tooling; never shipped.
/// </summary>
public static class Build2DTopDownScene
{
    private const string ArtRoot = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string ScenePath = "Assets/eventGameToolKit/ExampleScenes/Example2D_TopDown.unity";

    // Room layout, in tiles (one tile = one world unit).
    private const int RoomWidth = 20;   // outer walls at x = 0 and x = RoomWidth - 1
    private const int RoomHeight = 11;  // outer walls at y = 0 and y = RoomHeight - 1
    private const int DividerX = 12;
    private const int DoorY = 4;        // door fills DoorY and DoorY + 1

    private static TMP_FontAsset _font;

    private static Sprite Tile(string file)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{file}");
        if (s == null) Debug.LogError($"Build2DTopDownScene: missing sprite {ArtRoot}/{file}");
        return s;
    }

    private static GameObject MakeSprite(string name, Sprite sprite, Vector2 pos, Transform parent, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    private static void Label(string text, Vector2 pos, float width, float size)
        => SceneLabel.Create(_font, text, pos, width, size, Color.white);

    private static void SetProp(Object target, string name, System.Action<SerializedProperty> set)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // AddComponent leaves serialized UnityEvents null; the Inspector would have created them.
    private static void InitEvents(InputTriggerZone zone)
    {
        zone.onTriggerEnterEvent ??= new UnityEvent();
        zone.onTriggerStayEvent ??= new UnityEvent();
        zone.onTriggerExitEvent ??= new UnityEvent();
    }

    [MenuItem("EGTK/Rebuild 2D Top-Down Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        Sprite grass = Tile("tile_0038.png");
        Sprite wall = Tile("tile_0006.png");
        Sprite doorTop = Tile("tile_0130.png");
        Sprite doorBottom = Tile("tile_0150.png");
        Sprite coin = Tile("tile_0151.png");
        Sprite gem = Tile("tile_0067.png");
        Sprite prompt = Tile("tile_0010.png");
        Sprite playerArt = Tile("Characters/tile_0000.png");
        if (grass == null || wall == null || playerArt == null) return;

        // ---- Camera -------------------------------------------------------------
        // Fixed camera framing both rooms plus the title above and the hint below; the
        // scene has no camera follow.
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.backgroundColor = new Color(0.16f, 0.18f, 0.24f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3((RoomWidth - 1) / 2f, 5.8f, -10f);

        // ---- Floor and walls ------------------------------------------------------
        var level = new GameObject("Level").transform;
        for (int x = 1; x < RoomWidth - 1; x++)
            for (int y = 1; y < RoomHeight - 1; y++)
                MakeSprite($"Floor_{x}_{y}", grass, new Vector2(x, y), level, -10);

        var walls = new GameObject("Walls").transform;
        walls.SetParent(level);
        void Wall(int x, int y) =>
            MakeSprite($"Wall_{x}_{y}", wall, new Vector2(x, y), walls, 0).AddComponent<BoxCollider2D>();

        for (int x = 0; x < RoomWidth; x++) { Wall(x, 0); Wall(x, RoomHeight - 1); }
        for (int y = 1; y < RoomHeight - 1; y++) { Wall(0, y); Wall(RoomWidth - 1, y); }
        for (int y = 1; y < RoomHeight - 1; y++)
            if (y != DoorY && y != DoorY + 1) Wall(DividerX, y);

        // ---- Player -------------------------------------------------------------
        var player = new GameObject("Player") { tag = "Player" };
        player.transform.position = new Vector2(3f, 5f);
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = playerArt;
        psr.sortingOrder = 10;

        var body = player.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.7f, 0.9f);

        // PlayerInput delivers OnMove by SendMessage. Exactly one per scene — a second one
        // is silently paired to a different device and ignores the keyboard.
        var playerInput = player.AddComponent<UnityEngine.InputSystem.PlayerInput>();
        playerInput.actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");
        playerInput.defaultActionMap = "Player";
        playerInput.notificationBehavior = UnityEngine.InputSystem.PlayerNotifications.SendMessages;

        var controller = player.AddComponent<CharacterController2D>();
        SetProp(controller, "movementStyle",
            p => p.enumValueIndex = (int)CharacterController2D.MovementStyle.TopDown);
        SetProp(controller, "moveSpeed", p => p.floatValue = 6f);

        // ---- Score ----------------------------------------------------------------
        var managers = new GameObject("Managers");
        var score = managers.AddComponent<GameCollectionManager>();
        SetProp(score, "showUI", p => p.boolValue = true);
        SetProp(score, "labelPrefix", p => p.stringValue = "Coins: ");

        // ---- Coins (room 1) and gem (room 2) ---------------------------------------
        var pickups = new GameObject("Pickups").transform;
        Vector2[] coinSpots =
        {
            new Vector2(3f, 8f), new Vector2(6f, 2f), new Vector2(6f, 5f),
            new Vector2(9f, 8f), new Vector2(9f, 2f),
        };
        foreach (Vector2 spot in coinSpots) Pickup("Coin", coin, spot, 1, pickups, score);
        Pickup("Gem", gem, new Vector2(16f, 5f), 5, pickups, score);

        // ---- Door ---------------------------------------------------------------
        // A solid two-tile door filling the gap in the divider. Pressing E nearby hides it
        // through ActionToggle.AllOff, which also removes its collider from the way.
        var door = new GameObject("Door");
        door.transform.SetParent(level);
        door.transform.position = new Vector2(DividerX, DoorY + 0.5f);
        MakeSprite("DoorTop", doorTop, new Vector2(DividerX, DoorY + 1), door.transform, 1);
        MakeSprite("DoorBottom", doorBottom, new Vector2(DividerX, DoorY), door.transform, 1);
        door.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2f);

        var doorZone = new GameObject("DoorZone");
        doorZone.transform.position = door.transform.position;
        var zoneTrigger = doorZone.AddComponent<BoxCollider2D>();
        zoneTrigger.isTrigger = true;
        zoneTrigger.size = new Vector2(3f, 3f);

        var interact = doorZone.AddComponent<InputInteractionZone>();
        SetProp(interact, "promptSprite", p => p.objectReferenceValue = prompt);
        SetProp(interact, "promptOffset", p => p.vector3Value = new Vector3(0f, 1.8f, 0f));
        SetProp(interact, "promptSize", p => p.floatValue = 0.8f);
        interact.onInteract ??= new UnityEvent();
        interact.onEnter ??= new UnityEvent();
        interact.onExit ??= new UnityEvent();

        var toggle = doorZone.AddComponent<ActionToggle>();
        SetProp(toggle, "targets", p =>
        {
            p.arraySize = 1;
            p.GetArrayElementAtIndex(0).objectReferenceValue = door;
        });
        UnityEventTools.AddVoidPersistentListener(interact.onInteract, new UnityAction(toggle.AllOff));

        // ---- Labels -------------------------------------------------------------
        Label("EGTK 2D TOP-DOWN  —  CharacterController2D, Movement Style: Top-Down",
              new Vector2((RoomWidth - 1) / 2f, 12.4f), 18f, 0.7f);
        Label("WASD or arrows to move  ·  coins add to the score  ·  press E at the door",
              new Vector2((RoomWidth - 1) / 2f, -0.7f), 18f, 0.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Built top-down example scene at {ScenePath}");
    }

    /// <summary>
    /// A trigger sprite that adds to the score and removes itself, wired as persistent
    /// listeners so the Inspector shows exactly what a student would drag in.
    /// </summary>
    private static void Pickup(string name, Sprite art, Vector2 pos, int value,
                               Transform parent, GameCollectionManager score)
    {
        GameObject go = MakeSprite(name, art, pos, parent, 5);
        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;

        var zone = go.AddComponent<InputTriggerZone>();
        var destroy = go.AddComponent<ActionDestroyObject>();
        InitEvents(zone);
        UnityEventTools.AddIntPersistentListener(zone.onTriggerEnterEvent, new UnityAction<int>(score.Increment), value);
        UnityEventTools.AddVoidPersistentListener(zone.onTriggerEnterEvent, new UnityAction(destroy.DestroyObject));
    }
}
