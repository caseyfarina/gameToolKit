using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Timeline event data structure containing a trigger time and associated UnityEvent
/// </summary>
[System.Serializable]
public struct TimelineEvent
{
    [Tooltip("Name for this event (optional, for organization in Inspector)")]
    public string eventName;

    [Tooltip("Time in seconds when this event should trigger (relative to sequence start)")]
    public float triggerTime;

    /// <summary>
    /// Event that fires at the specified trigger time
    /// </summary>
    public UnityEvent onTrigger;
}

/// <summary>
/// Triggers a sequence of timed events with configurable duration and looping.
/// Common use: Cutscenes, timed puzzle sequences, wave spawners, tutorial sequences, or choreographed events.
/// </summary>
public class ActionEventSequencer : MonoBehaviour
{
    [Header("Timeline Settings")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool playOnStart = false;

    [Header("Timeline Events")]
    [Tooltip("Array of events to trigger at specific times during the sequence")]
    [SerializeField] private TimelineEvent[] events = new TimelineEvent[0];

    [Header("Sequence Events")]
    /// <summary>
    /// Fires when the sequence starts or restarts
    /// </summary>
    public UnityEvent onSequenceStart;

    /// <summary>
    /// Fires when the sequence completes (only for non-looping sequences)
    /// </summary>
    public UnityEvent onSequenceComplete;

    /// <summary>
    /// Fires when the sequence loops back to the beginning
    /// </summary>
    public UnityEvent onSequenceLoop;

    /// <summary>
    /// Fires when the sequence is manually stopped
    /// </summary>
    public UnityEvent onSequenceStopped;

    /// <summary>
    /// Fires when the sequence is paused
    /// </summary>
    public UnityEvent onSequencePaused;

    /// <summary>
    /// Fires when the sequence is resumed from pause
    /// </summary>
    public UnityEvent onSequenceResumed;

    private Coroutine sequenceCoroutine;
    private bool isPlaying = false;
    private bool isPaused = false;
    private float currentTime = 0f;
    private HashSet<int> triggeredEventsThisLoop = new HashSet<int>();
    private List<TimelineEvent> sortedEvents = new List<TimelineEvent>();

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
    public float CurrentTime => currentTime;
    public float Progress => duration > 0 ? Mathf.Clamp01(currentTime / duration) : 0f;

    private void Start()
    {
        // Sort events by trigger time for efficient playback
        SortEvents();

        if (playOnStart)
        {
            StartSequence();
        }
    }

    /// <summary>
    /// Sorts the timeline events by trigger time for efficient processing
    /// </summary>
    private void SortEvents()
    {
        sortedEvents.Clear();
        sortedEvents.AddRange(events);
        sortedEvents.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
    }

    /// <summary>
    /// Starts the event sequence from the beginning
    /// </summary>
    public void StartSequence()
    {
        if (isPlaying)
        {
            StopSequence();
        }

        currentTime = 0f;
        isPlaying = true;
        isPaused = false;
        triggeredEventsThisLoop.Clear();

        onSequenceStart?.Invoke();

        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    /// <summary>
    /// Stops the sequence and resets to the beginning
    /// </summary>
    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        isPlaying = false;
        isPaused = false;
        currentTime = 0f;
        triggeredEventsThisLoop.Clear();

        onSequenceStopped?.Invoke();
    }

    /// <summary>
    /// Restarts the sequence from the beginning (same as StartSequence)
    /// </summary>
    public void RestartSequence()
    {
        StartSequence();
    }

    /// <summary>
    /// Pauses the sequence at the current time
    /// </summary>
    public void PauseSequence()
    {
        if (isPlaying && !isPaused)
        {
            isPaused = true;
            onSequencePaused?.Invoke();
        }
    }

    /// <summary>
    /// Resumes the sequence from the paused time
    /// </summary>
    public void ResumeSequence()
    {
        if (isPlaying && isPaused)
        {
            isPaused = false;
            onSequenceResumed?.Invoke();
        }
    }

    /// <summary>
    /// Enables or disables looping for the sequence
    /// </summary>
    public void EnableLoop(bool enable)
    {
        loop = enable;
    }

    /// <summary>
    /// Sets the total duration of the sequence in seconds
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = Mathf.Max(0f, newDuration);
    }

