using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for ActionRandomMotion — shows range fields inline with axis toggles,
/// and adds play/pause/stop/return controls in play mode.
/// </summary>
[CustomEditor(typeof(ActionRandomMotion))]
public class ActionRandomMotionEditor : Editor
{
    private SerializedProperty moveXProp, rangeXProp;
    private SerializedProperty moveYProp, rangeYProp;
    private SerializedProperty moveZProp, rangeZProp;

    private SerializedProperty minDurationProp, maxDurationProp;
    private SerializedProperty minPauseProp, maxPauseProp;

    private SerializedProperty easeTypeProp;
    private SerializedProperty motionSpaceProp;
    private SerializedProperty playOnStartProp;
    private SerializedProperty returnDurationProp;

    private SerializedProperty onMotionStartProp, onMotionStopProp;
    private SerializedProperty onMoveStartProp, onMoveCompleteProp;

    private void OnEnable()
    {
        moveXProp  = serializedObject.FindProperty("moveX");
        rangeXProp = serializedObject.FindProperty("rangeX");
        moveYProp  = serializedObject.FindProperty("moveY");
        rangeYProp = serializedObject.FindProperty("rangeY");
        moveZProp  = serializedObject.FindProperty("moveZ");
        rangeZProp = serializedObject.FindProperty("rangeZ");

        minDurationProp = serializedObject.FindProperty("minDuration");
        maxDurationProp = serializedObject.FindProperty("maxDuration");
        minPauseProp    = serializedObject.FindProperty("minPause");
        maxPauseProp    = serializedObject.FindProperty("maxPause");

        easeTypeProp       = serializedObject.FindProperty("easeType");
        motionSpaceProp    = serializedObject.FindProperty("motionSpace");
        playOnStartProp    = serializedObject.FindProperty("playOnStart");
        returnDurationProp = serializedObject.FindProperty("returnDuration");

        onMotionStartProp  = serializedObject.FindProperty("onMotionStart");
        onMotionStopProp   = serializedObject.FindProperty("onMotionStop");
        onMoveStartProp    = serializedObject.FindProperty("onMoveStart");
        onMoveCompleteProp = serializedObject.FindProperty("onMoveComplete");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Axes — toggle and range on the same row
        EditorGUILayout.LabelField("Axes", EditorStyles.boldLabel);
        DrawAxisRow(moveXProp, rangeXProp, "X");
        DrawAxisRow(moveYProp, rangeYProp, "Y");
        DrawAxisRow(moveZProp, rangeZProp, "Z");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(minDurationProp, new GUIContent("Min Duration"));
        EditorGUILayout.PropertyField(maxDurationProp, new GUIContent("Max Duration"));
        EditorGUILayout.PropertyField(minPauseProp,    new GUIContent("Min Pause"));
        EditorGUILayout.PropertyField(maxPauseProp,    new GUIContent("Max Pause"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Easing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(easeTypeProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Space & Playback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(motionSpaceProp);
        EditorGUILayout.PropertyField(playOnStartProp);
        EditorGUILayout.PropertyField(returnDurationProp);

        // Play-mode controls
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            var t = (ActionRandomMotion)serializedObject.targetObject;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Is Playing", t.IsPlaying);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(t.IsPlaying ? "Pause" : "Play"))
                t.TogglePlayPause();
            if (GUILayout.Button("Stop"))
                t.Stop();
            if (GUILayout.Button("Return to Rest"))
                t.ReturnToRestPosition();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onMotionStartProp);
        EditorGUILayout.PropertyField(onMotionStopProp);
        EditorGUILayout.PropertyField(onMoveStartProp);
        EditorGUILayout.PropertyField(onMoveCompleteProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAxisRow(SerializedProperty toggleProp, SerializedProperty rangeProp, string axis)
    {
        EditorGUILayout.BeginHorizontal();
        // Fixed-width toggle label so all three rows align
        EditorGUILayout.PropertyField(toggleProp, new GUIContent($"Move {axis}"),
            GUILayout.Width(EditorGUIUtility.labelWidth + 18));
        if (toggleProp.boolValue)
            EditorGUILayout.PropertyField(rangeProp, new GUIContent($"Range {axis}"));
        EditorGUILayout.EndHorizontal();
    }
}
