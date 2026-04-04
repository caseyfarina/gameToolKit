using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameTimerManager that conditionally shows UI display fields
/// and provides an editor preview for positioning
/// </summary>
[CustomEditor(typeof(GameTimerManager))]
public class GameTimerManagerEditor : Editor
{
    private bool showPreview = false;

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        HidePreview();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            HidePreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameTimerManager)target), typeof(GameTimerManager), false);
        GUI.enabled = true;

        // Timer Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timer Settings", EditorStyles.boldLabel);

        SerializedProperty countUpProp = serializedObject.FindProperty("countUp");
        EditorGUILayout.PropertyField(countUpProp, new GUIContent("Count Up"));

        SerializedProperty startTimeProp = serializedObject.FindProperty("startTime");
        EditorGUILayout.PropertyField(startTimeProp, new GUIContent(countUpProp.boolValue ? "Start Time" : "Countdown From (seconds)"));

        if (countUpProp.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("totalTime"), new GUIContent("Total Time (seconds)", "Used as 100% reference for the bar and gradient. Set to your expected maximum time."));
        }
        else
        {
            EditorGUILayout.HelpBox("Countdown uses Start Time as the total duration for bar and gradient calculations.", MessageType.None);
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("startAutomatically"), new GUIContent("Start Automatically"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("respondToGamePause"), new GUIContent("Respond To Game Pause"));

        // Threshold Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Threshold Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("thresholds"), new GUIContent("Thresholds"), true);

        // Periodic Events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Periodic Events", EditorStyles.boldLabel);
        SerializedProperty enablePeriodicProp = serializedObject.FindProperty("enablePeriodicEvents");
        EditorGUILayout.PropertyField(enablePeriodicProp, new GUIContent("Enable Periodic Events"));
        if (enablePeriodicProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("periodicInterval"), new GUIContent("Interval (seconds)"));
            EditorGUI.indentLevel--;
        }

        // UI Text (Optional)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Text (Optional)", EditorStyles.boldLabel);

        SerializedProperty showUIProp = serializedObject.FindProperty("showUI");
        EditorGUILayout.PropertyField(showUIProp, new GUIContent("Show UI"));

        if (showUIProp.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Clock Styling", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("labelPrefix"), new GUIContent("Label Prefix"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayFormat"), new GUIContent("Display Format"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textPosition"), new GUIContent("Text Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textAlignment"), new GUIContent("Text Alignment"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Clock Color", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            SerializedProperty useGradientProp = serializedObject.FindProperty("useTextGradient");
            EditorGUILayout.PropertyField(useGradientProp, new GUIContent("Use Color Gradient"));

            if (useGradientProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textGradient"), new GUIContent("Color Gradient", "Color mapped across the timer's full duration. Left = start, Right = end/done."));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), new GUIContent("Custom Font"));
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("Enable Show UI to create a self-contained timer clock display.", MessageType.None);
        }

        // UI Bar (Optional)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Bar (Optional)", EditorStyles.boldLabel);

        SerializedProperty showBarProp = serializedObject.FindProperty("showBar");
        EditorGUILayout.PropertyField(showBarProp, new GUIContent("Show Bar"));

        if (showBarProp.boolValue)
        {
            EditorGUI.indentLevel++;

            // Warn if count-up and no total time set
            if (countUpProp.boolValue)
            {
                SerializedProperty totalTimeProp = serializedObject.FindProperty("totalTime");
                if (totalTimeProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("Set Total Time above 0 to use the bar display. The bar needs a maximum duration to calculate fill percentage.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Bar Settings", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barPosition"), new GUIContent("Bar Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barSize"), new GUIContent("Bar Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barBackgroundColor"), new GUIContent("Background Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barGradient"), new GUIContent("Fill Color Gradient", "Color mapped across the timer's full duration. Left = start, Right = end/done."));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Bar Animation", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            SerializedProperty animateBarProp = serializedObject.FindProperty("animateBar");
            EditorGUILayout.PropertyField(animateBarProp, new GUIContent("Animate Bar"));
            if (animateBarProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("barAnimationDuration"), new GUIContent("Animation Duration"));
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("Enable Show Bar to create a fill bar display of the timer's progress.", MessageType.None);
        }

        // Editor Preview
        if (showUIProp.boolValue || showBarProp.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (!showPreview)
            {
                if (GUILayout.Button("Show Canvas Preview", GUILayout.Height(30)))
                {
                    showPreview = true;
                    ShowPreview();
                }
            }
            else
            {
                if (GUILayout.Button("Hide Canvas Preview", GUILayout.Height(30)))
                {
                    showPreview = false;
                    HidePreview();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (showPreview)
            {
                EditorGUILayout.HelpBox("Preview shows at 75% time progress. Adjust settings to see changes in real-time.", MessageType.Info);
            }
        }
        else
        {
            if (showPreview)
            {
                showPreview = false;
                HidePreview();
            }
        }

        // Timer Events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timer Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTimerStarted"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTimerStopped"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTimerPaused"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTimerResumed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTimerRestarted"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onPeriodicEvent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTimerUpdate"));

        // Debug Tools
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugInfo"), new GUIContent("Show Debug Info"));

        bool changed = serializedObject.ApplyModifiedProperties();

        if (showPreview && changed)
            UpdatePreview();
    }

    private void ShowPreview()
    {
        GameTimerManager manager = (GameTimerManager)target;
        manager.CreatePreviewUI();
        EditorUtility.SetDirty(manager);
    }

    private void HidePreview()
    {
        if (target == null) return;

        GameTimerManager manager = (GameTimerManager)target;
        manager.DestroyPreviewUI();
        showPreview = false;
        EditorUtility.SetDirty(manager);
    }

    private void UpdatePreview()
    {
        GameTimerManager manager = (GameTimerManager)target;
        manager.UpdatePreviewUI();
        EditorUtility.SetDirty(manager);
    }
}
