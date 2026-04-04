using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for PuzzleSequenceChecker.
/// Shows the sequence as a numbered list, highlights the current step in play mode,
/// and provides Reset controls.
/// </summary>
[CustomEditor(typeof(PuzzleSequenceChecker))]
public class PuzzleSequenceCheckerEditor : Editor
{
    private SerializedProperty correctSequenceProp;
    private SerializedProperty resetOnMistakeProp;
    private SerializedProperty canBeResolvedProp;
    private SerializedProperty currentStepProp;
    private SerializedProperty isSolvedProp;
    private SerializedProperty hasBeenSolvedProp;
    private SerializedProperty onSequenceSolvedProp;
    private SerializedProperty onFirstTimeSolvedProp;
    private SerializedProperty onMistakeProp;
    private SerializedProperty onProgressProp;
    private SerializedProperty onResetProp;

    private void OnEnable()
    {
        correctSequenceProp   = serializedObject.FindProperty("correctSequence");
        resetOnMistakeProp    = serializedObject.FindProperty("resetOnMistake");
        canBeResolvedProp     = serializedObject.FindProperty("canBeReSolved");
        currentStepProp       = serializedObject.FindProperty("currentStep");
        isSolvedProp          = serializedObject.FindProperty("isSolved");
        hasBeenSolvedProp     = serializedObject.FindProperty("hasBeenSolved");
        onSequenceSolvedProp  = serializedObject.FindProperty("onSequenceSolved");
        onFirstTimeSolvedProp = serializedObject.FindProperty("onFirstTimeSolved");
        onMistakeProp         = serializedObject.FindProperty("onMistake");
        onProgressProp        = serializedObject.FindProperty("onProgress");
        onResetProp           = serializedObject.FindProperty("onReset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var checker = (PuzzleSequenceChecker)target;

        // ── Sequence list ─────────────────────────────
        EditorGUILayout.LabelField("Sequence Configuration", EditorStyles.boldLabel);

        int stepCount = correctSequenceProp.arraySize;
        int currentStep = Application.isPlaying ? checker.GetCurrentStep() : 0;
        bool isSolved = Application.isPlaying && checker.IsSolved();

        // Resize buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+", GUILayout.Width(24)))
        {
            correctSequenceProp.arraySize++;
        }
        if (GUILayout.Button("-", GUILayout.Width(24)) && stepCount > 0)
        {
            correctSequenceProp.arraySize--;
            stepCount--;
        }
        EditorGUILayout.LabelField($"{stepCount} step{(stepCount == 1 ? "" : "s")}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        for (int i = 0; i < stepCount; i++)
        {
            SerializedProperty elem = correctSequenceProp.GetArrayElementAtIndex(i);

            // Highlight current step in play mode
            bool isCurrent = Application.isPlaying && !isSolved && i == currentStep;
            bool isComplete = Application.isPlaying && i < currentStep;

            if (isCurrent)
            {
                var highlightRect = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(new Rect(highlightRect.x - 2, highlightRect.y, highlightRect.width + 4, EditorGUIUtility.singleLineHeight + 4), new Color(0.2f, 0.6f, 1f, 0.2f));
            }
            else
            {
                EditorGUILayout.BeginVertical();
            }

            EditorGUILayout.BeginHorizontal();

            // Step indicator
            GUIStyle numStyle = new GUIStyle(EditorStyles.miniLabel);
            if (isCurrent) { numStyle.fontStyle = FontStyle.Bold; numStyle.normal.textColor = new Color(0.3f, 0.7f, 1f); }
            else if (isComplete) { numStyle.normal.textColor = new Color(0.4f, 0.8f, 0.4f); }

            string prefix = isCurrent ? "▶" : (isComplete ? "✓" : " ");
            EditorGUILayout.LabelField($"{prefix} {i + 1}", numStyle, GUILayout.Width(32));

            EditorGUILayout.PropertyField(elem, GUIContent.none);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        // ── Behaviour ────────────────────────────────
        EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(resetOnMistakeProp, new GUIContent("Reset On Mistake"));
        EditorGUILayout.PropertyField(canBeResolvedProp,  new GUIContent("Can Be Re-Solved"));

        EditorGUILayout.Space();

        // ── Play-mode status ──────────────────────────
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.normal.textColor = isSolved ? new Color(0.3f, 0.9f, 0.3f) : Color.white;
            EditorGUILayout.LabelField(isSolved ? "SOLVED ✓" : $"Step {currentStep + 1} of {stepCount}", statusStyle);

            if (checker.HasBeenSolved() && !isSolved)
                EditorGUILayout.LabelField("Previously solved — waiting for reset", EditorStyles.miniLabel);

            float progress = stepCount > 0 ? (float)currentStep / stepCount : 0f;
            Rect bar = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.ProgressBar(bar, isSolved ? 1f : progress, isSolved ? "Complete" : $"{currentStep} / {stepCount}");

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Sequence"))
                checker.ResetSequence();
            if (GUILayout.Button("Reset All Switches"))
                checker.ResetAll();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see step progress and runtime controls.", MessageType.Info);
        }

        EditorGUILayout.Space();

        // ── Events ───────────────────────────────────
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onSequenceSolvedProp,  new GUIContent("On Sequence Solved"));
        EditorGUILayout.PropertyField(onFirstTimeSolvedProp, new GUIContent("On First Time Solved"));
        EditorGUILayout.PropertyField(onMistakeProp,         new GUIContent("On Mistake"));
        EditorGUILayout.PropertyField(onProgressProp,        new GUIContent("On Progress"));
        EditorGUILayout.PropertyField(onResetProp,           new GUIContent("On Reset"));

        serializedObject.ApplyModifiedProperties();
    }
}
