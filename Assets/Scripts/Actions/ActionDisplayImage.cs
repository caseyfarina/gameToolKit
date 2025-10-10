using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays UI images on screen with fade and scale animation effects.
/// Common use: Item pickup previews, achievement icons, cutscene frames, tutorial images, or inventory item displays.
/// </summary>
[RequireComponent(typeof(Image))]
public class ActionDisplayImage : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("How long the image stays visible on screen (in seconds)")]
    [SerializeField] private float timeOnScreen = 3f;
    
    [Header("Image Appearance")]
    [Tooltip("Default image to display (optional)")]
    [SerializeField] private Sprite defaultImage;
    
    [Tooltip("Should image fade in/out or appear instantly?")]
    [SerializeField] private bool useFading = true;
    
    [Tooltip("Duration of fade in/out animations")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Header("Scaling Options")]
    [Tooltip("Should the image scale in/out during display?")]
    [SerializeField] private bool useScaling = false;
    
    [Tooltip("Starting scale for scale-in animation")]
    [SerializeField] private Vector3 startScale = Vector3.zero;
    
    [Tooltip("Target scale during display")]
    [SerializeField] private Vector3 targetScale = Vector3.one;
    
    [Tooltip("Duration of scale animations")]
    [SerializeField] private float scaleDuration = 0.5f;
    
    private Image imageComponent;
    private RectTransform rectTransform;
    private Coroutine displayCoroutine;
    private Color originalColor;
    private Vector3 originalScale;
    
    private void Start()
    {
        // Get required components
        imageComponent = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        if (imageComponent == null)
        {
            Debug.LogError("ActionDisplayImage requires an Image component!");
            return;
        }
        
        // Store original values
        originalColor = imageComponent.color;
        originalScale = rectTransform.localScale;
        
        // Set default image if specified
        if (defaultImage != null)
        {
            imageComponent.sprite = defaultImage;
        }
        
        // Make image invisible initially
        SetImageVisibility(0f);
    }
    
    /// <summary>
    /// Display image on screen for the specified duration
    /// This method is designed to be called from UnityEvents with a Sprite parameter
    /// </summary>
    /// <param name="imageToDisplay">The sprite to display</param>
    public void DisplayImage(Sprite imageToDisplay)
    {
        if (imageComponent == null)
        {
            Debug.LogWarning("Image component is missing!");
            return;
        }
        
        if (imageToDisplay == null)
        {
            Debug.LogWarning("No sprite provided to display!");
            return;
        }
        
        // Stop any currently running display coroutine
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
        
        // Start the new display sequence
        displayCoroutine = StartCoroutine(DisplayImageSequence(imageToDisplay));
    }
    
    /// <summary>
    /// Display the default image (if set) for the specified duration
    /// </summary>
    public void DisplayDefaultImage()
    {
        if (defaultImage != null)
        {
            DisplayImage(defaultImage);
        }
        else
        {
            Debug.LogWarning("No default image set!");
        }
    }
    
    /// <summary>
    /// Display image with custom duration (for advanced use)
    /// </summary>
    /// <param name="imageToDisplay">The sprite to display</param>
    /// <param name="customDuration">How long to show the image</param>
    public void DisplayImage(Sprite imageToDisplay, float customDuration)
    {
        float originalDuration = timeOnScreen;
        timeOnScreen = customDuration;
        DisplayImage(imageToDisplay);
        timeOnScreen = originalDuration;
    }
    
    private IEnumerator DisplayImageSequence(Sprite imageToDisplay)
    {
        // Set the image sprite
        imageComponent.sprite = imageToDisplay;
        
        // Calculate animation durations
        float actualFadeDuration = useFading ? fadeDuration : 0f;
        float actualScaleDuration = useScaling ? scaleDuration : 0f;
        float maxAnimationDuration = Mathf.Max(actualFadeDuration, actualScaleDuration);
        
        // Set initial states
        if (useScaling)
        {
            rectTransform.localScale = startScale;
        }
        
        // Animate in (fade and/or scale)
        if (useFading || useScaling)
        {
            yield return StartCoroutine(AnimateIn(actualFadeDuration, actualScaleDuration));
        }
        else
        {
            // Show instantly
            SetImageVisibility(originalColor.a);
        }
        
        // Wait for display time (minus animation durations)
        float waitTime = Mathf.Max(0f, timeOnScreen - (maxAnimationDuration * 2f));
        yield return new WaitForSeconds(waitTime);
        
        // Animate out (fade and/or scale)
        if (useFading || useScaling)
        {
            yield return StartCoroutine(AnimateOut(actualFadeDuration, actualScaleDuration));
        }
        else
        {
            // Hide instantly
            SetImageVisibility(0f);
        }
        
        // Reset to original scale
        if (useScaling)
        {
            rectTransform.localScale = originalScale;
        }
        
        // Clear the image sprite (optional - keeps last image)
        // imageComponent.sprite = null;
        displayCoroutine = null;
    }
    
    private IEnumerator AnimateIn(float fadeTime, float scaleTime)
    {
        float maxTime = Mathf.Max(fadeTime, scaleTime);
        float elapsedTime = 0f;
        
        while (elapsedTime < maxTime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / maxTime;
            
            // Handle fading
            if (useFading && fadeTime > 0f)
            {
                float fadeProgress = Mathf.Clamp01(elapsedTime / fadeTime);
                float currentAlpha = Mathf.Lerp(0f, originalColor.a, fadeProgress);
                SetImageVisibility(currentAlpha);
            }
            
            // Handle scaling
            if (useScaling && scaleTime > 0f)
            {
                float scaleProgress = Mathf.Clamp01(elapsedTime / scaleTime);
                Vector3 currentScale = Vector3.Lerp(startScale, targetScale, scaleProgress);
                rectTransform.localScale = currentScale;
            }
            
            yield return null;
        }
        
        // Ensure we end at target values
        if (useFading)
            SetImageVisibility(originalColor.a);
        if (useScaling)
            rectTransform.localScale = targetScale;
    }
    
    private IEnumerator AnimateOut(float fadeTime, float scaleTime)
    {
        float maxTime = Mathf.Max(fadeTime, scaleTime);
        float elapsedTime = 0f;
        
        Vector3 currentScale = rectTransform.localScale;
        
        while (elapsedTime < maxTime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / maxTime;
            
            // Handle fading
            if (useFading && fadeTime > 0f)
            {
                float fadeProgress = Mathf.Clamp01(elapsedTime / fadeTime);
                float currentAlpha = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
                SetImageVisibility(currentAlpha);
            }
            
            // Handle scaling
            if (useScaling && scaleTime > 0f)
            {
                float scaleProgress = Mathf.Clamp01(elapsedTime / scaleTime);
                Vector3 targetScaleOut = startScale;
                Vector3 newScale = Vector3.Lerp(currentScale, targetScaleOut, scaleProgress);
                rectTransform.localScale = newScale;
            }
            
            yield return null;
        }
        
        // Ensure we end at target values
        if (useFading)
            SetImageVisibility(0f);
        if (useScaling)
            rectTransform.localScale = startScale;
    }
    
    private void SetImageVisibility(float alpha)
    {
        if (imageComponent != null)
        {
            Color newColor = originalColor;
            newColor.a = alpha;
            imageComponent.color = newColor;
        }
    }
    
    /// <summary>
    /// Immediately hide any currently displayed image
    /// </summary>
    public void HideImage()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
        
        SetImageVisibility(0f);
        
        // Reset scale if using scaling
        if (useScaling && rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }
    
    /// <summary>
    /// Set the display duration for future image displays
    /// </summary>
    public void SetDisplayDuration(float newDuration)
    {
        timeOnScreen = Mathf.Max(0.1f, newDuration);
    }
    
    /// <summary>
    /// Set the default image to use with DisplayDefaultImage()
    /// </summary>
    public void SetDefaultImage(Sprite newDefaultImage)
    {
        defaultImage = newDefaultImage;
    }
    
    /// <summary>
    /// Check if an image is currently being displayed
    /// </summary>
    public bool IsDisplaying()
    {
        return displayCoroutine != null;
    }
    
    /// <summary>
    /// Get the currently displayed sprite
    /// </summary>
    public Sprite GetCurrentSprite()
    {
        return imageComponent != null ? imageComponent.sprite : null;
    }
}