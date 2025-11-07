using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate a comprehensive example demonstrating GameStateManager, GameUIManager, and GameHealthManager working together.
/// Shows pause functionality, health tracking, score system, and victory conditions in one complete example.
/// </summary>
public class GameManagersExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate Game Managers Example")]
    public static void GenerateExample()
    {
        // Clear selection
        Selection.activeGameObject = null;

        // Validate URP shader
        if (!ExampleMaterialHelper.ValidateURPShader())
        {
            Debug.LogError("Cannot create example: URP shader not found!");
            return;
        }

        // Get or create materials
        Material pinkMat = ExampleMaterialHelper.GetPinkMaterial();

        // Create root container
        GameObject root = new GameObject("GameManagersExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate Game Managers Example");

        // Generate all components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject managers = CreateManagers(root.transform);
        GameObject damageZone = CreateDamageZone(root.transform, pinkMat);
        GameObject healZone = CreateHealZone(root.transform);
        GameObject collectibles = CreateCollectibles(root.transform, pinkMat);
        GameObject ground = CreateGround(root.transform);

        // Wire up event connections between managers
        WireManagerEvents(managers);

        // Add instructions
        CreateInstructions(canvas.transform);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("Game Managers example generated!\n\n" +
                  "Press Play to see:\n" +
                  "- Health system with damage/heal zones\n" +
                  "- Score tracking from collectibles\n" +
                  "- Pause system (P key)\n" +
                  "- Victory condition (collect 5 items)\n" +
                  "- All managers working together!");
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

    private static void CreateInstructions(Transform canvasTransform)
    {
        // Title
        CreateText(canvasTransform, new Vector2(0f, 450f), "Game Managers Demo", 56, FontStyles.Bold, TextAlignmentOptions.Center);

        // Instructions
        string instructions = "P to Pause | Walk into pink zones to take damage | Walk into green zones to heal\nCollect 5 pink cubes to win!";
        CreateText(canvasTransform, new Vector2(0f, -450f), instructions, 32, FontStyles.Normal, TextAlignmentOptions.Center);
    }

    private static GameObject CreateManagers(Transform parent)
    {
        GameObject managersObj = new GameObject("--- MANAGERS ---");
        managersObj.transform.SetParent(parent);

        // Create all manager GameObjects
        GameObject stateManagerObj = CreateGameStateManager(managersObj.transform);
        GameObject uiManagerObj = CreateGameUIManager(managersObj.transform, parent);
        GameObject healthManagerObj = CreateGameHealthManager(managersObj.transform);
        GameObject collectionManagerObj = CreateGameCollectionManager(managersObj.transform);

        return managersObj;
    }

    private static GameObject CreateGameStateManager(Transform parent)
    {
        GameObject obj = new GameObject("GameStateManager");
        obj.transform.SetParent(parent);

        GameStateManager manager = obj.AddComponent<GameStateManager>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("pauseKey").enumValueIndex = (int)KeyCode.P;
        so.FindProperty("startPaused").boolValue = false;
        so.FindProperty("autoPauseTimers").boolValue = true;

        // Pause panel and restart button will be set in WireManagerEvents
        so.ApplyModifiedProperties();

        return obj;
    }

    private static GameObject CreateGameUIManager(Transform parent, Transform rootParent)
    {
        GameObject obj = new GameObject("GameUIManager");
        obj.transform.SetParent(parent);

        GameUIManager manager = obj.AddComponent<GameUIManager>();

        // Find Canvas
        Canvas canvas = rootParent.GetComponentInChildren<Canvas>();
        if (canvas == null) return obj;

        // Create HUD Panel
        GameObject hudPanel = CreateHUDPanel(canvas.transform);

        // Create Pause Panel
        GameObject pausePanel = CreatePausePanel(canvas.transform);

        // Create Victory Panel
        GameObject victoryPanel = CreateVictoryPanel(canvas.transform);

        // Wire up UI manager references
        SerializedObject so = new SerializedObject(manager);

        // Score
        TextMeshProUGUI scoreText = hudPanel.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("scorePrefix").stringValue = "Score: ";
        so.FindProperty("currentScore").intValue = 0;

        // Health
        TextMeshProUGUI healthText = hudPanel.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
        Slider healthBar = hudPanel.transform.Find("HealthBar")?.GetComponent<Slider>();
        Image healthBarFill = healthBar?.transform.Find("Fill Area/Fill")?.GetComponent<Image>();

        so.FindProperty("healthText").objectReferenceValue = healthText;
        so.FindProperty("healthBar").objectReferenceValue = healthBar;
        so.FindProperty("healthBarFill").objectReferenceValue = healthBarFill;
        so.FindProperty("healthPrefix").stringValue = "Health: ";

        // Victory
        TextMeshProUGUI victoryText = victoryPanel.transform.Find("VictoryText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("victoryText").objectReferenceValue = victoryText;

        so.ApplyModifiedProperties();

        // Store references for GameStateManager
        obj.AddComponent<UIReferences>().pausePanel = pausePanel;
        obj.AddComponent<VictoryPanelReference>().victoryPanel = victoryPanel;

        return obj;
    }

    private static GameObject CreateGameHealthManager(Transform parent)
    {
        GameObject obj = new GameObject("GameHealthManager");
        obj.transform.SetParent(parent);

        GameHealthManager manager = obj.AddComponent<GameHealthManager>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("maxHealth").intValue = 100;
        so.FindProperty("currentHealth").intValue = 100;
        so.FindProperty("lowHealthThreshold").intValue = 30;
        so.FindProperty("healthDisplay").objectReferenceValue = null; // UI manager handles display
        so.ApplyModifiedProperties();

        return obj;
    }

    private static GameObject CreateGameCollectionManager(Transform parent)
    {
        GameObject obj = new GameObject("GameCollectionManager");
        obj.transform.SetParent(parent);

        GameCollectionManager manager = obj.AddComponent<GameCollectionManager>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("targetTag").stringValue = "Collectible";
        so.FindProperty("itemsToCollect").intValue = 5;
        so.FindProperty("currentCount").intValue = 0;
        so.FindProperty("destroyOnCollect").boolValue = true;

        // Add threshold for victory at 5 items
        SerializedProperty thresholds = so.FindProperty("thresholds");
        thresholds.arraySize = 1;
        SerializedProperty threshold = thresholds.GetArrayElementAtIndex(0);
        threshold.FindPropertyRelative("count").intValue = 5;
        threshold.FindPropertyRelative("eventName").stringValue = "Victory!";

        so.ApplyModifiedProperties();

        return obj;
    }

    private static GameObject CreateHUDPanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject("HUD");
        panel.transform.SetParent(canvasTransform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Score Text (top left)
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(panel.transform);
        RectTransform scoreRect = scoreObj.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0, 1);
        scoreRect.anchorMax = new Vector2(0, 1);
        scoreRect.pivot = new Vector2(0, 1);
        scoreRect.anchoredPosition = new Vector2(20, -20);
        scoreRect.sizeDelta = new Vector2(400, 80);

        TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 48;
        scoreText.color = Color.white;
        scoreText.alignment = TextAlignmentOptions.TopLeft;
        scoreText.fontStyle = FontStyles.Bold;

        // Health Text (top right)
        GameObject healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(panel.transform);
        RectTransform healthTextRect = healthTextObj.AddComponent<RectTransform>();
        healthTextRect.anchorMin = new Vector2(1, 1);
        healthTextRect.anchorMax = new Vector2(1, 1);
        healthTextRect.pivot = new Vector2(1, 1);
        healthTextRect.anchoredPosition = new Vector2(-20, -20);
        healthTextRect.sizeDelta = new Vector2(400, 80);

        TextMeshProUGUI healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "Health: 100/100";
        healthText.fontSize = 48;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.TopRight;
        healthText.fontStyle = FontStyles.Bold;

        // Health Bar (top center)
        GameObject healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(panel.transform);
        RectTransform healthBarRect = healthBarObj.AddComponent<RectTransform>();
        healthBarRect.anchorMin = new Vector2(0.5f, 1);
        healthBarRect.anchorMax = new Vector2(0.5f, 1);
        healthBarRect.pivot = new Vector2(0.5f, 1);
        healthBarRect.anchoredPosition = new Vector2(0, -120);
        healthBarRect.sizeDelta = new Vector2(600, 40);

        Slider slider = healthBarObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBarObj.transform);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(healthBarObj.transform);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return panel;
    }

    private static GameObject CreatePausePanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasTransform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);

        // Pause Text
        GameObject textObj = new GameObject("PauseText");
        textObj.transform.SetParent(panel.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(800, 200);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "PAUSED";
        text.fontSize = 120;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;

        // Resume Button
        GameObject buttonObj = new GameObject("ResumeButton");
        buttonObj.transform.SetParent(panel.transform);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(300, 80);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.6f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform);
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Resume (P)";
        buttonText.fontSize = 36;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateVictoryPanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(canvasTransform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0.5f, 0, 0.9f);

        // Victory Text
        GameObject textObj = new GameObject("VictoryText");
        textObj.transform.SetParent(panel.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1000, 400);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "VICTORY!";
        text.fontSize = 96;
        text.color = Color.yellow;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateText(Transform parent, Vector2 position, string content, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1600, 200);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.fontStyle = fontStyle;

        return textObj;
    }

    private static void WireManagerEvents(GameObject managersRoot)
    {
        GameStateManager stateManager = managersRoot.GetComponentInChildren<GameStateManager>();
        GameUIManager uiManager = managersRoot.GetComponentInChildren<GameUIManager>();
        GameHealthManager healthManager = managersRoot.GetComponentInChildren<GameHealthManager>();
        GameCollectionManager collectionManager = managersRoot.GetComponentInChildren<GameCollectionManager>();

        // Wire pause panel to state manager
        UIReferences uiRefs = uiManager.GetComponent<UIReferences>();
        if (uiRefs != null && stateManager != null)
        {
            SerializedObject so = new SerializedObject(stateManager);
            so.FindProperty("pausePanel").objectReferenceValue = uiRefs.pausePanel;

            // Wire resume button
            Button resumeButton = uiRefs.pausePanel.GetComponentInChildren<Button>();
            if (resumeButton != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(resumeButton.onClick, stateManager.ResumeGame);
                EditorUtility.SetDirty(resumeButton);
            }

            so.ApplyModifiedProperties();
        }

        // Wire health manager → UI manager
        SerializedObject healthSO = new SerializedObject(healthManager);
        SerializedProperty healthChanged = healthSO.FindProperty("onHealthChanged");
        AddPersistentListener(healthChanged, uiManager, "UpdateHealth", healthManager.CurrentHealth, healthManager.MaxHealth);
        healthSO.ApplyModifiedProperties();

        // Wire collection manager → UI manager (score)
        SerializedObject collectionSO = new SerializedObject(collectionManager);
        SerializedProperty onCollected = collectionSO.FindProperty("onItemCollected");
        AddPersistentListener(onCollected, uiManager, "AddScore", 10);
        collectionSO.ApplyModifiedProperties();

        // Wire collection victory → state manager + victory panel
        SerializedProperty thresholds = collectionSO.FindProperty("thresholds");
        if (thresholds.arraySize > 0)
        {
            SerializedProperty threshold = thresholds.GetArrayElementAtIndex(0);
            SerializedProperty thresholdEvent = threshold.FindPropertyRelative("onThresholdReached");

            AddPersistentListener(thresholdEvent, stateManager, "Victory");

            VictoryPanelReference victoryRef = uiManager.GetComponent<VictoryPanelReference>();
            if (victoryRef != null && victoryRef.victoryPanel != null)
            {
                AddPersistentListener(thresholdEvent, victoryRef.victoryPanel, "SetActive", true);
            }

            AddPersistentListener(thresholdEvent, uiManager, "DisplayVictory");
        }
        collectionSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(healthManager);
        EditorUtility.SetDirty(collectionManager);
        EditorUtility.SetDirty(stateManager);
        EditorUtility.SetDirty(uiManager);

        // Clean up temporary helper components (they're Editor scripts and would cause "Missing Script" errors in Play mode)
        if (uiRefs != null)
        {
            Object.DestroyImmediate(uiRefs);
        }
        VictoryPanelReference victoryRefCleanup = uiManager.GetComponent<VictoryPanelReference>();
        if (victoryRefCleanup != null)
        {
            Object.DestroyImmediate(victoryRefCleanup);
        }
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

    private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName, int intValue)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Int;
        call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue = intValue;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
    }

    private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName, int arg1, int arg2)
    {
        // For methods with 2 int parameters, we need to call it twice or use a wrapper
        // For now, we'll add a single call with the first parameter
        AddPersistentListener(unityEvent, target, methodName, arg1);
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
        call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = boolValue;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
    }

    private static GameObject CreateDamageZone(Transform parent, Material zoneMat)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "DamageZone";
        zone.transform.SetParent(parent);
        zone.transform.localPosition = new Vector3(-5f, 0.5f, 0f);
        zone.transform.localScale = new Vector3(3f, 1f, 3f);
        zone.tag = "DamageZone";

        // Apply material
        Renderer renderer = zone.GetComponent<Renderer>();
        renderer.sharedMaterial = zoneMat;

        // Make it a trigger
        Collider collider = zone.GetComponent<Collider>();
        collider.isTrigger = true;

        // Add trigger zone component
        InputTriggerZone triggerZone = zone.AddComponent<InputTriggerZone>();
        SerializedObject so = new SerializedObject(triggerZone);
        so.FindProperty("targetTag").stringValue = "Player";
        so.FindProperty("stayInterval").floatValue = 1f; // Damage every second

        // Wire to health manager
        GameObject managersRoot = parent.Find("--- MANAGERS ---").gameObject;
        GameHealthManager healthManager = managersRoot.GetComponentInChildren<GameHealthManager>();

        SerializedProperty onStay = so.FindProperty("onTriggerStay");
        AddPersistentListener(onStay, healthManager, "TakeDamage", 10);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(triggerZone);

        return zone;
    }

    private static GameObject CreateHealZone(Transform parent)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "HealZone";
        zone.transform.SetParent(parent);
        zone.transform.localPosition = new Vector3(5f, 0.5f, 0f);
        zone.transform.localScale = new Vector3(3f, 1f, 3f);
        zone.tag = "HealZone";

        // Green material
        Renderer renderer = zone.GetComponent<Renderer>();
        Material mat = ExampleMaterialHelper.CreateTemporaryMaterial(Color.green);
        renderer.sharedMaterial = mat;

        // Make it a trigger
        Collider collider = zone.GetComponent<Collider>();
        collider.isTrigger = true;

        // Add trigger zone component
        InputTriggerZone triggerZone = zone.AddComponent<InputTriggerZone>();
        SerializedObject so = new SerializedObject(triggerZone);
        so.FindProperty("targetTag").stringValue = "Player";
        so.FindProperty("stayInterval").floatValue = 1f;

        // Wire to health manager
        GameObject managersRoot = parent.Find("--- MANAGERS ---").gameObject;
        GameHealthManager healthManager = managersRoot.GetComponentInChildren<GameHealthManager>();

        SerializedProperty onStay = so.FindProperty("onTriggerStay");
        AddPersistentListener(onStay, healthManager, "Heal", 15);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(triggerZone);

        return zone;
    }

    private static GameObject CreateCollectibles(Transform parent, Material collectibleMat)
    {
        GameObject collectiblesParent = new GameObject("Collectibles");
        collectiblesParent.transform.SetParent(parent);

        // Create 5 collectibles in a pattern
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-3f, 0.5f, 3f),
            new Vector3(0f, 0.5f, 5f),
            new Vector3(3f, 0.5f, 3f),
            new Vector3(-3f, 0.5f, -3f),
            new Vector3(3f, 0.5f, -3f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collectible.name = $"Collectible_{i + 1}";
            collectible.transform.SetParent(collectiblesParent.transform);
            collectible.transform.localPosition = positions[i];
            collectible.transform.localScale = Vector3.one * 0.5f;
            collectible.tag = "Collectible";

            // Apply material
            Renderer renderer = collectible.GetComponent<Renderer>();
            renderer.sharedMaterial = collectibleMat;

            // Make it a trigger
            Collider collider = collectible.GetComponent<Collider>();
            collider.isTrigger = true;

            // Add rotation animation (spin using ActionAnimateTransform)
            ActionAnimateTransform animator = collectible.AddComponent<ActionAnimateTransform>();
            SerializedObject animSo = new SerializedObject(animator);

            // Configure Y-axis rotation
            animSo.FindProperty("animateRotationY").boolValue = true;
            animSo.FindProperty("rotationYTarget").floatValue = 360f;
            animSo.FindProperty("duration").floatValue = 4f; // 90 degrees/sec = 360 degrees in 4 seconds
            animSo.FindProperty("loop").boolValue = true;
            animSo.FindProperty("playOnStart").boolValue = true;

            // Set rotation curve to linear (constant speed)
            AnimationCurve linearCurve = AnimationCurve.Linear(0, 0, 1, 1);
            animSo.FindProperty("rotationYCurve").animationCurveValue = linearCurve;

            animSo.ApplyModifiedProperties();
        }

        return collectiblesParent;
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
        renderer.sharedMaterial = ExampleMaterialHelper.CreateGrayMaterial();

        return ground;
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 12f, -12f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
        }
    }
}

// Temporary helper components for storing references during scene generation
// These are destroyed after use to avoid "Missing Script" errors in Play mode
public class UIReferences : MonoBehaviour
{
    public GameObject pausePanel;
}

public class VictoryPanelReference : MonoBehaviour
{
    public GameObject victoryPanel;
}
