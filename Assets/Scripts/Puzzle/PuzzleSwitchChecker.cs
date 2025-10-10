using UnityEngine;
using UnityEngine.Events;
using System.Linq;

/// <summary>
/// Validates multiple PuzzleSwitch states against target configuration with automatic or manual checking modes.
/// Common use: Lock combination validators, puzzle door systems, safe cracking, or multi-step challenge verification.
/// </summary>
public class PuzzleSwitchChecker : MonoBehaviour
{
    [System.Serializable]
    public class SwitchTarget
    {
        [Tooltip("The switch to monitor")]
        public PuzzleSwitch targetSwitch;

        [Tooltip("Required state for this switch (0-based index)")]
        public int requiredState;

        [Tooltip("Is this switch currently in the correct state?")]
        [HideInInspector] public bool isCorrect;
    }

    [Header("Switch Configuration")]
    [Tooltip("All switches that are part of this puzzle")]
    [SerializeField] private SwitchTarget[] switchTargets;

    [Header("Validation Mode")]
    [Tooltip("Check puzzle automatically when switches change, or manually via CheckPuzzle()?")]
    [SerializeField] private bool automaticChecking = true;

    [Tooltip("Require all switches correct, or just a threshold count?")]
    [SerializeField] private bool requireAllCorrect = true;

    [Tooltip("If not requiring all, how many switches must be correct?")]
    [SerializeField] private int requiredCorrectCount = 1;

    [Tooltip("Allow puzzle to be re-broken after solving? (if false, onPuzzleSolved only fires once)")]
    [SerializeField] private bool canBeUnsolved = true;

    [Header("Puzzle State (Read-Only)")]
    [Tooltip("How many switches are currently correct?")]
    [SerializeField] private int currentCorrectCount = 0;

    [Tooltip("Is the puzzle currently solved?")]
    [SerializeField] private bool puzzleSolved = false;

    [Tooltip("Has the puzzle ever been solved? (for one-time rewards)")]
    [SerializeField] private bool hasBeenSolved = false;

    [Header("Events - Success")]
    [Tooltip("Fires when puzzle is solved")]
    /// <summary>
    /// Fires when all puzzle switches reach their required states and the puzzle is solved
    /// </summary>
    public UnityEvent onPuzzleSolved = new UnityEvent();

    [Tooltip("Fires only the first time puzzle is solved")]
    /// <summary>
    /// Fires only the first time the puzzle is solved (useful for one-time rewards)
    /// </summary>
    public UnityEvent onFirstTimeSolved = new UnityEvent();

    [Tooltip("Fires when puzzle becomes unsolved (if canBeUnsolved is true)")]
    /// <summary>
    /// Fires when a solved puzzle becomes unsolved due to a switch changing (only if canBeUnsolved is enabled)
    /// </summary>
    public UnityEvent onPuzzleUnsolved = new UnityEvent();

    [Header("Events - Progress")]
    [Tooltip("Fires whenever progress changes (passes correct count, total count)")]
    /// <summary>
    /// Fires whenever the number of correct switches changes, passing the correct count and total count as int parameters
    /// </summary>
    public UnityEvent<int, int> onProgressChanged = new UnityEvent<int, int>();

    [Tooltip("Fires when a switch changes to correct state")]
    /// <summary>
    /// Fires when a switch changes from incorrect to correct state, passing the switch index as an int parameter
    /// </summary>
    public UnityEvent<int> onSwitchBecameCorrect = new UnityEvent<int>();

    [Tooltip("Fires when a switch changes to incorrect state")]
    /// <summary>
    /// Fires when a switch changes from correct to incorrect state, passing the switch index as an int parameter
    /// </summary>
    public UnityEvent<int> onSwitchBecameIncorrect = new UnityEvent<int>();

    [Header("Events - Manual Checking")]
    [Tooltip("Fires when CheckPuzzle() is called but puzzle is incorrect")]
    /// <summary>
    /// Fires when CheckPuzzle() is manually called but the puzzle is not solved
    /// </summary>
    public UnityEvent onCheckFailed = new UnityEvent();

