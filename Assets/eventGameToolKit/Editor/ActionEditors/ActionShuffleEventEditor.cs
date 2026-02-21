using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for ActionShuffleEvent showing cycle progress in Play mode,
/// which entries are queued vs. already fired, and Trigger/Reshuffle test buttons.
/// </summary>
[CustomEditor(typeof(ActionShuffleEvent))]
public class ActionShuffleEventEditor : Editor
{
    private bool[] foldouts = new bool[0];

    // Colors for play-mode queue display
    private static readonly Color ColorFired  = new Color(0.55f, 0.55f, 0.55f, 1f); // greyed out
    private static readonly Color ColorNext   = new Color(0.35f, 0.85f, 0.45f, 1f); // green highlight
    private static readonly Color ColorQueued = new Color(0.85f, 0.85f, 0.85f, 1f); // normal

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script reference (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionShuffleEvent)target), typeof(ActionShuffleEvent), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        SerializedProperty arrayProp   = serializedObject.FindProperty("entries");
        SerializedProperty preventProp = serializedObject.FindProperty("preventLastRepeat");
        SerializedProperty cycleProp   = serializedObject.FindProperty("onCycleComplete");

        ActionShuffleEvent shuffle = (ActionShuffleEvent)target;

        // Sync foldout array
        if (foldouts.Length != arrayProp.arraySize)
        {
            bool[] newFoldouts = new bool[arrayProp.arraySize];
            for (int i = 0; i < Mathf.Min(foldouts.Length, newFoldouts.Length); i++)
                newFoldouts[i] = foldouts[i];
            for (int i = foldouts.Length; i < newFoldouts.Length; i++)
                newFoldouts[i] = true;
            foldouts = newFoldouts;
        }

        // ── Entries ──────────────────────────────────────────────
        EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            SerializedProperty element   = arrayProp.GetArrayElementAtIndex(i);
            SerializedProperty labelProp = element.FindPropertyRelative("label");
            SerializedProperty eventProp = element.FindPropertyRelative("onSelected");

            string displayLabel = string.IsNullOrEmpty(labelProp.stringValue)
                ? $"Entry {i}"
                : labelProp.stringValue;

            // In Play mode, annotate queue position
            string suffix = "";
            Color headerColor = ColorQueued;

            if (Application.isPlaying && shuffle.ShuffledIndices != null)
            {
                int step = shuffle.CurrentStep;
                int[] idx = shuffle.ShuffledIndices;
                int posInQueue = System.Array.IndexOf(idx, i);

                if (posInQueue < step)
                {
                    suffix = "  ✓ fired";
                    headerColor = ColorFired;
                }
                else if (posInQueue == step)
                {
                    suffix = "  ← next";
                    headerColor = ColorNext;
                }
                else
                {
                    suffix = $"  (#{posInQueue - step + 1} queued)";
                }
            }

            // Foldout header row
            EditorGUILayout.BeginHorizontal();
            Color prev = GUI.contentColor;
            GUI.contentColor = headerColor;
            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], displayLabel + suffix, true, EditorStyles.foldoutHeader);
            GUI.contentColor = prev;
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("−", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (foldouts[i])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(labelProp, new GUIContent("Label"));
                EditorGUILayout.PropertyField(eventProp, new GUIContent("On Selected"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }
        }

        // Add button
        EditorGUILayout.Space(2);
        if (GUILayout.Button("+ Add Entry", GUILayout.Height(22)))
        {
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
            SerializedProperty newEl = arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1);
            newEl.FindPropertyRelative("label").stringValue = $"Entry {(char)('A' + arrayProp.arraySize - 1)}";
            newEl.FindPropertyRelative("onSelected").FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
        }

        EditorGUILayout.Space(6);

        // ── Options ───────────────────────────────────────────────
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(preventProp, new GUIContent("Prevent Last Repeat",
            "When enabled, the first entry of a new cycle will never be the same as the last entry of the previous cycle."));

        EditorGUILayout.Space(4);

        // ── Cycle Events ──────────────────────────────────────────
        EditorGUILayout.PropertyField(cycleProp, new GUIContent("On Cycle Complete"));

        serializedObject.ApplyModifiedProperties();

        // ── Play Mode Controls ────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Test", EditorStyles.boldLabel);

        if (Application.isPlaying && shuffle.EntryCount > 0)
        {
            // Cycle progress bar
            int step  = shuffle.CurrentStep;
            int total = shuffle.EntryCount;
            float pct = total > 0 ? (float)step / total : 0f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel($"Cycle Progress");
            Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(barRect, pct, $"{step} / {total}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        GUI.enabled = Application.isPlaying;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ Trigger Next", GUILayout.Height(28)))
            shuffle.Trigger();

        if (GUILayout.Button("↺ Reshuffle", GUILayout.Height(28)))
            shuffle.Reshuffle();
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play mode to use the Trigger and Reshuffle buttons.", MessageType.Info);

        // Repaint continuously in play mode so progress updates
        if (Application.isPlaying)
            Repaint();
    }
}
