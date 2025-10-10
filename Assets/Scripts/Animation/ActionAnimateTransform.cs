using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Procedurally animates position, rotation, and scale using customizable animation curves with loop modes.
/// Common use: Door animations, floating objects, rotating collectibles, platform movement, or UI transitions.
/// </summary>
public class ActionAnimateTransform : MonoBehaviour
{
    public enum TransformProperty
    {
        PositionX,
        PositionY,
        PositionZ,
        RotationX,
        RotationY,
        RotationZ,
        ScaleX,
        ScaleY,
        ScaleZ
    }

    public enum AnimationMode
    {
        Offset,     // Add curve value to current transform value
        Absolute    // Replace transform value with curve value
    }

    [System.Serializable]
    public class CurveMapping
    {
        [Tooltip("Which transform property to animate")]
        public TransformProperty property = TransformProperty.PositionY;

        [Tooltip("Animation curve (0-1 time range, any value range)")]
        public AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f)
        );

        [Tooltip("Minimum output value (curve value 0 maps to this)")]
        public float minValue = 0f;

        [Tooltip("Maximum output value (curve value 1 maps to this)")]
        public float maxValue = 1f;

        [Tooltip("Add to current value (Offset) or replace it (Absolute)")]
        public AnimationMode mode = AnimationMode.Offset;

        [Tooltip("Is this curve mapping enabled?")]
        public bool enabled = true;

        // Store initial value for offset calculations
        [HideInInspector] public float initialValue;
    }

    [Header("Animation Configuration")]
    [Tooltip("Transform to animate (defaults to this GameObject)")]
    [SerializeField] private Transform targetTransform;

    [Tooltip("Duration of the animation in seconds")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Randomize duration by this percentage (0 = no randomization, 1 = +/- 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float durationRandomness = 0f;

    [Tooltip("All curve mappings for this animation")]
    [SerializeField] private CurveMapping[] curveMappings = new CurveMapping[]
    {
        new CurveMapping()
    };

    [Header("Playback Settings")]
    [Tooltip("Play animation automatically on Start")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Loop the animation continuously")]
    [SerializeField] private bool loop = true;

    [Tooltip("Reverse direction on each loop (ping-pong)")]
    [SerializeField] private bool pingPong = false;

    [Tooltip("Use physics timing (FixedUpdate) - REQUIRED for moving platforms with physics characters")]
    [SerializeField] private bool usePhysicsUpdate = false;

    [Tooltip("Delay before animation starts (seconds)")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("Randomize start delay by this percentage (0 = no randomization, 1 = +/- 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float startDelayRandomness = 0f;

    [Tooltip("Delay between loop iterations (seconds)")]
    [SerializeField] private float loopDelay = 0f;

    [Tooltip("Use unscaled time (ignores Time.timeScale)")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("State (Read-Only)")]
    [Tooltip("Is animation currently playing?")]
    [SerializeField] private bool isPlaying = false;

    [Tooltip("Current normalized time (0-1)")]
    [SerializeField] private float currentTime = 0f;

    [Header("Events")]
    [Tooltip("Log all event invocations to console for debugging")]
    [SerializeField] private bool debugEvents = false;

    [Tooltip("Fires when animation starts")]
    /// <summary>
    /// Fires when the animation starts playing
    /// </summary>
    public UnityEvent onAnimationStart = new UnityEvent();

    [Tooltip("Fires when animation completes (not fired on loop)")]
    /// <summary>
    /// Fires when the animation completes (only fires if loop is disabled)
    /// </summary>
    public UnityEvent onAnimationComplete = new UnityEvent();

    [Tooltip("Fires each time animation loops")]
    /// <summary>
    /// Fires each time the animation completes a loop iteration
    /// </summary>
    public UnityEvent onAnimationLoop = new UnityEvent();

    [Tooltip("Fires every frame during animation (passes normalized time 0-1)")]
    /// <summary>
    /// Fires every frame during animation, passing the normalized time (0-1) as a float parameter
    /// </summary>
    public UnityEvent<float> onAnimationUpdate = new UnityEvent<float>();

    // Internal state
    private Coroutine animationCoroutine;
    private bool isPingPongReverse = false;
    private Vector3 initialPosition;
    private Vector3 initialRotation;
    private Vector3 initialScale;
    private float actualDuration; // Randomized duration

    // Cached yield instruction to avoid allocations
    private WaitForSeconds cachedWaitForSeconds;

    // Cache which properties we're modifying (set once at start)
    private bool modifiesPosition = false;
    private bool modifiesRotation = false;
    private bool modifiesScale = false;
    private bool hasCachedPropertyFlags = false;

    void OnValidate()
    {
        // Warn about common mistakes
        if (duration <= 0f)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionAnimateTransform: Duration must be greater than 0. Setting to 0.1s.", this);
            duration = 0.1f;
        }

        // Check if all curve mappings are disabled
        bool hasEnabledMapping = false;
        foreach (var mapping in curveMappings)
        {
            if (mapping.enabled)
            {
                hasEnabledMapping = true;

                // Check for inverted min/max
                if (mapping.minValue > mapping.maxValue)
                {
                    Debug.LogWarning($"[{gameObject.name}] ActionAnimateTransform: Min value ({mapping.minValue}) is greater than max value ({mapping.maxValue}). Animation may behave unexpectedly.", this);
                }
            }
        }

        if (!hasEnabledMapping && curveMappings.Length > 0)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionAnimateTransform: All curve mappings are disabled. No animation will play.", this);
        }
    }

    void Start()
    {
        // Default to this transform if none specified
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        // Cache which properties we're modifying (do this once)
        CachePropertyFlags();

        // Calculate randomized duration
        if (durationRandomness > 0f)
        {
            float randomRange = duration * durationRandomness;
            actualDuration = duration + Random.Range(-randomRange, randomRange);
            actualDuration = Mathf.Max(0.01f, actualDuration); // Ensure positive duration
        }
        else
        {
            actualDuration = duration;
        }

        // Calculate randomized start delay and pre-allocate WaitForSeconds
        float actualStartDelay = startDelay;
        if (startDelayRandomness > 0f && startDelay > 0f)
        {
            float randomRange = startDelay * startDelayRandomness;
            actualStartDelay = startDelay + Random.Range(-randomRange, randomRange);
            actualStartDelay = Mathf.Max(0f, actualStartDelay); // Ensure non-negative
        }

        if (actualStartDelay > 0)
        {
            cachedWaitForSeconds = new WaitForSeconds(actualStartDelay);
        }

        // Store initial transform values
        CaptureInitialValues();

        // Auto-play if enabled
        if (playOnStart)
        {
            Play();
        }
    }

    /// <summary>
    /// Caches which transform properties we're going to modify (called once at start)
    /// </summary>
    private void CachePropertyFlags()
    {
        if (hasCachedPropertyFlags)
            return;

        modifiesPosition = false;
        modifiesRotation = false;
        modifiesScale = false;

        for (int i = 0; i < curveMappings.Length; i++)
        {
            if (!curveMappings[i].enabled)
                continue;

            TransformProperty prop = curveMappings[i].property;
            if (prop <= TransformProperty.PositionZ)
                modifiesPosition = true;
            else if (prop <= TransformProperty.RotationZ)
                modifiesRotation = true;
            else
                modifiesScale = true;
        }

        hasCachedPropertyFlags = true;
    }

    /// <summary>
    /// Captures the current transform values as initial state
    /// </summary>
    private void CaptureInitialValues()
    {
        if (targetTransform == null)
            return;

        initialPosition = targetTransform.localPosition;
        initialRotation = targetTransform.localEulerAngles;
        initialScale = targetTransform.localScale;

        // Store initial values for each curve mapping
        foreach (var mapping in curveMappings)
        {
            if (!mapping.enabled)
                continue;

            mapping.initialValue = GetPropertyValue(mapping.property);
        }
    }

    /// <summary>
    /// Starts or restarts the animation
    /// </summary>
    public void Play()
    {
        // Stop any existing animation
        Stop();

        // Capture current values as new initial state
        CaptureInitialValues();

        // Start animation coroutine
        animationCoroutine = StartCoroutine(AnimationRoutine());
    }

    /// <summary>
    /// Stops the animation at current position
    /// </summary>
    public void Stop()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        isPlaying = false;
    }

    /// <summary>
    /// Pauses the animation (can be resumed with Resume)
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Resumes a paused animation
    /// </summary>
    public void Resume()
    {
        if (animationCoroutine != null)
        {
            isPlaying = true;
        }
    }

    /// <summary>
    /// Resets transform to initial values
    /// </summary>
    public void ResetToInitial()
    {
        if (targetTransform == null)
            return;

        targetTransform.localPosition = initialPosition;
        targetTransform.localEulerAngles = initialRotation;
        targetTransform.localScale = initialScale;

        currentTime = 0f;
    }

    /// <summary>
    /// Sets animation to a specific normalized time (0-1)
    /// </summary>
    public void SetNormalizedTime(float normalizedTime)
    {
        currentTime = Mathf.Clamp01(normalizedTime);
        EvaluateAndApplyCurves(currentTime);
    }

    /// <summary>
    /// Main animation coroutine
    /// </summary>
    private IEnumerator AnimationRoutine()
    {
        // Start delay - use cached WaitForSeconds to avoid allocation
        if (startDelay > 0)
        {
            yield return cachedWaitForSeconds;
        }

        isPlaying = true;
        isPingPongReverse = false;
        currentTime = 0f;

        if (debugEvents)
            Debug.Log($"[{gameObject.name}] ActionAnimateTransform: onAnimationStart invoked", this);
        onAnimationStart.Invoke();

        do
        {
            float elapsed = 0f;

            // Animate from 0 to 1
            while (elapsed < actualDuration)
            {
                if (!isPlaying)
                {
                    yield return null;
                    continue;
                }

                // Update elapsed time
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;
                currentTime = Mathf.Clamp01(elapsed / actualDuration);

                // Apply ping-pong reversal
                float evaluationTime = (pingPong && isPingPongReverse) ? (1f - currentTime) : currentTime;

                // Evaluate and apply all curves
                EvaluateAndApplyCurves(evaluationTime);

                // Fire update event (skip debug log to avoid spam)
                onAnimationUpdate.Invoke(currentTime);

                // Wait for next frame (physics or normal update)
                if (usePhysicsUpdate)
                    yield return new WaitForFixedUpdate();
                else
                    yield return null;
            }

            // Ensure final values are set
            float finalTime = (pingPong && isPingPongReverse) ? 0f : 1f;
            EvaluateAndApplyCurves(finalTime);

            // Handle looping
            if (loop)
            {
                if (pingPong)
                {
                    isPingPongReverse = !isPingPongReverse;
                }
                else
                {
                    // Reset to initial for normal loop
                    CaptureInitialValues();
                }

                if (debugEvents)
                    Debug.Log($"[{gameObject.name}] ActionAnimateTransform: onAnimationLoop invoked (loop iteration)", this);
                onAnimationLoop.Invoke();

                // Delay between loops if specified
                if (loopDelay > 0f)
                {
                    if (useUnscaledTime)
                        yield return new WaitForSecondsRealtime(loopDelay);
                    else
                        yield return new WaitForSeconds(loopDelay);
                }
            }

        } while (loop);

        // Animation complete
        isPlaying = false;
        if (debugEvents)
            Debug.Log($"[{gameObject.name}] ActionAnimateTransform: onAnimationComplete invoked", this);
        onAnimationComplete.Invoke();
    }

    /// <summary>
    /// Evaluates all curve mappings and applies them to the transform
    /// </summary>
    private void EvaluateAndApplyCurves(float time)
    {
        if (targetTransform == null)
            return;

        // Use cached property flags instead of checking every frame
        Vector3 newPosition = modifiesPosition ? targetTransform.localPosition : Vector3.zero;
        Vector3 newRotation = modifiesRotation ? targetTransform.localEulerAngles : Vector3.zero;
        Vector3 newScale = modifiesScale ? targetTransform.localScale : Vector3.one;

        // Apply all curve mappings
        for (int i = 0; i < curveMappings.Length; i++)
        {
            ref CurveMapping mapping = ref curveMappings[i];

            if (!mapping.enabled)
                continue;

            // Evaluate curve at current time
            float curveValue = mapping.curve.Evaluate(time);

            // Map curve value to min/max range and apply mode in one step
            float finalValue = mapping.mode == AnimationMode.Offset
                ? mapping.initialValue + mapping.minValue + (curveValue * (mapping.maxValue - mapping.minValue))
                : mapping.minValue + (curveValue * (mapping.maxValue - mapping.minValue));

            // Set the appropriate transform property - inline for performance
            switch (mapping.property)
            {
                case TransformProperty.PositionX: newPosition.x = finalValue; break;
                case TransformProperty.PositionY: newPosition.y = finalValue; break;
                case TransformProperty.PositionZ: newPosition.z = finalValue; break;
                case TransformProperty.RotationX: newRotation.x = finalValue; break;
                case TransformProperty.RotationY: newRotation.y = finalValue; break;
                case TransformProperty.RotationZ: newRotation.z = finalValue; break;
                case TransformProperty.ScaleX: newScale.x = finalValue; break;
                case TransformProperty.ScaleY: newScale.y = finalValue; break;
                case TransformProperty.ScaleZ: newScale.z = finalValue; break;
            }
        }

        // Apply all changes - only set properties that were modified
        if (modifiesPosition)
            targetTransform.localPosition = newPosition;
        if (modifiesRotation)
            targetTransform.localEulerAngles = newRotation;
        if (modifiesScale)
            targetTransform.localScale = newScale;
    }

    /// <summary>
    /// Gets the current value of a transform property
    /// </summary>
    private float GetPropertyValue(TransformProperty property)
    {
        if (targetTransform == null)
            return 0f;

        switch (property)
        {
            case TransformProperty.PositionX: return targetTransform.localPosition.x;
            case TransformProperty.PositionY: return targetTransform.localPosition.y;
            case TransformProperty.PositionZ: return targetTransform.localPosition.z;
            case TransformProperty.RotationX: return targetTransform.localEulerAngles.x;
            case TransformProperty.RotationY: return targetTransform.localEulerAngles.y;
            case TransformProperty.RotationZ: return targetTransform.localEulerAngles.z;
            case TransformProperty.ScaleX: return targetTransform.localScale.x;
            case TransformProperty.ScaleY: return targetTransform.localScale.y;
            case TransformProperty.ScaleZ: return targetTransform.localScale.z;
            default: return 0f;
        }
    }


    /// <summary>
    /// Gets whether animation is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }

    /// <summary>
    /// Gets current animation progress (0-1)
    /// </summary>
    public float GetNormalizedTime()
    {
        return currentTime;
    }

    /// <summary>
    /// Sets the animation duration at runtime
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
    }

    /// <summary>
    /// Sets loop mode at runtime
    /// </summary>
    public void SetLoop(bool shouldLoop)
    {
        loop = shouldLoop;
    }

    /// <summary>
    /// Plays the animation in reverse
    /// </summary>
    public void PlayReverse()
    {
        isPingPongReverse = true;
        Play();
    }

    // Gizmo visualization
    void OnDrawGizmosSelected()
    {
        if (targetTransform == null)
            return;

        // Draw a line showing the transform will be animated
        Gizmos.color = isPlaying ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(targetTransform.position, 0.2f);

        // Draw line from this object to target if different
        if (targetTransform != transform)
        {
            Gizmos.DrawLine(transform.position, targetTransform.position);
        }
    }
}
