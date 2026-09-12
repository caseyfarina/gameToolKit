using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds a small playable 3D physics level covering the four 3D physics/character
/// components that have no 2D counterpart already demonstrated elsewhere:
///
///   PhysicsForceZone, PhysicsBallPlayerController, PhysicsEnemyController,
///   CharacterPushRigidBody.
///
/// Two lanes on one ground:
///   Ball lane  — a PhysicsBallPlayerController rolls toward a PhysicsForceZone that
///                launches it, while a PhysicsEnemyController chases it.
///   Push lane  — a CharacterControllerCC player carrying CharacterPushRigidBody shoves
///                three crates of different mass.
///
/// Both players read the same PlayerInput action map (Move / Jump on "Player"), so
/// driving one drives both — same pattern BuildPlatformPairingScene already uses.
///
/// Harness-only tooling; only the scene it produces is shipped.
/// </summary>
public static class BuildPhysics3DScene
{
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example3D_Physics.unity";

    private static TMP_FontAsset _font;
    private static InputActionAsset _actions;

    private static TextMeshPro Label(string text, Vector3 pos, float width = 9f, float size = 0.55f,
        Color? color = null)
        => SceneLabel.Create(_font, text, pos, width, size, color ?? Color.white,
            rotation: Quaternion.Euler(30f, 0f, 0f));

