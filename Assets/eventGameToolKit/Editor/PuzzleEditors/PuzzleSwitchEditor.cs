using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for PuzzleSwitch showing current state and quick test buttons
/// </summary>
[CustomEditor(typeof(PuzzleSwitch))]
public class PuzzleSwitchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PuzzleSwitch puzzleSwitch = (PuzzleSwitch)target;

        // Runtime controls during play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            // Show current state prominently
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current State:", GUILayout.Width(100));

            GUIStyle stateStyle = new GUIStyle(EditorStyles.label);
            stateStyle.fontStyle = FontStyle.Bold;
            stateStyle.fontSize = 14;
            stateStyle.normal.textColor = Color.cyan;

            EditorGUILayout.LabelField($"State {puzzleSwitch.GetCurrentState()}", stateStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // State manipulation buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("← Previous"))
            {
                puzzleSwitch.PreviousState();
            }

            if (GUILayout.Button("Activate"))
            {
                puzzleSwitch.Activate();
            }

            if (GUILayout.Button("Next →"))
            {
                puzzleSwitch.NextState();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset to 0"))
            {
                puzzleSwitch.ResetToInitialState();
            }

            EditorGUILayout.EndHorizontal();

            // Show direct state setter buttons if more than 2 states
            int numStates = GetNumberOfStates();
            if (numStates > 2 && numStates <= 8)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Set Specific State:", EditorStyles.miniBoldLabel);

                int buttonsPerRow = Mathf.Min(4, numStates);
                int currentRow = 0;

                for (int i = 0; i < numStates; i++)
                {
                    if (i % buttonsPerRow == 0)
                    {
                        if (currentRow > 0)
                            EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        currentRow++;
                    }

                    bool isCurrentState = (i == puzzleSwitch.GetCurrentState());
                    GUI.backgroundColor = isCurrentState ? Color.green : Color.white;

                    if (GUILayout.Button($"State {i}"))
                    {
                        puzzleSwitch.SetState(i);
                    }

                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Enter Play Mode to test switch functionality", MessageType.Info);
        }
    }

    // Helper to get numberOfStates from serialized property
    private int GetNumberOfStates()
    {
        SerializedProperty statesProp = serializedObject.FindProperty("numberOfStates");
        return statesProp != null ? statesProp.intValue : 2;
    }
}
