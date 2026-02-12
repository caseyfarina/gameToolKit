using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for InputFPMouseInteraction with conditional field visibility.
/// </summary>
[CustomEditor(typeof(InputFPMouseInteraction))]
public class InputFPMouseInteractionEditor : Editor
{
    private SerializedProperty mouseButtonProp;
    private SerializedProperty enableHoverProp;
    private SerializedProperty enableClickProp;

    private SerializedProperty targetCameraProp;
    private SerializedProperty maxRaycastDistanceProp;
    private SerializedProperty interactionLayerProp;

    private SerializedProperty hoverMaterialProp;
    private SerializedProperty scaleOnHoverProp;
    private SerializedProperty hoverScaleProp;

    private SerializedProperty useScaleEasingProp;
    private SerializedProperty scaleAnimationDurationProp;
    private SerializedProperty scaleEasingCurveProp;

    private SerializedProperty onMouseClickProp;
    private SerializedProperty onMouseDownProp;
    private SerializedProperty onMouseUpProp;
    private SerializedProperty onMouseEnterProp;
    private SerializedProperty onMouseExitProp;
    private SerializedProperty onMouseHoverProp;

    private SerializedProperty showDebugInfoProp;

    private void OnEnable()
    {
        mouseButtonProp = serializedObject.FindProperty("mouseButton");
        enableHoverProp = serializedObject.FindProperty("enableHover");
        enableClickProp = serializedObject.FindProperty("enableClick");

        targetCameraProp = serializedObject.FindProperty("targetCamera");
        maxRaycastDistanceProp = serializedObject.FindProperty("maxRaycastDistance");
        interactionLayerProp = serializedObject.FindProperty("interactionLayer");

        hoverMaterialProp = serializedObject.FindProperty("hoverMaterial");
        scaleOnHoverProp = serializedObject.FindProperty("scaleOnHover");
        hoverScaleProp = serializedObject.FindProperty("hoverScale");

        useScaleEasingProp = serializedObject.FindProperty("useScaleEasing");
        scaleAnimationDurationProp = serializedObject.FindProperty("scaleAnimationDuration");
        scaleEasingCurveProp = serializedObject.FindProperty("scaleEasingCurve");

        onMouseClickProp = serializedObject.FindProperty("onMouseClick");
        onMouseDownProp = serializedObject.FindProperty("onMouseDown");
        onMouseUpProp = serializedObject.FindProperty("onMouseUp");
        onMouseEnterProp = serializedObject.FindProperty("onMouseEnter");
        onMouseExitProp = serializedObject.FindProperty("onMouseExit");
        onMouseHoverProp = serializedObject.FindProperty("onMouseHover");

        showDebugInfoProp = serializedObject.FindProperty("showDebugInfo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((InputFPMouseInteraction)target), typeof(InputFPMouseInteraction), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // --- Interaction Settings ---
        EditorGUILayout.LabelField("Interaction Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mouseButtonProp);
        EditorGUILayout.PropertyField(enableHoverProp);
        EditorGUILayout.PropertyField(enableClickProp);

        EditorGUILayout.Space();

        // --- Raycast Settings ---
        EditorGUILayout.LabelField("Raycast Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetCameraProp);
        EditorGUILayout.PropertyField(maxRaycastDistanceProp);
        EditorGUILayout.PropertyField(interactionLayerProp);

        EditorGUILayout.Space();

        // --- Visual Feedback ---
        EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hoverMaterialProp);
        EditorGUILayout.PropertyField(scaleOnHoverProp);
        if (scaleOnHoverProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(hoverScaleProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // --- Scale Animation ---
        EditorGUILayout.LabelField("Scale Animation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useScaleEasingProp);
        if (useScaleEasingProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(scaleAnimationDurationProp);
            EditorGUILayout.PropertyField(scaleEasingCurveProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // --- Click Events ---
        EditorGUILayout.LabelField("Click Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onMouseClickProp);
        EditorGUILayout.PropertyField(onMouseDownProp);
        EditorGUILayout.PropertyField(onMouseUpProp);

        EditorGUILayout.Space();

        // --- Hover Events ---
        EditorGUILayout.LabelField("Hover Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onMouseEnterProp);
        EditorGUILayout.PropertyField(onMouseExitProp);
        EditorGUILayout.PropertyField(onMouseHoverProp);

        EditorGUILayout.Space();

        // --- Debug ---
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showDebugInfoProp);

        serializedObject.ApplyModifiedProperties();
    }
}
