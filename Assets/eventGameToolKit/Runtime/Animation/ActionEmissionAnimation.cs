using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Animates material emission intensity using a configurable animation curve.
/// Common use: Glowing teleporters, charging effects, pulsing pickups, alert indicators.
/// </summary>
public class ActionEmissionAnimation : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Renderer with the material to animate. If empty, uses this GameObject's Renderer.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Material index to animate (for multi-material objects)")]
    [SerializeField] private int materialIndex = 0;

    [Header("Emission Settings")]
    [Tooltip("Base emission color (RGB). Intensity is multiplied on top of this.")]
    [SerializeField] private Color emissionColor = Color.white;

    [Tooltip("Maximum emission intensity (HDR value, can exceed 1 for bloom)")]
    [SerializeField] private float maxIntensity = 2f;

    [Header("Animation")]
    [Tooltip("Duration of the animation in seconds")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Animation curve controlling intensity over time (0-1 time, 0-1 value)")]
    [SerializeField] private AnimationCurve intensityCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 10f),      // Start at 0, quick ramp up
        new Keyframe(0.1f, 1f, 0f, 0f),     // Peak at 0.1
        new Keyframe(1f, 0f, -1.1f, 0f)     // Ease down to 0
    );

    [Tooltip("Play animation automatically on Start")]
    [SerializeField] private bool playOnStart = false;

    [Tooltip("Loop the animation")]
    [SerializeField] private bool loop = false;

    [Tooltip("Loop type when looping is enabled")]
    [SerializeField] private LoopType loopType = LoopType.Restart;

    [Header("Events")]
    /// <summary>
    /// Fires when the emission animation starts
    /// </summary>
    public UnityEvent onAnimationStarted;

    /// <summary>
    /// Fires when the emission animation completes (doesn't fire if looping)
    /// </summary>
    public UnityEvent onAnimationCompleted;

    // Runtime state
    private Material targetMaterial;
    private Tween emissionTween;
    private float currentProgress;
    private bool isValidSetup = false;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        isValidSetup = false;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogWarning($"ActionEmissionAnimation: No Renderer found on {gameObject.name}. Add a Renderer component or assign one in the Inspector.");
            return;
        }

        // Get material instance to avoid modifying shared material
        Material[] materials = targetRenderer.materials;
        if (materials.Length == 0)
        {
            Debug.LogWarning($"ActionEmissionAnimation: Renderer on {gameObject.name} has no materials assigned.");
            return;
        }

        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
            Debug.LogWarning($"ActionEmissionAnimation: Material index {materialIndex} out of range on {gameObject.name}. Using index 0 instead.");
            materialIndex = 0;
        }

        targetMaterial = materials[materialIndex];

        if (targetMaterial == null)
        {
            Debug.LogWarning($"ActionEmissionAnimation: Material at index {materialIndex} is null on {gameObject.name}.");
            return;
        }

        // Check if material has emission property
        if (!targetMaterial.HasProperty(EmissionColorID))
        {
            Debug.LogWarning($"ActionEmissionAnimation: Material '{targetMaterial.name}' on {gameObject.name} does not have an _EmissionColor property. Make sure you're using a shader that supports emission (e.g., URP/Lit).");
            return;
        }

        isValidSetup = true;
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        emissionTween?.Kill();
    }

    /// <summary>
    /// Plays the emission animation from the beginning
    /// </summary>
    public void Play()
    {
        if (!isValidSetup)
        {
            // Silently fail - warning was already logged in ValidateSetup
            return;
        }

        // Kill any existing tween
        emissionTween?.Kill();

        // Enable emission keyword if not already enabled
        targetMaterial.EnableKeyword("_EMISSION");

        // Reset progress
        currentProgress = 0f;

        // Create tween that animates progress from 0 to 1
        emissionTween = DOTween.To(
            () => currentProgress,
            x => {
                currentProgress = x;
                ApplyEmission(x);
            },
            1f,
            duration
        );

        // Configure looping
        if (loop)
        {
            emissionTween.SetLoops(-1, loopType);
        }
        else
        {
            emissionTween.OnComplete(() => {
                onAnimationCompleted?.Invoke();
            });
        }

        emissionTween.SetEase(Ease.Linear); // Curve handles easing

        onAnimationStarted?.Invoke();
    }

    /// <summary>
    /// Stops the emission animation and resets to zero intensity
    /// </summary>
    public void Stop()
    {
        emissionTween?.Kill();
        ApplyEmission(0f);
    }

    /// <summary>
    /// Stops the emission animation at current intensity
    /// </summary>
    public void Pause()
    {
        emissionTween?.Pause();
    }

    /// <summary>
    /// Resumes a paused emission animation
    /// </summary>
    public void Resume()
    {
        emissionTween?.Play();
    }

    /// <summary>
    /// Sets the emission to a specific intensity immediately (0-1 range, mapped to maxIntensity)
    /// </summary>
    public void SetIntensity(float normalizedIntensity)
    {
        if (!isValidSetup) return;
        emissionTween?.Kill();
        ApplyEmission(Mathf.Clamp01(normalizedIntensity));
    }

    private void ApplyEmission(float progress)
    {
        if (!isValidSetup || targetMaterial == null) return;

        // Sample the curve at current progress
        float curveValue = intensityCurve.Evaluate(progress);

        // Calculate final intensity
        float intensity = curveValue * maxIntensity;

        // Apply emission color with intensity
        Color finalColor = emissionColor * intensity;
        targetMaterial.SetColor(EmissionColorID, finalColor);
    }

    /// <summary>
    /// Sets the emission color at runtime
    /// </summary>
    public void SetEmissionColor(Color color)
    {
        emissionColor = color;
    }

    /// <summary>
    /// Sets the maximum intensity at runtime
    /// </summary>
    public void SetMaxIntensity(float intensity)
    {
        maxIntensity = intensity;
    }

    /// <summary>
    /// Sets the animation duration at runtime
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
    }

    /// <summary>
    /// Returns true if the animation is currently playing
    /// </summary>
    public bool IsPlaying => emissionTween != null && emissionTween.IsPlaying();

    /// <summary>
    /// Returns the current emission intensity (0-1 normalized)
    /// </summary>
    public float CurrentIntensity => intensityCurve.Evaluate(currentProgress);

    /// <summary>
    /// Returns true if the component is properly configured (has valid Renderer and material with emission support)
    /// </summary>
    public bool IsValidSetup => isValidSetup;
}
