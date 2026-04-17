using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reacts to a named flag in a GameFlagManager. Place this on any object that needs to
/// reflect flag state — a door that should open if already unlocked, a pickup that should
/// stay gone if already collected, an NPC that should show different dialogue if already met.
///
/// On Start, fires onFlagAlreadySet or onFlagNotSet based on the current flag state.
/// At runtime, fires onFlagBecameSet or onFlagBecameCleared when the flag changes.
///
/// Common use: restoring door/pickup/NPC state after a scene load.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class GameFlagListener : MonoBehaviour
{
    [Tooltip("The GameFlagManager that owns the flag to watch")]
    [SerializeField] private GameFlagManager flagManager;

    [Tooltip("Name of the flag to watch (must match exactly what is passed to GameFlagManager.SetFlag)")]
    [SerializeField] private string flagName;

    [Header("On Scene Load")]
    /// <summary>Fires on Start if the flag is already set (e.g. restore door-open state).</summary>
    public UnityEvent onFlagAlreadySet;

    /// <summary>Fires on Start if the flag is not yet set (e.g. show collectible).</summary>
    public UnityEvent onFlagNotSet;

    [Header("At Runtime")]
    /// <summary>Fires immediately when the flag becomes set during play.</summary>
    public UnityEvent onFlagBecameSet;

    /// <summary>Fires immediately when the flag is cleared during play.</summary>
    public UnityEvent onFlagBecameCleared;

    private void OnEnable()
    {
        GameFlagManager.OnFlagChanged += HandleFlagChanged;
    }

    private void OnDisable()
    {
        GameFlagManager.OnFlagChanged -= HandleFlagChanged;
    }

    private void Start()
    {
        if (flagManager == null)
        {
            Debug.LogWarning($"[GameFlagListener] No GameFlagManager assigned on '{gameObject.name}'. Assign one in the Inspector.", this);
            return;
        }

        if (string.IsNullOrEmpty(flagName))
        {
            Debug.LogWarning($"[GameFlagListener] Flag Name is empty on '{gameObject.name}'. Enter the flag name to watch.", this);
            return;
        }

        if (flagManager.HasFlag(flagName))
            onFlagAlreadySet.Invoke();
        else
            onFlagNotSet.Invoke();
    }

    private void HandleFlagChanged(string changedFlag, bool isSet)
    {
        if (changedFlag != flagName) return;

        if (isSet)
            onFlagBecameSet.Invoke();
        else
            onFlagBecameCleared.Invoke();
    }
}
