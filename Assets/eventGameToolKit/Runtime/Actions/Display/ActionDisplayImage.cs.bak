using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    private Sequence displaySequence;
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
        
        // Stop any currently running display sequence
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
        }

        // Start the new display sequence
        StartCoroutine(DisplayImageSequence(imageToDisplay));
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
        if (useFading)
        {
            SetImageVisibility(0f);
        }
        else
        {
            SetImageVisibility(originalColor.a);
        }

        if (useScaling)
        {
            rectTransform.localScale = startScale;
        }

        // Create the animation sequence
        displaySequence = DOTween.Sequence();

        // Animate in (fade and/or scale)
        if (useFading)
        {
            // Use DOTween.To to animate the alpha value of the Image color
            displaySequence.Join(DOTween.To(() => imageComponent.color, x => imageComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a), actualFadeDuration));
        }

        if (useScaling)
        {
            displaySequence.Join(rectTransform.DOScale(targetScale, actualScaleDuration));
        }

        // Wait for display time (minus animation durations)
        float waitTime = Mathf.Max(0f, timeOnScreen - (maxAnimationDuration * 2f));
        displaySequence.AppendInterval(waitTime);

        // Animate out (fade and/or scale)
        if (useFading)
        {
            // Use DOTween.To to animate the alpha value of the Image color to 0
            displaySequence.Append(DOTween.To(() => imageComponent.color, x => imageComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, 0f), actualFadeDuration));
        }

        if (useScaling)
        {
            displaySequence.Join(rectTransform.DOScale(startScale, actualScaleDuration));
        }

        // Reset to original scale when complete
        displaySequence.OnComplete(() =>
        {
            if (useScaling)
            {
                rectTransform.localScale = originalScale;
            }
            displaySequence = null;
        });

        yield break;
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
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
            displaySequence = null;
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
        return displaySequence != null && displaySequence.IsActive();
    }

    private void OnDestroy()
    {
        // Clean up DOTween sequences when this object is destroyed
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
        }
    }
    
    /// <summary>
    /// Get the currently displayed sprite
    /// </summary>
    public Sprite GetCurrentSprite()
    {
        return imageComponent != null ? imageComponent.sprite : null;
    }
}