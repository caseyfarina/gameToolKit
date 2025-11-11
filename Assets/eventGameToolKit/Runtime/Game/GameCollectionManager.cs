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
    [SerializeField] private int currentValue = 0;
    [SerializeField] private int threshold = 10;

    /// <summary>
    /// Fires when the collection count reaches or exceeds the threshold value
    /// </summary>
    public UnityEvent onThresholdReached;
    /// <summary>
    /// Fires whenever the collection value changes, passing the new value as an int parameter
    /// </summary>
    public UnityEvent<int> onValueChanged;

    /// <summary>
    /// Increases the collection value by the specified amount
    /// </summary>
    public void Increment(int amount = 1)
    {
        currentValue += amount;
        onValueChanged.Invoke(currentValue);
        CheckThreshold();
    }

    /// <summary>
    /// Decreases the collection value by the specified amount
    /// </summary>
    public void Decrement(int amount = 1)
    {
        currentValue -= amount;
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
        if (currentValue >= threshold)
        {
            onThresholdReached.Invoke();
        }
    }
}

