using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects when tagged objects enter, exit, or remain in a trigger zone, with optional
/// repeating stay events.
///
/// Works in both 2D and 3D. Give the zone a Collider for a 3D game or a Collider2D for a
/// 2D game — the component responds to whichever one is attached, with no setting to change.
///
/// Common use: Damage zones, checkpoints, area triggers, hazard areas, or proximity-based events.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
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

    // Holds Collider or Collider2D; both derive from Component, so one set serves both.
    private readonly HashSet<Component> occupants = new HashSet<Component>();
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
        // Physics stops delivering enter/exit while this is disabled, and re-enabling does
        // not replay an enter for anything still overlapping. Rather than leave listeners
        // believing an object is still inside, treat disabling as everyone leaving: fire
        // exit once if the zone was occupied, then clear.
        bool wasOccupied = occupants.Count > 0;
        occupants.Clear();

        if (wasOccupied)
        {
            onTriggerExitEvent?.Invoke();
        }
    }

    // Unity dispatches the 3D and 2D callbacks independently: a zone with a Collider only
    // ever hears the 3D pair, one with a Collider2D only ever hears the 2D pair. Both
    // delegate to the same handlers, so behaviour is identical in either dimension.

    private void OnTriggerEnter(Collider other) => HandleEnter(other, other.tag);
    private void OnTriggerEnter2D(Collider2D other) => HandleEnter(other, other.tag);

    private void OnTriggerExit(Collider other) => HandleExit(other, other.tag);
    private void OnTriggerExit2D(Collider2D other) => HandleExit(other, other.tag);

    private void HandleEnter(Component other, string otherTag)
    {
        if (otherTag != triggerObjectTag) return;

        bool wasEmpty = occupants.Count == 0;
        occupants.Add(other);

        if (wasEmpty)
        {
            lastStayEventTime = Time.time;
        }

        onTriggerEnterEvent?.Invoke();
    }

    // Stay events are driven from Update rather than OnTriggerStay, because physics
    // stops delivering stay callbacks once a body falls asleep — which a stationary
    // player does within a second. Relying on those callbacks meant a damage-over-time
    // zone quietly stopped damaging anyone who stood still. Tracking occupancy from
    // enter/exit and running the timer ourselves fires reliably in both 2D and 3D.
    private void Update()
    {
        if (!enableStayEvent) return;

        // Drop occupants destroyed while inside the zone
        occupants.RemoveWhere(c => c == null);

        if (occupants.Count == 0) return;
        if (Time.time < lastStayEventTime + stayInterval) return;

        lastStayEventTime = Time.time;
        onTriggerStayEvent?.Invoke();
    }

    private void HandleExit(Component other, string otherTag)
    {
        if (otherTag != triggerObjectTag) return;

        // Only report an exit for something being tracked. Without this, an object that
        // leaves after the zone was disabled and re-enabled fires a second, phantom exit
        // that never had a matching enter.
        if (!occupants.Remove(other)) return;

        onTriggerExitEvent?.Invoke();
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
