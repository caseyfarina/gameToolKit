using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Validates that PuzzleSwitches are activated in a specific order.
/// Unlike PuzzleSwitchChecker (which checks final states), this tracks the sequence
/// of activations over time — the puzzle is solved only when switches are activated
/// in the configured order.
/// Common use: Musical combination locks, ordered button sequences, ritual step puzzles,
/// Simon-says style challenges, or any puzzle where sequence matters as much as state.
/// </summary>
public class PuzzleSequenceChecker : MonoBehaviour
{
    [Header("Sequence Configuration")]
    [Tooltip("The switches in the order they must be activated. The same switch can appear more than once.")]
    [SerializeField] private PuzzleSwitch[] correctSequence;

    [Header("Behaviour")]
    [Tooltip("Reset progress to step 0 when the wrong switch is activated. If false, wrong activations are silently ignored.")]
    [SerializeField] private bool resetOnMistake = true;

    [Tooltip("Allow the sequence to be solved more than once after canBeReSolved is enabled.")]
    [SerializeField] private bool canBeReSolved = true;

    [Header("State (Read-Only)")]
    [Tooltip("Which step in the sequence is expected next (0 = waiting for first switch)")]
    [SerializeField] private int currentStep = 0;

    [Tooltip("Is the sequence currently in a solved state?")]
    [SerializeField] private bool isSolved = false;

    [Tooltip("Has the sequence ever been completed?")]
    [SerializeField] private bool hasBeenSolved = false;

    [Header("Events")]
    /// <summary>
    /// Fires when the full sequence is completed correctly
    /// </summary>
    public UnityEvent onSequenceSolved;

    /// <summary>
    /// Fires only the first time the sequence is completed (useful for one-time rewards)
    /// </summary>
    public UnityEvent onFirstTimeSolved;

    /// <summary>
    /// Fires when the wrong switch is activated, passing the expected step index
    /// </summary>
    public UnityEvent<int> onMistake;

    /// <summary>
    /// Fires after each correct activation, passing the new step count and total steps
    /// </summary>
    public UnityEvent<int, int> onProgress;

    /// <summary>
    /// Fires when the sequence resets to step 0 (due to a mistake or a manual ResetSequence call)
    /// </summary>
    public UnityEvent onReset;

    // Stored delegate references keyed by switch — storing the reference allows RemoveListener
    // to find the exact delegate, unlike inline lambdas which create a new instance each time.
    private Dictionary<PuzzleSwitch, UnityAction> switchListeners;

    private void Start()
    {
        if (correctSequence == null || correctSequence.Length == 0)
        {
            Debug.LogWarning("PuzzleSequenceChecker: correctSequence is empty — nothing to check.", this);
            return;
        }

        switchListeners = new Dictionary<PuzzleSwitch, UnityAction>();

        foreach (var sw in correctSequence)
        {
            if (sw == null)
            {
                Debug.LogWarning("PuzzleSequenceChecker: Null entry in correctSequence — skipped.", this);
                continue;
            }

            if (switchListeners.ContainsKey(sw))
                continue; // already subscribed — same switch can appear multiple times in sequence

            PuzzleSwitch captured = sw;
            UnityAction listener = () => OnSwitchActivated(captured);
            switchListeners[sw] = listener;
            sw.onActivated.AddListener(listener);
        }
    }

    private void OnDestroy()
    {
        if (switchListeners == null) return;

        foreach (var kvp in switchListeners)
        {
            if (kvp.Key != null)
                kvp.Key.onActivated.RemoveListener(kvp.Value);
        }
    }

    // ──────────────────────────────────────────────
    // Core sequence logic
    // ──────────────────────────────────────────────

    private void OnSwitchActivated(PuzzleSwitch activatedSwitch)
    {
        if (isSolved && !canBeReSolved) return;
        if (isSolved)
        {
            // canBeReSolved=true — first activation after solve starts a fresh attempt
            currentStep = 0;
            isSolved = false;
        }

        if (activatedSwitch == correctSequence[currentStep])
        {
            // Correct step — advance
            currentStep++;
            onProgress.Invoke(currentStep, correctSequence.Length);

            if (currentStep >= correctSequence.Length)
            {
                isSolved = true;
                onSequenceSolved.Invoke();

                if (!hasBeenSolved)
                {
                    hasBeenSolved = true;
                    onFirstTimeSolved.Invoke();
                }
            }
        }
        else
        {
            // Wrong switch
            onMistake.Invoke(currentStep);

            if (resetOnMistake)
            {
                currentStep = 0;
                isSolved = false;
                onReset.Invoke();
            }
        }
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Resets the sequence back to step 0. Does not change switch states.
    /// </summary>
    public void ResetSequence()
    {
        currentStep = 0;
        isSolved = false;
        onReset.Invoke();
    }

    /// <summary>
    /// Resets all switches in the sequence to state 0 and resets the step counter.
    /// </summary>
    public void ResetAll()
    {
        if (correctSequence == null) return;

        var seen = new HashSet<PuzzleSwitch>();
        foreach (var sw in correctSequence)
        {
            if (sw != null && seen.Add(sw))
                sw.ResetToInitialState();
        }

        ResetSequence();
    }

    /// <summary>
    /// Returns the current step index (0-based, where 0 means waiting for the first switch)
    /// </summary>
    public int GetCurrentStep() => currentStep;

    /// <summary>
    /// Returns the total number of steps in the sequence
    /// </summary>
    public int GetTotalSteps() => correctSequence != null ? correctSequence.Length : 0;

    /// <summary>
    /// Returns true if the sequence is currently in a solved state
    /// </summary>
    public bool IsSolved() => isSolved;

    /// <summary>
    /// Returns true if the sequence has ever been completed
    /// </summary>
    public bool HasBeenSolved() => hasBeenSolved;

    /// <summary>
    /// Returns the switch expected at the given step index, or null if out of range
    /// </summary>
    public PuzzleSwitch GetExpectedSwitch(int step)
    {
        if (correctSequence == null || step < 0 || step >= correctSequence.Length)
            return null;
        return correctSequence[step];
    }
}
