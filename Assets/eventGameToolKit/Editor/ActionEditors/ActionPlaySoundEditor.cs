using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

/// <summary>
/// Custom Inspector for ActionPlaySound that clamps min/max ranges visually
/// and provides a Play-mode test button.
/// </summary>
[CustomEditor(typeof(ActionPlaySound))]
public class ActionPlaySoundEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script reference (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionPlaySound)target), typeof(ActionPlaySound), false);
        GUI.enabled = true;

        EditorGUILayout.Space(6);

        // ── Audio Clips ───────────────────────────────────────────
        EditorGUILayout.LabelField("Audio Clips", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("audioClips"), new GUIContent("Clips"), true);

        EditorGUILayout.Space(6);

        // ── Mixer ─────────────────────────────────────────────────
        EditorGUILayout.LabelField("Mixer", EditorStyles.boldLabel);
        SerializedProperty mixerProp = serializedObject.FindProperty("outputMixerGroup");
        EditorGUILayout.PropertyField(mixerProp, new GUIContent("Output Mixer Group"));
        if (mixerProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("No mixer group assigned — audio will play through the default output. Assign a group (e.g. SFX) to control this sound via an Audio Mixer.", MessageType.None);
        else
            EditorGUILayout.HelpBox($"Routing through: {((AudioMixerGroup)mixerProp.objectReferenceValue).name}", MessageType.None);

        EditorGUILayout.Space(6);

        // ── Volume ────────────────────────────────────────────────
        EditorGUILayout.LabelField("Volume", EditorStyles.boldLabel);

        SerializedProperty volMin = serializedObject.FindProperty("volumeMin");
        SerializedProperty volMax = serializedObject.FindProperty("volumeMax");

        DrawMinMaxRow("Min", volMin, "Max", volMax, 0f, 1f);

        if (Mathf.Approximately(volMin.floatValue, volMax.floatValue))
            EditorGUILayout.HelpBox($"Fixed volume: {volMin.floatValue:F2}", MessageType.None);
        else
            EditorGUILayout.HelpBox($"Random volume each play: {volMin.floatValue:F2} – {volMax.floatValue:F2}", MessageType.None);

        EditorGUILayout.Space(6);

        // ── Pitch ─────────────────────────────────────────────────
        EditorGUILayout.LabelField("Pitch", EditorStyles.boldLabel);

        SerializedProperty pitchMin = serializedObject.FindProperty("pitchMin");
        SerializedProperty pitchMax = serializedObject.FindProperty("pitchMax");

        DrawMinMaxRow("Min", pitchMin, "Max", pitchMax, 0.1f, 3f);

        if (Mathf.Approximately(pitchMin.floatValue, pitchMax.floatValue))
        {
            string label = Mathf.Approximately(pitchMin.floatValue, 1f)
                ? "No pitch variation (normal speed)"
                : $"Fixed pitch: {pitchMin.floatValue:F2}×";
            EditorGUILayout.HelpBox(label, MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Random pitch each play: {pitchMin.floatValue:F2}× – {pitchMax.floatValue:F2}×\n" +
                "Tip: 0.9 – 1.1 gives subtle natural variation. 0.5 – 1.5 gives dramatic range.",
                MessageType.None);
        }

        EditorGUILayout.Space(6);

        // ── Events ────────────────────────────────────────────────
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onPlay"), new GUIContent("On Play"));

        serializedObject.ApplyModifiedProperties();

        // ── Test ──────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Test", EditorStyles.boldLabel);

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("▶ Play Sound", GUILayout.Height(28)))
            ((ActionPlaySound)target).Play();
        GUI.enabled = true;

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play mode to use the Play button.", MessageType.Info);
    }

    // Draws two labeled float fields side by side and enforces min <= max.
    private void DrawMinMaxRow(string minLabel, SerializedProperty minProp,
                                string maxLabel, SerializedProperty maxProp,
                                float absMin, float absMax)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel(" ");

        EditorGUILayout.LabelField(minLabel, GUILayout.Width(28));
        minProp.floatValue = Mathf.Clamp(
            EditorGUILayout.FloatField(minProp.floatValue, GUILayout.Width(48)),
            absMin, absMax);

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(maxLabel, GUILayout.Width(28));
        maxProp.floatValue = Mathf.Clamp(
            EditorGUILayout.FloatField(maxProp.floatValue, GUILayout.Width(48)),
            absMin, absMax);

        EditorGUILayout.EndHorizontal();

        // Enforce min <= max
        if (minProp.floatValue > maxProp.floatValue)
            maxProp.floatValue = minProp.floatValue;
    }
}
