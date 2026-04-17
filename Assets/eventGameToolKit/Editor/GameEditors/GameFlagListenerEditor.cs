using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameFlagListener. Warns when required fields are empty.
/// </summary>
[CustomEditor(typeof(GameFlagListener))]
public class GameFlagListenerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameFlagListener)target), typeof(GameFlagListener), false);
        GUI.enabled = true;

        // Flag source
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Flag Source", EditorStyles.boldLabel);

        SerializedProperty managerProp = serializedObject.FindProperty("flagManager");
        EditorGUILayout.PropertyField(managerProp, new GUIContent("Flag Manager"));

        SerializedProperty nameProp = serializedObject.FindProperty("flagName");
        EditorGUILayout.PropertyField(nameProp, new GUIContent("Flag Name"));

        if (managerProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign a GameFlagManager.", MessageType.Warning);
        else if (string.IsNullOrEmpty(nameProp.stringValue))
            EditorGUILayout.HelpBox("Enter the flag name to watch (must match exactly what is passed to SetFlag).", MessageType.Warning);
        else
            EditorGUILayout.HelpBox($"Watching flag: \"{nameProp.stringValue}\"", MessageType.None);

        // On scene load events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("On Scene Load", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onFlagAlreadySet"),  new GUIContent("Flag Already Set"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onFlagNotSet"),      new GUIContent("Flag Not Set"));

        // Runtime events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("At Runtime", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onFlagBecameSet"),     new GUIContent("Flag Became Set"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onFlagBecameCleared"), new GUIContent("Flag Became Cleared"));

        serializedObject.ApplyModifiedProperties();
    }
}
