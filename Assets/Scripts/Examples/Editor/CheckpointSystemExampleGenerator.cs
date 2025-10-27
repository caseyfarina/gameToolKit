using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating GameCheckpointManager
/// Creates checkpoints that save player position with a death/respawn system
/// </summary>
public class CheckpointSystemExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate CheckpointSystem Example")]
    public static void GenerateExample()
    {
        // Clear selection
        Selection.activeGameObject = null;

        // Load materials
        Material pinkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/pink.mat");
        Material blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/blue.mat");

        if (pinkMat == null || blueMat == null)
        {
            Debug.LogError("Could not find pink.mat or blue.mat in Assets/Materials/!");
            return;
        }

        // Create root container
        GameObject root = new GameObject("CheckpointSystemExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate CheckpointSystem Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject checkpointManager = CreateCheckpointManager(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat, checkpointManager);
        GameObject checkpoint1 = CreateCheckpoint(root.transform, new Vector3(-5f, 1f, 0f), pinkMat, "Checkpoint_1", checkpointManager);
        GameObject checkpoint2 = CreateCheckpoint(root.transform, new Vector3(5f, 1f, 5f), pinkMat, "Checkpoint_2", checkpointManager);
        GameObject deathZone = CreateDeathZone(root.transform, new Vector3(0f, -5f, 0f), player);
        GameObject ground = CreatePlatformLevel(root.transform);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 400f), "Checkpoint System Demo\n\nTouch pink checkpoints to save!", 46);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "WASD to Move | Fall off to respawn at checkpoint", 34);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("CheckpointSystem example generated! Touch checkpoints and try falling off!");
    }

    private static GameObject CreateCanvas(Transform parent)
    {
        // Create EventSystem if needed
        if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(parent);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvasObj;
    }

    private static GameObject CreateAnnotation(Transform parent, Vector2 position, string text, float fontSize)
    {
        GameObject textObj = new GameObject("Annotation");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1600f, 300f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
    }

    private static GameObject CreateCheckpointManager(Transform parent)
    {
        GameObject manager = new GameObject("CheckpointManager");
        manager.transform.SetParent(parent);

        GameCheckpointManager checkpointMgr = manager.AddComponent<GameCheckpointManager>();

        SerializedObject so = new SerializedObject(checkpointMgr);
        so.FindProperty("saveGameState").boolValue = false; // Just position for this example
        so.ApplyModifiedProperties();

        return manager;
    }

    private static GameObject CreatePlayer(Transform parent, Material playerMat, GameObject checkpointManager)
    {
        GameObject player = ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 2f, 0f), playerMat);

        // Wire checkpoint manager to player
        GameCheckpointManager checkpointMgr = checkpointManager.GetComponent<GameCheckpointManager>();
        SerializedObject so = new SerializedObject(checkpointMgr);
        so.FindProperty("player").objectReferenceValue = player;
        so.ApplyModifiedProperties();

        return player;
    }

    private static GameObject CreateCheckpoint(Transform parent, Vector3 position, Material checkpointMat, string name, GameObject checkpointManager)
    {
        GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        checkpoint.name = name;
        checkpoint.transform.SetParent(parent);
        checkpoint.transform.localPosition = position;
        checkpoint.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);

        // Apply material
        checkpoint.GetComponent<Renderer>().sharedMaterial = checkpointMat;

        // Make it a trigger
        CapsuleCollider col = checkpoint.GetComponent<CapsuleCollider>();
        col.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = checkpoint.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Wire to checkpoint manager
        GameCheckpointManager checkpointMgr = checkpointManager.GetComponent<GameCheckpointManager>();
        SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
        AddPersistentListener(onEnter, checkpointMgr, "SaveCheckpoint");
        triggerSO.ApplyModifiedProperties();

        return checkpoint;
    }

    private static GameObject CreateDeathZone(Transform parent, Vector3 position, GameObject player)
    {
        GameObject deathZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deathZone.name = "DeathZone";
        deathZone.transform.SetParent(parent);
        deathZone.transform.localPosition = position;
        deathZone.transform.localScale = new Vector3(50f, 0.5f, 50f);

        // Make transparent red
        Renderer renderer = deathZone.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0f, 0f, 0.3f);
        renderer.sharedMaterial = mat;

        // Make it a trigger
        BoxCollider col = deathZone.GetComponent<BoxCollider>();
        col.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = deathZone.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Get checkpoint manager from scene
        GameCheckpointManager checkpointMgr = Object.FindFirstObjectByType<GameCheckpointManager>();
        if (checkpointMgr != null)
        {
            SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
            AddPersistentListener(onEnter, checkpointMgr, "RestoreFromCheckpoint");
            triggerSO.ApplyModifiedProperties();
        }

        return deathZone;
    }

    private static GameObject CreatePlatformLevel(Transform parent)
    {
        GameObject level = new GameObject("PlatformLevel");
        level.transform.SetParent(parent);

        // Create main platform
        GameObject mainPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainPlatform.name = "MainPlatform";
        mainPlatform.transform.SetParent(level.transform);
        mainPlatform.transform.localPosition = new Vector3(0f, 0f, 0f);
        mainPlatform.transform.localScale = new Vector3(8f, 0.5f, 8f);

        // Create left platform
        GameObject leftPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPlatform.name = "LeftPlatform";
        leftPlatform.transform.SetParent(level.transform);
        leftPlatform.transform.localPosition = new Vector3(-5f, 0f, 0f);
        leftPlatform.transform.localScale = new Vector3(3f, 0.5f, 3f);

        // Create right platform
        GameObject rightPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPlatform.name = "RightPlatform";
        rightPlatform.transform.SetParent(level.transform);
        rightPlatform.transform.localPosition = new Vector3(5f, 0f, 5f);
        rightPlatform.transform.localScale = new Vector3(3f, 0.5f, 3f);

        // Apply gray material to all
        Material grayMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        grayMat.color = new Color(0.4f, 0.4f, 0.4f);

        mainPlatform.GetComponent<Renderer>().sharedMaterial = grayMat;
        leftPlatform.GetComponent<Renderer>().sharedMaterial = grayMat;
        rightPlatform.GetComponent<Renderer>().sharedMaterial = grayMat;

        return level;
    }

    private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.EventDefined;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 10f, -12f);
            mainCam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
