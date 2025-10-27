using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating GameCollectionManager
/// Creates collectible items that increase score when the player touches them
/// </summary>
public class CollectionSystemExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate CollectionSystem Example")]
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
        GameObject root = new GameObject("CollectionSystemExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate CollectionSystem Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject collectionManager = CreateCollectionManager(root.transform);
        GameObject scoreDisplay = CreateScoreDisplay(canvas.transform, collectionManager);
        GameObject ground = CreateGround(root.transform);

        // Create collectibles in a circle
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 5f, 1f, Mathf.Sin(angle) * 5f);
            CreateCollectible(root.transform, pos, pinkMat, $"Collectible_{i}", collectionManager);
        }

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 400f), "Collection System Demo\n\nCollect the pink items!", 48);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "WASD to Move\nScore: Reach 8 to complete!", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("CollectionSystem example generated! Press Play and collect all pink items.");
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
        rect.sizeDelta = new Vector2(1400f, 300f);

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

    private static GameObject CreateCollectionManager(Transform parent)
    {
        GameObject manager = new GameObject("CollectionManager");
        manager.transform.SetParent(parent);

        GameCollectionManager collectionMgr = manager.AddComponent<GameCollectionManager>();

        SerializedObject so = new SerializedObject(collectionMgr);
        so.FindProperty("startingScore").intValue = 0;
        so.FindProperty("displayScore").boolValue = true;

        // Add threshold at 8
        SerializedProperty thresholds = so.FindProperty("thresholds");
        thresholds.arraySize = 1;
        SerializedProperty threshold = thresholds.GetArrayElementAtIndex(0);
        threshold.FindPropertyRelative("thresholdValue").intValue = 8;

        so.ApplyModifiedProperties();

        return manager;
    }

    private static GameObject CreateScoreDisplay(Transform parent, GameObject collectionManager)
    {
        GameObject textObj = new GameObject("ScoreDisplay");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(50f, -50f);
        rect.sizeDelta = new Vector2(400f, 100f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Score: 0";
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        // Wire up to collection manager
        GameCollectionManager collectionMgr = collectionManager.GetComponent<GameCollectionManager>();
        SerializedObject so = new SerializedObject(collectionMgr);
        so.FindProperty("scoreDisplay").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        return textObj;
    }

    private static GameObject CreateCollectible(Transform parent, Vector3 position, Material collectibleMat, string name, GameObject collectionManager)
    {
        GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Cube);
        collectible.name = name;
        collectible.transform.SetParent(parent);
        collectible.transform.localPosition = position;
        collectible.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        // Apply material
        collectible.GetComponent<Renderer>().sharedMaterial = collectibleMat;

        // Make it a trigger
        BoxCollider col = collectible.GetComponent<BoxCollider>();
        col.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = collectible.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Wire to collection manager
        GameCollectionManager collectionMgr = collectionManager.GetComponent<GameCollectionManager>();
        SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
        AddPersistentListenerInt(onEnter, collectionMgr, "AddToScore", 1);
        triggerSO.ApplyModifiedProperties();

        // Add rotation animation
        ActionAnimateTransform animator = collectible.AddComponent<ActionAnimateTransform>();
        SerializedObject animSO = new SerializedObject(animator);
        animSO.FindProperty("targetTransform").objectReferenceValue = collectible.transform;
        animSO.FindProperty("duration").floatValue = 2f;
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

        // Wire trigger to destroy self
        AddPersistentListener(onEnter, collectible, "SetActive", false);

        return collectible;
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

    private static void AddPersistentListenerInt(SerializedProperty unityEvent, Object target, string methodName, int intValue)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Int;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue = intValue;
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 12f, -12f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
