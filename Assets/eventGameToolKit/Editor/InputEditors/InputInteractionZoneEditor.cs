using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for InputInteractionZone.
/// Hides prompt sub-fields when Show Prompt is off, and shows live zone status in play mode.
/// </summary>
[CustomEditor(typeof(InputInteractionZone))]
public class InputInteractionZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var zone = (InputInteractionZone)target;

        // Script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(zone), typeof(InputInteractionZone), false);
        GUI.enabled = true;

        // ── Zone Settings ─────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Zone Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerTag"), new GUIContent("Player Tag"));

        // ── Interact Input ────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interact Input", EditorStyles.boldLabel);

        var interactActionProp = serializedObject.FindProperty("interactAction");
        EditorGUILayout.PropertyField(interactActionProp, new GUIContent("Interact Action"));

        var fallbackKeyProp = serializedObject.FindProperty("fallbackKey");
        EditorGUILayout.PropertyField(fallbackKeyProp, new GUIContent("Fallback Key"));

        if (interactActionProp.objectReferenceValue != null)
            EditorGUILayout.HelpBox("Fallback key is ignored while an Input Action is assigned.", MessageType.None);
        else
            EditorGUILayout.HelpBox("No Input Action assigned — fallback key will be used.", MessageType.Warning);

        // ── Prompt ────────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prompt (Optional)", EditorStyles.boldLabel);

        var showPromptProp = serializedObject.FindProperty("showPrompt");
        EditorGUILayout.PropertyField(showPromptProp, new GUIContent("Show Prompt"));

        if (showPromptProp.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("promptSprite"),      new GUIContent("Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("promptOffset"),      new GUIContent("Offset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("promptSize"),        new GUIContent("Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("promptOrientation"), new GUIContent("Orientation"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Appear Animation", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("promptAnimation"),   new GUIContent("Style"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationDuration"), new GUIContent("Duration"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Hover", EditorStyles.miniBoldLabel);
            var enableHoverProp = serializedObject.FindProperty("enableHover");
            EditorGUILayout.PropertyField(enableHoverProp, new GUIContent("Enable Hover"));

            if (enableHoverProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverHeight"), new GUIContent("Height"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverSpeed"),  new GUIContent("Speed"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Glow", EditorStyles.miniBoldLabel);
            var enableGlowProp = serializedObject.FindProperty("enableGlow");
            EditorGUILayout.PropertyField(enableGlowProp, new GUIContent("Enable Glow"));

            if (enableGlowProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glowColor"),        new GUIContent("Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glowIntensity"),    new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glowRange"),        new GUIContent("Range"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glowPulseDuration"),new GUIContent("Pulse Duration"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        // ── Play-mode status ──────────────────────────────────────────────────
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            bool inZone = zone.IsPlayerInZone;
            var style = new GUIStyle(EditorStyles.label);
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = inZone ? new Color(0.3f, 0.9f, 0.3f) : Color.gray;
            EditorGUILayout.LabelField(inZone ? "● Player in zone" : "○ Player not in zone", style);

            if (GUILayout.Button("Test Interact"))
                zone.TriggerInteract();

            Repaint();
        }

        // ── Events ────────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onInteract"),    new GUIContent("On Interact"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onPlayerEnter"), new GUIContent("On Player Enter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onPlayerExit"),  new GUIContent("On Player Exit"));

        serializedObject.ApplyModifiedProperties();
    }
}
