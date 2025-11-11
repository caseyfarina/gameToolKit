using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

/// <summary>
/// Setup generator for Collection Pattern: collectibles that increase score with UI display
/// Creates: GameCollectionManager + GameUIManager + collectibles with wired UnityEvents
/// </summary>
public class CollectionPatternSetup : EditorWindow
{
    [MenuItem("Tools/Setup Patterns/Collection Pattern")]
    static void ShowWindow()
    {
        GetWindow<CollectionPatternSetup>("Collection Pattern Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Collection Pattern Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("This creates:", EditorStyles.helpBox);
        GUILayout.Label("• GameCollectionManager (tracks score)");
        GUILayout.Label("• GameUIManager (displays score)");
        GUILayout.Label("• 5 collectible spheres");
        GUILayout.Label("• All UnityEvents automatically wired");
        GUILayout.Space(10);

        if (GUILayout.Button("Create Collection Pattern", GUILayout.Height(40)))
        {
            CreateCollectionPattern();
            Close();
        }
    }

    static void CreateCollectionPattern()
    {
        // Create main container
        GameObject container = new GameObject("CollectionPattern_Setup");
        Undo.RegisterCreatedObjectUndo(container, "Create Collection Pattern");

        // Create GameCollectionManager
        GameObject managerObj = new GameObject("GameCollectionManager");
        managerObj.transform.SetParent(container.transform);
        GameCollectionManager collectionManager = managerObj.AddComponent<GameCollectionManager>();
        Undo.RegisterCreatedObjectUndo(managerObj, "Create GameCollectionManager");

        // Create GameUIManager
        GameObject uiObj = new GameObject("GameUIManager");
        uiObj.transform.SetParent(container.transform);
        GameUIManager uiManager = uiObj.AddComponent<GameUIManager>();
        Undo.RegisterCreatedObjectUndo(uiObj, "Create GameUIManager");

        // Configure UI Manager to only show score
        SerializedObject uiSO = new SerializedObject(uiManager);
        uiSO.FindProperty("showScore").boolValue = true;
        uiSO.FindProperty("showHealthText").boolValue = false;
        uiSO.FindProperty("showHealthBar").boolValue = false;
        uiSO.FindProperty("showTimer").boolValue = false;
        uiSO.FindProperty("showInventory").boolValue = false;
        uiSO.FindProperty("enableEditorPreview").boolValue = true;
        uiSO.ApplyModifiedProperties();

        // Wire UnityEvent: collectionManager.onValueChanged -> uiManager.UpdateScore
        SerializedObject managerSO = new SerializedObject(collectionManager);
        SerializedProperty onValueChanged = managerSO.FindProperty("onValueChanged");
        AddPersistentListener(onValueChanged, uiManager, "UpdateScore");
        managerSO.ApplyModifiedProperties();

        // Create collectibles
        GameObject collectiblesParent = new GameObject("Collectibles");
        collectiblesParent.transform.SetParent(container.transform);
        Undo.RegisterCreatedObjectUndo(collectiblesParent, "Create Collectibles Parent");

        Material collectibleMaterial = ExampleMaterialHelper.GetOrCreateMaterial("CollectibleMaterial", Color.yellow);

        for (int i = 0; i < 5; i++)
        {
            GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            collectible.name = $"Collectible_{i + 1}";
            collectible.transform.SetParent(collectiblesParent.transform);
            collectible.transform.position = new Vector3(i * 2f - 4f, 1f, 0f);
            collectible.transform.localScale = Vector3.one * 0.5f;
            Undo.RegisterCreatedObjectUndo(collectible, $"Create Collectible {i + 1}");

            // Apply material
            collectible.GetComponent<Renderer>().sharedMaterial = collectibleMaterial;

            // Make it a trigger
            Collider col = collectible.GetComponent<Collider>();
            col.isTrigger = true;

            // Add InputTriggerZone
            InputTriggerZone triggerZone = collectible.AddComponent<InputTriggerZone>();
            SerializedObject triggerSO = new SerializedObject(triggerZone);
            triggerSO.FindProperty("targetTag").stringValue = "Player";
            triggerSO.ApplyModifiedProperties();

            // Wire trigger -> increment collection
            SerializedProperty onEnter = triggerSO.FindProperty("onTriggerEnter");
            AddPersistentListener(onEnter, collectionManager, "Increment");
            triggerSO.ApplyModifiedProperties();

            // Wire trigger -> destroy self
            AddPersistentListenerToDestroy(onEnter, collectible);
            triggerSO.ApplyModifiedProperties();

            // Add rotation animation
            ActionAnimateTransform animator = collectible.AddComponent<ActionAnimateTransform>();
            SerializedObject animSO = new SerializedObject(animator);
            animSO.FindProperty("playOnStart").boolValue = true;
            animSO.FindProperty("loop").boolValue = true;

            // Configure rotation animation
            SerializedProperty curveMappings = animSO.FindProperty("curveMappings");
            curveMappings.arraySize = 1;
            SerializedProperty mapping = curveMappings.GetArrayElementAtIndex(0);
            mapping.FindPropertyRelative("property").enumValueIndex = 4; // RotationY
            mapping.FindPropertyRelative("enabled").boolValue = true;
            mapping.FindPropertyRelative("minValue").floatValue = 0f;
            mapping.FindPropertyRelative("maxValue").floatValue = 360f;

            animSO.ApplyModifiedProperties();
        }

        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(container.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        Material groundMaterial = ExampleMaterialHelper.GetOrCreateMaterial("GroundMaterial", new Color(0.3f, 0.3f, 0.3f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        // Select the container
        Selection.activeGameObject = container;

        Debug.Log("Collection Pattern created! Add a Player with tag 'Player' to collect items.");
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

    // Helper to add destroy listener
    static void AddPersistentListenerToDestroy(SerializedProperty unityEvent, GameObject target)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = "SetActive";
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Bool;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = false;
    }
}
