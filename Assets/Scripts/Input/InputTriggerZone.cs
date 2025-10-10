using System.Collections;
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
    public string triggerObjectTag = "Player";

    [Header("Stay Event Settings")]
    [SerializeField] private bool enableStayEvent = false;
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

    private bool objectInTrigger = false;
    private float lastStayEventTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerObjectTag))
        {
            objectInTrigger = true;
            lastStayEventTime = Time.time;
            onTriggerEnterEvent?.Invoke();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(triggerObjectTag) && enableStayEvent)
        {
            if (Time.time >= lastStayEventTime + stayInterval)
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
            objectInTrigger = false;
            onTriggerExitEvent?.Invoke();
        }
    }

    public bool IsObjectInTrigger => objectInTrigger;
}
