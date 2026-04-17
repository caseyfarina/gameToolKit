using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Stores named boolean flags that automatically persist across scene loads — no setup required.
/// Use this to remember one-time game events: a door was opened, a power-up was collected,
/// an NPC was already talked to. Pair with GameFlagListener to react to flags in other objects.
/// Flags reset at the start of each play session and can optionally reset on RestartScene.
///
/// Common use: one-time pickups, opened doors, triggered cutscenes, unlocked shortcuts.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class GameFlagManager : MonoBehaviour
{
    [Header("On Restart")]
    [Tooltip("What happens to flags when the scene is loaded via RestartScene. Keep Value means flags survive death — opened doors stay open, collected items stay gone. Reset To Default clears all flags so the scene resets fully.")]
    [SerializeField] private RestartBehavior onRestartBehavior = RestartBehavior.KeepValue;

    [Header("Events")]
    /// <summary>Fires when any flag is set, passing the flag name.</summary>
    public UnityEvent<string> onFlagSet;

    /// <summary>Fires when any flag is cleared, passing the flag name.</summary>
    public UnityEvent<string> onFlagCleared;

    // ── Static notification for GameFlagListeners ─────────────────────
    // Any listener in the scene can subscribe to be notified when a flag changes.
    internal static event System.Action<string, bool> OnFlagChanged;

    private void Start()
    {
        if (GameData.IsRestart && onRestartBehavior == RestartBehavior.ResetToDefault)
            GameData.Instance.ClearAllFlags();
    }

    // ──────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets a named flag. Wire this to any UnityEvent to mark that something happened.
    /// Listeners watching this flag name will be notified immediately.
    /// </summary>
    public void SetFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        GameData.Instance.SetFlag(flagName);
        OnFlagChanged?.Invoke(flagName, true);
        onFlagSet.Invoke(flagName);
    }

    /// <summary>
    /// Clears a named flag. Use this to "un-mark" something (e.g. respawn a collectible).
    /// </summary>
    public void ClearFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        GameData.Instance.ClearFlag(flagName);
        OnFlagChanged?.Invoke(flagName, false);
        onFlagCleared.Invoke(flagName);
    }

    /// <summary>
    /// Returns true if the named flag has been set this session.
    /// Use GameFlagListener for no-code conditional reactions.
    /// </summary>
    public bool HasFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return false;
        return GameData.Instance.HasFlag(flagName);
    }

    /// <summary>
    /// Clears all flags. Wire this to a reset or new-game event.
    /// </summary>
    public void ClearAllFlags()
    {
        GameData.Instance.ClearAllFlags();
    }
}
