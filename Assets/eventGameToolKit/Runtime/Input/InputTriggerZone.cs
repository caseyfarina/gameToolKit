using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects when tagged objects enter, exit, or remain in a 3D trigger zone with optional continuous damage.
/// Common use: Damage zones, checkpoints, area triggers, hazard areas, or proximity-based events.
/// </summary>
public class InputTriggerZone : MonoBehaviour
{
    [Header("Trigger Detection")]
    [Tooltip("Tag to detect (e.g. Player, Enemy)")]
    [SerializeField] private string triggerObjectTag = "Player";

    [Header("Stay Event Settings")]
    [Tooltip("Enable the onTriggerStayEvent to fire at regular intervals")]
    [SerializeField] private bool enableStayEvent = false;
    [Tooltip("Time in seconds between each stay event firing (minimum 0.3)")]
    [SerializeField] private float stayInterval = 1f;

    [Header("Events")]
    /// <summary>
    /// Fires when an object with the target tag enters the trigger zone
    /// </summary>
    public UnityEvent onTriggerEnterEvent;
    /// <summary>
    /// Fires at regular intervals while an object with the target tag remains in the trigger zone
    /// </summary>
    public UnityEvent onTriggerStayEvent;
    /// <summary>
    /// Fires when an object with the target tag exits the trigger zone
    /// </summary>
    public UnityEvent onTriggerExitEvent;

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private float lastStayEventTime = 0f;

    private void OnValidate()
    {
        if (stayInterval < 0.3f)
        {
            stayInterval = 0.3f;
        }
    }

    private void OnDisable()
    {
        occupants.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerObjectTag))
        {
            bool wasEmpty = occupants.Count == 0;
            occupants.Add(other);

            if (wasEmpty)
            {
                lastStayEventTime = Time.time;
            }

            onTriggerEnterEvent?.Invoke();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(triggerObjectTag) && enableStayEvent)
        {
            // Clean up any destroyed objects still in the set
            occupants.RemoveWhere(c => c == null);

            if (occupants.Count > 0 && Time.time >= lastStayEventTime + stayInterval)
            {
                lastStayEventTime = Time.time;
                onTriggerStayEvent?.Invoke();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(triggerObjectTag))
        {
            occupants.Remove(other);
            onTriggerExitEvent?.Invoke();
        }
    }

    /// <summary>
    /// Returns true if any object with the target tag is currently in the trigger zone
    /// </summary>
    public bool IsObjectInTrigger
    {
        get
        {
            // Clean up destroyed objects before reporting
            occupants.RemoveWhere(c => c == null);
            return occupants.Count > 0;
        }
    }
}
