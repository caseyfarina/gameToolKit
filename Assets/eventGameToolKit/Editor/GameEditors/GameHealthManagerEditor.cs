using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameHealthManager that conditionally shows UI display fields
/// and provides an editor preview for positioning
/// </summary>
[CustomEditor(typeof(GameHealthManager))]
public class GameHealthManagerEditor : Editor
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
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameHealthManager)target), typeof(GameHealthManager), false);
        GUI.enabled = true;

        // Scene Persistence
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Persistence", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("persistAcrossScenes"), new GUIContent("Persist Across Scenes"));

        // Health Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Health Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHealth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currentHealth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lowHealthThreshold"));

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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showMaxInText"), new GUIContent("Show Max In Text"));
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

            GameHealthManager.ValueAnimation animType = (GameHealthManager.ValueAnimation)animProp.enumValueIndex;

            if (animType != GameHealthManager.ValueAnimation.None)
            {
                if (animType == GameHealthManager.ValueAnimation.PunchScale)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationDuration"), new GUIContent("Punch Duration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationStrength"), new GUIContent("Punch Strength"));
                }
                else if (animType == GameHealthManager.ValueAnimation.FadeFlash)
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
            EditorGUILayout.HelpBox("Enable Show UI to create a self-contained text display for health.", MessageType.None);
        }

        // UI Bar (Optional)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Bar (Optional)", EditorStyles.boldLabel);

        SerializedProperty showBarProp = serializedObject.FindProperty("showBar");
        EditorGUILayout.PropertyField(showBarProp);

        if (showBarProp.boolValue)
        {
            EditorGUI.indentLevel++;

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
            EditorGUILayout.HelpBox("Enable Show Bar to create a health bar display.", MessageType.None);
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
        EditorGUILayout.LabelField("Health Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onHealthChanged"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDamageReceived"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onHealthGained"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onLowHealthReached"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onLowHealthRecovered"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDeath"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onRevived"));

        bool changed = serializedObject.ApplyModifiedProperties();

        // Update preview if showing and values changed
        if (showPreview && changed)
        {
            UpdatePreview();
        }
    }

    private void ShowPreview()
    {
        GameHealthManager manager = (GameHealthManager)target;
        manager.CreatePreviewUI();
        EditorUtility.SetDirty(manager);
    }

    private void HidePreview()
    {
        if (target == null) return;

        GameHealthManager manager = (GameHealthManager)target;
        manager.DestroyPreviewUI();
        showPreview = false;
        EditorUtility.SetDirty(manager);
    }

    private void UpdatePreview()
    {
        GameHealthManager manager = (GameHealthManager)target;
        manager.UpdatePreviewUI();
        EditorUtility.SetDirty(manager);
    }
}
