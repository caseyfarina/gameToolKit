using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Displays text messages on screen with optional fade effects and customizable duration.
/// Common use: Tutorial hints, dialogue systems, notification messages, score popups, or objective updates.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ActionDisplayText : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("How long the text stays visible on screen (in seconds)")]
    [SerializeField] private float timeOnScreen = 3f;

    [Header("Text Appearance")]
    [Tooltip("Font to use for displayed text")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("Should text fade in/out or appear instantly?")]
    [SerializeField] private bool useFading = true;

    [Tooltip("Duration of fade in/out animations")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Typewriter Effect")]
    [Tooltip("Should text appear one character at a time (typewriter effect)?")]
    [SerializeField] private bool useTypewriter = false;

    [Tooltip("How many characters appear per second (typewriter speed)")]
    [SerializeField] private float charactersPerSecond = 20f;
    
    private TextMeshProUGUI textComponent;
    private Coroutine displayCoroutine;
    private Sequence displaySequence;
    private Color originalColor;
    
    private void Start()
    {
        // Get the TextMeshPro component
        textComponent = GetComponent<TextMeshProUGUI>();
        
        if (textComponent == null)
        {
            Debug.LogError("ActionDisplayText requires a TextMeshProUGUI component!");
            return;
        }
        
        // Apply font if specified
        if (font != null)
        {
            textComponent.font = font;
        }
        
        // Store original color and make text invisible initially
        originalColor = textComponent.color;
        SetTextVisibility(0f);
    }
    
    /// <summary>
    /// Display text on screen for the specified duration
    /// This method is designed to be called from UnityEvents with a string parameter
    /// </summary>
    /// <param name="message">The text to display</param>
    public void DisplayText(string message)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("TextMeshProUGUI component is missing!");
            return;
        }
        
        // Stop any currently running display coroutine
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        // Stop any currently running DOTween sequence
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
        }

        // Start the new display sequence
        displayCoroutine = StartCoroutine(DisplayTextSequence(message));
    }
    
    /// <summary>
    /// Display text with custom duration (for advanced use)
    /// </summary>
    /// <param name="message">The text to display</param>
    /// <param name="customDuration">How long to show the text</param>
    public void DisplayText(string message, float customDuration)
    {
        float originalDuration = timeOnScreen;
        timeOnScreen = customDuration;
        DisplayText(message);
        timeOnScreen = originalDuration;
    }
    
    private IEnumerator DisplayTextSequence(string message)
    {
        float typewriterDuration = 0f;

        if (useTypewriter)
        {
            // Calculate typewriter duration
            typewriterDuration = message.Length / charactersPerSecond;

            // Set full text (invisible) to reserve space
            textComponent.text = message;
            textComponent.maxVisibleCharacters = 0;
        }
        else
        {
            // Set the text content normally
            textComponent.text = message;
        }

        // Create DOTween sequence for the display animation
        displaySequence = DOTween.Sequence();

        if (useFading)
        {
            // Set initial alpha to 0 and fade in
            SetTextVisibility(0f);
            displaySequence.Append(DOTween.To(() => textComponent.color, x => textComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a), fadeDuration));
        }
        else
        {
            // Show instantly
            SetTextVisibility(originalColor.a);
        }

        // Typewriter effect (if enabled) - still uses coroutine as it's character-based
        if (useTypewriter)
        {
            yield return StartCoroutine(TypewriterText(message));
        }

        // Wait for display time (minus fade and typewriter durations)
        float waitTime = Mathf.Max(0f, timeOnScreen - (useFading ? fadeDuration * 2f : 0f) - typewriterDuration);
        yield return new WaitForSeconds(waitTime);

        if (useFading)
        {
            // Fade out using DOTween
            DOTween.To(() => textComponent.color, x => textComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, 0f), fadeDuration).OnComplete(() =>
            {
                // Clear the text content
                textComponent.text = "";
                textComponent.maxVisibleCharacters = 99999; // Reset to default
                displaySequence = null;
                displayCoroutine = null;
            });
        }
        else
        {
            // Hide instantly
            SetTextVisibility(0f);

            // Clear the text content
            textComponent.text = "";
            textComponent.maxVisibleCharacters = 99999; // Reset to default
            displaySequence = null;
            displayCoroutine = null;
        }
    }

    private IEnumerator TypewriterText(string message)
    {
        int totalCharacters = message.Length;
        float delay = 1f / charactersPerSecond;

        for (int i = 0; i <= totalCharacters; i++)
        {
            textComponent.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
    }
    private void SetTextVisibility(float alpha)
    {
        if (textComponent != null)
        {
            Color newColor = originalColor;
            newColor.a = alpha;
            textComponent.color = newColor;
        }
    }
    
    /// <summary>
    /// Immediately hide any currently displayed text
    /// </summary>
    public void HideText()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
            displaySequence = null;
        }

        // Kill any individual fade tweens
        textComponent.DOKill();

        SetTextVisibility(0f);
        if (textComponent != null)
        {
            textComponent.text = "";
        }
    }

    private void OnDestroy()
    {
        // Clean up DOTween sequences and tweens when this object is destroyed
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
        }
        if (textComponent != null)
        {
            textComponent.DOKill();
        }
    }
    
    /// <summary>
    /// Set the display duration for future text displays
    /// </summary>
    public void SetDisplayDuration(float newDuration)
    {
        timeOnScreen = Mathf.Max(0.1f, newDuration);
    }
    
    /// <summary>
    /// Check if text is currently being displayed
    /// </summary>
    public bool IsDisplaying()
    {
        return displayCoroutine != null;
    }

    /// <summary>
    /// Enable or disable the typewriter effect
    /// </summary>
    public void SetTypewriterEffect(bool enabled)
    {
        useTypewriter = enabled;
    }

    /// <summary>
    /// Set the typewriter speed (characters per second)
    /// </summary>
    public void SetTypewriterSpeed(float speed)
    {
        charactersPerSecond = Mathf.Max(1f, speed);
    }
}