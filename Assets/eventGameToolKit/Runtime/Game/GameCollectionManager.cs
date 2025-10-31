using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Tracks and displays a numeric value (score, coins, items) with threshold-based event triggers.
/// Common use: Score systems, collectible counters, resource tracking, or objective progress meters.
/// </summary>
public class GameCollectionManager : MonoBehaviour
{ 
    [SerializeField] private int currentValue = 0;
    [SerializeField] private int threshold = 10;
    [SerializeField] private TextMeshProUGUI displayText;

    /// <summary>
    /// Fires when the collection count reaches or exceeds the threshold value
    /// </summary>
    public UnityEvent onThresholdReached;
    /// <summary>
    /// Fires whenever the collection value changes
    /// </summary>
    public UnityEvent onValueChanged;

    private void Start()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Increases the collection value by the specified amount
    /// </summary>
    public void Increment(int amount = 1)
    {
        currentValue += amount;
        UpdateDisplay();
        CheckThreshold();
    }

    /// <summary>
    /// Decreases the collection value by the specified amount
    /// </summary>
    public void Decrement(int amount = 1)
    {
        currentValue -= amount;
        UpdateDisplay();
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
        UpdateDisplay();
        CheckThreshold();
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            displayText.text = currentValue.ToString();
        }
        onValueChanged.Invoke();
    }

    private void CheckThreshold()
    {
        if (currentValue >= threshold)
        {
            onThresholdReached.Invoke();
        }
    }
}

