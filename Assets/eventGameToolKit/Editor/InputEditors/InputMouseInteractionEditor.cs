using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for InputMouseInteraction that shows/hides fields based on DetectionMode and CursorAppearance.
/// </summary>
[CustomEditor(typeof(InputMouseInteraction))]
public class InputMouseInteractionEditor : Editor
{
    // Detection Settings
    private SerializedProperty detectionModeProp;
    private SerializedProperty mouseButtonProp;
    private SerializedProperty enableHoverProp;
    private SerializedProperty enableClickProp;

    // CenterScreen Settings
    private SerializedProperty targetCameraProp;
    private SerializedProperty maxRaycastDistanceProp;
    private SerializedProperty interactionLayerProp;

    // Cursor Appearance
    private SerializedProperty cursorAppearanceProp;
    private SerializedProperty customCursorTextureProp;
    private SerializedProperty cursorHotspotProp;

    // Visual Feedback
    private SerializedProperty hoverMaterialProp;
    private SerializedProperty scaleOnHoverProp;
    private SerializedProperty hoverScaleProp;

    // Scale Animation
    private SerializedProperty useScaleEasingProp;
    private SerializedProperty scaleAnimationDurationProp;
    private SerializedProperty scaleEasingCurveProp;

    // Events
    private SerializedProperty onMouseClickProp;
    private SerializedProperty onMouseDownProp;
    private SerializedProperty onMouseUpProp;
    private SerializedProperty onMouseEnterProp;
    private SerializedProperty onMouseExitProp;
    private SerializedProperty onMouseHoverProp;

    // Debug
    private SerializedProperty showDebugInfoProp;

    private void OnEnable()
    {
        detectionModeProp = serializedObject.FindProperty("detectionMode");
        mouseButtonProp = serializedObject.FindProperty("mouseButton");
        enableHoverProp = serializedObject.FindProperty("enableHover");
        enableClickProp = serializedObject.FindProperty("enableClick");

        targetCameraProp = serializedObject.FindProperty("targetCamera");
        maxRaycastDistanceProp = serializedObject.FindProperty("maxRaycastDistance");
        interactionLayerProp = serializedObject.FindProperty("interactionLayer");

        cursorAppearanceProp = serializedObject.FindProperty("cursorAppearance");
        customCursorTextureProp = serializedObject.FindProperty("customCursorTexture");
        cursorHotspotProp = serializedObject.FindProperty("cursorHotspot");

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

        // Script reference (disabled)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((InputMouseInteraction)target), typeof(InputMouseInteraction), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // --- Detection Settings ---
        EditorGUILayout.LabelField("Detection Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(detectionModeProp);
        EditorGUILayout.PropertyField(mouseButtonProp);
        EditorGUILayout.PropertyField(enableHoverProp);
        EditorGUILayout.PropertyField(enableClickProp);

        EditorGUILayout.Space();

        // --- CenterScreen Settings (conditional) ---
        if (detectionModeProp.enumValueIndex == (int)InputMouseInteraction.DetectionMode.CenterScreen)
        {
            EditorGUILayout.LabelField("CenterScreen Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetCameraProp);
            EditorGUILayout.PropertyField(maxRaycastDistanceProp);
            EditorGUILayout.PropertyField(interactionLayerProp);
            EditorGUILayout.Space();
        }

        // --- Cursor Appearance ---
        EditorGUILayout.LabelField("Cursor Appearance", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cursorAppearanceProp);

        if (cursorAppearanceProp.enumValueIndex == (int)InputMouseInteraction.CursorAppearance.Visible)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(customCursorTextureProp);

            if (customCursorTextureProp.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(cursorHotspotProp);
            }
            EditorGUI.indentLevel--;
        }

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
