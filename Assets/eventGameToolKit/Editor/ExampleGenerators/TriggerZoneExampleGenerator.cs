using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating InputTriggerZone
/// Creates various trigger zones with enter, exit, and stay events
/// </summary>
public class TriggerZoneExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate TriggerZone Example")]
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
        GameObject root = new GameObject("TriggerZoneExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate TriggerZone Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject enterZone = CreateEnterZone(root.transform, new Vector3(-5f, 0.1f, 0f), pinkMat, canvas);
        GameObject stayZone = CreateStayZone(root.transform, new Vector3(5f, 0.1f, 0f), pinkMat, canvas);
        GameObject exitZone = CreateExitZone(root.transform, new Vector3(0f, 0.1f, 5f), pinkMat, canvas);
        GameObject ground = CreateGround(root.transform);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 450f), "Trigger Zone Demo\n\nDifferent zones trigger different events!", 44);
        CreateAnnotation(canvas.transform, new Vector2(0f, -430f), "WASD to Move through zones", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("TriggerZone example generated! Move through the zones to see events fire!");
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
        rect.sizeDelta = new Vector2(1600f, 280f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
    }

    private static GameObject CreatePlayer(Transform parent, Material playerMat)
    {
        return ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 1f, 0f), playerMat);
    }

    private static GameObject CreateEnterZone(Transform parent, Vector3 position, Material zoneMat, GameObject canvas)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "EnterZone";
        zone.transform.SetParent(parent);
        zone.transform.localPosition = position;
        zone.transform.localScale = new Vector3(3f, 0.2f, 3f);

        // Apply material with transparency
        Material mat = new Material(zoneMat);
        Color col = mat.color;
        col.a = 0.4f;
        mat.color = col;
        zone.GetComponent<Renderer>().sharedMaterial = mat;

        // Make it a trigger
        BoxCollider boxCol = zone.GetComponent<BoxCollider>();
        boxCol.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = zone.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Create feedback text
        GameObject feedbackText = CreateFeedbackText(canvas.transform, new Vector2(-600f, 200f), "ENTER ZONE\n(Not entered)");
        TextMeshProUGUI tmp = feedbackText.GetComponent<TextMeshProUGUI>();

        // Wire events to update text
        ActionDisplayText displayAction = feedbackText.AddComponent<ActionDisplayText>();
        SerializedObject displaySO = new SerializedObject(displayAction);
        displaySO.FindProperty("timeOnScreen").floatValue = 2f;
        displaySO.FindProperty("useFading").boolValue = false;
        displaySO.FindProperty("useTypewriter").boolValue = false;
        displaySO.ApplyModifiedProperties();

        SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
        AddPersistentListenerString(onEnter, displayAction, "DisplayText", "ENTER ZONE\n(Player Entered!)");
        triggerSO.ApplyModifiedProperties();

        return zone;
    }

    private static GameObject CreateStayZone(Transform parent, Vector3 position, Material zoneMat, GameObject canvas)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "StayZone";
        zone.transform.SetParent(parent);
        zone.transform.localPosition = position;
        zone.transform.localScale = new Vector3(3f, 0.2f, 3f);

        // Apply material with transparency
        Material mat = new Material(zoneMat);
        Color col = mat.color;
        col.a = 0.4f;
        mat.color = col;
        zone.GetComponent<Renderer>().sharedMaterial = mat;

        // Make it a trigger
        BoxCollider boxCol = zone.GetComponent<BoxCollider>();
        boxCol.isTrigger = true;

        // Add trigger zone with stay events
        InputTriggerZone trigger = zone.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.FindProperty("enableStayEvent").boolValue = true;
        triggerSO.FindProperty("stayInterval").floatValue = 1f;
        triggerSO.ApplyModifiedProperties();

        // Create feedback text
        GameObject feedbackText = CreateFeedbackText(canvas.transform, new Vector2(600f, 200f), "STAY ZONE\n(Tick every 1s)");
        TextMeshProUGUI tmp = feedbackText.GetComponent<TextMeshProUGUI>();

        // Wire stay event to update text
        ActionDisplayText displayAction = feedbackText.AddComponent<ActionDisplayText>();
        SerializedObject displaySO = new SerializedObject(displayAction);
        displaySO.FindProperty("timeOnScreen").floatValue = 1f;
        displaySO.FindProperty("useFading").boolValue = false;
        displaySO.FindProperty("useTypewriter").boolValue = false;
        displaySO.ApplyModifiedProperties();

        SerializedProperty onStay = triggerSO.FindProperty("onTriggerStayEvent");
        AddPersistentListenerString(onStay, displayAction, "DisplayText", "STAY ZONE\n(TICK!)");
        triggerSO.ApplyModifiedProperties();

        return zone;
    }

    private static GameObject CreateExitZone(Transform parent, Vector3 position, Material zoneMat, GameObject canvas)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "ExitZone";
        zone.transform.SetParent(parent);
        zone.transform.localPosition = position;
        zone.transform.localScale = new Vector3(3f, 0.2f, 3f);

        // Apply material with transparency
        Material mat = new Material(zoneMat);
        Color col = mat.color;
        col.a = 0.4f;
        mat.color = col;
        zone.GetComponent<Renderer>().sharedMaterial = mat;

        // Make it a trigger
        BoxCollider boxCol = zone.GetComponent<BoxCollider>();
        boxCol.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = zone.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Create feedback text
        GameObject feedbackText = CreateFeedbackText(canvas.transform, new Vector2(0f, -150f), "EXIT ZONE\n(Not exited)");
        TextMeshProUGUI tmp = feedbackText.GetComponent<TextMeshProUGUI>();

        // Wire exit event to update text
        ActionDisplayText displayAction = feedbackText.AddComponent<ActionDisplayText>();
        SerializedObject displaySO = new SerializedObject(displayAction);
        displaySO.FindProperty("timeOnScreen").floatValue = 2f;
        displaySO.FindProperty("useFading").boolValue = false;
        displaySO.FindProperty("useTypewriter").boolValue = false;
        displaySO.ApplyModifiedProperties();

        SerializedProperty onExit = triggerSO.FindProperty("onTriggerExitEvent");
        AddPersistentListenerString(onExit, displayAction, "DisplayText", "EXIT ZONE\n(Player Exited!)");
        triggerSO.ApplyModifiedProperties();

        return zone;
    }

    private static GameObject CreateFeedbackText(Transform parent, Vector2 position, string initialText)
    {
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(500f, 180f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = initialText;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.7f, 1f);
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
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

    private static void AddPersistentListenerString(SerializedProperty unityEvent, Object target, string methodName, string stringValue)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.String;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = stringValue;
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 12f, -10f);
            mainCam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
