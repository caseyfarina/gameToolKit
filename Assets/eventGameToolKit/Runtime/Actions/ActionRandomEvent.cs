using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A single weighted outcome containing a label, probability weight, and the event to fire when selected.
/// </summary>
[System.Serializable]
public class WeightedEvent
{
    [Tooltip("Label for this option (for your reference only — not used at runtime)")]
    public string label;

    [Tooltip("Relative probability weight. Values are normalized automatically, so any positive values work (e.g., 1/1/2 gives 25%/25%/50%).")]
    [Min(0f)]
    public float probability = 50f;

    /// <summary>
    /// Event fired when this option is randomly selected by ActionRandomEvent.Trigger()
    /// </summary>
    public UnityEvent onSelected;
}

/// <summary>
/// Randomly fires one UnityEvent from a weighted list when Trigger() is called.
/// Weights are normalized automatically — students don't need to sum to 100.
/// Common use: random rewards, branching dialogue, procedural variety, unpredictable hazards.
/// </summary>
public class ActionRandomEvent : MonoBehaviour
{
    [Header("Weighted Events")]
    [Tooltip("List of events and their relative probability weights. Call Trigger() to randomly fire one.")]
    [SerializeField] private WeightedEvent[] weightedEvents;

    private void Reset()
    {
        weightedEvents = new WeightedEvent[]
        {
            new WeightedEvent { label = "Option A", probability = 50f },
            new WeightedEvent { label = "Option B", probability = 50f }
        };
    }

    private void OnValidate()
    {
        if (weightedEvents == null) return;
        for (int i = 0; i < weightedEvents.Length; i++)
        {
            if (weightedEvents[i] != null)
                weightedEvents[i].probability = Mathf.Max(0f, weightedEvents[i].probability);
        }
    }

    /// <summary>
    /// Randomly selects and fires one event from the weighted list.
    /// Selection is weighted by each entry's probability value (normalized at runtime).
    /// Wire this to any UnityEvent source such as InputKeyPress or InputTriggerZone.
    /// </summary>
    public void Trigger()
    {
        if (weightedEvents == null || weightedEvents.Length == 0)
        {
            Debug.LogWarning($"[ActionRandomEvent] '{name}': No weighted events defined. Add at least one entry.", this);
            return;
        }

        // Sum all non-negative weights
        float total = 0f;
        for (int i = 0; i < weightedEvents.Length; i++)
        {
            if (weightedEvents[i] != null)
                total += Mathf.Max(0f, weightedEvents[i].probability);
        }

        if (total <= 0f)
        {
            Debug.LogWarning($"[ActionRandomEvent] '{name}': All probability weights are zero. Nothing will fire.", this);
            return;
        }

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < weightedEvents.Length; i++)
        {
            if (weightedEvents[i] == null) continue;
            cumulative += Mathf.Max(0f, weightedEvents[i].probability);
            if (roll < cumulative)
            {
                weightedEvents[i].onSelected?.Invoke();
                return;
            }
        }

        // Floating-point safety: fire the last valid entry as fallback
        for (int i = weightedEvents.Length - 1; i >= 0; i--)
        {
            if (weightedEvents[i] != null && weightedEvents[i].probability > 0f)
            {
                weightedEvents[i].onSelected?.Invoke();
                return;
            }
        }
    }
}
