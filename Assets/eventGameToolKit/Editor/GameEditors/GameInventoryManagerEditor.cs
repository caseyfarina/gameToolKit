using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameInventoryManager that conditionally shows UI card settings
/// and provides an editor preview for positioning
/// </summary>
[CustomEditor(typeof(GameInventoryManager))]
public class GameInventoryManagerEditor : Editor
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
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameInventoryManager)target), typeof(GameInventoryManager), false);
        GUI.enabled = true;

        // Scene Persistence
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Persistence", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("persistAcrossScenes"), new GUIContent("Persist Across Scenes"));

        // Inventory Slots
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory Slots", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("slots"), true);

        // UI Cards (Optional)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Cards (Optional)", EditorStyles.boldLabel);

        SerializedProperty showUIProp = serializedObject.FindProperty("showUI");
        EditorGUILayout.PropertyField(showUIProp);

        if (showUIProp.boolValue)
        {
            EditorGUI.indentLevel++;

            SerializedProperty showCountProp = serializedObject.FindProperty("showCount");
            EditorGUILayout.PropertyField(showCountProp, new GUIContent("Show Count"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Layout", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uiPosition"), new GUIContent("Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cardSize"), new GUIContent("Card Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cardSpacing"), new GUIContent("Card Spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cardBackgroundColor"), new GUIContent("Card Color"));
            EditorGUI.indentLevel--;

            if (showCountProp.boolValue)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Count Text", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("Font Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), new GUIContent("Custom Font"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Preview
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
                EditorGUILayout.HelpBox("Preview is visible in the Game view. Adjust layout settings to see changes.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Enable Show UI to create a self-contained card row showing each slot's icon and count.", MessageType.None);

            if (showPreview)
            {
                showPreview = false;
                HidePreview();
            }
        }

        bool changed = serializedObject.ApplyModifiedProperties();

        if (showPreview && changed)
            UpdatePreview();
    }

    private void ShowPreview()
    {
        GameInventoryManager manager = (GameInventoryManager)target;
        manager.CreatePreviewUI();
        EditorUtility.SetDirty(manager);
    }

    private void HidePreview()
    {
        if (target == null) return;
        GameInventoryManager manager = (GameInventoryManager)target;
        manager.DestroyPreviewUI();
        showPreview = false;
        EditorUtility.SetDirty(manager);
    }

    private void UpdatePreview()
    {
        GameInventoryManager manager = (GameInventoryManager)target;
        manager.UpdatePreviewUI();
        EditorUtility.SetDirty(manager);
    }
}
