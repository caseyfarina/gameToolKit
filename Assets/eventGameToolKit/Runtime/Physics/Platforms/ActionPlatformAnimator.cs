using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Animates objects between multiple waypoint positions with loop and ping-pong modes.
/// Common use: Moving platforms, elevators, patrol routes, floating islands, or rotating mechanisms.
/// </summary>
public class ActionPlatformAnimator : MonoBehaviour
{
    [System.Serializable]
    public struct Waypoint
    {
        [Tooltip("Transform position to move to")]
        public Transform transform;

        [Tooltip("How long to pause at this waypoint (seconds)")]
        public float pauseTime;
    }

    public enum AnimationMode
    {
        Loop,       // Goes from last waypoint back to first
        PingPong    // Reverses direction at the end
    }

    [Header("Waypoint Configuration")]
    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();

    [Header("Animation Settings")]
    [Tooltip("Total time to move through all waypoints, NOT including pause times (seconds)")]
    [SerializeField] private float totalAnimationTime = 5f;

    [Tooltip("Animation curve for easing between waypoints")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Loop mode returns to start, PingPong reverses direction")]
    [SerializeField] private AnimationMode mode = AnimationMode.Loop;

    [Tooltip("Start animating automatically when scene begins")]
    [SerializeField] private bool playOnStart = true;

    [Header("Events")]
    [Tooltip("Called when reaching a waypoint")]
    /// <summary>
    /// Fires when the platform reaches a waypoint, passing the waypoint index as an int parameter
    /// </summary>
    public UnityEvent<int> onWaypointReached;

    [Tooltip("Called when animation completes one full cycle")]
    /// <summary>
    /// Fires when the animation completes one full cycle through all waypoints
    /// </summary>
    public UnityEvent onCycleComplete;

    private bool isPlaying = false;
    private int fromWaypointIndex = 0;
    private int toWaypointIndex = 1;
    private float segmentTimer = 0f;
    private float pauseTimer = 0f;
    private bool isPausing = false;
    private bool movingForward = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Get or add Rigidbody for smooth interpolated movement
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody for kinematic platform
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (playOnStart && waypoints.Count >= 2)
        {
            Play();
        }
    }

    private void FixedUpdate()
    {
        if (!isPlaying || waypoints.Count < 2) return;

        // Handle pause
        if (isPausing)
        {
            pauseTimer -= Time.fixedDeltaTime;
            if (pauseTimer <= 0f)
            {
                isPausing = false;
                MoveToNextSegment();
            }
            return;
        }

        // Animate current segment
        float segmentDuration = totalAnimationTime / (waypoints.Count - 1);
        segmentTimer += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(segmentTimer / segmentDuration);
        float easedT = easingCurve.Evaluate(t);

        // Lerp position and rotation using Rigidbody methods for smooth interpolation
        if (waypoints[fromWaypointIndex].transform != null && waypoints[toWaypointIndex].transform != null)
        {
            Vector3 targetPosition = Vector3.Lerp(
                waypoints[fromWaypointIndex].transform.position,
                waypoints[toWaypointIndex].transform.position,
                easedT
            );

            Quaternion targetRotation = Quaternion.Slerp(
                waypoints[fromWaypointIndex].transform.rotation,
                waypoints[toWaypointIndex].transform.rotation,
                easedT
            );

            // Use Rigidbody methods for interpolated movement
            rb.MovePosition(targetPosition);
            rb.MoveRotation(targetRotation);
        }

        // Reached target waypoint
        if (segmentTimer >= segmentDuration)
        {
            segmentTimer = 0f;
            onWaypointReached?.Invoke(toWaypointIndex);

            // Check for pause
            if (waypoints[toWaypointIndex].pauseTime > 0f)
            {
                isPausing = true;
                pauseTimer = waypoints[toWaypointIndex].pauseTime;
            }
            else
            {
                MoveToNextSegment();
            }
        }
    }

    private void MoveToNextSegment()
    {
        fromWaypointIndex = toWaypointIndex;

        if (movingForward)
        {
            toWaypointIndex++;

            // Reached end
            if (toWaypointIndex >= waypoints.Count)
            {
                onCycleComplete?.Invoke();

                if (mode == AnimationMode.Loop)
                {
                    fromWaypointIndex = waypoints.Count - 1;
                    toWaypointIndex = 0;
                }
                else // PingPong
                {
                    movingForward = false;
                    toWaypointIndex = waypoints.Count - 2;
                    fromWaypointIndex = waypoints.Count - 1;
                }
            }
        }
        else // Moving backward (PingPong)
        {
            toWaypointIndex--;

            // Reached start
            if (toWaypointIndex < 0)
            {
                onCycleComplete?.Invoke();
                movingForward = true;
                fromWaypointIndex = 0;
                toWaypointIndex = 1;
            }
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
        segmentTimer = 0f;
        fromWaypointIndex = 0;
        toWaypointIndex = 1;
        pauseTimer = 0f;
        isPausing = false;
        movingForward = true;
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
