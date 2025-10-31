using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for PuzzleSwitchChecker that shows visual progress and state information
/// </summary>
[CustomEditor(typeof(PuzzleSwitchChecker))]
public class PuzzleSwitchCheckerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PuzzleSwitchChecker checker = (PuzzleSwitchChecker)target;

        // Only show runtime info during play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            // Overall status
            bool isSolved = checker.IsSolved();
            bool hasBeenSolved = checker.HasBeenSolved();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Puzzle Status:", GUILayout.Width(120));

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.normal.textColor = isSolved ? Color.green : Color.red;

            EditorGUILayout.LabelField(isSolved ? "SOLVED ✓" : "UNSOLVED ✗", statusStyle);
            EditorGUILayout.EndHorizontal();

            if (hasBeenSolved && !isSolved)
            {
                EditorGUILayout.LabelField("Previously solved, now broken", EditorStyles.miniLabel);
            }

            // Progress bar
            float progress = checker.GetProgressPercentage();
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, progress, $"{checker.GetCorrectCount()} / {checker.GetTotalSwitchCount()} Correct");

            EditorGUILayout.Space();

            // Utility buttons
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Check Puzzle Now"))
            {
                checker.CheckPuzzle();
            }

            if (GUILayout.Button("Refresh State"))
            {
                checker.RefreshState();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset All Switches"))
            {
                checker.ResetAllSwitches();
            }

            if (GUILayout.Button("Reveal Solution"))
            {
                checker.RevealSolution();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Enter Play Mode to see runtime puzzle state and utilities", MessageType.Info);
        }

        // Always show switch count
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Switches: {checker.GetTotalSwitchCount()}");
    }
}
