using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Automates eye blinking animation by switching materials on a URP Decal Projector.
/// Perfect for facial animations using texture-based eye decals.
/// </summary>
[RequireComponent(typeof(DecalProjector))]
public class ActionBlinkDecal : MonoBehaviour
{
    [Header("Decal Materials")]
    [Tooltip("Material to use for open eyes (default state)")]
    [SerializeField] private Material openEyesMaterial;

    [Tooltip("Material to use for closed eyes (blink state)")]
    [SerializeField] private Material closedEyesMaterial;

    [Header("Blink Timing")]
    [Tooltip("Base time in seconds between blinks")]
    [SerializeField] private float timeBetweenBlinks = 3.0f;

    [Tooltip("Random variation percentage (0-1). Example: 0.5 = ±50% variation")]
    [SerializeField] [Range(0f, 1f)] private float randomPercentage = 0.3f;

    [Tooltip("How long the eyes stay closed during a blink (in seconds)")]
    [SerializeField] private float blinkDuration = 0.15f;

    [Header("Playback Settings")]
    [Tooltip("Start blinking automatically when the scene starts")]
    [SerializeField] private bool blinkOnStart = true;

    [Header("Blink Events")]
    /// <summary>
    /// Fires when a blink starts (eyes close)
    /// </summary>
    public UnityEvent onBlinkStart;

    /// <summary>
    /// Fires when a blink completes (eyes open)
    /// </summary>
    public UnityEvent onBlinkComplete;

    // Private state
    private DecalProjector decalProjector;
    private Coroutine blinkCoroutine;
    private bool isBlinking = false;

    private void Awake()
    {
        // Get the DecalProjector component
        decalProjector = GetComponent<DecalProjector>();

        if (decalProjector == null)
        {
            Debug.LogError($"[{gameObject.name}] ActionBlinkDecal requires a DecalProjector component!", this);
            enabled = false;
            return;
        }

        // Validate materials
        if (openEyesMaterial == null || closedEyesMaterial == null)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionBlinkDecal: Open or closed eyes material not assigned. Blinking disabled.", this);
            enabled = false;
            return;
        }

        // Set initial material to open eyes
        decalProjector.material = openEyesMaterial;
    }

    private void Start()
    {
        if (blinkOnStart)
        {
            StartBlinking();
        }
    }

    /// <summary>
    /// Starts the automatic blinking loop
    /// </summary>
    public void StartBlinking()
    {
        if (isBlinking) return;

        if (openEyesMaterial == null || closedEyesMaterial == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot start blinking: Materials not assigned.", this);
            return;
        }

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkLoop());
    }

    /// <summary>
    /// Stops the automatic blinking loop
    /// </summary>
    public void StopBlinking()
    {
        if (!isBlinking) return;

        isBlinking = false;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // Ensure eyes are open when stopped
        if (decalProjector != null)
        {
            decalProjector.material = openEyesMaterial;
        }
    }

    /// <summary>
    /// Triggers a single blink manually (can be called from UnityEvents)
    /// </summary>
    public void BlinkOnce()
    {
        if (decalProjector == null || openEyesMaterial == null || closedEyesMaterial == null)
            return;

        StartCoroutine(PerformSingleBlink());
    }

    /// <summary>
    /// Sets new materials for the blink animation at runtime
    /// </summary>
    public void SetBlinkMaterials(Material openMaterial, Material closedMaterial)
    {
        openEyesMaterial = openMaterial;
        closedEyesMaterial = closedMaterial;

        if (decalProjector != null && openEyesMaterial != null)
        {
            decalProjector.material = openEyesMaterial;
        }
    }

    /// <summary>
    /// Sets the time between blinks at runtime
    /// </summary>
    public void SetTimeBetweenBlinks(float time)
    {
        timeBetweenBlinks = Mathf.Max(0.1f, time);
    }

    /// <summary>
    /// Sets the blink duration at runtime
    /// </summary>
    public void SetBlinkDuration(float duration)
    {
        blinkDuration = Mathf.Max(0.05f, duration);
    }

    /// <summary>
    /// Sets the random variation percentage at runtime
    /// </summary>
    public void SetRandomPercentage(float percentage)
    {
        randomPercentage = Mathf.Clamp01(percentage);
    }

    private IEnumerator BlinkLoop()
    {
        while (isBlinking)
        {
            // Calculate randomized wait time
            float variation = timeBetweenBlinks * randomPercentage;
            float randomWaitTime = timeBetweenBlinks + Random.Range(-variation, variation);
            randomWaitTime = Mathf.Max(0.1f, randomWaitTime); // Ensure positive

            // Wait before next blink
            yield return new WaitForSeconds(randomWaitTime);

            // Perform blink
            yield return PerformSingleBlink();
        }
    }

    private IEnumerator PerformSingleBlink()
    {
        // Close eyes
        decalProjector.material = closedEyesMaterial;
        onBlinkStart?.Invoke();

        // Wait for blink duration
        yield return new WaitForSeconds(blinkDuration);

        // Open eyes
        decalProjector.material = openEyesMaterial;
        onBlinkComplete?.Invoke();
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
    }

    private void OnDisable()
    {
        // Stop blinking when disabled
        StopBlinking();
    }

#if UNITY_EDITOR
    // Preview in editor
    private void OnValidate()
    {
        // Clamp values
        timeBetweenBlinks = Mathf.Max(0.1f, timeBetweenBlinks);
        blinkDuration = Mathf.Max(0.05f, blinkDuration);
        randomPercentage = Mathf.Clamp01(randomPercentage);
    }
#endif
}
