using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

/// <summary>
/// Setup generator for Health Pattern: health system with damage zones and UI display
/// Creates: GameHealthManager + GameUIManager + damage zones with wired UnityEvents
/// </summary>
public class HealthPatternSetup : EditorWindow
{
    [MenuItem("Tools/Setup Patterns/Health Pattern")]
    static void ShowWindow()
    {
        GetWindow<HealthPatternSetup>("Health Pattern Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Health Pattern Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("This creates:", EditorStyles.helpBox);
        GUILayout.Label("• GameHealthManager (tracks health)");
        GUILayout.Label("• GameUIManager (displays health bar & text)");
        GUILayout.Label("• 3 damage zones (each deals 10 damage)");
        GUILayout.Label("• 1 healing zone (heals 20 health)");
        GUILayout.Label("• All UnityEvents automatically wired");
        GUILayout.Space(10);

        if (GUILayout.Button("Create Health Pattern", GUILayout.Height(40)))
        {
            CreateHealthPattern();
            Close();
        }
    }

    static void CreateHealthPattern()
    {
        // Create main container
        GameObject container = new GameObject("HealthPattern_Setup");
        Undo.RegisterCreatedObjectUndo(container, "Create Health Pattern");

        // Create GameHealthManager
        GameObject managerObj = new GameObject("GameHealthManager");
        managerObj.transform.SetParent(container.transform);
        GameHealthManager healthManager = managerObj.AddComponent<GameHealthManager>();
        Undo.RegisterCreatedObjectUndo(managerObj, "Create GameHealthManager");

        // Configure health manager
        SerializedObject healthSO = new SerializedObject(healthManager);
        healthSO.FindProperty("maxHealth").intValue = 100;
        healthSO.FindProperty("currentHealth").intValue = 100;
        healthSO.FindProperty("lowHealthThreshold").intValue = 30;
        healthSO.ApplyModifiedProperties();

        // Create GameUIManager
        GameObject uiObj = new GameObject("GameUIManager");
        uiObj.transform.SetParent(container.transform);
        GameUIManager uiManager = uiObj.AddComponent<GameUIManager>();
        Undo.RegisterCreatedObjectUndo(uiObj, "Create GameUIManager");

        // Configure UI Manager to show health
        SerializedObject uiSO = new SerializedObject(uiManager);
        uiSO.FindProperty("showScore").boolValue = false;
        uiSO.FindProperty("showHealthText").boolValue = true;
        uiSO.FindProperty("showHealthBar").boolValue = true;
        uiSO.FindProperty("showTimer").boolValue = false;
        uiSO.FindProperty("showInventory").boolValue = false;
        uiSO.FindProperty("enableEditorPreview").boolValue = true;
        uiSO.ApplyModifiedProperties();

        // Wire UnityEvent: healthManager.onHealthChanged -> uiManager.UpdateHealth
        SerializedProperty onHealthChanged = healthSO.FindProperty("onHealthChanged");
        AddPersistentListener(onHealthChanged, uiManager, "UpdateHealth");
        healthSO.ApplyModifiedProperties();

        // Create damage zones
        GameObject damageZonesParent = new GameObject("DamageZones");
        damageZonesParent.transform.SetParent(container.transform);
        Undo.RegisterCreatedObjectUndo(damageZonesParent, "Create Damage Zones Parent");

        Material damageMaterial = ExampleMaterialHelper.GetOrCreateMaterial("DamageMaterial", Color.red);

        Vector3[] damagePositions = {
            new Vector3(-3f, 0.25f, 3f),
            new Vector3(0f, 0.25f, 3f),
            new Vector3(3f, 0.25f, 3f)
        };

        for (int i = 0; i < damagePositions.Length; i++)
        {
            GameObject damageZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            damageZone.name = $"DamageZone_{i + 1}";
            damageZone.transform.SetParent(damageZonesParent.transform);
            damageZone.transform.position = damagePositions[i];
            damageZone.transform.localScale = new Vector3(2f, 0.5f, 2f);
            Undo.RegisterCreatedObjectUndo(damageZone, $"Create Damage Zone {i + 1}");

            // Apply material
            damageZone.GetComponent<Renderer>().sharedMaterial = damageMaterial;

            // Make it a trigger
            Collider col = damageZone.GetComponent<Collider>();
            col.isTrigger = true;

            // Add InputTriggerZone
            InputTriggerZone triggerZone = damageZone.AddComponent<InputTriggerZone>();
            SerializedObject triggerSO = new SerializedObject(triggerZone);
            triggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
            triggerSO.FindProperty("enableStayEvent").boolValue = true;
            triggerSO.FindProperty("stayInterval").floatValue = 0.5f; // Damage every 0.5 seconds
            triggerSO.ApplyModifiedProperties();

            // Wire trigger stay -> deal damage
            SerializedProperty onStay = triggerSO.FindProperty("onTriggerStayEvent");
            AddPersistentListenerWithInt(onStay, healthManager, "TakeDamage", 10);
            triggerSO.ApplyModifiedProperties();
        }

        // Create healing zone
        GameObject healZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        healZone.name = "HealingZone";
        healZone.transform.SetParent(container.transform);
        healZone.transform.position = new Vector3(0f, 0.25f, -3f);
        healZone.transform.localScale = new Vector3(2f, 0.25f, 2f);
        Undo.RegisterCreatedObjectUndo(healZone, "Create Healing Zone");

        Material healMaterial = ExampleMaterialHelper.GetOrCreateMaterial("HealMaterial", Color.green);
        healZone.GetComponent<Renderer>().sharedMaterial = healMaterial;

        // Make it a trigger
        Collider healCol = healZone.GetComponent<Collider>();
        healCol.isTrigger = true;

        // Add InputTriggerZone
        InputTriggerZone healTrigger = healZone.AddComponent<InputTriggerZone>();
        SerializedObject healTriggerSO = new SerializedObject(healTrigger);
        healTriggerSO.FindProperty("triggerObjectTag").stringValue = "Player";
        healTriggerSO.ApplyModifiedProperties();

        // Wire trigger enter -> heal
        SerializedProperty onEnter = healTriggerSO.FindProperty("onTriggerEnterEvent");
        AddPersistentListenerWithInt(onEnter, healthManager, "Heal", 20);
        healTriggerSO.ApplyModifiedProperties();

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

        Debug.Log("Health Pattern created! Add a Player with tag 'Player' to test damage/healing. Red zones damage, green zone heals.");
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

    // Helper to add listener with int parameter
    static void AddPersistentListenerWithInt(SerializedProperty unityEvent, Object target, string methodName, int value)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Int;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue = value;
    }
}
