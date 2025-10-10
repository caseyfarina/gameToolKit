using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    
    private TextMeshProUGUI textComponent;
    private Coroutine displayCoroutine;
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
        // Set the text content
        textComponent.text = message;
        
        if (useFading)
        {
            // Fade in
            yield return StartCoroutine(FadeText(0f, originalColor.a, fadeDuration));
            
            // Wait for display time (minus fade durations)
            float waitTime = Mathf.Max(0f, timeOnScreen - (fadeDuration * 2f));
            yield return new WaitForSeconds(waitTime);
            
            // Fade out
            yield return StartCoroutine(FadeText(originalColor.a, 0f, fadeDuration));
        }
        else
        {
            // Show instantly
            SetTextVisibility(originalColor.a);
            
            // Wait for display time
            yield return new WaitForSeconds(timeOnScreen);
            
            // Hide instantly
            SetTextVisibility(0f);
        }
        
        // Clear the text content
        textComponent.text = "";
        displayCoroutine = null;
    }
    
    private IEnumerator FadeText(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;
            
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, normalizedTime);
            SetTextVisibility(currentAlpha);
            
            yield return null;
        }
        
        // Ensure we end at exactly the target alpha
        SetTextVisibility(toAlpha);
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
        
        SetTextVisibility(0f);
        if (textComponent != null)
        {
            textComponent.text = "";
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
}