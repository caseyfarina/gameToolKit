using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScriptableObject that holds an integer value which persists across scene loads.
/// Use this to share data between managers in different scenes (health, score, lives, etc.).
///
/// RUNTIME BEHAVIOR: The value automatically resets to defaultValue when entering Play mode.
/// During gameplay, the value persists across scene loads until the game is stopped.
///
/// Common use: Player health, score, collectible counts, lives remaining.
/// </summary>
[CreateAssetMenu(fileName = "NewIntVariable", menuName = "eventGameToolKit/Variables/Int Variable")]
public class IntVariable : ScriptableObject
{
    [Header("Default Value")]
    [Tooltip("The starting value when the game begins. Value resets to this when Play mode starts.")]
    [SerializeField] private int defaultValue;

    [Header("Optional Constraints")]
    [Tooltip("If enabled, value cannot go below minValue")]
    [SerializeField] private bool useMinValue = false;
    [SerializeField] private int minValue = 0;

    [Tooltip("If enabled, value cannot exceed maxValue")]
    [SerializeField] private bool useMaxValue = false;
    [SerializeField] private int maxValue = 100;

    [Header("Events")]
    /// <summary>
    /// Fires whenever the value changes, passing the new value
    /// </summary>
    public UnityEvent<int> onValueChanged;

    // Runtime value - not serialized, resets each play session
    [System.NonSerialized]
    private int runtimeValue;

    [System.NonSerialized]
    private bool initialized;

    /// <summary>
    /// The current runtime value. Persists across scene loads but resets when Play mode starts.
    /// </summary>
    public int Value
    {
        get
        {
            EnsureInitialized();
            return runtimeValue;
        }
        set
        {
            EnsureInitialized();
            int newValue = ClampValue(value);
            if (runtimeValue != newValue)
            {
                runtimeValue = newValue;
                onValueChanged?.Invoke(runtimeValue);
            }
        }
    }

    /// <summary>
    /// The default starting value configured in the Inspector.
    /// </summary>
    public int DefaultValue => defaultValue;

    /// <summary>
    /// Add to the current value.
    /// </summary>
    /// <param name="amount">Amount to add (can be negative)</param>
    public void Add(int amount)
    {
        Value += amount;
    }

    /// <summary>
    /// Subtract from the current value.
    /// </summary>
    /// <param name="amount">Amount to subtract</param>
    public void Subtract(int amount)
    {
        Value -= amount;
    }

    /// <summary>
    /// Set the value directly.
    /// </summary>
    /// <param name="newValue">The new value</param>
    public void SetValue(int newValue)
    {
        Value = newValue;
    }

    /// <summary>
    /// Reset to the default value. Call this for "New Game" or full restart.
    /// </summary>
    public void ResetToDefault()
    {
        Value = defaultValue;
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            runtimeValue = ClampValue(defaultValue);
            initialized = true;
        }
    }

    private int ClampValue(int value)
    {
        if (useMinValue && value < minValue)
            return minValue;
        if (useMaxValue && value > maxValue)
            return maxValue;
        return value;
    }

    private void OnEnable()
    {
        // Reset initialization flag when entering Play mode or when asset is loaded
        initialized = false;
    }

    private void OnValidate()
    {
        // Ensure min <= max in editor
        if (useMinValue && useMaxValue && minValue > maxValue)
        {
            maxValue = minValue;
        }
    }
}
