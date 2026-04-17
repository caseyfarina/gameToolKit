using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameFlagManager.
/// </summary>
[CustomEditor(typeof(GameFlagManager))]
public class GameFlagManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameFlagManager)target), typeof(GameFlagManager), false);
        GUI.enabled = true;

        // On Restart
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("On Restart", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onRestartBehavior"), new GUIContent("On Restart"));

        EditorGUILayout.HelpBox(
            "Call SetFlag(\"flagName\") from any UnityEvent to mark that something happened.\n" +
            "Add a GameFlagListener to objects that need to react to or restore flag state.",
            MessageType.None);

        // Events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onFlagSet"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onFlagCleared"));

        serializedObject.ApplyModifiedProperties();
    }
}
