using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for ActionRandomEvent that displays each entry's normalized probability
/// as a percentage and provides a Play-mode Trigger button for quick testing.
/// </summary>
[CustomEditor(typeof(ActionRandomEvent))]
public class ActionRandomEventEditor : Editor
{
    // Foldout state per element (persisted per Inspector session)
    private bool[] foldouts = new bool[0];

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script reference (read-only, standard Unity convention)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionRandomEvent)target), typeof(ActionRandomEvent), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        SerializedProperty arrayProp = serializedObject.FindProperty("weightedEvents");
        float[] percentages = GetNormalizedPercentages(arrayProp);

        // Sync foldout array length
        if (foldouts.Length != arrayProp.arraySize)
        {
            bool[] newFoldouts = new bool[arrayProp.arraySize];
            for (int i = 0; i < Mathf.Min(foldouts.Length, newFoldouts.Length); i++)
                newFoldouts[i] = foldouts[i];
            // Default new elements to expanded
            for (int i = foldouts.Length; i < newFoldouts.Length; i++)
                newFoldouts[i] = true;
            foldouts = newFoldouts;
        }

        // Header row
        EditorGUILayout.LabelField("Weighted Events", EditorStyles.boldLabel);

        // Draw each element manually
        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
            SerializedProperty labelProp = element.FindPropertyRelative("label");
            SerializedProperty probProp = element.FindPropertyRelative("probability");
            SerializedProperty eventProp = element.FindPropertyRelative("onSelected");

            string elementLabel = string.IsNullOrEmpty(labelProp.stringValue)
                ? $"Element {i}"
                : labelProp.stringValue;

            string pctDisplay = $"→ {percentages[i]:F1}%";

            // Foldout header with percentage label
            EditorGUILayout.BeginHorizontal();
            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], elementLabel, true, EditorStyles.foldoutHeader);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(pctDisplay, EditorStyles.miniLabel, GUILayout.Width(60));

            // Remove button
            if (GUILayout.Button("−", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return; // Rebuild next frame
            }
            EditorGUILayout.EndHorizontal();

            if (foldouts[i])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(labelProp, new GUIContent("Label"));
                EditorGUILayout.PropertyField(probProp, new GUIContent("Probability Weight"));
                EditorGUILayout.PropertyField(eventProp, new GUIContent("On Selected"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }
        }

        // Add button
        EditorGUILayout.Space(2);
        if (GUILayout.Button("+ Add Event", GUILayout.Height(22)))
        {
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
            SerializedProperty newElement = arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1);
            newElement.FindPropertyRelative("label").stringValue = $"Option {(char)('A' + arrayProp.arraySize - 1)}";
            newElement.FindPropertyRelative("probability").floatValue = 50f;
            newElement.FindPropertyRelative("onSelected").FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
        }

        serializedObject.ApplyModifiedProperties();

        // Help box if all weights are zero
        if (arrayProp.arraySize > 0 && AllWeightsZero(arrayProp))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("All probability weights are zero. Trigger() will log a warning and nothing will fire.", MessageType.Warning);
        }

        // Play-mode Trigger button
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Test", EditorStyles.boldLabel);

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("▶ Trigger", GUILayout.Height(28)))
        {
            ((ActionRandomEvent)target).Trigger();
        }
        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to use the Trigger button.", MessageType.Info);
        }
    }

    // --------------- Helpers ---------------

    private float[] GetNormalizedPercentages(SerializedProperty arrayProp)
    {
        float total = 0f;
        float[] weights = new float[arrayProp.arraySize];

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            weights[i] = Mathf.Max(0f, arrayProp.GetArrayElementAtIndex(i).FindPropertyRelative("probability").floatValue);
            total += weights[i];
        }

        float[] pcts = new float[weights.Length];
        for (int i = 0; i < weights.Length; i++)
            pcts[i] = total > 0f ? (weights[i] / total * 100f) : 0f;

        return pcts;
    }

    private bool AllWeightsZero(SerializedProperty arrayProp)
    {
        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            if (arrayProp.GetArrayElementAtIndex(i).FindPropertyRelative("probability").floatValue > 0f)
                return false;
        }
        return true;
    }
}
