using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls what happens to a manager's persisted value when a scene is loaded as a restart.
/// </summary>
public enum RestartBehavior
{
    /// <summary>Clear the persisted value so the manager starts from its Inspector default.</summary>
    ResetToDefault,
    /// <summary>Keep the persisted value even on restart (e.g. permanent store upgrades).</summary>
    KeepValue
}

/// <summary>
/// Internal runtime store for cross-scene persistent values.
/// Auto-created on play start — students never create or configure this directly.
///
/// Slot layout (internal, managed automatically):
///   Int  slot  0 : GameHealthManager (health)
///   Int  slot  1 : GameCollectionManager (score / collection value)
///   Int  slots 2–21 : GameInventoryManager (up to 20 inventory item counts)
///   Float slot 0 : reserved for future use (e.g. GameTimerManager)
///
/// Values reset to the manager's own Inspector defaults at the start of each play session.
/// During gameplay they survive every LoadScene call.
/// </summary>
public class GameData : ScriptableObject
{
    // ── Hardwired slot assignments (internal use only) ────────────────
    internal const int HEALTH_SLOT          = 0;
    internal const int COLLECTION_SLOT      = 1;
    internal const int INVENTORY_SLOT_START = 2;
    internal const int INVENTORY_SLOT_COUNT = 20;   // slots 2–21
    internal const int STORE_SLOT_START     = 22;
    internal const int STORE_SLOT_COUNT     = 20;   // slots 22–41
    internal const int TIMER_FLOAT_SLOT     = 0;

    private const int INT_CAPACITY   = 100;
    private const int FLOAT_CAPACITY = 100;

    // ── Singleton ─────────────────────────────────────────────────────
    private static GameData _instance;
    private static bool _isRestart;

    /// <summary>Internal accessor used by the manager scripts.</summary>
    internal static GameData Instance
    {
        get
        {
            if (_instance == null)
                CreateInstance();
            return _instance;
        }
    }

    /// <summary>True when the current scene was loaded via RestartScene — managers use this to decide whether to restore or reset.</summary>
    internal static bool IsRestart => _isRestart;

    /// <summary>Set by GameSceneManager before loading — true for restarts, false for progression.</summary>
    internal static void SetIsRestart(bool value) { _isRestart = value; }

    // Reset the singleton at the start of every play session so values
    // return to each manager's own Inspector defaults.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        _instance  = null;
        _isRestart = false;
    }

    private static void CreateInstance()
    {
        _instance = ScriptableObject.CreateInstance<GameData>();
        _instance.hideFlags = HideFlags.HideAndDontSave; // invisible in Project window
        _instance.Initialize();
    }

    // ── Runtime storage ───────────────────────────────────────────────
    [System.NonSerialized] private int[]   runtimeInts;
    [System.NonSerialized] private bool[]  intSet;       // true once a manager has written to a slot
    [System.NonSerialized] private float[] runtimeFloats;
    [System.NonSerialized] private bool[]  floatSet;
    [System.NonSerialized] private HashSet<string> _flags;

    private void Initialize()
    {
        runtimeInts   = new int[INT_CAPACITY];
        intSet        = new bool[INT_CAPACITY];
        runtimeFloats = new float[FLOAT_CAPACITY];
        floatSet      = new bool[FLOAT_CAPACITY];
        _flags        = new HashSet<string>();
    }

    // ── Integer API (internal) ────────────────────────────────────────

    /// <summary>
    /// Returns true if a manager has already written a value to this slot this session.
    /// Used to decide whether to use the persisted value or the manager's Inspector default.
    /// </summary>
    internal bool IsIntSet(int slot)
    {
        return slot >= 0 && slot < INT_CAPACITY && intSet[slot];
    }

    /// <summary>
    /// Read an integer slot. Returns the stored value if it has been set this session,
    /// otherwise returns <paramref name="defaultValue"/> (the manager's own Inspector default).
    /// </summary>
    internal int GetInt(int slot, int defaultValue = 0)
    {
        if (slot < 0 || slot >= INT_CAPACITY) return defaultValue;
        return intSet[slot] ? runtimeInts[slot] : defaultValue;
    }

    /// <summary>
    /// Write an integer slot and mark it as set.
    /// </summary>
    internal void SetInt(int slot, int value)
    {
        if (slot < 0 || slot >= INT_CAPACITY) return;
        runtimeInts[slot] = value;
        intSet[slot]      = true;
    }

    /// <summary>
    /// Clear an integer slot so the next read returns the manager's Inspector default.
    /// </summary>
    internal void ClearInt(int slot)
    {
        if (slot < 0 || slot >= INT_CAPACITY) return;
        runtimeInts[slot] = 0;
        intSet[slot]      = false;
    }

    // ── Flag API (internal) ───────────────────────────────────────────

    /// <summary>Returns true if the named flag has been set this session.</summary>
    internal bool HasFlag(string flagName) => _flags.Contains(flagName);

    /// <summary>Sets a named flag.</summary>
    internal void SetFlag(string flagName) => _flags.Add(flagName);

    /// <summary>Clears a named flag.</summary>
    internal void ClearFlag(string flagName) => _flags.Remove(flagName);

    /// <summary>Clears all flags (used on restart).</summary>
    internal void ClearAllFlags() => _flags.Clear();

    // ── Float API (internal) ──────────────────────────────────────────

    /// <summary>
    /// Returns true if a value has been written to this float slot this session.
    /// </summary>
    internal bool IsFloatSet(int slot)
    {
        return slot >= 0 && slot < FLOAT_CAPACITY && floatSet[slot];
    }

    /// <summary>
    /// Read a float slot. Returns the stored value if set, otherwise <paramref name="defaultValue"/>.
    /// </summary>
    internal float GetFloat(int slot, float defaultValue = 0f)
    {
        if (slot < 0 || slot >= FLOAT_CAPACITY) return defaultValue;
        return floatSet[slot] ? runtimeFloats[slot] : defaultValue;
    }

    /// <summary>
    /// Write a float slot and mark it as set.
    /// </summary>
    internal void SetFloat(int slot, float value)
    {
        if (slot < 0 || slot >= FLOAT_CAPACITY) return;
        runtimeFloats[slot] = value;
        floatSet[slot]      = true;
    }
}
