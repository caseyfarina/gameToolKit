using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameCollectionManager that conditionally shows UI display fields
/// and provides an editor preview for positioning
/// </summary>
[CustomEditor(typeof(GameCollectionManager))]
public class GameCollectionManagerEditor : Editor
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
        {
            HidePreview();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameCollectionManager)target), typeof(GameCollectionManager), false);
        GUI.enabled = true;

        // Scene Persistence
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Persistence", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("persistAcrossScenes"), new GUIContent("Persist Across Scenes"));

        // Value Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Value Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currentValue"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minValue"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxValue"));

        // Thresholds
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Thresholds", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("thresholds"));

        // UI Text (Optional)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Text (Optional)", EditorStyles.boldLabel);

        SerializedProperty showUIProp = serializedObject.FindProperty("showUI");
        EditorGUILayout.PropertyField(showUIProp);

        if (showUIProp.boolValue)
        {
            EditorGUI.indentLevel++;

            // Text Styling
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Text Styling", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("labelPrefix"), new GUIContent("Label Prefix"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textPosition"), new GUIContent("Text Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textAlignment"), new GUIContent("Text Alignment"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), new GUIContent("Custom Font"));
            EditorGUI.indentLevel--;

            // Text Animation
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Text Animation", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            SerializedProperty animProp = serializedObject.FindProperty("valueAnimation");
            EditorGUILayout.PropertyField(animProp, new GUIContent("Animation"));

            GameCollectionManager.ValueAnimation animType = (GameCollectionManager.ValueAnimation)animProp.enumValueIndex;

            if (animType != GameCollectionManager.ValueAnimation.None)
            {
                if (animType == GameCollectionManager.ValueAnimation.PunchScale)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationDuration"), new GUIContent("Punch Duration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationStrength"), new GUIContent("Punch Strength"));
                }
                else if (animType == GameCollectionManager.ValueAnimation.FadeFlash)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationDuration"), new GUIContent("Flash Duration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationStrength"), new GUIContent("Flash Strength"));
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("Enable Show UI to create a self-contained text display for this value.", MessageType.None);
        }

        // UI Bar (Optional)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Bar (Optional)", EditorStyles.boldLabel);

        SerializedProperty showBarProp = serializedObject.FindProperty("showBar");
        EditorGUILayout.PropertyField(showBarProp);

        if (showBarProp.boolValue)
        {
            EditorGUI.indentLevel++;

            // Check maxValue
            SerializedProperty maxValueProp = serializedObject.FindProperty("maxValue");
            if (maxValueProp.intValue <= 0)
            {
                EditorGUILayout.HelpBox("Set Max Value above 0 to use the bar display. The bar needs a maximum to calculate fill percentage.", MessageType.Warning);
            }

            // Bar Settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Bar Settings", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barPosition"), new GUIContent("Bar Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barSize"), new GUIContent("Bar Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barBackgroundColor"), new GUIContent("Background Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barGradient"), new GUIContent("Fill Color Gradient"));
            EditorGUI.indentLevel--;

            // Bar Animation
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
            EditorGUILayout.HelpBox("Enable Show Bar to create a fill bar display. Requires Max Value > 0.", MessageType.None);
        }

        // Editor Preview (show if either UI is enabled)
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
                EditorGUILayout.HelpBox("Preview is visible in the Game view. Adjust styling settings to see changes in real-time.", MessageType.Info);
            }
        }
        else
        {
            // Clean up preview if both UI options are disabled
            if (showPreview)
            {
                showPreview = false;
                HidePreview();
            }
        }

        // Events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onValueChanged"));

        // Limit Events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Limit Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onMaxReached"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onMinReached"));

        bool changed = serializedObject.ApplyModifiedProperties();

        // Update preview if showing and values changed
        if (showPreview && changed)
        {
            UpdatePreview();
        }
    }

    private void ShowPreview()
    {
        GameCollectionManager manager = (GameCollectionManager)target;
        manager.CreatePreviewUI();
        EditorUtility.SetDirty(manager);
    }

    private void HidePreview()
    {
        if (target == null) return;

        GameCollectionManager manager = (GameCollectionManager)target;
        manager.DestroyPreviewUI();
        showPreview = false;
        EditorUtility.SetDirty(manager);
    }

    private void UpdatePreview()
    {
        GameCollectionManager manager = (GameCollectionManager)target;
        manager.UpdatePreviewUI();
        EditorUtility.SetDirty(manager);
    }
}
