using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating GameHealthManager
/// Creates damage zones and a health bar showing player health
/// </summary>
public class HealthSystemExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate HealthSystem Example")]
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
        GameObject root = new GameObject("HealthSystemExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate HealthSystem Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject healthBar = CreateHealthBar(canvas.transform, player);
        GameObject ground = CreateGround(root.transform);
        GameObject damageZone1 = CreateDamageZone(root.transform, new Vector3(-4f, 0.1f, 0f), pinkMat, player, "DamageZone_Left");
        GameObject damageZone2 = CreateDamageZone(root.transform, new Vector3(4f, 0.1f, 0f), pinkMat, player, "DamageZone_Right");

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 400f), "Health System Demo\n\nAvoid the pink damage zones!", 48);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "WASD to Move | Health depletes on contact", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("HealthSystem example generated! Press Play and avoid the pink zones.");
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
        GameObject player = ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 1f, 0f), playerMat);

        // Add health manager
        GameHealthManager healthMgr = player.AddComponent<GameHealthManager>();
        SerializedObject so = new SerializedObject(healthMgr);
        so.FindProperty("maxHealth").floatValue = 100f;
        so.FindProperty("currentHealth").floatValue = 100f;
        so.FindProperty("displayHealth").boolValue = true;

        // Add threshold at 50% and 25%
        SerializedProperty thresholds = so.FindProperty("healthThresholds");
        thresholds.arraySize = 2;

        SerializedProperty threshold1 = thresholds.GetArrayElementAtIndex(0);
        threshold1.FindPropertyRelative("thresholdPercentage").floatValue = 50f;
        threshold1.FindPropertyRelative("thresholdName").stringValue = "Half Health";

        SerializedProperty threshold2 = thresholds.GetArrayElementAtIndex(1);
        threshold2.FindPropertyRelative("thresholdPercentage").floatValue = 25f;
        threshold2.FindPropertyRelative("thresholdName").stringValue = "Critical Health";

        so.ApplyModifiedProperties();

        return player;
    }

    private static GameObject CreateHealthBar(Transform parent, GameObject player)
    {
        // Create health bar background
        GameObject healthBarBg = new GameObject("HealthBar_Background");
        healthBarBg.transform.SetParent(parent);

        RectTransform bgRect = healthBarBg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.anchoredPosition = new Vector2(0f, -50f);
        bgRect.sizeDelta = new Vector2(600f, 50f);

        Image bgImage = healthBarBg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Create health bar fill
        GameObject healthBarFill = new GameObject("HealthBar_Fill");
        healthBarFill.transform.SetParent(healthBarBg.transform);

        RectTransform fillRect = healthBarFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        Image fillImage = healthBarFill.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        // Wire to health manager
        GameHealthManager healthMgr = player.GetComponent<GameHealthManager>();
        SerializedObject so = new SerializedObject(healthMgr);
        so.FindProperty("healthBar").objectReferenceValue = fillImage;
        so.ApplyModifiedProperties();

        // Add label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(healthBarBg.transform);

        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = "HEALTH";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return healthBarBg;
    }

    private static GameObject CreateDamageZone(Transform parent, Vector3 position, Material zoneMat, GameObject player, string name)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = name;
        zone.transform.SetParent(parent);
        zone.transform.localPosition = position;
        zone.transform.localScale = new Vector3(2f, 0.2f, 2f);

        // Apply material with transparency
        Material mat = new Material(zoneMat);
        Color col = mat.color;
        col.a = 0.5f;
        mat.color = col;
        zone.GetComponent<Renderer>().sharedMaterial = mat;

        // Make it a trigger
        BoxCollider col2 = zone.GetComponent<BoxCollider>();
        col2.isTrigger = true;

        // Add trigger zone that deals damage
        InputTriggerZone trigger = zone.AddComponent<InputTriggerZone>();
        SerializedObject triggerSO = new SerializedObject(trigger);
        triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        triggerSO.FindProperty("enableStayEvent").boolValue = true;
        triggerSO.FindProperty("stayInterval").floatValue = 1f;
        triggerSO.ApplyModifiedProperties();

        // Wire to health manager damage
        GameHealthManager healthMgr = player.GetComponent<GameHealthManager>();
        SerializedProperty onStay = triggerSO.FindProperty("onTriggerStayEvent");
        AddPersistentListenerFloat(onStay, healthMgr, "TakeDamage", 10f);
        triggerSO.ApplyModifiedProperties();

        return zone;
    }

    private static GameObject CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f);

        // Gray material
        Renderer renderer = ground.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.3f, 0.3f);
        renderer.sharedMaterial = mat;

        return ground;
    }

    private static void AddPersistentListenerFloat(SerializedProperty unityEvent, Object target, string methodName, float floatValue)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Float;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue = floatValue;
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 8f, -8f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
