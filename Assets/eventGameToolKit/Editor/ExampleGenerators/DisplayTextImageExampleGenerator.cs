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
        GameObject dialogueText = CreateDialogueText(canvas.transform);
        GameObject leftCharacter = CreateCharacterPortrait(canvas.transform, "BlueNoodle", new Vector2(-400f, -100f), noodleSprite, new Color(0.3f, 0.6f, 1f));
        GameObject rightCharacter = CreateCharacterPortrait(canvas.transform, "RedNoodle", new Vector2(400f, -100f), noodleSprite, new Color(1f, 0.3f, 0.3f));
        GameObject restartButton = CreateRestartButton(canvas.transform);
        GameObject sequencer = CreateSequencer(root.transform);

        // Configure the dialogue sequence
        ConfigureDialogueSequence(sequencer, dialogueText, leftCharacter, rightCharacter, restartButton);

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

    private static GameObject CreateDialogueText(Transform parent)
    {
        // Create dialogue text box
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.2f);
        rect.anchorMax = new Vector2(0.5f, 0.2f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1200f, 200f);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        // Add ActionDisplayText component
        ActionDisplayText displayAction = textObj.AddComponent<ActionDisplayText>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(displayAction);
        so.FindProperty("timeOnScreen").floatValue = 2.5f;
        so.FindProperty("useFading").boolValue = true;
        so.FindProperty("fadeDuration").floatValue = 0.5f;
        so.FindProperty("useTypewriter").boolValue = true;
        so.FindProperty("charactersPerSecond").floatValue = 30f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(displayAction);

        return textObj;
    }

    private static GameObject CreateCharacterPortrait(Transform parent, string name, Vector2 position, Sprite sprite, Color tint)
    {
        // Create character portrait
        GameObject portraitObj = new GameObject(name);
        portraitObj.transform.SetParent(parent);

        RectTransform rect = portraitObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 300f);

        Image image = portraitObj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;

        // Add ActionDisplayImage component
        ActionDisplayImage displayAction = portraitObj.AddComponent<ActionDisplayImage>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(displayAction);
        so.FindProperty("timeOnScreen").floatValue = 3f;
        so.FindProperty("useFading").boolValue = true;
        so.FindProperty("fadeDuration").floatValue = 0.5f;
        so.FindProperty("useScaling").boolValue = true;
        so.FindProperty("startScale").vector3Value = new Vector3(0.5f, 0.5f, 1f);
        so.FindProperty("targetScale").vector3Value = Vector3.one;
        so.FindProperty("scaleDuration").floatValue = 0.5f;
        so.FindProperty("defaultImage").objectReferenceValue = sprite;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(displayAction);

        return portraitObj;
    }

    private static GameObject CreateRestartButton(Transform parent)
    {
        // Create restart button
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(parent);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(350f, 80f);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.7f, 0.4f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        // Create Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Restart Dialogue";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        // Start disabled
        buttonObj.SetActive(false);

        return buttonObj;
    }

    private static GameObject CreateSequencer(Transform parent)
    {
        GameObject sequencerObj = new GameObject("DialogueSequencer");
        sequencerObj.transform.SetParent(parent);

        ActionEventSequencer sequencer = sequencerObj.AddComponent<ActionEventSequencer>();

        // Use SerializedObject to set private fields
        SerializedObject so = new SerializedObject(sequencer);
        so.FindProperty("duration").floatValue = 8f;
        so.FindProperty("loop").boolValue = false;
        so.FindProperty("playOnStart").boolValue = true;

        // Initialize events array
        SerializedProperty eventsProperty = so.FindProperty("events");
        eventsProperty.arraySize = 5;

        so.ApplyModifiedProperties();

        return sequencerObj;
    }

    private static void ConfigureDialogueSequence(GameObject sequencer, GameObject dialogueText, GameObject leftCharacter, GameObject rightCharacter, GameObject restartButton)
    {
        ActionEventSequencer seq = sequencer.GetComponent<ActionEventSequencer>();
        ActionDisplayText textAction = dialogueText.GetComponent<ActionDisplayText>();
        ActionDisplayImage leftImage = leftCharacter.GetComponent<ActionDisplayImage>();
        ActionDisplayImage rightImage = rightCharacter.GetComponent<ActionDisplayImage>();

        SerializedObject so = new SerializedObject(seq);
        SerializedProperty eventsArray = so.FindProperty("events");
        eventsArray.arraySize = 5;

        // Event 0: At 1s, blue noodle appears and says their line
        SerializedProperty event0 = eventsArray.GetArrayElementAtIndex(0);
        event0.FindPropertyRelative("eventName").stringValue = "Blue Noodle Speaks";
        event0.FindPropertyRelative("triggerTime").floatValue = 1f;
        SerializedProperty event0Trigger = event0.FindPropertyRelative("onTrigger");
        AddPersistentListener(event0Trigger, leftImage, "DisplayDefaultImage");
        AddPersistentListenerString(event0Trigger, textAction, "DisplayText", "Hey, how are you doing?");

        // Event 1: At 4s, red noodle appears and responds
        SerializedProperty event1 = eventsArray.GetArrayElementAtIndex(1);
        event1.FindPropertyRelative("eventName").stringValue = "Red Noodle Responds";
        event1.FindPropertyRelative("triggerTime").floatValue = 4f;
        SerializedProperty event1Trigger = event1.FindPropertyRelative("onTrigger");
        AddPersistentListener(event1Trigger, rightImage, "DisplayDefaultImage");
        AddPersistentListenerString(event1Trigger, textAction, "DisplayText", "I'm doing well, thanks for asking!");

        // Event 2: At 6s, restart button appears
        SerializedProperty event2 = eventsArray.GetArrayElementAtIndex(2);
        event2.FindPropertyRelative("eventName").stringValue = "Show Restart Button";
        event2.FindPropertyRelative("triggerTime").floatValue = 6f;
        SerializedProperty event2Trigger = event2.FindPropertyRelative("onTrigger");
        AddPersistentListenerBool(event2Trigger, restartButton, "SetActive", true);

        // Event 3: At 0s (on restart), hide characters
        SerializedProperty event3 = eventsArray.GetArrayElementAtIndex(3);
        event3.FindPropertyRelative("eventName").stringValue = "Hide Left Character";
        event3.FindPropertyRelative("triggerTime").floatValue = 0f;
        SerializedProperty event3Trigger = event3.FindPropertyRelative("onTrigger");
        AddPersistentListener(event3Trigger, leftImage, "HideImage");

        // Event 4: At 0s (on restart), hide right character
        SerializedProperty event4 = eventsArray.GetArrayElementAtIndex(4);
        event4.FindPropertyRelative("eventName").stringValue = "Hide Right Character";
        event4.FindPropertyRelative("triggerTime").floatValue = 0f;
        SerializedProperty event4Trigger = event4.FindPropertyRelative("onTrigger");
        AddPersistentListener(event4Trigger, rightImage, "HideImage");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(seq);
    }

    private static void WireRestartButton(GameObject button, GameObject sequencer)
    {
        Button buttonComponent = button.GetComponent<Button>();
        ActionEventSequencer seq = sequencer.GetComponent<ActionEventSequencer>();

        SerializedObject so = new SerializedObject(buttonComponent);
        SerializedProperty onClickProperty = so.FindProperty("m_OnClick");

        // Restart sequence
        AddPersistentListener(onClickProperty, seq, "RestartSequence");
        // Hide button
        AddPersistentListenerBool(onClickProperty, button, "SetActive", false);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(buttonComponent);
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
}