    [Tooltip("Fires any time CheckPuzzle() is called")]
    /// <summary>
    /// Fires every time CheckPuzzle() is manually called, regardless of puzzle state
    /// </summary>
    public UnityEvent onPuzzleChecked = new UnityEvent();

    void Start()
    {
        // Validate configuration
        if (switchTargets == null || switchTargets.Length == 0)
        {
            Debug.LogWarning("PuzzleSwitchChecker: No switch targets configured!", this);
            return;
        }

        // Subscribe to switch events if automatic checking
        if (automaticChecking)
        {
            foreach (var target in switchTargets)
            {
                if (target.targetSwitch != null)
                {
                    // Subscribe to state changes
                    target.targetSwitch.onStateChanged.AddListener((newState) => OnSwitchChanged(target.targetSwitch));
                }
                else
                {
                    Debug.LogWarning("PuzzleSwitchChecker: Switch target is null!", this);
                }
            }
        }

        // Initial check
        CheckConfiguration(false); // Don't fire events on initial check
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (automaticChecking)
        {
            foreach (var target in switchTargets)
            {
                if (target.targetSwitch != null)
                {
                    target.targetSwitch.onStateChanged.RemoveListener((newState) => OnSwitchChanged(target.targetSwitch));
                }
            }
        }
    }

    /// <summary>
    /// Called when any switch changes state (automatic mode only)
    /// </summary>
    private void OnSwitchChanged(PuzzleSwitch changedSwitch)
    {
        CheckConfiguration(true);
    }

    /// <summary>
    /// Manually check the puzzle configuration (for manual mode or event-triggered checking)
    /// </summary>
    public void CheckPuzzle()
    {
        CheckConfiguration(true);
        onPuzzleChecked.Invoke();

        // Fire fail event if not solved (manual mode feedback)
        if (!puzzleSolved)
        {
            onCheckFailed.Invoke();
        }
    }

    /// <summary>
    /// Forces a recheck of the puzzle state without firing solve/unsolve events
    /// </summary>
    public void RefreshState()
    {
        CheckConfiguration(false);
    }

    /// <summary>
    /// Core validation logic - checks all switches against requirements
    /// </summary>
    /// <param name="fireEvents">Should events be fired for state changes?</param>
    private void CheckConfiguration(bool fireEvents)
    {
        int previousCorrectCount = currentCorrectCount;
        currentCorrectCount = 0;

        // Check each switch
        for (int i = 0; i < switchTargets.Length; i++)
        {
            var target = switchTargets[i];

            if (target.targetSwitch == null)
                continue;

            bool wasCorrect = target.isCorrect;
            target.isCorrect = (target.targetSwitch.GetCurrentState() == target.requiredState);

            if (target.isCorrect)
            {
                currentCorrectCount++;

                // Fire event if switch became correct
                if (fireEvents && !wasCorrect)
                {
                    onSwitchBecameCorrect.Invoke(i);
                }
            }
            else
            {
                // Fire event if switch became incorrect
                if (fireEvents && wasCorrect)
                {
                    onSwitchBecameIncorrect.Invoke(i);
                }
            }
        }

        // Fire progress event if count changed
        if (fireEvents && currentCorrectCount != previousCorrectCount)
        {
            onProgressChanged.Invoke(currentCorrectCount, switchTargets.Length);
        }

        // Determine if puzzle is solved
        bool nowSolved = requireAllCorrect ?
            (currentCorrectCount == switchTargets.Length) :
            (currentCorrectCount >= requiredCorrectCount);

        // Handle state transitions
        if (nowSolved && !puzzleSolved)
        {
            // Puzzle just became solved
            puzzleSolved = true;

            if (fireEvents)
            {
                onPuzzleSolved.Invoke();

                if (!hasBeenSolved)
                {
                    hasBeenSolved = true;
                    onFirstTimeSolved.Invoke();
                }
            }
        }
        else if (!nowSolved && puzzleSolved && canBeUnsolved)
        {
            // Puzzle just became unsolved
            puzzleSolved = false;

            if (fireEvents)
            {
                onPuzzleUnsolved.Invoke();
            }
        }
    }

