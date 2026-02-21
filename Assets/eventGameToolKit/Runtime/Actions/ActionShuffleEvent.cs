using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A single entry in an ActionShuffleEvent list, containing a label and the event to fire when selected.
/// </summary>
[System.Serializable]
public class ShuffleEntry
{
    [Tooltip("Label for this entry (for your reference only — not used at runtime)")]
    public string label;

    /// <summary>
    /// Event fired when this entry is reached in the shuffled sequence.
    /// </summary>
    public UnityEvent onSelected;
}

/// <summary>
/// Cycles through all entries in a random (shuffled) order, guaranteeing each fires exactly once
/// before the sequence resets and reshuffles. Based on the urn model from probability theory.
/// Common use: non-repeating random rewards, varied NPC dialogue, procedural level variety.
/// </summary>
public class ActionShuffleEvent : MonoBehaviour
{
    [Header("Entries")]
    [Tooltip("List of events to cycle through. Each entry fires exactly once per cycle before reshuffling.")]
    [SerializeField] private ShuffleEntry[] entries;

    [Header("Options")]
    [Tooltip("When enabled, the first event of a new cycle will never be the same as the last event of the previous cycle.")]
    [SerializeField] private bool preventLastRepeat = true;

    [Header("Cycle Events")]
    /// <summary>
    /// Fires when every entry has been selected once and the sequence is about to reshuffle.
    /// </summary>
    public UnityEvent onCycleComplete;

    // Runtime state
    private int[] shuffledIndices;
    private int currentStep = 0;
    private int lastFiredIndex = -1;
    private bool initialized = false;

    /// <summary>
    /// How many steps have been taken in the current cycle (read-only, for Editor display).
    /// </summary>
    public int CurrentStep => currentStep;

    /// <summary>
    /// Total number of entries in the shuffle pool (read-only, for Editor display).
    /// </summary>
    public int EntryCount => entries != null ? entries.Length : 0;

    /// <summary>
    /// The shuffled index array (read-only, for Editor display).
    /// </summary>
    public int[] ShuffledIndices => shuffledIndices;

    private void Reset()
    {
        entries = new ShuffleEntry[]
        {
            new ShuffleEntry { label = "Entry A" },
            new ShuffleEntry { label = "Entry B" },
            new ShuffleEntry { label = "Entry C" }
        };
    }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        if (entries == null || entries.Length == 0) return;
        BuildShuffledIndices(-1);
        initialized = true;
    }

    /// <summary>
    /// Fires the next entry in the shuffled sequence. When all entries have been fired,
    /// triggers onCycleComplete, reshuffles, and continues from the new first entry.
    /// Wire this to any UnityEvent source such as InputKeyPress or InputTriggerZone.
    /// </summary>
    public void Trigger()
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning($"[ActionShuffleEvent] '{name}': No entries defined. Add at least one entry.", this);
            return;
        }

        InitializeIfNeeded();

        if (currentStep >= shuffledIndices.Length)
        {
            // Cycle complete — notify, then reshuffle
            onCycleComplete?.Invoke();
            BuildShuffledIndices(lastFiredIndex);
            currentStep = 0;
        }

        int index = shuffledIndices[currentStep];
        currentStep++;
        lastFiredIndex = index;

        entries[index].onSelected?.Invoke();
    }

    /// <summary>
    /// Immediately reshuffles and resets the cycle to step 0.
    /// The last-repeat prevention applies to the first event of the new shuffle.
    /// </summary>
    public void Reshuffle()
    {
        if (entries == null || entries.Length == 0) return;
        BuildShuffledIndices(lastFiredIndex);
        currentStep = 0;
    }

    /// <summary>
    /// Resets both the shuffle state and the last-fired memory, as if the component just started.
    /// </summary>
    public void ResetFull()
    {
        lastFiredIndex = -1;
        if (entries == null || entries.Length == 0) return;
        BuildShuffledIndices(-1);
        currentStep = 0;
    }

    // Fisher-Yates shuffle. If preventLastRepeat is on and lastIndex is valid,
    // ensures the first element of the new shuffle differs from lastIndex.
    private void BuildShuffledIndices(int lastIndex)
    {
        int count = entries.Length;
        shuffledIndices = new int[count];
        for (int i = 0; i < count; i++)
            shuffledIndices[i] = i;

        // Fisher-Yates
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[j];
            shuffledIndices[j] = tmp;
        }

        // Prevent the same entry starting the new cycle as ended the last
        if (preventLastRepeat && count > 1 && lastIndex >= 0 && shuffledIndices[0] == lastIndex)
        {
            // Swap first with any other position
            int swapWith = Random.Range(1, count);
            int tmp = shuffledIndices[0];
            shuffledIndices[0] = shuffledIndices[swapWith];
            shuffledIndices[swapWith] = tmp;
        }
    }
}