    private static Material Lit(Color colour)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = colour;
        return mat;
    }

    private static GameObject Box(string name, Vector3 pos, Vector3 scale, Color colour)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Lit(colour);
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

    private static void AddPlayerInput(GameObject go)
    {
        var pi = go.AddComponent<PlayerInput>();
        pi.actions = _actions;
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;
    }

    [MenuItem("EGTK/Rebuild 3D Physics Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        _actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;
        int groundMask = 1 << groundLayer;
        int defaultMask = 1 << 0; // players live on the Default layer

        // ---- Camera and light -------------------------------------------------------
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.transform.position = new Vector3(10f, 14f, -20f);
        cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Label("3D PHYSICS PLAYGROUND\n" +
              "Both players read the same WASD + Space — driving one drives both.",
              new Vector3(10f, 9.5f, 0f), 26f, 0.6f).alignment = TextAlignmentOptions.Center;

        // =====================================================================
        // BALL LANE  (z = +4.5)  — PhysicsBallPlayerController, PhysicsForceZone,
        //                          PhysicsEnemyController
        // =====================================================================
        const float ballLaneZ = 4.5f;

        Box("Ball_Ground", new Vector3(10f, -0.5f, ballLaneZ), new Vector3(20f, 1f, 5f),
            new Color(0.35f, 0.45f, 0.35f)).layer = groundLayer;

        var ball = new GameObject("Ball_Player_PhysicsBallPlayerController") { tag = "Player" };
        ball.transform.position = new Vector3(2f, 0.6f, ballLaneZ);

        var ballVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballVisual.name = "Visual";
        ballVisual.transform.SetParent(ball.transform, false);
        Object.DestroyImmediate(ballVisual.GetComponent<Collider>());
        ballVisual.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.9f, 0.35f, 0.3f));

        var ballCollider = ball.AddComponent<SphereCollider>();
        ballCollider.radius = 0.5f;

        var ballBody = ball.AddComponent<Rigidbody>();
        ballBody.mass = 1f;
        ballBody.linearDamping = 0.15f;
        ballBody.interpolation = RigidbodyInterpolation.Interpolate;

        AddPlayerInput(ball);

        var ballController = ball.AddComponent<PhysicsBallPlayerController>();
        // groundLayer defaults to 0 (nothing) on this component — the documented trap.
        SetProp(ballController, "groundLayer", p => p.intValue = groundMask);

        Label("BALL LANE\nPhysicsBallPlayerController\n\n" +
              "Roll right into the force zone\nto get launched. The enemy\n" +
              "behind it gives chase.",
              new Vector3(-1f, 4.5f, ballLaneZ), 9f);

        // Force zone: a trigger volume that launches the ball forward/up on entry.
        var forceZoneGo = new GameObject("Ball_ForceZone");
        forceZoneGo.transform.position = new Vector3(10f, 1f, ballLaneZ);
        var forceZoneCollider = forceZoneGo.AddComponent<BoxCollider>();
        forceZoneCollider.isTrigger = true;
        forceZoneCollider.size = new Vector3(3f, 3f, 5f);
        var forceZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forceZoneVisual.name = "Visual";
        forceZoneVisual.transform.SetParent(forceZoneGo.transform, false);
        forceZoneVisual.transform.localScale = new Vector3(3f, 3f, 5f);
        Object.DestroyImmediate(forceZoneVisual.GetComponent<Collider>());
        forceZoneVisual.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.9f, 0.8f, 0.2f));

        var forceZone = forceZoneGo.AddComponent<PhysicsForceZone>();
        // targetTag defaults to "Player", which the ball already carries.
        SetProp(forceZone, "forceDirection", p => p.vector3Value = new Vector3(0f, 1f, 3f));
        SetProp(forceZone, "minForce", p => p.floatValue = 8f);
        SetProp(forceZone, "maxForce", p => p.floatValue = 12f);
        SetProp(forceZone, "applyOnEnter", p => p.boolValue = true);

        Label("PhysicsForceZone\nisTrigger + Apply On Enter\nlaunches Player-tagged bodies",
              new Vector3(10f, 3.2f, ballLaneZ + 3f), 7f, 0.4f);

        // Enemy: chases the Player-tagged ball.
        var enemy = new GameObject("Ball_Enemy_PhysicsEnemyController");
        enemy.transform.position = new Vector3(17f, 1f, ballLaneZ);
        var enemyCapsule = enemy.AddComponent<CapsuleCollider>();
        enemyCapsule.height = 2f;
        enemyCapsule.radius = 0.5f;
        var enemyBody = enemy.AddComponent<Rigidbody>();
        enemyBody.interpolation = RigidbodyInterpolation.Interpolate;

        var enemyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyVisual.name = "Visual";
        enemyVisual.transform.SetParent(enemy.transform, false);
        Object.DestroyImmediate(enemyVisual.GetComponent<Collider>());
        enemyVisual.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.6f, 0.2f, 0.7f));

        // [RequireComponent(Rigidbody, CapsuleCollider)] — must exist before AddComponent.
        var enemyController = enemy.AddComponent<PhysicsEnemyController>();
        // playerTag defaults to "Player", matching the ball. groundLayer defaults to 1
        // (Default layer, not Ground) and playerLayer defaults to -1 (everything) — both
        // are set explicitly here per the documented trap, even though playerLayer is not
        // read by the component's own logic (player lookup is by tag, not layer mask).
        SetProp(enemyController, "groundLayer", p => p.intValue = groundMask);
        SetProp(enemyController, "playerLayer", p => p.intValue = defaultMask);

        Label("PhysicsEnemyController\nChases the Player tag,\nNoJumping mode",
              new Vector3(17f, 3.2f, ballLaneZ - 2f), 7f, 0.4f);

        // =====================================================================
        // PUSH LANE  (z = -4.5)  — CharacterControllerCC + CharacterPushRigidBody
        // =====================================================================
        const float pushLaneZ = -4.5f;

        Box("Push_Ground", new Vector3(10f, -0.5f, pushLaneZ), new Vector3(20f, 1f, 5f),
            new Color(0.45f, 0.35f, 0.35f)).layer = groundLayer;

        var pushPlayer = new GameObject("Push_Player_CharacterControllerCC") { tag = "Player" };
        pushPlayer.transform.position = new Vector3(2f, 1f, pushLaneZ);

        var pushVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pushVisual.name = "Visual";
        pushVisual.transform.SetParent(pushPlayer.transform, false);
        Object.DestroyImmediate(pushVisual.GetComponent<Collider>());
        pushVisual.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.4f, 0.75f, 0.9f));

        var pushCC = pushPlayer.AddComponent<CharacterController>();
        pushCC.height = 2f;
        pushCC.radius = 0.4f;
        AddPlayerInput(pushPlayer);

        var ccController = pushPlayer.AddComponent<CharacterControllerCC>();
        SetProp(ccController, "groundLayer", p => p.intValue = groundMask);

        // CharacterPushRigidBody requires CharacterController, which already exists above.
        var pushComponent = pushPlayer.AddComponent<CharacterPushRigidBody>();
        // Defaults (pushLayers = everything, minimumMass/maximumMass = 0/0 = no filter)
        // already let all three crates below be pushed; set explicitly for clarity.
        SetProp(pushComponent, "pushLayers", p => p.intValue = ~0);
        SetProp(pushComponent, "minimumMass", p => p.floatValue = 0f);
        SetProp(pushComponent, "maximumMass", p => p.floatValue = 0f);

        Label("PUSH LANE\nCharacterControllerCC\n+ CharacterPushRigidBody\n\n" +
              "Walk into the crates.\nAll three masses (1 / 3 / 6)\n" +
              "are within the push filter.",
              new Vector3(-1f, 4.5f, pushLaneZ), 9f);

        // Three crates of differing mass — all non-kinematic so they can be pushed.
        (float mass, float scale, Color colour)[] crates =
        {
            (1f, 0.8f, new Color(0.8f, 0.7f, 0.4f)),
            (3f, 1.1f, new Color(0.8f, 0.55f, 0.3f)),
            (6f, 1.5f, new Color(0.7f, 0.4f, 0.25f)),
        };

        for (int i = 0; i < crates.Length; i++)
        {
            float x = 8f + i * 4f;
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = $"Crate_Mass{crates[i].mass:0}";
            crate.transform.position = new Vector3(x, crates[i].scale * 0.5f, pushLaneZ);
            crate.transform.localScale = Vector3.one * crates[i].scale;
            crate.GetComponent<Renderer>().sharedMaterial = Lit(crates[i].colour);

            var crateBody = crate.AddComponent<Rigidbody>();
            crateBody.mass = crates[i].mass;
            crateBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EGTKPHYS3D built {ScenePath}");
    }
}
