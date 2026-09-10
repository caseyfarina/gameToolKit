using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds the 2D physics test scene: bumper, force zone, moving platform and platform
/// sticking, all driven by walking a character into them.
///
/// Harness-only tooling; only the scene it produces is shipped.
/// </summary>
public static class BuildPhysics2DTestScene
{
    private const string Art = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example2D_PhysicsTest.unity";

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

    private static TextMeshPro Label(string text, Vector2 pos, float width = 5f, float size = 0.62f)
    {
        var go = new GameObject("Label");
        go.transform.position = pos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = Color.white;
        if (_font != null) tmp.font = _font;

        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(width / 0.35f, 3.4f / 0.35f);
        rt.pivot = new Vector2(0.5f, 1f);
        go.transform.localScale = Vector3.one * 0.35f;
        tmp.fontSize = size / 0.35f * 2.4f;
        return tmp;
    }

    private static void SetProp(Object target, string name, System.Action<SerializedProperty> set)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("EGTK/Rebuild 2D Physics Test Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int groundLayer = LayerMask.NameToLayer("Ground");
        int platformLayer = LayerMask.NameToLayer("MovingPlatform");
        if (platformLayer < 0) platformLayer = groundLayer;

        Sprite ground = Tile("tile_0000.png");
        Sprite crate = Tile("tile_0130.png");
        Sprite coin = Tile("tile_0151.png");
        Sprite playerSprite = Tile("Characters/tile_0000.png");
        if (ground == null || playerSprite == null) { Debug.LogError("Art2D sprites missing"); return; }

        // ---- Camera ---------------------------------------------------------------
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.14f, 0.19f);
        camGo.transform.position = new Vector3(10f, 4f, -10f);

        Label("EGTK 2D PHYSICS TEST  —  walk into each one",
              new Vector2(10f, 10.6f), 34f, 0.72f).alignment = TextAlignmentOptions.Center;

        // ---- Floor, with a gap the platform ferries you across ---------------------
        var level = new GameObject("Level");
        for (int x = -3; x <= 22; x++)
        {
            if (x >= 13 && x <= 17) continue;   // the gap
            var t = Sprite($"Ground_{x}", ground, new Vector2(x, -1f), 0);
            t.transform.SetParent(level.transform);
            t.layer = groundLayer;
            t.AddComponent<BoxCollider2D>();
        }

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
        SetProp(controller, "groundLayer", p => p.intValue = (1 << groundLayer) | (1 << platformLayer));

        // 4. PhysicsPlatformStick2D lives on the character, not the platform.
        var stick = player.AddComponent<PhysicsPlatformStick2D>();
        SetProp(stick, "platformLayer", p => p.intValue = 1 << platformLayer);
        SetProp(stick, "platformTag", p => p.stringValue = "movingPlatform");

        Label("WASD to move\nSpace to jump", new Vector2(0f, 3.6f), 6f, 0.5f);

        // ---- 1. PhysicsBumper2D ---------------------------------------------------
        var bumper = Sprite("1_PhysicsBumper2D", crate, new Vector2(5f, 0f));
        bumper.GetComponent<SpriteRenderer>().color = new Color(1f, 0.6f, 0.5f);
        bumper.AddComponent<BoxCollider2D>();          // solid, not a trigger
        var bump = bumper.AddComponent<PhysicsBumper2D>();
        SetProp(bump, "bumperTag", p => p.stringValue = "Player");
        SetProp(bump, "bumperForce", p => p.floatValue = 14f);
        Label("1. PhysicsBumper2D\nWalk into it to be\nbounced away", new Vector2(5f, 3.6f));

        // ---- 2. PhysicsForceZone2D ------------------------------------------------
        var zone = new GameObject("2_PhysicsForceZone2D");
        zone.transform.position = new Vector2(9.5f, 1.5f);
        var zoneCol = zone.AddComponent<BoxCollider2D>();
        zoneCol.isTrigger = true;                      // trigger, unlike the bumper
        zoneCol.size = new Vector2(3f, 5f);
        var force = zone.AddComponent<PhysicsForceZone2D>();
        SetProp(force, "targetTag", p => p.stringValue = "Player");
        SetProp(force, "forceDirection", p => p.vector2Value = Vector2.up);
        SetProp(force, "minForce", p => p.floatValue = 22f);
        SetProp(force, "maxForce", p => p.floatValue = 30f);

        for (int i = 0; i < 3; i++)
        {
            var puff = Sprite("ZoneMarker", coin, new Vector2(8.6f + i * 0.9f, 2.4f), 1);
            puff.transform.SetParent(zone.transform);
            puff.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.9f, 1f, 0.35f);
        }
        Label("2. PhysicsForceZone2D\nStand in it: an updraft\nlifts you", new Vector2(9.5f, 3.6f));

        // ---- 3. PhysicsPlatformAnimator + 4. PhysicsPlatformStick2D ---------------
        // PhysicsPlatformAnimator moves by transform and uses no physics API at all, so
        // it already works in 2D — no 2D version of it is needed.
        var platform = new GameObject("3_MovingPlatform") { tag = "movingPlatform", layer = platformLayer };
        platform.transform.position = new Vector2(13f, 0f);

        for (int i = 0; i < 3; i++)
        {
            var piece = Sprite($"PlatformPiece_{i}", ground, new Vector2(13f + i, 0f), 3);
            piece.transform.SetParent(platform.transform);
            piece.layer = platformLayer;
        }
        var platformCol = platform.AddComponent<BoxCollider2D>();
        platformCol.size = new Vector2(3f, 1f);
        platformCol.offset = new Vector2(1f, 0f);

        // Kinematic body so Unity does not rebuild a static collider every frame.
        var platformBody = platform.AddComponent<Rigidbody2D>();
        platformBody.bodyType = RigidbodyType2D.Kinematic;

        var wpA = new GameObject("Waypoint_A");
        wpA.transform.position = new Vector2(13f, 0f);
        var wpB = new GameObject("Waypoint_B");
        wpB.transform.position = new Vector2(17f, 0f);

        var animator = platform.AddComponent<PhysicsPlatformAnimator>();
        SetProp(animator, "totalAnimationTime", p => p.floatValue = 4f);
        SetProp(animator, "playOnStart", p => p.boolValue = true);
        // Waypoint is a struct with a Transform plus timing, not a plain object
        // reference, so each element's fields are set individually.
        SetProp(animator, "waypoints", p =>
        {
            p.arraySize = 2;
            Transform[] points = { wpA.transform, wpB.transform };
            for (int i = 0; i < points.Length; i++)
            {
                SerializedProperty element = p.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("transform").objectReferenceValue = points[i];
                element.FindPropertyRelative("pauseTime").floatValue = 0.5f;
                element.FindPropertyRelative("normalizedTime").floatValue = i;
            }
        });

        Label("3. PhysicsPlatformAnimator\n(works in 2D unchanged)\n\n4. PhysicsPlatformStick2D\nis on the Player — ride the\nplatform across the gap",
              new Vector2(15f, 3.6f), 6.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EGTKPHYS2D built {ScenePath}");
    }
}
