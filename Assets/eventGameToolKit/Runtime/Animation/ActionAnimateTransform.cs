using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening;

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
    private Sequence animationSequence;
    private Vector3 initialPosition;
    private Vector3 initialRotation;
    private Vector3 initialScale;
    private float actualDuration; // Randomized duration

    // Track if Start() has been called to avoid double-playing
    private bool hasStarted = false;

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

    void OnEnable()
    {
        // If Start() has already been called and playOnStart is enabled,
        // restart the animation when the GameObject is re-enabled
        if (hasStarted && playOnStart)
        {
            Play();
        }
    }

    void Start()
    {
        // Default to this transform if none specified
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

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

        // Store initial transform values
        CaptureInitialValues();

        // Mark that Start() has been called
        hasStarted = true;

        // Auto-play if enabled
        if (playOnStart)
        {
            Play();
        }
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

        // Create DOTween animation sequence
        CreateAnimationSequence();
    }

    /// <summary>
    /// Stops the animation at current position
    /// </summary>
    public void Stop()
    {
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
            animationSequence = null;
        }

        isPlaying = false;
        currentTime = 0f;
    }

    /// <summary>
    /// Pauses the animation (can be resumed with Resume)
    /// </summary>
    public void Pause()
    {
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Pause();
            isPlaying = false;
        }
    }

    /// <summary>
    /// Resumes a paused animation
    /// </summary>
    public void Resume()
    {
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Play();
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
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Goto(normalizedTime * actualDuration, true);
        }
    }

    /// <summary>
    /// Creates the DOTween animation sequence
    /// </summary>
    private void CreateAnimationSequence()
    {
        if (targetTransform == null) return;

        // Calculate randomized start delay
        float actualStartDelay = startDelay;
        if (startDelayRandomness > 0f && startDelay > 0f)
        {
            float randomRange = startDelay * startDelayRandomness;
            actualStartDelay = startDelay + Random.Range(-randomRange, randomRange);
            actualStartDelay = Mathf.Max(0f, actualStartDelay);
        }

        // Create the sequence
        animationSequence = DOTween.Sequence();

        // Add start delay if needed
        if (actualStartDelay > 0f)
        {
            animationSequence.AppendInterval(actualStartDelay);
        }

        // Create tweens for each enabled curve mapping
        foreach (var mapping in curveMappings)
        {
            if (!mapping.enabled) continue;

            // Calculate target value based on mode
            float startValue = mapping.initialValue;
            float endValue;

            if (mapping.mode == AnimationMode.Offset)
            {
                // For offset mode: initial + (min + curve_range)
                endValue = mapping.initialValue + mapping.maxValue;
                startValue = mapping.initialValue + mapping.minValue;
            }
            else
            {
                // For absolute mode: replace with curve value
                startValue = mapping.minValue;
                endValue = mapping.maxValue;
            }

            // Create a tween for this property
            Tween propertyTween = CreatePropertyTween(mapping.property, startValue, endValue, actualDuration);

            if (propertyTween != null)
            {
                // Apply the animation curve
                propertyTween.SetEase(mapping.curve);

                // Join all property tweens so they run simultaneously
                if (animationSequence.Duration() == 0 || (actualStartDelay > 0 && animationSequence.Duration() == actualStartDelay))
                {
                    animationSequence.Append(propertyTween);
                }
                else
                {
                    animationSequence.Join(propertyTween);
                }
            }
        }

        // Set loop behavior
        if (loop)
        {
            int loops = -1; // Infinite loops
            LoopType loopType = pingPong ? LoopType.Yoyo : LoopType.Restart;
            animationSequence.SetLoops(loops, loopType);
        }

        // Set update mode (scaled/unscaled time)
        if (useUnscaledTime)
        {
            animationSequence.SetUpdate(true);
        }

        // Note: usePhysicsUpdate is not directly supported by DOTween
        // Students using moving platforms should use PhysicsPlatformAnimator instead
        if (usePhysicsUpdate)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionAnimateTransform: usePhysicsUpdate is not supported with DOTween. For physics-based platform movement, use PhysicsPlatformAnimator instead.", this);
        }

        // Note: loopDelay is not currently supported with DOTween-based animations
        if (loop && loopDelay > 0f)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionAnimateTransform: loopDelay is not currently supported with DOTween. The delay will be ignored.", this);
        }

        // Setup callbacks
        animationSequence.OnStart(() =>
        {
            isPlaying = true;
            currentTime = 0f;
            if (debugEvents)
                Debug.Log($"[{gameObject.name}] ActionAnimateTransform: onAnimationStart invoked", this);
            onAnimationStart.Invoke();
        });

        animationSequence.OnUpdate(() =>
        {
            // Calculate normalized time
            if (animationSequence != null)
            {
                float elapsed = animationSequence.Elapsed();
                currentTime = Mathf.Clamp01(elapsed / actualDuration);
                onAnimationUpdate.Invoke(currentTime);
            }
        });

        if (loop)
        {
            animationSequence.OnStepComplete(() =>
            {
                if (debugEvents)
                    Debug.Log($"[{gameObject.name}] ActionAnimateTransform: onAnimationLoop invoked", this);
                onAnimationLoop.Invoke();
            });
        }
        else
        {
            animationSequence.OnComplete(() =>
            {
                isPlaying = false;
                if (debugEvents)
                    Debug.Log($"[{gameObject.name}] ActionAnimateTransform: onAnimationComplete invoked", this);
                onAnimationComplete.Invoke();
            });
        }

        // Start the sequence
        animationSequence.Play();
    }

    /// <summary>
    /// Creates a DOTween tween for a specific transform property
    /// </summary>
    private Tween CreatePropertyTween(TransformProperty property, float startValue, float endValue, float duration)
    {
        if (targetTransform == null) return null;

        // Set the start value immediately
        SetPropertyValue(property, startValue);

        // Create tween based on property type
        switch (property)
        {
            case TransformProperty.PositionX:
                return DOTween.To(() => targetTransform.localPosition.x,
                    x => { var pos = targetTransform.localPosition; pos.x = x; targetTransform.localPosition = pos; },
                    endValue, duration);

            case TransformProperty.PositionY:
                return DOTween.To(() => targetTransform.localPosition.y,
                    y => { var pos = targetTransform.localPosition; pos.y = y; targetTransform.localPosition = pos; },
                    endValue, duration);

            case TransformProperty.PositionZ:
                return DOTween.To(() => targetTransform.localPosition.z,
                    z => { var pos = targetTransform.localPosition; pos.z = z; targetTransform.localPosition = pos; },
                    endValue, duration);

            case TransformProperty.RotationX:
                return DOTween.To(() => targetTransform.localEulerAngles.x,
                    x => { var rot = targetTransform.localEulerAngles; rot.x = x; targetTransform.localEulerAngles = rot; },
                    endValue, duration);

            case TransformProperty.RotationY:
                return DOTween.To(() => targetTransform.localEulerAngles.y,
                    y => { var rot = targetTransform.localEulerAngles; rot.y = y; targetTransform.localEulerAngles = rot; },
                    endValue, duration);

            case TransformProperty.RotationZ:
                return DOTween.To(() => targetTransform.localEulerAngles.z,
                    z => { var rot = targetTransform.localEulerAngles; rot.z = z; targetTransform.localEulerAngles = rot; },
                    endValue, duration);

            case TransformProperty.ScaleX:
                return DOTween.To(() => targetTransform.localScale.x,
                    x => { var scale = targetTransform.localScale; scale.x = x; targetTransform.localScale = scale; },
                    endValue, duration);

            case TransformProperty.ScaleY:
                return DOTween.To(() => targetTransform.localScale.y,
                    y => { var scale = targetTransform.localScale; scale.y = y; targetTransform.localScale = scale; },
                    endValue, duration);

            case TransformProperty.ScaleZ:
                return DOTween.To(() => targetTransform.localScale.z,
                    z => { var scale = targetTransform.localScale; scale.z = z; targetTransform.localScale = scale; },
                    endValue, duration);

            default:
                return null;
        }
    }

    /// <summary>
    /// Sets a specific transform property value
    /// </summary>
    private void SetPropertyValue(TransformProperty property, float value)
    {
        if (targetTransform == null) return;

        switch (property)
        {
            case TransformProperty.PositionX:
                var pos1 = targetTransform.localPosition;
                pos1.x = value;
                targetTransform.localPosition = pos1;
                break;
            case TransformProperty.PositionY:
                var pos2 = targetTransform.localPosition;
                pos2.y = value;
                targetTransform.localPosition = pos2;
                break;
            case TransformProperty.PositionZ:
                var pos3 = targetTransform.localPosition;
                pos3.z = value;
                targetTransform.localPosition = pos3;
                break;
            case TransformProperty.RotationX:
                var rot1 = targetTransform.localEulerAngles;
                rot1.x = value;
                targetTransform.localEulerAngles = rot1;
                break;
            case TransformProperty.RotationY:
                var rot2 = targetTransform.localEulerAngles;
                rot2.y = value;
                targetTransform.localEulerAngles = rot2;
                break;
            case TransformProperty.RotationZ:
                var rot3 = targetTransform.localEulerAngles;
                rot3.z = value;
                targetTransform.localEulerAngles = rot3;
                break;
            case TransformProperty.ScaleX:
                var scale1 = targetTransform.localScale;
                scale1.x = value;
                targetTransform.localScale = scale1;
                break;
            case TransformProperty.ScaleY:
                var scale2 = targetTransform.localScale;
                scale2.y = value;
                targetTransform.localScale = scale2;
                break;
            case TransformProperty.ScaleZ:
                var scale3 = targetTransform.localScale;
                scale3.z = value;
                targetTransform.localScale = scale3;
                break;
        }
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
        // For DOTween, we can achieve reverse by playing backwards
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.PlayBackwards();
        }
        else
        {
            // Create sequence and immediately play backwards
            CreateAnimationSequence();
            if (animationSequence != null)
            {
                animationSequence.PlayBackwards();
            }
        }
    }

    void OnDestroy()
    {
        // Clean up DOTween sequence when destroyed
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
            animationSequence = null;
        }
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
