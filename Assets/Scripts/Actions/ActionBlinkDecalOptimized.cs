using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Optimized version: Automates eye blinking by switching textures on a single material.
/// More efficient than switching entire materials - uses shader property changes instead.
/// Perfect for facial animations using texture-based eye decals.
/// </summary>
[RequireComponent(typeof(DecalProjector))]
public class ActionBlinkDecalOptimized : MonoBehaviour
{
    [Header("Decal Textures")]
    [Tooltip("Texture for open eyes (default state)")]
    [SerializeField] private Texture openEyesTexture;

    [Tooltip("Texture for closed eyes (blink state)")]
    [SerializeField] private Texture closedEyesTexture;

    [Tooltip("Name of the texture property in the shader (usually '_BaseMap' or '_MainTex')")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

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
    private Material materialInstance;
    private Coroutine blinkCoroutine;
    private bool isBlinking = false;
    private int texturePropertyID;

    private void Awake()
    {
        // Get the DecalProjector component
        decalProjector = GetComponent<DecalProjector>();

        if (decalProjector == null)
        {
            Debug.LogError($"[{gameObject.name}] ActionBlinkDecalOptimized requires a DecalProjector component!", this);
            enabled = false;
            return;
        }

        // Validate textures
        if (openEyesTexture == null || closedEyesTexture == null)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionBlinkDecalOptimized: Open or closed eyes texture not assigned. Blinking disabled.", this);
            enabled = false;
            return;
        }

        // Create a material instance (important: prevents modifying the original asset)
        if (decalProjector.material != null)
        {
            materialInstance = new Material(decalProjector.material);
            decalProjector.material = materialInstance;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ActionBlinkDecalOptimized: DecalProjector has no material assigned!", this);
            enabled = false;
            return;
        }

        // Cache the property ID for faster access
        texturePropertyID = Shader.PropertyToID(texturePropertyName);

        // Set initial texture to open eyes
        materialInstance.SetTexture(texturePropertyID, openEyesTexture);
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

        if (openEyesTexture == null || closedEyesTexture == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot start blinking: Textures not assigned.", this);
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
        if (materialInstance != null)
        {
            materialInstance.SetTexture(texturePropertyID, openEyesTexture);
        }
    }

    /// <summary>
    /// Triggers a single blink manually (can be called from UnityEvents)
    /// </summary>
    public void BlinkOnce()
    {
        if (materialInstance == null || openEyesTexture == null || closedEyesTexture == null)
            return;

        StartCoroutine(PerformSingleBlink());
    }

    /// <summary>
    /// Sets new textures for the blink animation at runtime
    /// </summary>
    public void SetBlinkTextures(Texture openTexture, Texture closedTexture)
    {
        openEyesTexture = openTexture;
        closedEyesTexture = closedTexture;

        if (materialInstance != null && openEyesTexture != null)
        {
            materialInstance.SetTexture(texturePropertyID, openEyesTexture);
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
        // Close eyes (switch texture)
        materialInstance.SetTexture(texturePropertyID, closedEyesTexture);
        onBlinkStart?.Invoke();

        // Wait for blink duration
        yield return new WaitForSeconds(blinkDuration);

        // Open eyes (switch texture back)
        materialInstance.SetTexture(texturePropertyID, openEyesTexture);
        onBlinkComplete?.Invoke();
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        // Clean up material instance
        if (materialInstance != null)
        {
            Destroy(materialInstance);
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
