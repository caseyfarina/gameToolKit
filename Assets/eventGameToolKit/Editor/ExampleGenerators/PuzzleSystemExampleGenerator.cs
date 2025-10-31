using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating PuzzleSwitch and PuzzleSwitchChecker
/// Creates a combination lock puzzle with 3 switches that must be set correctly
/// </summary>
public class PuzzleSystemExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate PuzzleSystem Example")]
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
        GameObject root = new GameObject("PuzzleSystemExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate PuzzleSystem Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject puzzleChecker = CreatePuzzleChecker(root.transform);
        GameObject switch1 = CreatePuzzleSwitch(root.transform, new Vector3(-3f, 1f, 3f), pinkMat, "Switch_1", 2);
        GameObject switch2 = CreatePuzzleSwitch(root.transform, new Vector3(0f, 1f, 3f), pinkMat, "Switch_2", 3);
        GameObject switch3 = CreatePuzzleSwitch(root.transform, new Vector3(3f, 1f, 3f), pinkMat, "Switch_3", 3);
        GameObject door = CreateDoor(root.transform, new Vector3(0f, 2f, 6f), pinkMat, puzzleChecker);
        GameObject ground = CreateGround(root.transform);

        // Wire switches to checker
        ConfigurePuzzleChecker(puzzleChecker, switch1, switch2, switch3);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 420f), "Puzzle System Demo\n\nSolve the combination lock!", 46);
        CreateAnnotation(canvas.transform, new Vector2(0f, 300f), "Solution: Switch1=State1, Switch2=State2, Switch3=State1", 28);
        CreateAnnotation(canvas.transform, new Vector2(0f, -420f), "WASD to Move | Walk into switches to change them", 32);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("PuzzleSystem example generated! Solve the puzzle to open the door!");
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
        rect.sizeDelta = new Vector2(1800f, 200f);

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
        return ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 1f, -3f), playerMat);
    }

    private static GameObject CreatePuzzleChecker(Transform parent)
    {
        GameObject checker = new GameObject("PuzzleChecker");
        checker.transform.SetParent(parent);
        checker.transform.localPosition = new Vector3(0f, 1f, 5f);

        PuzzleSwitchChecker puzzleChecker = checker.AddComponent<PuzzleSwitchChecker>();

        SerializedObject so = new SerializedObject(puzzleChecker);
        so.FindProperty("automaticChecking").boolValue = true;
        so.FindProperty("requireAllCorrect").boolValue = true;
        so.FindProperty("canBeUnsolved").boolValue = true;
        so.ApplyModifiedProperties();

        return checker;
    }

    private static void ConfigurePuzzleChecker(GameObject checker, GameObject switch1, GameObject switch2, GameObject switch3)
    {
        PuzzleSwitchChecker puzzleChecker = checker.GetComponent<PuzzleSwitchChecker>();
        SerializedObject so = new SerializedObject(puzzleChecker);

        // Configure switch targets: Solution is [1, 2, 1]
        SerializedProperty switchTargets = so.FindProperty("switchTargets");
        switchTargets.arraySize = 3;

        // Switch 1: Required state = 1
        SerializedProperty target0 = switchTargets.GetArrayElementAtIndex(0);
        target0.FindPropertyRelative("targetSwitch").objectReferenceValue = switch1.GetComponent<PuzzleSwitch>();
        target0.FindPropertyRelative("requiredState").intValue = 1;

        // Switch 2: Required state = 2
        SerializedProperty target1 = switchTargets.GetArrayElementAtIndex(1);
        target1.FindPropertyRelative("targetSwitch").objectReferenceValue = switch2.GetComponent<PuzzleSwitch>();
        target1.FindPropertyRelative("requiredState").intValue = 2;

        // Switch 3: Required state = 1
        SerializedProperty target2 = switchTargets.GetArrayElementAtIndex(2);
        target2.FindPropertyRelative("targetSwitch").objectReferenceValue = switch3.GetComponent<PuzzleSwitch>();
        target2.FindPropertyRelative("requiredState").intValue = 1;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(puzzleChecker);
    }

    private static GameObject CreatePuzzleSwitch(Transform parent, Vector3 position, Material switchMat, string switchName, int numStates)
    {
        GameObject switchObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        switchObj.name = switchName;
        switchObj.transform.SetParent(parent);
        switchObj.transform.localPosition = position;
        switchObj.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

        // Apply material
        switchObj.GetComponent<Renderer>().sharedMaterial = switchMat;

        // Make it a trigger
        CapsuleCollider col = switchObj.GetComponent<CapsuleCollider>();
        col.isTrigger = true;

        // Add PuzzleSwitch
        PuzzleSwitch puzzleSwitch = switchObj.AddComponent<PuzzleSwitch>();

        SerializedObject so = new SerializedObject(puzzleSwitch);
        so.FindProperty("switchID").stringValue = switchName;
        so.FindProperty("numberOfStates").intValue = numStates;
        so.FindProperty("currentState").intValue = 0;
        so.FindProperty("cycleStates").boolValue = true;
        so.FindProperty("canBeActivated").boolValue = true;
        so.FindProperty("requiredTag").stringValue = "Player";
        so.FindProperty("useTriggerActivation").boolValue = true;

        // Setup colors for different states
        SerializedProperty colors = so.FindProperty("stateColors");
        colors.arraySize = numStates;
        colors.GetArrayElementAtIndex(0).colorValue = Color.red;
        if (numStates > 1) colors.GetArrayElementAtIndex(1).colorValue = Color.green;
        if (numStates > 2) colors.GetArrayElementAtIndex(2).colorValue = Color.blue;
        if (numStates > 3) colors.GetArrayElementAtIndex(3).colorValue = Color.yellow;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(puzzleSwitch);

        // Add label above switch
        GameObject label = CreateSwitchLabel(switchObj.transform, switchName);

        return switchObj;
    }

    private static GameObject CreateSwitchLabel(Transform parent, string labelText)
    {
        // We can't easily create 3D text in code, so just create a marker
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Label";
        marker.transform.SetParent(parent);
        marker.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // Yellow material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.yellow;
        marker.GetComponent<Renderer>().sharedMaterial = mat;

        // Remove collider
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        return marker;
    }

    private static GameObject CreateDoor(Transform parent, Vector3 position, Material doorMat, GameObject puzzleChecker)
    {
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door";
        door.transform.SetParent(parent);
        door.transform.localPosition = position;
        door.transform.localScale = new Vector3(4f, 4f, 0.3f);

        // Apply material
        door.GetComponent<Renderer>().sharedMaterial = doorMat;

        // Wire puzzle checker to door
        PuzzleSwitchChecker checker = puzzleChecker.GetComponent<PuzzleSwitchChecker>();
        SerializedObject so = new SerializedObject(checker);

        // When solved, hide door
        SerializedProperty onSolved = so.FindProperty("onPuzzleSolved");
        AddPersistentListenerBool(onSolved, door, "SetActive", false);

        // When unsolved, show door
        SerializedProperty onUnsolved = so.FindProperty("onPuzzleUnsolved");
        AddPersistentListenerBool(onUnsolved, door, "SetActive", true);

        so.ApplyModifiedProperties();

        return door;
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
            mainCam.transform.position = new Vector3(0f, 8f, -5f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