    /// <summary>
    /// Main coroutine that plays the sequence timeline
    /// </summary>
    private IEnumerator PlaySequence()
    {
        while (isPlaying)
        {
            // Wait if paused
            while (isPaused)
            {
                yield return null;
            }

            // Update current time
            currentTime += Time.deltaTime;

            // Check and trigger events
            CheckAndTriggerEvents();

            // Check if sequence has completed
            if (currentTime >= duration)
            {
                if (loop)
                {
                    // Loop back to start
                    currentTime = 0f;
                    triggeredEventsThisLoop.Clear();
                    onSequenceLoop?.Invoke();
                }
                else
                {
                    // Sequence complete
                    isPlaying = false;
                    onSequenceComplete?.Invoke();
                    yield break;
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Checks timeline and triggers events that should fire at current time
    /// </summary>
    private void CheckAndTriggerEvents()
    {
        for (int i = 0; i < sortedEvents.Count; i++)
        {
            TimelineEvent timelineEvent = sortedEvents[i];

            // Skip if already triggered this loop
            if (triggeredEventsThisLoop.Contains(i))
                continue;

            // Skip events beyond current time
            if (timelineEvent.triggerTime > currentTime)
                break; // Since sorted, no need to check further

            // Skip events beyond duration
            if (timelineEvent.triggerTime > duration)
                continue;

            // Trigger event
            timelineEvent.onTrigger?.Invoke();
            triggeredEventsThisLoop.Add(i);
        }
    }

    /// <summary>
    /// Manually triggers a specific event by index
    /// </summary>
    public void TriggerEventByIndex(int index)
    {
        if (index >= 0 && index < events.Length)
        {
            events[index].onTrigger?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Event index {index} is out of range. Total events: {events.Length}");
        }
    }

    /// <summary>
    /// Manually triggers all events with a specific name
    /// </summary>
    public void TriggerEventByName(string eventName)
    {
        bool foundAny = false;
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i].eventName == eventName)
            {
                events[i].onTrigger?.Invoke();
                foundAny = true;
            }
        }

        if (!foundAny)
        {
            Debug.LogWarning($"No events found with name '{eventName}'");
        }
    }

    /// <summary>
    /// Returns the total number of events in the sequence
    /// </summary>
    public int GetEventCount()
    {
        return events.Length;
    }

    /// <summary>
    /// Returns how many events have been triggered in the current loop
    /// </summary>
    public int GetTriggeredEventCount()
    {
        return triggeredEventsThisLoop.Count;
    }

    private void OnDrawGizmosSelected()
    {
        if (events == null || events.Length == 0 || duration <= 0)
            return;

        // Draw timeline visualization
        Gizmos.color = Color.cyan;
        Vector3 basePos = transform.position;

        // Draw timeline bar
        float timelineLength = 3f;
        Vector3 startPos = basePos + Vector3.left * (timelineLength * 0.5f);
        Vector3 endPos = basePos + Vector3.right * (timelineLength * 0.5f);
        Gizmos.DrawLine(startPos, endPos);

        // Draw start and end markers
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPos, 0.1f);

        // Draw event markers
        Gizmos.color = Color.yellow;
        foreach (TimelineEvent timelineEvent in events)
        {
            if (timelineEvent.triggerTime >= 0 && timelineEvent.triggerTime <= duration)
            {
                float normalizedTime = timelineEvent.triggerTime / duration;
                Vector3 eventPos = Vector3.Lerp(startPos, endPos, normalizedTime);
                Gizmos.DrawWireSphere(eventPos, 0.08f);

                // Draw vertical line down to base
                Gizmos.DrawLine(eventPos, eventPos + Vector3.down * 0.3f);
            }
        }

        // Draw current time marker if playing
        if (Application.isPlaying && isPlaying)
        {
            Gizmos.color = isPaused ? Color.yellow : Color.white;
            float normalizedCurrentTime = Mathf.Clamp01(currentTime / duration);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, normalizedCurrentTime);
            Gizmos.DrawWireSphere(currentPos, 0.12f);
            Gizmos.DrawLine(currentPos, currentPos + Vector3.up * 0.5f);
        }
    }

    private void OnDisable()
    {
        // Stop sequence when component is disabled
        if (isPlaying)
        {
            StopSequence();
        }
    }
}
