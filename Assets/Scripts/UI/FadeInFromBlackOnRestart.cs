using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Automatically fades in from black every time the scene loads or restarts.
/// No event wiring required - just attach to a full-screen black UI Image.
/// Common use: Scene transition coverage, hiding checkpoint restoration, smooth restart transitions.
/// </summary>
[RequireComponent(typeof(Image))]
public class FadeInFromBlackOnRestart : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("How long the fade from black takes (in seconds)")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Tooltip("Optional delay before fade starts (useful for waiting for checkpoint restoration)")]
    [SerializeField] private float fadeStartDelay = 0f;

    [Header("Events")]
    [Tooltip("Called when fade completes")]
    public UnityEvent onFadeComplete;

    private Image imageComponent;

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent == null)
        {
            Debug.LogError("FadeInFromBlackOnRestart requires an Image component!");
            enabled = false;
            return;
        }

        // Enable Image component when game starts (allows it to be disabled in editor to not block view)
        imageComponent.enabled = true;

        // Start fully black (opaque)
        Color color = imageComponent.color;
        color.a = 1f;
        imageComponent.color = color;
    }

    private void Start()
    {
        // Automatically fade out on every scene load/restart
        FadeOutFromBlack();
    }

    private void FadeOutFromBlack()
    {
        // Fade from alpha 1 (black) to alpha 0 (transparent) using DOTween
        imageComponent.DOFade(0f, fadeDuration)
            .SetDelay(fadeStartDelay)
            .SetUpdate(true) // Use unscaled time so it works during pause
            .OnComplete(() => onFadeComplete?.Invoke());
    }

    private void OnDestroy()
    {
        // Clean up DOTween tweens when this object is destroyed
        if (imageComponent != null)
        {
            imageComponent.DOKill();
        }
    }

    /// <summary>
    /// Manually set fade duration at runtime
    /// </summary>
    public void SetFadeDuration(float newDuration)
    {
        fadeDuration = Mathf.Max(0f, newDuration);
    }

    /// <summary>
    /// Manually set fade delay at runtime
    /// </summary>
    public void SetFadeDelay(float newDelay)
    {
        fadeStartDelay = Mathf.Max(0f, newDelay);
    }
}
