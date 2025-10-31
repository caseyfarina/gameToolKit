using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Animates a platform between multiple waypoints with configurable easing and animation modes.
/// Educational component for creating moving platform behaviors without coding.
/// </summary>
public class PhysicsPlatformAnimator : MonoBehaviour
{
    [System.Serializable]
    public struct Waypoint
    {
        [Tooltip("Transform position to move to")]
        public Transform transform;

        [Tooltip("How long to pause at this waypoint (seconds)")]
        public float pauseTime;

        [Tooltip("When in the animation sequence to reach this waypoint (0-1)")]
        [Range(0f, 1f)]
        public float normalizedTime;
    }

    public enum AnimationMode
    {
        Loop,       // Goes from last waypoint back to first
        PingPong    // Reverses direction at the end
    }

    [Header("Waypoint Configuration")]
    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();

    [Header("Animation Settings")]
    [Tooltip("Total time to complete one full animation cycle (seconds)")]
    [SerializeField] private float totalAnimationTime = 5f;

    [Tooltip("Animation curve for easing between waypoints")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Loop mode returns to start, PingPong reverses direction")]
    [SerializeField] private AnimationMode mode = AnimationMode.Loop;

    [Tooltip("Start animating automatically when scene begins")]
    [SerializeField] private bool playOnStart = true;

    [Header("Events")]
    [Tooltip("Called when reaching a waypoint")]
    public UnityEvent<int> onWaypointReached;

    [Tooltip("Called when animation completes one full cycle")]
    public UnityEvent onCycleComplete;

    private bool isPlaying = false;
    private float currentTime = 0f;
    private int currentWaypointIndex = 0;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private bool isReversing = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isPlaying || waypoints.Count < 2) return;

        // Handle pause at waypoint
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
            }
            return;
        }

        // Update animation time
        float timeDirection = isReversing ? -1f : 1f;
        currentTime += (Time.deltaTime / totalAnimationTime) * timeDirection;

        // Handle animation bounds based on mode
        if (currentTime >= 1f)
        {
            if (mode == AnimationMode.Loop)
            {
                currentTime = 0f;
                onCycleComplete?.Invoke();
            }
            else // PingPong
            {
                currentTime = 1f;
                isReversing = true;
                onCycleComplete?.Invoke();
            }
        }
        else if (currentTime <= 0f && isReversing)
        {
            currentTime = 0f;
            isReversing = false;
            onCycleComplete?.Invoke();
        }

        // Animate between waypoints
        AnimatePlatform();
    }

    private void AnimatePlatform()
    {
        if (waypoints.Count < 2) return;

        // Find the two waypoints we're between
        int nextIndex = -1;
        int prevIndex = -1;

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i].normalizedTime <= currentTime)
            {
                prevIndex = i;
            }
            if (waypoints[i].normalizedTime >= currentTime && nextIndex == -1)
            {
                nextIndex = i;
            }
        }

        // Handle edge cases
        if (prevIndex == -1) prevIndex = 0;
        if (nextIndex == -1) nextIndex = waypoints.Count - 1;

        // If we've reached a waypoint exactly
        if (prevIndex == nextIndex)
        {
            if (currentWaypointIndex != prevIndex)
            {
                currentWaypointIndex = prevIndex;
                onWaypointReached?.Invoke(currentWaypointIndex);

                if (waypoints[currentWaypointIndex].pauseTime > 0f)
                {
                    isPaused = true;
                    pauseTimer = waypoints[currentWaypointIndex].pauseTime;
                }
            }

            if (waypoints[prevIndex].transform != null)
            {
                transform.position = waypoints[prevIndex].transform.position;
                transform.rotation = waypoints[prevIndex].transform.rotation;
            }
            return;
        }

        // Calculate interpolation value
        float t = Mathf.InverseLerp(
            waypoints[prevIndex].normalizedTime,
            waypoints[nextIndex].normalizedTime,
            currentTime
        );

        // Apply easing curve
        float easedT = easingCurve.Evaluate(t);

        // Interpolate position and rotation
        if (waypoints[prevIndex].transform != null && waypoints[nextIndex].transform != null)
        {
            transform.position = Vector3.Lerp(
                waypoints[prevIndex].transform.position,
                waypoints[nextIndex].transform.position,
                easedT
            );

            transform.rotation = Quaternion.Slerp(
                waypoints[prevIndex].transform.rotation,
                waypoints[nextIndex].transform.rotation,
                easedT
            );
        }
    }

    /// <summary>
    /// Start or resume the platform animation
    /// </summary>
    public void Play()
    {
        isPlaying = true;
    }

    /// <summary>
    /// Pause the platform animation
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Stop and reset the platform animation to the beginning
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        currentTime = 0f;
        currentWaypointIndex = 0;
        isPaused = false;
        isReversing = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    /// <summary>
    /// Toggle between play and pause states
    /// </summary>
    public void TogglePlayPause()
    {
        if (isPlaying)
            Pause();
        else
            Play();
    }

    private void OnValidate()
    {
        // Ensure normalized times are sorted
        if (waypoints.Count > 1)
        {
            for (int i = 1; i < waypoints.Count; i++)
            {
                if (waypoints[i].normalizedTime < waypoints[i - 1].normalizedTime)
                {
                    Debug.LogWarning($"Waypoint {i} has normalized time less than waypoint {i - 1}. Times should be in ascending order.");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints.Count < 2) return;

        // Draw lines between waypoints
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i].transform != null && waypoints[i + 1].transform != null)
            {
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
            }
        }

        // Draw loop connection if in loop mode
        if (mode == AnimationMode.Loop && waypoints.Count > 1)
        {
            if (waypoints[waypoints.Count - 1].transform != null && waypoints[0].transform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].transform.position, waypoints[0].transform.position);
            }
        }

        // Draw waypoint spheres
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i].transform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(waypoints[i].transform.position, 0.3f);
            }
        }
    }
}
