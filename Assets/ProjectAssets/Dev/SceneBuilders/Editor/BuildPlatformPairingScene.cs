using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds a 3D scene that confirms each moving-platform animator works with the character
/// controller it was written for.
///
/// There are two animators and they are NOT interchangeable, which the naming hides:
///
///   PhysicsPlatformAnimator moves by transform.position. CharacterControllerCC has its
///   own platform riding built in — it tracks the platform and applies the delta itself —
///   so it needs a platform whose transform moves.
///
///   ActionPlatformAnimator moves a kinematic Rigidbody with MovePosition.
///   PhysicsCharacterController is rigidbody-based and rides it through
///   PhysicsPlatformStick, which also requires a Rigidbody.
///
/// Both lanes run side by side and both characters share the same input, so pressing D
/// walks both of them onto their platforms at once and the two pairings can be compared
/// directly. Each lane has a gap only the platform can cross: if a pairing is broken, the
/// character is left behind and falls.
///
/// Harness-only tooling; only the scene it produces is shipped.
/// </summary>
public static class BuildPlatformPairingScene
{
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example3D_PlatformPairing.unity";

    private const string PlatformTag = "movingPlatform";

    private static TMP_FontAsset _font;
    private static InputActionAsset _actions;

    private static TextMeshPro Label(string text, Vector3 pos, float width = 9f, float size = 0.5f)
    {
        var go = new GameObject("Label");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = Color.white;
        if (_font != null) tmp.font = _font;

        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(width / 0.3f, 4f / 0.3f);
        rt.pivot = new Vector2(0.5f, 1f);
        go.transform.localScale = Vector3.one * 0.3f;
        tmp.fontSize = size / 0.3f * 2.4f;
        return tmp;
    }

    private static GameObject Box(string name, Vector3 pos, Vector3 scale, Color colour)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;

