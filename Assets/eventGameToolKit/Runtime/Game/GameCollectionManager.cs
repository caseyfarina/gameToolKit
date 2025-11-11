using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks a numeric value (score, coins, items) with threshold-based event triggers.
/// Does NOT handle display - wire onValueChanged to GameUIManager for visual updates.
/// Common use: Score systems, collectible counters, resource tracking, or objective progress meters.
/// </summary>
public class GameCollectionManager : MonoBehaviour
{
    [Header("Value Settings")]
    [Tooltip("Current value (score, coins, items, etc.)")]
    [SerializeField] private int currentValue = 0;

    [Tooltip("Minimum allowed value (0 = no minimum)")]
    [SerializeField] private int minValue = 0;

    [Tooltip("Maximum allowed value (0 = no maximum)")]
    [SerializeField] private int maxValue = 0;

    [Header("Threshold Settings")]
    [Tooltip("Threshold value for event triggers")]
    [SerializeField] private int threshold = 10;

    [Header("Events")]
    [Tooltip("Fires ONCE when value crosses threshold going UP (below threshold → at/above threshold)\nExample: Score goes from 99 to 100")]
    /// <summary>
    /// Fires when the value crosses the threshold going UP (from below to at/above threshold)
    /// </summary>
    public UnityEvent onCountUpToThreshold;

    [Tooltip("Fires ONCE when value crosses threshold going DOWN (at/above threshold → below threshold)\nExample: Ammo goes from 1 to 0")]
    /// <summary>
    /// Fires when the value crosses the threshold going DOWN (from at/above to below threshold)
    /// </summary>
    public UnityEvent onCountDownToThreshold;

    [Tooltip("Fires every time the value changes, passes new value as parameter")]
    /// <summary>
    /// Fires whenever the collection value changes, passing the new value as an int parameter
    /// </summary>
    public UnityEvent<int> onValueChanged;

    [Header("Limit Events")]
    [Tooltip("Fires when value reaches maximum limit")]
    /// <summary>
    /// Fires when the value reaches the maximum limit
    /// </summary>
    public UnityEvent onMaxReached;

    [Tooltip("Fires when value reaches minimum limit")]
    /// <summary>
    /// Fires when the value reaches the minimum limit
    /// </summary>
    public UnityEvent onMinReached;

    private bool wasAboveThreshold = false;

    private void Start()
    {
        // Initialize threshold state based on starting value
        wasAboveThreshold = currentValue >= threshold;

        // Fire initial value event to update UI on start
        onValueChanged.Invoke(currentValue);
    }

    /// <summary>
    /// Increases the collection value by the specified amount
    /// </summary>
    public void Increment(int amount = 1)
    {
        int previousValue = currentValue;
        currentValue += amount;

        // Enforce maximum limit
        if (maxValue > 0 && currentValue > maxValue)
        {
            currentValue = maxValue;
        }

        // Check if we hit the max
        if (maxValue > 0 && currentValue >= maxValue && previousValue < maxValue)
        {
            onMaxReached.Invoke();
        }

        onValueChanged.Invoke(currentValue);
        CheckThreshold();
    }

    /// <summary>
    /// Decreases the collection value by the specified amount
    /// </summary>
    public void Decrement(int amount = 1)
    {
        int previousValue = currentValue;
        currentValue -= amount;

        // Enforce minimum limit
        if (currentValue < minValue)
        {
            currentValue = minValue;
        }

        // Check if we hit the min
        if (currentValue <= minValue && previousValue > minValue)
        {
            onMinReached.Invoke();
        }

        onValueChanged.Invoke(currentValue);
        CheckThreshold();
    }

    /// <summary>
    /// Get current collection value for saving/loading
    /// </summary>
    public int GetCurrentValue()
    {
        return currentValue;
    }

    /// <summary>
    /// Set collection value directly (for checkpoint restoration)
    /// </summary>
    public void SetValue(int newValue)
    {
        currentValue = newValue;
        onValueChanged.Invoke(currentValue);
        CheckThreshold();
    }

    private void CheckThreshold()
    {
        bool isAtOrAboveThreshold = currentValue >= threshold;

        // Crossed upward: was below, now at or above
        if (isAtOrAboveThreshold && !wasAboveThreshold)
        {
            onCountUpToThreshold.Invoke();
        }
        // Crossed downward: was above, now below
        else if (!isAtOrAboveThreshold && wasAboveThreshold)
        {
            onCountDownToThreshold.Invoke();
        }

        // Update state for next check
        wasAboveThreshold = isAtOrAboveThreshold;
    }
}

