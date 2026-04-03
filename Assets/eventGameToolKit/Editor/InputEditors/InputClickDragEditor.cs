using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for InputClickDrag — hides snapSize when snapping is off,
/// hides limit vectors when limits are off.
/// </summary>
[CustomEditor(typeof(InputClickDrag))]
public class InputClickDragEditor : Editor
{
    private SerializedProperty dragPlaneProp;
    private SerializedProperty grabModeProp;

    private SerializedProperty snapToGridProp;
    private SerializedProperty snapSizeProp;

    private SerializedProperty useLimitsProp;
    private SerializedProperty minLimitProp;
    private SerializedProperty maxLimitProp;

    private SerializedProperty enableDampingProp;
    private SerializedProperty dampingTimeProp;

    private SerializedProperty onDragStartProp;
    private SerializedProperty onDragEndProp;
    private SerializedProperty onDraggedProp;

    private void OnEnable()
    {
        dragPlaneProp  = serializedObject.FindProperty("dragPlane");
        grabModeProp   = serializedObject.FindProperty("grabMode");

        snapToGridProp = serializedObject.FindProperty("snapToGrid");
        snapSizeProp   = serializedObject.FindProperty("snapSize");

        useLimitsProp  = serializedObject.FindProperty("useLimits");
        minLimitProp   = serializedObject.FindProperty("minLimit");
        maxLimitProp   = serializedObject.FindProperty("maxLimit");

        enableDampingProp = serializedObject.FindProperty("enableDamping");
        dampingTimeProp   = serializedObject.FindProperty("dampingTime");

        onDragStartProp = serializedObject.FindProperty("onDragStart");
        onDragEndProp   = serializedObject.FindProperty("onDragEnd");
        onDraggedProp   = serializedObject.FindProperty("onDragged");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Drag Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dragPlaneProp);
        EditorGUILayout.PropertyField(grabModeProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Snapping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(snapToGridProp);
        if (snapToGridProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(snapSizeProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useLimitsProp);
        if (useLimitsProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minLimitProp);
            EditorGUILayout.PropertyField(maxLimitProp);
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

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onDragStartProp);
        EditorGUILayout.PropertyField(onDragEndProp);
        EditorGUILayout.PropertyField(onDraggedProp);

        serializedObject.ApplyModifiedProperties();
    }
}
