using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for InputClickRotate — hides snapAngle when snapping is off,
/// hides limit fields when limits are off.
/// </summary>
[CustomEditor(typeof(InputClickRotate))]
public class InputClickRotateEditor : Editor
{
    private SerializedProperty rotationAxisProp;
    private SerializedProperty mouseAxisProp;
    private SerializedProperty sensitivityProp;

    private SerializedProperty snapToAngleProp;
    private SerializedProperty snapAngleProp;

    private SerializedProperty useLimitsProp;
    private SerializedProperty limitSpaceProp;
    private SerializedProperty minAngleProp;
    private SerializedProperty maxAngleProp;

    private SerializedProperty enableDampingProp;
    private SerializedProperty dampingTimeProp;

    private SerializedProperty onRotateStartProp;
    private SerializedProperty onRotateEndProp;
    private SerializedProperty onRotatedProp;

    private void OnEnable()
    {
        rotationAxisProp = serializedObject.FindProperty("rotationAxis");
        mouseAxisProp    = serializedObject.FindProperty("mouseAxis");
        sensitivityProp  = serializedObject.FindProperty("sensitivity");

        snapToAngleProp = serializedObject.FindProperty("snapToAngle");
        snapAngleProp   = serializedObject.FindProperty("snapAngle");

        useLimitsProp  = serializedObject.FindProperty("useLimits");
        limitSpaceProp = serializedObject.FindProperty("limitSpace");
        minAngleProp   = serializedObject.FindProperty("minAngle");
        maxAngleProp   = serializedObject.FindProperty("maxAngle");

        enableDampingProp = serializedObject.FindProperty("enableDamping");
        dampingTimeProp   = serializedObject.FindProperty("dampingTime");

        onRotateStartProp = serializedObject.FindProperty("onRotateStart");
        onRotateEndProp   = serializedObject.FindProperty("onRotateEnd");
        onRotatedProp     = serializedObject.FindProperty("onRotated");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rotationAxisProp);
        EditorGUILayout.PropertyField(mouseAxisProp);
        EditorGUILayout.PropertyField(sensitivityProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Snapping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(snapToAngleProp);
        if (snapToAngleProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(snapAngleProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useLimitsProp);
        if (useLimitsProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(limitSpaceProp);
            EditorGUILayout.PropertyField(minAngleProp);
            EditorGUILayout.PropertyField(maxAngleProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Damping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableDampingProp);
        if (enableDampingProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(dampingTimeProp);
            EditorGUI.indentLevel--;
        }

        // Show current angle read-out during play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            var target = (InputClickRotate)serializedObject.targetObject;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Current Angle", target.GetCurrentAngle());
            EditorGUILayout.Toggle("Is Rotating", target.IsRotating);
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onRotateStartProp);
        EditorGUILayout.PropertyField(onRotateEndProp);
        EditorGUILayout.PropertyField(onRotatedProp);

        serializedObject.ApplyModifiedProperties();
    }
}
