using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating GameTimerManager
/// Creates a countdown timer with visual feedback and threshold events
/// </summary>
public class TimerSystemExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate TimerSystem Example")]
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
        GameObject root = new GameObject("TimerSystemExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate TimerSystem Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject timerManager = CreateTimerManager(root.transform);
        GameObject timerDisplay = CreateTimerDisplay(canvas.transform, timerManager);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject goal = CreateGoal(root.transform, pinkMat, timerManager);
        GameObject ground = CreateGround(root.transform);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 350f), "Timer System Demo\n\nReach the pink cube before time runs out!", 42);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "WASD to Move | You have 30 seconds!", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("TimerSystem example generated! Press Play and race against the clock!");
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

    private static GameObject CreateTimerManager(Transform parent)
    {
        GameObject manager = new GameObject("TimerManager");
        manager.transform.SetParent(parent);

        GameTimerManager timerMgr = manager.AddComponent<GameTimerManager>();

        SerializedObject so = new SerializedObject(timerMgr);
        so.FindProperty("timerMode").enumValueIndex = 1; // Countdown
        so.FindProperty("startTime").floatValue = 30f;
        so.FindProperty("autoStart").boolValue = true;
        so.FindProperty("displayFormat").enumValueIndex = 0; // MM:SS

        // Add thresholds at 10 and 5 seconds
        SerializedProperty thresholds = so.FindProperty("timeThresholds");
        thresholds.arraySize = 2;

        SerializedProperty threshold1 = thresholds.GetArrayElementAtIndex(0);
        threshold1.FindPropertyRelative("thresholdTime").floatValue = 10f;
        threshold1.FindPropertyRelative("thresholdName").stringValue = "10 Seconds Left";

        SerializedProperty threshold2 = thresholds.GetArrayElementAtIndex(1);
        threshold2.FindPropertyRelative("thresholdTime").floatValue = 5f;
        threshold2.FindPropertyRelative("thresholdName").stringValue = "5 Seconds Left";

        so.ApplyModifiedProperties();

        return manager;
    }

    private static GameObject CreateTimerDisplay(Transform parent, GameObject timerManager)
    {
        GameObject textObj = new GameObject("TimerDisplay");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -50f);
        rect.sizeDelta = new Vector2(600f, 120f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "00:30";
        tmp.fontSize = 72;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        // Wire to timer manager
        GameTimerManager timerMgr = timerManager.GetComponent<GameTimerManager>();
        SerializedObject so = new SerializedObject(timerMgr);
        so.FindProperty("timerDisplay").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        return textObj;
    }

    private static GameObject CreatePlayer(Transform parent, Material playerMat)
    {
        return ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 1f, 0f), playerMat);
    }

    private static GameObject CreateGoal(Transform parent, Material goalMat, GameObject timerManager)
    {
        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "Goal";
        goal.transform.SetParent(parent);
        goal.transform.localPosition = new Vector3(8f, 1f, 0f);
        goal.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        // Apply material
        goal.GetComponent<Renderer>().sharedMaterial = goalMat;

        // Make it a trigger
        BoxCollider col = goal.GetComponent<BoxCollider>();
        col.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = goal.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Wire to stop timer
        GameTimerManager timerMgr = timerManager.GetComponent<GameTimerManager>();
        SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
        AddPersistentListener(onEnter, timerMgr, "StopTimer");
        triggerSO.ApplyModifiedProperties();

        // Add rotation animation
        ActionAnimateTransform animator = goal.AddComponent<ActionAnimateTransform>();
        SerializedObject animSO = new SerializedObject(animator);
        animSO.FindProperty("targetTransform").objectReferenceValue = goal.transform;
        animSO.FindProperty("duration").floatValue = 3f;
        animSO.FindProperty("loop").boolValue = true;
        animSO.FindProperty("playOnStart").boolValue = true;

        SerializedProperty mappings = animSO.FindProperty("curveMappings");
        mappings.arraySize = 1;
        SerializedProperty mapping = mappings.GetArrayElementAtIndex(0);
        mapping.FindPropertyRelative("property").enumValueIndex = (int)ActionAnimateTransform.TransformProperty.RotationY;
        mapping.FindPropertyRelative("mode").enumValueIndex = (int)ActionAnimateTransform.AnimationMode.Offset;
        mapping.FindPropertyRelative("minValue").floatValue = 0f;
        mapping.FindPropertyRelative("maxValue").floatValue = 360f;
        mapping.FindPropertyRelative("enabled").boolValue = true;

        AnimationCurve linearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        mapping.FindPropertyRelative("curve").animationCurveValue = linearCurve;

        animSO.ApplyModifiedProperties();

        return goal;
    }

    private static GameObject CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);

        // Gray material
        Renderer renderer = ground.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.3f, 0.3f);
        renderer.sharedMaterial = mat;

        return ground;
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
            mainCam.transform.position = new Vector3(4f, 8f, -8f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
