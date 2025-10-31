using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating GameInventorySlot
/// Creates collectible items that fill an inventory with capacity limits
/// </summary>
public class InventorySystemExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate InventorySystem Example")]
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
        GameObject root = new GameObject("InventorySystemExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate InventorySystem Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject inventory = CreateInventorySlot(root.transform);
        GameObject inventoryDisplay = CreateInventoryDisplay(canvas.transform, inventory);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject ground = CreateGround(root.transform);

        // Create 10 collectibles (inventory only holds 5)
        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 6f, 1f, Mathf.Sin(angle) * 6f);
            CreateCollectible(root.transform, pos, pinkMat, $"Item_{i}", inventory);
        }

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 380f), "Inventory System Demo\n\nCollect pink items!", 48);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "WASD to Move | Inventory Capacity: 5 items", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("InventorySystem example generated! Collect items - inventory is limited to 5!");
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

    private static GameObject CreateInventorySlot(Transform parent)
    {
        GameObject inventory = new GameObject("InventorySlot");
        inventory.transform.SetParent(parent);

        GameInventorySlot invSlot = inventory.AddComponent<GameInventorySlot>();

        SerializedObject so = new SerializedObject(invSlot);
        so.FindProperty("itemType").stringValue = "Item";
        so.FindProperty("maxCapacity").intValue = 5;
        so.FindProperty("currentValue").intValue = 0;
        so.ApplyModifiedProperties();

        return inventory;
    }

    private static GameObject CreateInventoryDisplay(Transform parent, GameObject inventory)
    {
        GameObject textObj = new GameObject("InventoryDisplay");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(50f, -50f);
        rect.sizeDelta = new Vector2(500f, 100f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Inventory: 0 / 5";
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        // Wire to inventory
        GameInventorySlot invSlot = inventory.GetComponent<GameInventorySlot>();
        SerializedObject so = new SerializedObject(invSlot);
        so.FindProperty("displayText").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        return textObj;
    }

    private static GameObject CreatePlayer(Transform parent, Material playerMat)
    {
        return ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 1f, 0f), playerMat);
    }

    private static GameObject CreateCollectible(Transform parent, Vector3 position, Material collectibleMat, string name, GameObject inventory)
    {
        GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        collectible.name = name;
        collectible.transform.SetParent(parent);
        collectible.transform.localPosition = position;
        collectible.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        // Apply material
        collectible.GetComponent<Renderer>().sharedMaterial = collectibleMat;

        // Make it a trigger
        SphereCollider col = collectible.GetComponent<SphereCollider>();
        col.isTrigger = true;

        // Add trigger zone
        InputTriggerZone trigger = collectible.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.ApplyModifiedProperties();

        // Wire to inventory
        GameInventorySlot invSlot = inventory.GetComponent<GameInventorySlot>();
        SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
        AddPersistentListener(onEnter, invSlot, "Increment");
        AddPersistentListenerBool(onEnter, collectible, "SetActive", false);
        triggerSO.ApplyModifiedProperties();

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

    private static void AddPersistentListenerBool(SerializedProperty unityEvent, Object target, string methodName, bool boolValue)
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