        var renderer = go.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = colour;
        renderer.sharedMaterial = material;
        return go;
    }

    private static void SetProp(Object target, string name, System.Action<SerializedProperty> set)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The two animators' Waypoint structs are nearly, but not quite, the same:
    // PhysicsPlatformAnimator's has a normalizedTime field and ActionPlatformAnimator's
    // does not. Each field is set only if it exists.
    private static void SetWaypoints(Object animator, Transform a, Transform b)
    {
        SetProp(animator, "waypoints", p =>
        {
            p.arraySize = 2;
            Transform[] points = { a, b };
            for (int i = 0; i < points.Length; i++)
            {
                SerializedProperty element = p.GetArrayElementAtIndex(i);

                SerializedProperty transformProp = element.FindPropertyRelative("transform");
                if (transformProp != null) transformProp.objectReferenceValue = points[i];

                SerializedProperty pauseProp = element.FindPropertyRelative("pauseTime");
                if (pauseProp != null) pauseProp.floatValue = 0.75f;

                SerializedProperty timeProp = element.FindPropertyRelative("normalizedTime");
                if (timeProp != null) timeProp.floatValue = i;
            }
        });
    }

    private static void AddPlayerInput(GameObject go)
    {
        var pi = go.AddComponent<PlayerInput>();
        pi.actions = _actions;
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;
    }

    [MenuItem("EGTK/Rebuild Platform Pairing Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        _actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int platformLayer = LayerMask.NameToLayer("MovingPlatform");
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (platformLayer < 0) platformLayer = 0;
        if (groundLayer < 0) groundLayer = 0;

        // ---- Camera and light -----------------------------------------------------
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.transform.position = new Vector3(9f, 12f, -14f);
        cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Label("PLATFORM PAIRING TEST  —  press D to walk both characters right.\n" +
              "Each must ride its platform across the gap. If one falls, that pairing is broken.",
              new Vector3(9f, 7f, 0f), 26f, 0.55f).alignment = TextAlignmentOptions.Center;

        // =====================================================================
        // LANE A  —  CharacterControllerCC  +  PhysicsPlatformAnimator
        // =====================================================================
        const float laneAz = 4f;

        Box("A_GroundStart", new Vector3(0f, -0.5f, laneAz), new Vector3(8f, 1f, 4f),
            new Color(0.35f, 0.45f, 0.35f)).layer = groundLayer;
        Box("A_GroundEnd", new Vector3(16f, -0.5f, laneAz), new Vector3(8f, 1f, 4f),
            new Color(0.35f, 0.45f, 0.35f)).layer = groundLayer;

        // Transform-driven platform: what CharacterControllerCC's built-in riding expects.
        GameObject platformA = Box("A_Platform_PhysicsPlatformAnimator",
                                   new Vector3(5f, -0.5f, laneAz), new Vector3(3f, 1f, 4f),
                                   new Color(0.45f, 0.6f, 0.85f));
        platformA.tag = PlatformTag;
        platformA.layer = platformLayer;

        var wpA1 = new GameObject("A_Waypoint_1");
        wpA1.transform.position = new Vector3(5f, -0.5f, laneAz);
        var wpA2 = new GameObject("A_Waypoint_2");
        wpA2.transform.position = new Vector3(11f, -0.5f, laneAz);

        var animA = platformA.AddComponent<PhysicsPlatformAnimator>();
        SetProp(animA, "totalAnimationTime", p => p.floatValue = 5f);
        SetProp(animA, "playOnStart", p => p.boolValue = true);
        SetWaypoints(animA, wpA1.transform, wpA2.transform);

        var playerA = new GameObject("A_Player_CharacterControllerCC") { tag = "Player" };
        // Starts above the platform and drops onto it when Play begins, so the
        // pairing demonstrates itself without anyone touching the keyboard.
        playerA.transform.position = new Vector3(5f, 4f, laneAz);
        var visualA = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visualA.name = "Visual";
        visualA.transform.SetParent(playerA.transform, false);
        Object.DestroyImmediate(visualA.GetComponent<Collider>());
        var visualAMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        visualAMat.color = new Color(0.5f, 0.85f, 0.6f);
        visualA.GetComponent<Renderer>().sharedMaterial = visualAMat;

        var ccA = playerA.AddComponent<CharacterController>();
        ccA.height = 2f;
        ccA.radius = 0.4f;
        AddPlayerInput(playerA);

        var controllerA = playerA.AddComponent<CharacterControllerCC>();
        // Tag detection is the component's default and the easiest for students to follow.
        SetProp(controllerA, "platformTag", p => p.stringValue = PlatformTag);
        SetProp(controllerA, "platformLayer", p => p.intValue = 1 << platformLayer);

        Label("LANE A\nCharacterControllerCC\n+ PhysicsPlatformAnimator\n\n" +
              "The controller rides the platform itself.\nThe platform moves by transform.",
              new Vector3(-2f, 4.5f, laneAz), 9f);

        // =====================================================================
        // LANE B  —  PhysicsCharacterController + PhysicsPlatformStick
        //            +  ActionPlatformAnimator
        // =====================================================================
        const float laneBz = -4f;

        Box("B_GroundStart", new Vector3(0f, -0.5f, laneBz), new Vector3(8f, 1f, 4f),
            new Color(0.45f, 0.35f, 0.35f)).layer = groundLayer;
        Box("B_GroundEnd", new Vector3(16f, -0.5f, laneBz), new Vector3(8f, 1f, 4f),
            new Color(0.45f, 0.35f, 0.35f)).layer = groundLayer;

        // Kinematic-rigidbody platform: what PhysicsPlatformStick expects to ride.
        GameObject platformB = Box("B_Platform_ActionPlatformAnimator",
                                   new Vector3(5f, -0.5f, laneBz), new Vector3(3f, 1f, 4f),
                                   new Color(0.85f, 0.6f, 0.45f));
        platformB.tag = PlatformTag;
        platformB.layer = platformLayer;

        var wpB1 = new GameObject("B_Waypoint_1");
        wpB1.transform.position = new Vector3(5f, -0.5f, laneBz);
        var wpB2 = new GameObject("B_Waypoint_2");
        wpB2.transform.position = new Vector3(11f, -0.5f, laneBz);

        // ActionPlatformAnimator adds its own kinematic Rigidbody if there is not one.
        var animB = platformB.AddComponent<ActionPlatformAnimator>();
        SetProp(animB, "totalAnimationTime", p => p.floatValue = 5f);
        SetProp(animB, "playOnStart", p => p.boolValue = true);
        SetWaypoints(animB, wpB1.transform, wpB2.transform);

        var playerB = new GameObject("B_Player_PhysicsCharacterController");
        // Same idea as lane A: dropped on rather than placed, which lets physics
        // settle the contact instead of forcing an exact overlap.
        playerB.transform.position = new Vector3(5f, 4f, laneBz);
        var visualB = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visualB.name = "Visual";
        visualB.transform.SetParent(playerB.transform, false);
        Object.DestroyImmediate(visualB.GetComponent<Collider>());
        var visualBMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        visualBMat.color = new Color(0.9f, 0.7f, 0.4f);
        visualB.GetComponent<Renderer>().sharedMaterial = visualBMat;

        var capsuleB = playerB.AddComponent<CapsuleCollider>();
        capsuleB.height = 2f;
        capsuleB.radius = 0.4f;

        var bodyB = playerB.AddComponent<Rigidbody>();
        bodyB.freezeRotation = true;
        bodyB.interpolation = RigidbodyInterpolation.Interpolate;

        AddPlayerInput(playerB);

        var controllerB = playerB.AddComponent<PhysicsCharacterController>();
        SetProp(controllerB, "groundLayer", p => p.intValue = (1 << groundLayer) | (1 << platformLayer));

        var stickB = playerB.AddComponent<PhysicsPlatformStick>();
        SetProp(stickB, "platformLayer", p => p.intValue = 1 << platformLayer);
        SetProp(stickB, "platformTag", p => p.stringValue = PlatformTag);
        SetProp(stickB, "capsuleCollider", p => p.objectReferenceValue = capsuleB);

        Label("LANE B\nPhysicsCharacterController\n+ PhysicsPlatformStick\n+ ActionPlatformAnimator\n\n" +
              "PhysicsPlatformStick does the riding.\nThe platform moves a kinematic Rigidbody.",
              new Vector3(-2f, 4.5f, laneBz), 9f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EGTKPAIR built {ScenePath}");
    }
}
