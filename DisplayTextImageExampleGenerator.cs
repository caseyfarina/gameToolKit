using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating ActionDisplayText and ActionDisplayImage
/// Creates a dialogue sequence between two characters (blue and red noodle) with a restart button
/// </summary>
public class DisplayTextImageExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate DisplayText & DisplayImage Example")]
    public static void GenerateExample()
    {
        // Clear selection
        Selection.activeGameObject = null;

        // Load the noodle sprite from Assets
        Sprite noodleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/noodle.png");
        if (noodleSprite == null)
        {
            Debug.LogError("Could not find noodle.png in Assets folder! Please ensure the sprite exists.");
            return;
        }

        // Create root container
        GameObject root = new GameObject("DialogueExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate Dialogue Example");

        // Generate all components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject displayTextDialogue = CreateDialogueText(canvas.transform);
        GameObject leftCharacter = CreateCharacterPortrait(canvas.transform, "LeftCharacter", new Vector2(-400f, -100f), noodleSprite, new Color(0.3f, 0.6f, 1f)); // Blue
        GameObject rightCharacter = CreateCharacterPortrait(canvas.transform, "RightCharacter", new Vector2(400f, -100f), noodleSprite, new Color(1f, 0.3f, 0.3f)); // Red
        GameObject restartButton = CreateRestartButton(canvas.transform);
        GameObject sequencer = CreateSequencer(root.transform);

        // Configure sequencer timeline events
        ConfigureDialogueSequence(sequencer, displayTextDialogue, leftCharacter, rightCharacter, restartButton);

        // Wire restart button to restart sequence and hide itself
        WireRestartButton(restartButton, sequencer);

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("Dialogue example scene generated successfully! Press Play to see the dialogue sequence.");
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

        return canvasObj;
    }

    private static GameObject CreateDisplayText(Transform parent)
    {
        // Create text display object
        GameObject textObj = new GameObject("DisplayText");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1200f, 300f);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 72;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;

        // Add ActionDisplayText component
        ActionDisplayText displayAction = textObj.AddComponent<ActionDisplayText>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(displayAction);
        so.FindProperty("timeOnScreen").floatValue = 2f;
        so.FindProperty("useFading").boolValue = true;
        so.FindProperty("fadeDuration").floatValue = 0.5f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(displayAction);

        return textObj;
    }

    private static GameObject CreateDisplayImage(Transform parent)
    {
        // Create image display object
        GameObject imageObj = new GameObject("DisplayImage");
        imageObj.transform.SetParent(parent);

        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(400f, 400f);

        Image image = imageObj.AddComponent<Image>();
        image.color = Color.white;

        // Add ActionDisplayImage component
        ActionDisplayImage displayAction = imageObj.AddComponent<ActionDisplayImage>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(displayAction);
        so.FindProperty("timeOnScreen").floatValue = 2f;
        so.FindProperty("useFading").boolValue = true;
        so.FindProperty("fadeDuration").floatValue = 0.5f;
        so.FindProperty("useScaling").boolValue = true;
        so.FindProperty("startScale").vector3Value = Vector3.zero;
        so.FindProperty("targetScale").vector3Value = Vector3.one;
        so.FindProperty("scaleDuration").floatValue = 0.4f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(displayAction);

        return imageObj;
    }

    private static GameObject CreateSequencer(Transform parent)
    {
        GameObject sequencerObj = new GameObject("DisplaySequencer");
        sequencerObj.transform.SetParent(parent);

        ActionEventSequencer sequencer = sequencerObj.AddComponent<ActionEventSequencer>();

        // Use SerializedObject to set private fields
        SerializedObject so = new SerializedObject(sequencer);
        so.FindProperty("duration").floatValue = 12f;
        so.FindProperty("loop").boolValue = true;
        so.FindProperty("playOnStart").boolValue = false;

        // Initialize events array (we'll configure it later)
        SerializedProperty eventsProperty = so.FindProperty("events");
        eventsProperty.arraySize = 5;

        so.ApplyModifiedProperties();

        return sequencerObj;
    }

    private static void ConfigureSequencerEvents(GameObject sequencer, GameObject displayText, GameObject displayImage,
                                                  Sprite sprite1, Sprite sprite2, Sprite sprite3)
    {
        ActionEventSequencer seq = sequencer.GetComponent<ActionEventSequencer>();
        ActionDisplayText textAction = displayText.GetComponent<ActionDisplayText>();
        ActionDisplayImage imageAction = displayImage.GetComponent<ActionDisplayImage>();

        SerializedObject so = new SerializedObject(seq);
        SerializedProperty eventsArray = so.FindProperty("events");
        eventsArray.arraySize = 5;

        // Event 0: At 0s, display welcome text
        SerializedProperty event0 = eventsArray.GetArrayElementAtIndex(0);
        event0.FindPropertyRelative("eventName").stringValue = "Welcome Text";
        event0.FindPropertyRelative("triggerTime").floatValue = 0f;
        SerializedProperty event0Trigger = event0.FindPropertyRelative("onTrigger");
        AddPersistentListenerString(event0Trigger, textAction, "DisplayText", "Welcome to Display Actions!");

        // Event 1: At 2.5s, display blue circle image
        SerializedProperty event1 = eventsArray.GetArrayElementAtIndex(1);
        event1.FindPropertyRelative("eventName").stringValue = "Blue Circle";
        event1.FindPropertyRelative("triggerTime").floatValue = 2.5f;
        SerializedProperty event1Trigger = event1.FindPropertyRelative("onTrigger");
        AddPersistentListenerSprite(event1Trigger, imageAction, "DisplayImage", sprite1);

        // Event 2: At 5s, display text
        SerializedProperty event2 = eventsArray.GetArrayElementAtIndex(2);
        event2.FindPropertyRelative("eventName").stringValue = "Text and Images";
        event2.FindPropertyRelative("triggerTime").floatValue = 5f;
        SerializedProperty event2Trigger = event2.FindPropertyRelative("onTrigger");
        AddPersistentListenerString(event2Trigger, textAction, "DisplayText", "Text and Images!");

        // Event 3: At 7.5s, display red square image
        SerializedProperty event3 = eventsArray.GetArrayElementAtIndex(3);
        event3.FindPropertyRelative("eventName").stringValue = "Red Square";
        event3.FindPropertyRelative("triggerTime").floatValue = 7.5f;
        SerializedProperty event3Trigger = event3.FindPropertyRelative("onTrigger");
        AddPersistentListenerSprite(event3Trigger, imageAction, "DisplayImage", sprite2);

        // Event 4: At 10s, display final text
        SerializedProperty event4 = eventsArray.GetArrayElementAtIndex(4);
        event4.FindPropertyRelative("eventName").stringValue = "Final Text";
        event4.FindPropertyRelative("triggerTime").floatValue = 10f;
        SerializedProperty event4Trigger = event4.FindPropertyRelative("onTrigger");
        AddPersistentListenerString(event4Trigger, textAction, "DisplayText", "Sequence Complete!");

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

    private static void AddPersistentListenerSprite(SerializedProperty unityEvent, Object target, string methodName, Sprite spriteValue)
    {
        SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int index = calls.arraySize;
        calls.InsertArrayElementAtIndex(index);

        SerializedProperty call = calls.GetArrayElementAtIndex(index);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Object;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue = spriteValue;
        call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Sprite, UnityEngine.CoreModule";
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

    // Helper methods to create simple sprites programmatically
    private static Sprite CreateCircleSprite(Color color, string name)
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < radius)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        texture.name = name;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateSquareSprite(Color color, string name)
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Create square with some padding
                if (x > 10 && x < size - 10 && y > 10 && y < size - 10)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        texture.name = name;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateStarSprite(Color color, string name)
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Simple star approximation using diamond shape
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);

                if (dx + dy < size / 2f - 10)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        texture.name = name;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
