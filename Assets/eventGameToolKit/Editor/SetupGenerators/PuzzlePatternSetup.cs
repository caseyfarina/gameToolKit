using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

/// <summary>
/// Setup generator for Puzzle Pattern: switches with timer and UI display
/// Creates: GameTimerManager + GameUIManager + PuzzleSwitches + victory trigger with wired UnityEvents
/// </summary>
public class PuzzlePatternSetup : EditorWindow
{
    [MenuItem("Tools/Setup Patterns/Puzzle Pattern")]
    static void ShowWindow()
    {
        GetWindow<PuzzlePatternSetup>("Puzzle Pattern Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Puzzle Pattern Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("This creates:", EditorStyles.helpBox);
        GUILayout.Label("• GameTimerManager (countdown from 60 seconds)");
        GUILayout.Label("• GameUIManager (displays timer)");
        GUILayout.Label("• 3 PuzzleSwitches (rotate to change states)");
        GUILayout.Label("• Victory trigger (activates when all switches on state 2)");
        GUILayout.Label("• All UnityEvents automatically wired");
        GUILayout.Space(10);

        if (GUILayout.Button("Create Puzzle Pattern", GUILayout.Height(40)))
        {
            CreatePuzzlePattern();
            Close();
        }
    }

    static void CreatePuzzlePattern()
    {
        // Create main container
        GameObject container = new GameObject("PuzzlePattern_Setup");
        Undo.RegisterCreatedObjectUndo(container, "Create Puzzle Pattern");

        // Create GameTimerManager
        GameObject timerObj = new GameObject("GameTimerManager");
        timerObj.transform.SetParent(container.transform);
        GameTimerManager timerManager = timerObj.AddComponent<GameTimerManager>();
        Undo.RegisterCreatedObjectUndo(timerObj, "Create GameTimerManager");

        // Configure timer (countdown from 60)
        SerializedObject timerSO = new SerializedObject(timerManager);
        timerSO.FindProperty("countUp").boolValue = false;
        timerSO.FindProperty("startTime").floatValue = 60f;
        timerSO.FindProperty("startAutomatically").boolValue = true;
        timerSO.ApplyModifiedProperties();

        // Create GameUIManager
        GameObject uiObj = new GameObject("GameUIManager");
        uiObj.transform.SetParent(container.transform);
        GameUIManager uiManager = uiObj.AddComponent<GameUIManager>();
        Undo.RegisterCreatedObjectUndo(uiObj, "Create GameUIManager");

        // Configure UI Manager to show timer
        SerializedObject uiSO = new SerializedObject(uiManager);
        uiSO.FindProperty("showScore").boolValue = false;
        uiSO.FindProperty("showHealthText").boolValue = false;
        uiSO.FindProperty("showHealthBar").boolValue = false;
        uiSO.FindProperty("showTimer").boolValue = true;
        uiSO.FindProperty("showInventory").boolValue = false;
        uiSO.FindProperty("enableEditorPreview").boolValue = true;
        uiSO.ApplyModifiedProperties();

        // Wire UnityEvent: timerManager.onTimerUpdate -> uiManager.UpdateTimer
        SerializedProperty onTimerUpdate = timerSO.FindProperty("onTimerUpdate");
        AddPersistentListener(onTimerUpdate, uiManager, "UpdateTimer");
        timerSO.ApplyModifiedProperties();

        // Create switches
        GameObject switchesParent = new GameObject("PuzzleSwitches");
        switchesParent.transform.SetParent(container.transform);
        Undo.RegisterCreatedObjectUndo(switchesParent, "Create Switches Parent");

        Material[] switchMaterials = {
            ExampleMaterialHelper.GetOrCreateMaterial("SwitchState0", Color.red),
            ExampleMaterialHelper.GetOrCreateMaterial("SwitchState1", Color.yellow),
            ExampleMaterialHelper.GetOrCreateMaterial("SwitchState2", Color.green)
        };

        Vector3[] switchPositions = {
            new Vector3(-4f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(4f, 1f, 0f)
        };

        PuzzleSwitch[] switches = new PuzzleSwitch[3];

        for (int i = 0; i < switchPositions.Length; i++)
        {
            GameObject switchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            switchObj.name = $"PuzzleSwitch_{i + 1}";
            switchObj.transform.SetParent(switchesParent.transform);
            switchObj.transform.position = switchPositions[i];
            switchObj.transform.localScale = new Vector3(1f, 2f, 1f);
            Undo.RegisterCreatedObjectUndo(switchObj, $"Create Switch {i + 1}");

            // Make it a trigger
            Collider col = switchObj.GetComponent<Collider>();
            col.isTrigger = true;

            // Add PuzzleSwitch
            PuzzleSwitch puzzleSwitch = switchObj.AddComponent<PuzzleSwitch>();
            switches[i] = puzzleSwitch;

            SerializedObject switchSO = new SerializedObject(puzzleSwitch);
            switchSO.FindProperty("switchID").stringValue = $"Switch{i + 1}";
            switchSO.FindProperty("numberOfStates").intValue = 3;
            switchSO.FindProperty("currentState").intValue = 0;
            switchSO.FindProperty("cycleStates").boolValue = true;

            // Configure materials
            SerializedProperty stateMaterials = switchSO.FindProperty("stateMaterials");
            stateMaterials.arraySize = 3;
            for (int j = 0; j < 3; j++)
            {
                stateMaterials.GetArrayElementAtIndex(j).objectReferenceValue = switchMaterials[j];
            }

            // Configure rotations (0°, 90°, 180°)
            SerializedProperty stateRotations = switchSO.FindProperty("stateRotations");
            stateRotations.arraySize = 3;
            stateRotations.GetArrayElementAtIndex(0).floatValue = 0f;
            stateRotations.GetArrayElementAtIndex(1).floatValue = 90f;
            stateRotations.GetArrayElementAtIndex(2).floatValue = 180f;

            switchSO.ApplyModifiedProperties();

            // Add InputTriggerZone for interaction
            InputTriggerZone trigger = switchObj.AddComponent<InputTriggerZone>();
            SerializedObject triggerSO = new SerializedObject(trigger);
            triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
            triggerSO.ApplyModifiedProperties();

            // Wire trigger -> nextState
            SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnterEvent");
            AddPersistentListener(onEnter, puzzleSwitch, "NextState");
            triggerSO.ApplyModifiedProperties();
        }

        // Create PuzzleSwitchChecker
        GameObject checkerObj = new GameObject("PuzzleSwitchChecker");
        checkerObj.transform.SetParent(container.transform);
        PuzzleSwitchChecker checker = checkerObj.AddComponent<PuzzleSwitchChecker>();
        Undo.RegisterCreatedObjectUndo(checkerObj, "Create PuzzleSwitchChecker");

        // Configure checker
        SerializedObject checkerSO = new SerializedObject(checker);
        SerializedProperty targetSwitches = checkerSO.FindProperty("targetSwitches");
        targetSwitches.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            targetSwitches.GetArrayElementAtIndex(i).FindPropertyRelative("puzzleSwitch").objectReferenceValue = switches[i];
            targetSwitches.GetArrayElementAtIndex(i).FindPropertyRelative("requiredState").intValue = 2; // All need state 2
        }
        checkerSO.ApplyModifiedProperties();

        // Create victory display (disabled by default)
        GameObject victoryTextObj = new GameObject("VictoryText");
        victoryTextObj.transform.SetParent(container.transform);
        Canvas victoryCanvas = victoryTextObj.AddComponent<Canvas>();
        victoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        victoryTextObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        victoryTextObj.SetActive(false);
        Undo.RegisterCreatedObjectUndo(victoryTextObj, "Create Victory Text");

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(victoryTextObj.transform);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600, 200);

        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "PUZZLE SOLVED!";
        text.fontSize = 72;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.yellow;

        // Wire checker -> stop timer and show victory
        SerializedProperty onPuzzleSolved = checkerSO.FindProperty("onPuzzleSolved");
        AddPersistentListener(onPuzzleSolved, timerManager, "StopTimer");
        AddPersistentListenerToSetActive(onPuzzleSolved, victoryTextObj, true);
        checkerSO.ApplyModifiedProperties();

        // Wire timer stop -> show message (for timeout)
        SerializedProperty onTimerStopped = timerSO.FindProperty("onTimerStopped");
        AddPersistentListenerToDebugLog(onTimerStopped, "Time's up! Puzzle failed.");
        timerSO.ApplyModifiedProperties();

        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(container.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 1f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        Material groundMaterial = ExampleMaterialHelper.GetOrCreateMaterial("GroundMaterial", new Color(0.3f, 0.3f, 0.3f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        // Select the container
        Selection.activeGameObject = container;

        Debug.Log("Puzzle Pattern created! Walk into switches (with Player tag) to rotate them. Solve the puzzle by setting all 3 switches to green (state 2) before time runs out!");
    }

    // Helper to add persistent listener
    static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName)
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

    // Helper to add SetActive listener
    static void AddPersistentListenerToSetActive(SerializedProperty unityEvent, GameObject target, bool value)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = "SetActive";
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Bool;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = value;
    }

    // Helper to add Debug.Log listener
    static void AddPersistentListenerToDebugLog(SerializedProperty unityEvent, string message)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = null;
        call.FindPropertyRelative("m_MethodName").stringValue = "Log";
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.String;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = message;
        call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Debug, UnityEngine";
    }
}
