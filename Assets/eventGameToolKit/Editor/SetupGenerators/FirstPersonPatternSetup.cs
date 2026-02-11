using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

/// <summary>
/// Setup generator for First-Person Controller Pattern: creates a complete first-person
/// player setup with camera, ground, obstacles, and an elevated platform.
/// Creates: Player (CharacterControllerFP + Camera child) + environment objects
/// </summary>
public class FirstPersonPatternSetup : EditorWindow
{
    [MenuItem("Tools/Setup Patterns/First-Person Controller Pattern")]
    static void ShowWindow()
    {
        GetWindow<FirstPersonPatternSetup>("First-Person Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("First-Person Controller Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("This creates:", EditorStyles.helpBox);
        GUILayout.Label("• Player with CharacterControllerFP");
        GUILayout.Label("• Camera as child at eye height (0, 1.6, 0)");
        GUILayout.Label("• Ground plane");
        GUILayout.Label("• 3 obstacles to navigate around");
        GUILayout.Label("• 1 elevated platform with ramp");
        GUILayout.Space(5);
        GUILayout.Label("After creating, assign InputSystem_Actions\nto the PlayerInput component.", EditorStyles.miniLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create First-Person Pattern", GUILayout.Height(40)))
        {
            CreateFirstPersonPattern();
            Close();
        }
    }

    static void CreateFirstPersonPattern()
    {
        // Create main container
        GameObject container = new GameObject("FirstPersonPattern_Setup");
        Undo.RegisterCreatedObjectUndo(container, "Create First-Person Pattern");

        // Create Player
        GameObject player = new GameObject("Player");
        player.transform.SetParent(container.transform);
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.tag = "Player";
        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        // Add CharacterControllerFP (auto-adds CharacterController and PlayerInput)
        CharacterControllerFP fpController = player.AddComponent<CharacterControllerFP>();

        // Configure CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        SerializedObject ccSO = new SerializedObject(cc);
        ccSO.FindProperty("m_Center").vector3Value = new Vector3(0f, 1f, 0f);
        ccSO.FindProperty("m_Radius").floatValue = 0.5f;
        ccSO.FindProperty("m_Height").floatValue = 2f;
        ccSO.ApplyModifiedProperties();

        // Configure PlayerInput
        PlayerInput playerInputComp = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComp != null)
        {
            SerializedObject piSO = new SerializedObject(playerInputComp);
            piSO.FindProperty("m_DefaultActionMap").stringValue = "Player";
            piSO.ApplyModifiedProperties();
        }

        // Create Camera as child of Player at eye height
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObj.transform.localRotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(cameraObj, "Create Player Camera");

        Camera cam = cameraObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.fieldOfView = 70f;
        cameraObj.AddComponent<AudioListener>();
        cameraObj.tag = "MainCamera";

        // Wire camera to controller
        SerializedObject fpSO = new SerializedObject(fpController);
        fpSO.FindProperty("playerCamera").objectReferenceValue = cameraObj.transform;
        fpSO.ApplyModifiedProperties();

        // Remove default Main Camera if one exists in scene
        Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera sceneCam in sceneCameras)
        {
            if (sceneCam != cam && sceneCam.CompareTag("MainCamera"))
            {
                Debug.Log($"First-Person Setup: Found existing MainCamera '{sceneCam.name}'. You may want to disable or remove it.");
            }
        }

        // Create Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(container.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        Material groundMaterial = ExampleMaterialHelper.GetOrCreateMaterial("GroundMaterial", new Color(0.3f, 0.3f, 0.3f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        // Create Obstacles parent
        GameObject obstaclesParent = new GameObject("Obstacles");
        obstaclesParent.transform.SetParent(container.transform);
        Undo.RegisterCreatedObjectUndo(obstaclesParent, "Create Obstacles Parent");

        Material obstacleMaterial = ExampleMaterialHelper.GetOrCreateMaterial("ObstacleMaterial", new Color(0.6f, 0.4f, 0.2f));

        // Obstacle 1 - tall pillar
        GameObject obstacle1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle1.name = "Pillar_1";
        obstacle1.transform.SetParent(obstaclesParent.transform);
        obstacle1.transform.position = new Vector3(5f, 1.5f, 5f);
        obstacle1.transform.localScale = new Vector3(2f, 3f, 2f);
        obstacle1.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
        Undo.RegisterCreatedObjectUndo(obstacle1, "Create Obstacle 1");

        // Obstacle 2 - wide wall
        GameObject obstacle2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle2.name = "Wall_1";
        obstacle2.transform.SetParent(obstaclesParent.transform);
        obstacle2.transform.position = new Vector3(-4f, 1f, 8f);
        obstacle2.transform.localScale = new Vector3(6f, 2f, 0.5f);
        obstacle2.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
        Undo.RegisterCreatedObjectUndo(obstacle2, "Create Obstacle 2");

        // Obstacle 3 - crate
        GameObject obstacle3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle3.name = "Crate_1";
        obstacle3.transform.SetParent(obstaclesParent.transform);
        obstacle3.transform.position = new Vector3(3f, 0.5f, -4f);
        obstacle3.transform.localScale = new Vector3(1f, 1f, 1f);
        obstacle3.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
        Undo.RegisterCreatedObjectUndo(obstacle3, "Create Obstacle 3");

        // Create elevated platform
        Material platformMaterial = ExampleMaterialHelper.GetOrCreateMaterial("PlatformMaterial", new Color(0.4f, 0.5f, 0.6f));

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.SetParent(container.transform);
        platform.transform.position = new Vector3(-6f, 1f, -6f);
        platform.transform.localScale = new Vector3(4f, 0.3f, 4f);
        platform.GetComponent<Renderer>().sharedMaterial = platformMaterial;
        Undo.RegisterCreatedObjectUndo(platform, "Create Platform");

        // Create ramp to platform
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.SetParent(container.transform);
        ramp.transform.position = new Vector3(-3.5f, 0.5f, -6f);
        ramp.transform.localScale = new Vector3(3f, 0.2f, 2f);
        ramp.transform.rotation = Quaternion.Euler(0f, 0f, -20f);
        ramp.GetComponent<Renderer>().sharedMaterial = platformMaterial;
        Undo.RegisterCreatedObjectUndo(ramp, "Create Ramp");

        // Select the player so students can see the components
        Selection.activeGameObject = player;

        Debug.Log("First-Person Controller Pattern created! Assign your InputSystem_Actions asset to the PlayerInput component on the Player, then press Play. WASD to move, Mouse to look, Space to jump, Shift to sprint, Escape to toggle cursor.");
    }
}
