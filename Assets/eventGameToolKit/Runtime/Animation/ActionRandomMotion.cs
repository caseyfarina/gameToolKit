using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Continuously moves a GameObject to random positions near its rest point.
/// Each move picks a random offset within the configured per-axis ranges, then
/// waits a random pause before the next move. Start, Stop, and Pause are available
/// as public methods for UnityEvent wiring.
/// Common use: Floating collectibles, ambient prop oscillation, idle enemy drift, drifting obstacles.
/// </summary>
public class ActionRandomMotion : MonoBehaviour
{
    public enum MotionSpace
    {
        World,  // Moves in world space
        Local   // Moves relative to the parent transform
    }

    [Header("Axes")]
    [Tooltip("Move randomly along the X axis")]
    [SerializeField] private bool moveX = true;

    [Tooltip("Maximum offset in units from the rest position on the X axis (object moves between -range and +range)")]
    [SerializeField] private float rangeX = 1f;

    [Tooltip("Move randomly along the Y axis")]
    [SerializeField] private bool moveY = false;

    [Tooltip("Maximum offset in units from the rest position on the Y axis")]
    [SerializeField] private float rangeY = 1f;

    [Tooltip("Move randomly along the Z axis")]
    [SerializeField] private bool moveZ = false;

    [Tooltip("Maximum offset in units from the rest position on the Z axis")]
    [SerializeField] private float rangeZ = 1f;

    [Header("Timing")]
    [Tooltip("Minimum time in seconds for each move")]
    [SerializeField] private float minDuration = 0.5f;

    [Tooltip("Maximum time in seconds for each move")]
    [SerializeField] private float maxDuration = 2f;

    [Tooltip("Minimum pause in seconds at each destination before the next move begins")]
    [SerializeField] private float minPause = 0f;

    [Tooltip("Maximum pause in seconds at each destination before the next move begins")]
    [SerializeField] private float maxPause = 0.5f;

    [Header("Easing")]
    [Tooltip("Easing applied to each move — InOutSine gives a natural floating feel")]
    [SerializeField] private Ease easeType = Ease.InOutSine;

    [Header("Space & Playback")]
    [Tooltip("World moves in world space; Local moves relative to the parent transform")]
    [SerializeField] private MotionSpace motionSpace = MotionSpace.World;

    [Tooltip("Begin moving automatically when the scene starts")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Duration in seconds to tween back when ReturnToRestPosition() is called")]
    [SerializeField] private float returnDuration = 0.5f;

    [Header("Events")]
    /// <summary>
    /// Fires when motion begins (Play is called)
    /// </summary>
    public UnityEvent onMotionStart;

    /// <summary>
    /// Fires when motion stops (Stop is called)
    /// </summary>
    public UnityEvent onMotionStop;

    /// <summary>
    /// Fires at the start of each individual move
    /// </summary>
    public UnityEvent onMoveStart;

    /// <summary>
    /// Fires when each individual move reaches its destination
    /// </summary>
    public UnityEvent onMoveComplete;

    private bool isPlaying = false;
    private Vector3 restPosition;   // captured at Start() in the chosen space
    private Tween currentTween;

    private void Start()
    {
        restPosition = motionSpace == MotionSpace.Local
            ? transform.localPosition
            : transform.position;

        if (playOnStart)
            Play();
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Starts random motion. Has no effect if already playing.
    /// </summary>
    public void Play()
    {
        if (isPlaying) return;
        isPlaying = true;
        onMotionStart.Invoke();
        StartNextMove();
    }

    /// <summary>
    /// Stops random motion and leaves the object at its current position. Fires onMotionStop.
    /// </summary>
    public void Stop()
    {
        if (!isPlaying) return;
        isPlaying = false;
        currentTween?.Kill();
        currentTween = null;
        onMotionStop.Invoke();
    }

    /// <summary>
    /// Pauses motion without firing onMotionStop. Resume with Play().
    /// </summary>
    public void Pause()
    {
        if (!isPlaying) return;
        isPlaying = false;
        currentTween?.Kill();
        currentTween = null;
    }

    /// <summary>
    /// Toggles between playing and paused states.
    /// </summary>
    public void TogglePlayPause()
    {
        if (isPlaying) Pause();
        else Play();
    }

    /// <summary>
    /// Tweens the object back to its rest position (the position when the scene started).
    /// Does not affect the playing state.
    /// </summary>
    public void ReturnToRestPosition()
    {
        currentTween?.Kill();
        float dur = Mathf.Max(0.01f, returnDuration);
        currentTween = (motionSpace == MotionSpace.Local
            ? transform.DOLocalMove(restPosition, dur)
            : transform.DOMove(restPosition, dur))
            .SetEase(easeType)
            .OnComplete(() => { if (isPlaying) StartNextMove(); });
    }

    /// <summary>
    /// Returns true if the object is currently moving
    /// </summary>
    public bool IsPlaying => isPlaying;

    // ──────────────────────────────────────────────
    // Internal motion loop
    // ──────────────────────────────────────────────

    private void StartNextMove()
    {
        if (!isPlaying) return;
        currentTween?.Kill();

        Vector3 offset = new Vector3(
            moveX ? Random.Range(-rangeX, rangeX) : 0f,
            moveY ? Random.Range(-rangeY, rangeY) : 0f,
            moveZ ? Random.Range(-rangeZ, rangeZ) : 0f
        );

        Vector3 target = restPosition + offset;
        float duration = Random.Range(minDuration, maxDuration);

        onMoveStart.Invoke();

        currentTween = (motionSpace == MotionSpace.Local
            ? transform.DOLocalMove(target, duration)
            : transform.DOMove(target, duration))
            .SetEase(easeType)
            .OnComplete(OnMoveFinished);
    }

    private void OnMoveFinished()
    {
        onMoveComplete.Invoke();

        if (!isPlaying) return;

        float pause = Random.Range(minPause, maxPause);
        if (pause <= 0f)
        {
            StartNextMove();
            return;
        }

        currentTween = DOVirtual.DelayedCall(pause, StartNextMove);
    }

    private void OnValidate()
    {
        minDuration = Mathf.Max(0.01f, minDuration);
        maxDuration = Mathf.Max(minDuration, maxDuration);
        minPause    = Mathf.Max(0f, minPause);
        maxPause    = Mathf.Max(minPause, maxPause);
        rangeX      = Mathf.Max(0f, rangeX);
        rangeY      = Mathf.Max(0f, rangeY);
        rangeZ      = Mathf.Max(0f, rangeZ);
    }
}
