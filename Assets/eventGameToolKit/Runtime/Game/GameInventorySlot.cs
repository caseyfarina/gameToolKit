using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a single inventory slot with capacity limits, triggering events when full or empty.
/// Does NOT handle display - wire onValueChanged to GameUIManager for visual updates.
/// Common use: Item storage systems, ammunition counters, key collections, or resource pouches.
/// </summary>
public class GameInventorySlot : MonoBehaviour
{
    [Header("Inventory Slot")]
    [SerializeField] private string itemType = "Item";
    [SerializeField] private int maxCapacity = 10;
    [SerializeField] private int currentValue = 0;

    [Header("Inventory Events")]
    /// <summary>
    /// Fires when an item is successfully used from the inventory
    /// </summary>
    public UnityEvent onItemUsed;
    /// <summary>
    /// Fires when the inventory slot becomes empty (count reaches zero)
    /// </summary>
    public UnityEvent onSlotEmpty;
    /// <summary>
    /// Fires when the inventory slot becomes full (count reaches max capacity)
    /// </summary>
    public UnityEvent onSlotFull;
    /// <summary>
    /// Fires whenever the item count changes, passing item type and count as parameters
    /// </summary>
    public UnityEvent<string, int> onValueChanged;

    public string ItemType => itemType;
    public int MaxCapacity => maxCapacity;
    public int CurrentValue => currentValue;
    public bool IsEmpty => currentValue <= 0;
    public bool IsFull => currentValue >= maxCapacity;

    private void Start()
    {
        // Fire initial value event to update UI on start
        onValueChanged.Invoke(itemType, currentValue);
    }

    /// <summary>
    /// Adds items to the inventory slot up to max capacity
    /// </summary>
    public void Increment(int amount = 1)
    {
        int previousValue = currentValue;
        int newValue = Mathf.Clamp(currentValue + amount, 0, maxCapacity);

        if (newValue != currentValue)
        {
            currentValue = newValue;
            onValueChanged.Invoke(itemType, currentValue);

            if (currentValue >= maxCapacity && previousValue < maxCapacity)
            {
                onSlotFull.Invoke();
            }
        }
    }

    /// <summary>
    /// Removes items from the inventory slot down to zero
    /// </summary>
    public void Decrement(int amount = 1)
    {
        int previousValue = currentValue;
        currentValue = Mathf.Max(0, currentValue - amount);
        onValueChanged.Invoke(itemType, currentValue);

        if (currentValue <= 0 && previousValue > 0)
        {
            onSlotEmpty.Invoke();
        }
    }

    /// <summary>
    /// Uses the specified number of items if available, triggering the onItemUsed event
    /// </summary>
    public void UseItem(int amount = 1)
    {
        if (currentValue >= amount)
        {
            Decrement(amount);
            onItemUsed.Invoke();
        }
    }

    /// <summary>
    /// Checks if the specified number of items can be added without exceeding capacity
    /// </summary>
    public bool CanAddItem(int amount = 1)
    {
        return currentValue + amount <= maxCapacity;
    }

    /// <summary>
    /// Changes the item type name for this inventory slot
    /// </summary>
    public void SetItemType(string newItemType)
    {
        itemType = newItemType;
    }

    /// <summary>
    /// Changes the maximum capacity for this inventory slot
    /// </summary>
    public void SetMaxCapacity(int newCapacity)
    {
        maxCapacity = Mathf.Max(1, newCapacity);
        if (currentValue > maxCapacity)
        {
            currentValue = maxCapacity;
            onValueChanged.Invoke(itemType, currentValue);
        }
    }
}