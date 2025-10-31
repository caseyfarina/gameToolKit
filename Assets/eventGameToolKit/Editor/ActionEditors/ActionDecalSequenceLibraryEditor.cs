using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for ActionDecalSequenceLibrary that provides a better Inspector experience.
/// Shows library stats and provides playback controls in play mode.
/// </summary>
[CustomEditor(typeof(ActionDecalSequenceLibrary))]
public class ActionDecalSequenceLibraryEditor : Editor
{
    private SerializedProperty sequencesProp;
    private SerializedProperty defaultSequenceIndexProp;
    private SerializedProperty playOnStartProp;
    private SerializedProperty onSequenceChangedProp;
    private SerializedProperty onLibraryStoppedProp;

    private void OnEnable()
    {
        // Cache serialized properties
        sequencesProp = serializedObject.FindProperty("sequences");
        defaultSequenceIndexProp = serializedObject.FindProperty("defaultSequenceIndex");
        playOnStartProp = serializedObject.FindProperty("playOnStart");
        onSequenceChangedProp = serializedObject.FindProperty("onSequenceChanged");
        onLibraryStoppedProp = serializedObject.FindProperty("onLibraryStopped");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionDecalSequenceLibrary)target), typeof(ActionDecalSequenceLibrary), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Sequence Library section
        EditorGUILayout.LabelField("Sequence Library", EditorStyles.boldLabel);

        // Show library stats
        int sequenceCount = sequencesProp.arraySize;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Total Sequences: {sequenceCount}");

        // Show sequence names
        if (sequenceCount > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Sequences:", EditorStyles.miniBoldLabel);
            for (int i = 0; i < sequenceCount; i++)
            {
                SerializedProperty seqProp = sequencesProp.GetArrayElementAtIndex(i);
                ActionDecalSequence seq = seqProp.objectReferenceValue as ActionDecalSequence;

                string seqName = seq != null ? seq.gameObject.name : "<null>";
                string status = "";

                if (Application.isPlaying && seq != null)
                {
                    ActionDecalSequenceLibrary library = (ActionDecalSequenceLibrary)target;
                    if (library.CurrentSequenceIndex == i)
                    {
                        status = seq.IsPlaying ? (seq.IsPaused ? " [PAUSED]" : " [PLAYING]") : " [SELECTED]";
                    }
                }

                EditorGUILayout.LabelField($"  [{i}] {seqName}{status}");
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Draw sequences array
        EditorGUILayout.PropertyField(sequencesProp, new GUIContent("Sequences"), true);

        EditorGUILayout.Space();

        // Playback Settings section
        EditorGUILayout.LabelField("Playback Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultSequenceIndexProp);
        EditorGUILayout.PropertyField(playOnStartProp);

        EditorGUILayout.Space();

        // Runtime controls (only show in play mode)
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Library Controls", EditorStyles.boldLabel);

            ActionDecalSequenceLibrary library = (ActionDecalSequenceLibrary)target;

            // Show current state
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Current Sequence: {(library.CurrentSequenceIndex >= 0 ? library.CurrentSequenceIndex.ToString() : "None")}");
            EditorGUILayout.LabelField($"Status: {(library.IsPlaying ? "Playing" : "Stopped")}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Play sequence by index
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Play Sequence:", GUILayout.Width(100));

            for (int i = 0; i < Mathf.Min(sequenceCount, 10); i++)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Width(30)))
                {
                    library.PlaySequence(i);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (sequenceCount > 10)
            {
                EditorGUILayout.HelpBox("Only showing first 10 sequences. Use PlaySequence(index) in code for sequences 10+", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Navigation controls
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Previous", GUILayout.Height(25)))
            {
                library.PlayPreviousSequence();
            }

            if (GUILayout.Button("Next", GUILayout.Height(25)))
            {
                library.PlayNextSequence();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Playback controls
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = library.IsPlaying && library.GetCurrentSequence() != null && !library.GetCurrentSequence().IsPaused;
            if (GUILayout.Button("Pause", GUILayout.Height(30)))
            {
                library.PauseCurrentSequence();
            }
            GUI.enabled = true;

            GUI.enabled = library.IsPlaying && library.GetCurrentSequence() != null && library.GetCurrentSequence().IsPaused;
            if (GUILayout.Button("Resume", GUILayout.Height(30)))
            {
                library.ResumeCurrentSequence();
            }
            GUI.enabled = true;

            GUI.enabled = library.IsPlaying;
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                library.StopCurrentSequence();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        // Library Events section
        EditorGUILayout.LabelField("Library Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onSequenceChangedProp);
        EditorGUILayout.PropertyField(onLibraryStoppedProp);

        serializedObject.ApplyModifiedProperties();

        // Repaint in play mode to update UI
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
