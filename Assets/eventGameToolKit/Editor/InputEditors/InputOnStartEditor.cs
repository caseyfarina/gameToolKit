using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for InputOnStart that explains Awake vs Start with help boxes
/// so students understand when each event fires relative to other objects.
/// </summary>
[CustomEditor(typeof(InputOnStart))]
public class InputOnStartEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script reference (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((InputOnStart)target), typeof(InputOnStart), false);
        GUI.enabled = true;

        EditorGUILayout.Space(6);

        // ── Awake ─────────────────────────────────────────────────
        EditorGUILayout.LabelField("Awake Event", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Awake() fires first — before Start() on any object in the scene. " +
            "All objects run their Awake() before any object runs its Start(). " +
            "Use this for actions that must happen as early as possible, or that " +
            "other scripts depend on being initialized first.",
            MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("onAwake"), new GUIContent("On Awake"));

        EditorGUILayout.Space(10);

        // ── Start ─────────────────────────────────────────────────
        EditorGUILayout.LabelField("Start Event", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Start() fires after all Awake() calls have finished across the whole scene. " +
            "This means every object is guaranteed to be initialized before your event fires. " +
            "Use this for most scene-start actions — it's the safest choice for triggering " +
            "gameplay, UI, audio, or anything that interacts with other components.",
            MessageType.Info);

        SerializedProperty delayProp = serializedObject.FindProperty("startDelay");
        EditorGUILayout.PropertyField(delayProp, new GUIContent("Start Delay (seconds)"));

        if (delayProp.floatValue > 0f)
        {
            EditorGUILayout.HelpBox(
                $"On Start event will fire {delayProp.floatValue:F2}s after the scene begins. " +
                "Useful for fade-ins, intro pauses, or timed reveals.",
                MessageType.None);
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("onStart"), new GUIContent("On Start"));

        serializedObject.ApplyModifiedProperties();

        // ── Quick reference ───────────────────────────────────────
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Order of execution:  Awake() → (all objects) → Start() → (optional delay) → On Start event\n\n" +
            "When in doubt, use On Start — it works correctly in almost every situation.",
            MessageType.None);
    }
}
