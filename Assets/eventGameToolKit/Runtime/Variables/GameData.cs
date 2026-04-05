using UnityEngine;

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
    internal const int TIMER_FLOAT_SLOT     = 0;

    private const int INT_CAPACITY   = 100;
    private const int FLOAT_CAPACITY = 100;

    // ── Singleton ─────────────────────────────────────────────────────
    private static GameData _instance;

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

    // Reset the singleton at the start of every play session so values
    // return to each manager's own Inspector defaults.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        _instance = null;
    }

    private static void CreateInstance()
    {
        _instance = ScriptableObject.CreateInstance<GameData>();
        _instance.hideFlags = HideFlags.HideAndDontSave; // invisible in Project window
        _instance.Initialize();
    }

    // ── Runtime storage ───────────────────────────────────────────────
    [System.NonSerialized] private int[]  runtimeInts;
    [System.NonSerialized] private bool[] intSet;       // true once a manager has written to a slot
    [System.NonSerialized] private float[] runtimeFloats;
    [System.NonSerialized] private bool[] floatSet;

    private void Initialize()
    {
        runtimeInts   = new int[INT_CAPACITY];
        intSet        = new bool[INT_CAPACITY];
        runtimeFloats = new float[FLOAT_CAPACITY];
        floatSet      = new bool[FLOAT_CAPACITY];
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