    /// <summary>
    /// Resets the puzzle to unsolved state (does not change switch positions)
    /// </summary>
    public void ResetPuzzleState()
    {
        puzzleSolved = false;
        hasBeenSolved = false;
        CheckConfiguration(false);
    }

    /// <summary>
    /// Resets all switches to state 0
    /// </summary>
    public void ResetAllSwitches()
    {
        foreach (var target in switchTargets)
        {
            if (target.targetSwitch != null)
            {
                target.targetSwitch.ResetToInitialState();
            }
        }
        CheckConfiguration(true);
    }

    /// <summary>
    /// Sets all switches to their solution states (cheat/reveal function)
    /// </summary>
    public void RevealSolution()
    {
        foreach (var target in switchTargets)
        {
            if (target.targetSwitch != null)
            {
                target.targetSwitch.SetState(target.requiredState);
            }
        }
        CheckConfiguration(true);
    }

    /// <summary>
    /// Gets the current puzzle progress as a percentage (0-1)
    /// </summary>
    public float GetProgressPercentage()
    {
        if (switchTargets.Length == 0)
            return 0f;

        return (float)currentCorrectCount / switchTargets.Length;
    }

    /// <summary>
    /// Returns true if puzzle is currently solved
    /// </summary>
    public bool IsSolved()
    {
        return puzzleSolved;
    }

    /// <summary>
    /// Returns true if puzzle has ever been solved
    /// </summary>
    public bool HasBeenSolved()
    {
        return hasBeenSolved;
    }

    /// <summary>
    /// Gets the number of switches currently in correct state
    /// </summary>
    public int GetCorrectCount()
    {
        return currentCorrectCount;
    }

    /// <summary>
    /// Gets the total number of switches in this puzzle
    /// </summary>
    public int GetTotalSwitchCount()
    {
        return switchTargets.Length;
    }

    /// <summary>
    /// Changes checking mode at runtime
    /// </summary>
    public void SetAutomaticChecking(bool automatic)
    {
        if (automaticChecking == automatic)
            return;

        automaticChecking = automatic;

        if (automatic)
        {
            // Subscribe to all switches
            foreach (var target in switchTargets)
            {
                if (target.targetSwitch != null)
                {
                    target.targetSwitch.onStateChanged.AddListener((newState) => OnSwitchChanged(target.targetSwitch));
                }
            }
            CheckConfiguration(true);
        }
        else
        {
            // Unsubscribe from all switches
            foreach (var target in switchTargets)
            {
                if (target.targetSwitch != null)
                {
                    target.targetSwitch.onStateChanged.RemoveListener((newState) => OnSwitchChanged(target.targetSwitch));
                }
            }
        }
    }

    // Gizmo visualization
    void OnDrawGizmos()
    {
        if (switchTargets == null || switchTargets.Length == 0)
            return;

        // Draw lines to all connected switches
        Gizmos.color = puzzleSolved ? Color.green : Color.yellow;

        foreach (var target in switchTargets)
        {
            if (target.targetSwitch != null)
            {
                Gizmos.DrawLine(transform.position, target.targetSwitch.transform.position);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (switchTargets == null || switchTargets.Length == 0)
            return;

        // Draw more detailed connections when selected
        for (int i = 0; i < switchTargets.Length; i++)
        {
            var target = switchTargets[i];
            if (target.targetSwitch == null)
                continue;

            // Color based on correctness (only accurate during play mode)
            if (Application.isPlaying)
            {
                Gizmos.color = target.isCorrect ? Color.green : Color.red;
            }
            else
            {
                Gizmos.color = Color.cyan;
            }

            // Draw sphere at switch position
            Gizmos.DrawWireSphere(target.targetSwitch.transform.position, 0.3f);

            // Draw line from checker to switch
            Gizmos.DrawLine(transform.position, target.targetSwitch.transform.position);
        }

        // Draw checker position
        Gizmos.color = puzzleSolved ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
