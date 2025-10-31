using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for ActionDecalSequence that provides a better Inspector experience.
/// Shows total sequence duration and provides playback controls in edit mode.
/// </summary>
[CustomEditor(typeof(ActionDecalSequence))]
public class ActionDecalSequenceEditor : Editor
{
    private SerializedProperty materialFramesProp;
    private SerializedProperty playOnStartProp;
    private SerializedProperty loopProp;
    private SerializedProperty playbackSpeedProp;
    private SerializedProperty onSequenceStartProp;
    private SerializedProperty onSequenceCompleteProp;
    private SerializedProperty onSequencePauseProp;
    private SerializedProperty onSequenceResumeProp;
    private SerializedProperty onSequenceStopProp;
    private SerializedProperty onFrameChangedProp;

    private void OnEnable()
    {
        // Cache serialized properties
        materialFramesProp = serializedObject.FindProperty("materialFrames");
        playOnStartProp = serializedObject.FindProperty("playOnStart");
        loopProp = serializedObject.FindProperty("loop");
        playbackSpeedProp = serializedObject.FindProperty("playbackSpeed");
        onSequenceStartProp = serializedObject.FindProperty("onSequenceStart");
        onSequenceCompleteProp = serializedObject.FindProperty("onSequenceComplete");
        onSequencePauseProp = serializedObject.FindProperty("onSequencePause");
        onSequenceResumeProp = serializedObject.FindProperty("onSequenceResume");
        onSequenceStopProp = serializedObject.FindProperty("onSequenceStop");
        onFrameChangedProp = serializedObject.FindProperty("onFrameChanged");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionDecalSequence)target), typeof(ActionDecalSequence), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Material Sequence section
        EditorGUILayout.LabelField("Material Sequence", EditorStyles.boldLabel);

        // Show sequence stats
        int frameCount = materialFramesProp.arraySize;
        float totalDuration = CalculateTotalDuration();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Total Frames: {frameCount}");
        EditorGUILayout.LabelField($"Total Duration: {totalDuration:F2} seconds");
        if (playbackSpeedProp.floatValue != 1.0f)
        {
            float adjustedDuration = totalDuration / playbackSpeedProp.floatValue;
            EditorGUILayout.LabelField($"Adjusted Duration: {adjustedDuration:F2} seconds (at {playbackSpeedProp.floatValue}x speed)");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Draw material frames array
        EditorGUILayout.PropertyField(materialFramesProp, new GUIContent("Material Frames"), true);

        EditorGUILayout.Space();

        // Playback Settings section
        EditorGUILayout.LabelField("Playback Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playOnStartProp);
        EditorGUILayout.PropertyField(loopProp);
        EditorGUILayout.PropertyField(playbackSpeedProp);

        EditorGUILayout.Space();

        // Runtime controls (only show in play mode)
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);

            ActionDecalSequence sequence = (ActionDecalSequence)target;

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !sequence.IsPlaying;
            if (GUILayout.Button("Play", GUILayout.Height(30)))
            {
                sequence.Play();
            }
            GUI.enabled = true;

            GUI.enabled = sequence.IsPlaying && !sequence.IsPaused;
            if (GUILayout.Button("Pause", GUILayout.Height(30)))
            {
                sequence.Pause();
            }
            GUI.enabled = true;

            GUI.enabled = sequence.IsPlaying && sequence.IsPaused;
            if (GUILayout.Button("Resume", GUILayout.Height(30)))
            {
                sequence.Resume();
            }
            GUI.enabled = true;

            GUI.enabled = sequence.IsPlaying;
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                sequence.Stop();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Show current state
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Status: {(sequence.IsPlaying ? (sequence.IsPaused ? "Paused" : "Playing") : "Stopped")}");
            EditorGUILayout.LabelField($"Current Frame: {sequence.CurrentFrameIndex + 1} / {sequence.TotalFrames}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        // Sequence Events section
        EditorGUILayout.LabelField("Sequence Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onSequenceStartProp);
        EditorGUILayout.PropertyField(onSequenceCompleteProp);
        EditorGUILayout.PropertyField(onSequencePauseProp);
        EditorGUILayout.PropertyField(onSequenceResumeProp);
        EditorGUILayout.PropertyField(onSequenceStopProp);
        EditorGUILayout.PropertyField(onFrameChangedProp);

        serializedObject.ApplyModifiedProperties();

        // Repaint in play mode to update UI
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private float CalculateTotalDuration()
    {
        float total = 0f;

        for (int i = 0; i < materialFramesProp.arraySize; i++)
        {
            SerializedProperty frameProp = materialFramesProp.GetArrayElementAtIndex(i);
            SerializedProperty durationProp = frameProp.FindPropertyRelative("duration");
            total += durationProp.floatValue;
        }

        return total;
    }
}
