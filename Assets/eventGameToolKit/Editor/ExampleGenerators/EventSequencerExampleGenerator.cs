using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating ActionEventSequencer
/// Creates a looping 5-second sequence with rotating cube and bouncing ball
/// </summary>
public class EventSequencerExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate EventSequencer Example")]
    public static void GenerateExample()
    {
        // Clear selection
        Selection.activeGameObject = null;

        // Create root container
        GameObject root = new GameObject("EventSequencerExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate EventSequencer Example");

        // Generate all components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject sequencer = CreateSequencer(root.transform);
        GameObject cube = CreateRotatingCube(root.transform);
        GameObject ball = CreateBouncingBall(root.transform);

        // Configure sequencer timeline events
        ConfigureSequencerEvents(sequencer, cube, ball);

        // Wire button to sequencer
        WireButtonToSequencer(canvas, sequencer);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("EventSequencer example scene generated successfully! Press Play and click the 'Start Sequence' button.");
    }

    private static GameObject CreateCanvas(Transform parent)
    {
        // Create EventSystem if it doesn't exist (required for UI button interaction)
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

        // Create Button
        GameObject buttonObj = new GameObject("StartButton");
        buttonObj.transform.SetParent(canvasObj.transform);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 50f);
        buttonRect.sizeDelta = new Vector2(300f, 80f);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        // Create Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Start Sequence";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return canvasObj;
    }

    private static GameObject CreateSequencer(Transform parent)
    {
        GameObject sequencerObj = new GameObject("EventSequencer");
        sequencerObj.transform.SetParent(parent);

        ActionEventSequencer sequencer = sequencerObj.AddComponent<ActionEventSequencer>();

        // Use SerializedObject to set private fields
        SerializedObject so = new SerializedObject(sequencer);
        so.FindProperty("duration").floatValue = 5f;
        so.FindProperty("loop").boolValue = true;
        so.FindProperty("playOnStart").boolValue = false;

        // Initialize empty events array (we'll configure it later)
        SerializedProperty eventsProperty = so.FindProperty("events");
        eventsProperty.arraySize = 3;

        so.ApplyModifiedProperties();

        return sequencerObj;
    }

    private static GameObject CreateRotatingCube(Transform parent)
    {
        // Create cube primitive
        GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeObj.name = "RotatingCube";
        cubeObj.transform.SetParent(parent);
        cubeObj.transform.localPosition = new Vector3(-3f, 1f, 0f);

        // Make it red
        Renderer renderer = cubeObj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.2f, 0.2f);
        renderer.sharedMaterial = mat;

        // Add ActionAnimateTransform
        ActionAnimateTransform animator = cubeObj.AddComponent<ActionAnimateTransform>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(animator);
        so.FindProperty("targetTransform").objectReferenceValue = cubeObj.transform;
        so.FindProperty("duration").floatValue = 2f;
        so.FindProperty("loop").boolValue = true;
        so.FindProperty("playOnStart").boolValue = true;  // Auto-start when enabled

        // Configure curve mapping for continuous rotation
        SerializedProperty mappings = so.FindProperty("curveMappings");
        mappings.arraySize = 1;

        SerializedProperty mapping = mappings.GetArrayElementAtIndex(0);
        mapping.FindPropertyRelative("property").enumValueIndex = (int)ActionAnimateTransform.TransformProperty.RotationY;
        mapping.FindPropertyRelative("mode").enumValueIndex = (int)ActionAnimateTransform.AnimationMode.Offset;
        mapping.FindPropertyRelative("minValue").floatValue = 0f;
        mapping.FindPropertyRelative("maxValue").floatValue = 360f;
        mapping.FindPropertyRelative("enabled").boolValue = true;

        // Create linear curve (0 to 1) for smooth rotation
        AnimationCurve linearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        mapping.FindPropertyRelative("curve").animationCurveValue = linearCurve;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(animator);

        // Disable initially
        cubeObj.SetActive(false);

        return cubeObj;
    }

    private static GameObject CreateBouncingBall(Transform parent)
    {
        // Create sphere primitive
        GameObject ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObj.name = "BouncingBall";
        ballObj.transform.SetParent(parent);
        ballObj.transform.localPosition = new Vector3(3f, 1f, 0f);

        // Make it blue
        Renderer renderer = ballObj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.2f, 0.6f, 1f);
        renderer.sharedMaterial = mat;

        // Add ActionAnimateTransform
        ActionAnimateTransform animator = ballObj.AddComponent<ActionAnimateTransform>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(animator);
        so.FindProperty("targetTransform").objectReferenceValue = ballObj.transform;
        so.FindProperty("duration").floatValue = 1f;
        so.FindProperty("loop").boolValue = true;
        so.FindProperty("playOnStart").boolValue = true;  // Auto-start when enabled

        // Configure curve mapping for bouncing
        SerializedProperty mappings = so.FindProperty("curveMappings");
        mappings.arraySize = 1;

        SerializedProperty mapping = mappings.GetArrayElementAtIndex(0);
        mapping.FindPropertyRelative("property").enumValueIndex = (int)ActionAnimateTransform.TransformProperty.PositionY;
        mapping.FindPropertyRelative("mode").enumValueIndex = (int)ActionAnimateTransform.AnimationMode.Offset;
        mapping.FindPropertyRelative("minValue").floatValue = 0f;
        mapping.FindPropertyRelative("maxValue").floatValue = 2f;
        mapping.FindPropertyRelative("enabled").boolValue = true;

        // Create bounce curve (sine wave: 0 to 1 to 0)
        AnimationCurve bounceCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
        );
        // Smooth the curve
        for (int i = 0; i < bounceCurve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(bounceCurve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(bounceCurve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        mapping.FindPropertyRelative("curve").animationCurveValue = bounceCurve;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(animator);

        // Disable initially
        ballObj.SetActive(false);

        return ballObj;
    }

    private static void ConfigureSequencerEvents(GameObject sequencer, GameObject cube, GameObject ball)
    {
        ActionEventSequencer seq = sequencer.GetComponent<ActionEventSequencer>();

        SerializedObject so = new SerializedObject(seq);

        SerializedProperty eventsArray = so.FindProperty("events");
        eventsArray.arraySize = 3;

        // Event 0: At 1 second, enable cube (animation auto-starts via OnEnable + playOnStart)
        SerializedProperty event0 = eventsArray.GetArrayElementAtIndex(0);
        event0.FindPropertyRelative("eventName").stringValue = "Enable Cube";
        event0.FindPropertyRelative("triggerTime").floatValue = 1f;

        SerializedProperty event0Trigger = event0.FindPropertyRelative("onTrigger");
        AddPersistentListener(event0Trigger, cube, "SetActive", true);

        // Event 1: At 3 seconds, enable ball (animation auto-starts via OnEnable + playOnStart)
        SerializedProperty event1 = eventsArray.GetArrayElementAtIndex(1);
        event1.FindPropertyRelative("eventName").stringValue = "Enable Ball";
        event1.FindPropertyRelative("triggerTime").floatValue = 3f;

        SerializedProperty event1Trigger = event1.FindPropertyRelative("onTrigger");
        AddPersistentListener(event1Trigger, ball, "SetActive", true);

        // Event 2: At 5 seconds, disable both
        SerializedProperty event2 = eventsArray.GetArrayElementAtIndex(2);
        event2.FindPropertyRelative("eventName").stringValue = "Disable Both";
        event2.FindPropertyRelative("triggerTime").floatValue = 5f;

        SerializedProperty event2Trigger = event2.FindPropertyRelative("onTrigger");
        AddPersistentListener(event2Trigger, cube, "SetActive", false);
        AddPersistentListener(event2Trigger, ball, "SetActive", false);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(seq);
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

    private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName, bool boolValue)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Bool;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = boolValue;
    }

    private static void WireButtonToSequencer(GameObject canvas, GameObject sequencer)
    {
        Button button = canvas.GetComponentInChildren<Button>();
        ActionEventSequencer seq = sequencer.GetComponent<ActionEventSequencer>();

        SerializedObject so = new SerializedObject(button);
        SerializedProperty onClickProperty = so.FindProperty("m_OnClick");

        AddPersistentListener(onClickProperty, seq, "StartSequence");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(button);
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 2f, -10f);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
