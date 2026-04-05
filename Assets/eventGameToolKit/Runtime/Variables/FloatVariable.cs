using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScriptableObject that holds a float value which persists across scene loads.
/// Use this to share data between managers in different scenes (timer, percentage values, etc.).
///
/// RUNTIME BEHAVIOR: The value automatically resets to defaultValue when entering Play mode.
/// During gameplay, the value persists across scene loads until the game is stopped.
///
/// Common use: Timer values, percentage-based stats, smooth health values.
/// </summary>
[CreateAssetMenu(fileName = "NewFloatVariable", menuName = "eventGameToolKit/Variables/Float Variable")]
public class FloatVariable : ScriptableObject
{
    [Header("Default Value")]
    [Tooltip("The starting value when the game begins. Value resets to this when Play mode starts.")]
    [SerializeField] private float defaultValue;

    [Header("Optional Constraints")]
    [Tooltip("If enabled, value cannot go below minValue")]
    [SerializeField] private bool useMinValue = false;
    [SerializeField] private float minValue = 0f;

    [Tooltip("If enabled, value cannot exceed maxValue")]
    [SerializeField] private bool useMaxValue = false;
    [SerializeField] private float maxValue = 100f;

    [Header("Events")]
    /// <summary>
    /// Fires whenever the value changes, passing the new value
    /// </summary>
    public UnityEvent<float> onValueChanged;

    // Runtime value - not serialized, resets each play session
    [System.NonSerialized]
    private float runtimeValue;

    [System.NonSerialized]
    private bool initialized;

    /// <summary>
    /// The current runtime value. Persists across scene loads but resets when Play mode starts.
    /// </summary>
    public float Value
    {
        get
        {
            EnsureInitialized();
            return runtimeValue;
        }
        set
        {
            EnsureInitialized();
            float newValue = ClampValue(value);
            if (!Mathf.Approximately(runtimeValue, newValue))
            {
                runtimeValue = newValue;
                onValueChanged?.Invoke(runtimeValue);
            }
        }
    }

    /// <summary>
    /// The default starting value configured in the Inspector.
    /// </summary>
    public float DefaultValue => defaultValue;

    /// <summary>
    /// Add to the current value.
    /// </summary>
    /// <param name="amount">Amount to add (can be negative)</param>
    public void Add(float amount)
    {
        Value += amount;
    }

    /// <summary>
    /// Subtract from the current value.
    /// </summary>
    /// <param name="amount">Amount to subtract</param>
    public void Subtract(float amount)
    {
        Value -= amount;
    }

    /// <summary>
    /// Set the value directly.
    /// </summary>
    /// <param name="newValue">The new value</param>
    public void SetValue(float newValue)
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

    /// <summary>
    /// Get the value as a normalized percentage (0-1) between min and max.
    /// Requires useMinValue and useMaxValue to be enabled.
    /// </summary>
    public float GetNormalized()
    {
        if (!useMinValue || !useMaxValue || Mathf.Approximately(maxValue, minValue))
            return 0f;

        return Mathf.InverseLerp(minValue, maxValue, Value);
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            runtimeValue = ClampValue(defaultValue);
            initialized = true;
        }
    }

    private float ClampValue(float value)
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
