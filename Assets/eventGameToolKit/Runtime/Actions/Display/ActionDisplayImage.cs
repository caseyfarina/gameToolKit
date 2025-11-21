using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// Displays UI images on screen with fade and scale animation effects.
/// Creates its own Canvas and Image at runtime - no manual UI setup required.
/// Common use: Item pickup previews, achievement icons, cutscene frames, tutorial images, or inventory item displays.
/// </summary>
public class ActionDisplayImage : MonoBehaviour
{
    [Header("Image Settings")]
    [Tooltip("Default image to display (optional)")]
    [SerializeField] private Sprite defaultImage;

    [Tooltip("Position of the image on screen (0,0 = center)")]
    [SerializeField] private Vector2 imagePosition = Vector2.zero;

    [Tooltip("Size of the image in pixels")]
    [SerializeField] private Vector2 imageSize = new Vector2(400, 400);

    [Header("Display Duration")]
    [Tooltip("How long the image stays visible on screen (in seconds)")]
    [SerializeField] private float timeOnScreen = 3f;

    [Header("Fade Animation")]
    [Tooltip("Should image fade in/out or appear instantly?")]
    [SerializeField] private bool useFading = true;

    [Tooltip("Duration of fade in/out animations")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Scale Animation")]
    [Tooltip("Should the image scale in/out during display?")]
    [SerializeField] private bool useScaling = false;

    [Tooltip("Starting scale for scale-in animation")]
    [SerializeField] private Vector3 startScale = Vector3.zero;

    [Tooltip("Target scale during display")]
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Tooltip("Duration of scale animations")]
    [SerializeField] private float scaleDuration = 0.5f;

    [Header("Events")]
    /// <summary>
    /// Fires when the image starts displaying
    /// </summary>
    public UnityEvent onImageDisplayStart;

    /// <summary>
    /// Fires when the image finishes displaying and hides
    /// </summary>
    public UnityEvent onImageDisplayComplete;

    // Runtime UI references
    private Canvas canvas;
    private GameObject imageCanvas;
    private Image imageComponent;
    private RectTransform imageRectTransform;
    private Sequence displaySequence;
    private Color originalColor = Color.white;

    private void Start()
    {
        // Create UI at runtime
        if (Application.isPlaying)
        {
            CreateImageUI();
        }
    }

    /// <summary>
    /// Creates the canvas and image element at runtime
    /// </summary>
    private void CreateImageUI()
    {
        // Create Canvas container
        imageCanvas = new GameObject("ImageCanvas");
        imageCanvas.transform.SetParent(transform);

        canvas = imageCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = imageCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        imageCanvas.AddComponent<GraphicRaycaster>();

        // Create Image GameObject
        GameObject imageObj = new GameObject("DisplayImage");
        imageObj.transform.SetParent(imageCanvas.transform);

        imageRectTransform = imageObj.AddComponent<RectTransform>();
        imageRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        imageRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
        imageRectTransform.anchoredPosition = imagePosition;
        imageRectTransform.sizeDelta = imageSize;

        imageComponent = imageObj.AddComponent<Image>();
        imageComponent.sprite = defaultImage;
        imageComponent.color = new Color(1f, 1f, 1f, 0f); // Start invisible

        // Store original color
        originalColor = new Color(1f, 1f, 1f, 1f);

        // Hide canvas initially
        imageCanvas.SetActive(false);
    }

    /// <summary>
    /// Display image on screen for the configured duration (uses timeOnScreen parameter)
    /// Image will automatically hide after the duration with animations
    /// </summary>
    public void DisplayImageTimed(Sprite imageToDisplay)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("DisplayImageTimed can only be called at runtime!");
            return;
        }

        if (imageComponent == null)
        {
            Debug.LogWarning("Image component is missing! Canvas may not have been created.");
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

        // Set the image sprite
        imageComponent.sprite = imageToDisplay;

        // Show canvas
        imageCanvas.SetActive(true);

        // Start animation sequence with auto-hide
        AnimateImageDisplay(true);
    }

    /// <summary>
    /// Display image on screen indefinitely (stays visible until HideImage is called)
    /// Image will play fade-in/scale-in animations but will NOT auto-hide
    /// </summary>
    public void DisplayImage(Sprite imageToDisplay)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("DisplayImage can only be called at runtime!");
            return;
        }

        if (imageComponent == null)
        {
            Debug.LogWarning("Image component is missing! Canvas may not have been created.");
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

        // Set the image sprite
        imageComponent.sprite = imageToDisplay;

        // Show canvas
        imageCanvas.SetActive(true);

        // Start animation sequence WITHOUT auto-hide
        AnimateImageDisplay(false);
    }

    /// <summary>
    /// Display the default image (if set) for the configured duration
    /// </summary>
    public void DisplayDefaultImageTimed()
    {
        if (defaultImage != null)
        {
            DisplayImageTimed(defaultImage);
        }
        else
        {
            Debug.LogWarning("No default image set!");
        }
    }

    /// <summary>
    /// Display the default image (if set) indefinitely (stays until HideImage is called)
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
    public void DisplayImageTimed(Sprite imageToDisplay, float customDuration)
    {
        float originalDuration = timeOnScreen;
        timeOnScreen = customDuration;
        DisplayImageTimed(imageToDisplay);
        timeOnScreen = originalDuration;
    }

    private void AnimateImageDisplay(bool autoHide)
    {
        // Fire start event
        onImageDisplayStart?.Invoke();

        // Calculate animation durations
        float actualFadeDuration = useFading ? fadeDuration : 0f;
        float actualScaleDuration = useScaling ? scaleDuration : 0f;
        float maxAnimationDuration = Mathf.Max(actualFadeDuration, actualScaleDuration);

        // Set initial states
        if (useFading)
        {
            imageComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        else
        {
            imageComponent.color = originalColor;
        }

        if (useScaling)
        {
            imageRectTransform.localScale = startScale;
        }
        else
        {
            imageRectTransform.localScale = Vector3.one;
        }

        // Create the animation sequence
        displaySequence = DOTween.Sequence();

        // Animate in (fade and/or scale)
        if (useFading)
        {
            displaySequence.Join(DOTween.To(
                () => imageComponent.color,
                x => imageComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a),
                actualFadeDuration
            ));
        }

        if (useScaling)
        {
            displaySequence.Join(imageRectTransform.DOScale(targetScale, actualScaleDuration));
        }

        // If autoHide is true, add wait time and animate out
        if (autoHide)
        {
            // Wait for display time (minus animation durations)
            float waitTime = Mathf.Max(0f, timeOnScreen - (maxAnimationDuration * 2f));
            displaySequence.AppendInterval(waitTime);

            // Animate out (fade and/or scale)
            if (useFading)
            {
                displaySequence.Append(DOTween.To(
                    () => imageComponent.color,
                    x => imageComponent.color = x,
                    new Color(originalColor.r, originalColor.g, originalColor.b, 0f),
                    actualFadeDuration
                ));
            }

            if (useScaling)
            {
                displaySequence.Join(imageRectTransform.DOScale(startScale, actualScaleDuration));
            }

            // Cleanup when complete
            displaySequence.OnComplete(() =>
            {
                imageCanvas.SetActive(false);
                imageRectTransform.localScale = Vector3.one;
                displaySequence = null;
                onImageDisplayComplete?.Invoke();
            });
        }
        else
        {
            // For indefinite display, just cleanup the sequence after fade-in completes
            displaySequence.OnComplete(() =>
            {
                displaySequence = null;
            });
        }
    }

    /// <summary>
    /// Hide the currently displayed image with fade-out/scale-out animations
    /// Use this to manually hide images displayed with DisplayImage()
    /// </summary>
    public void HideImage()
    {
        if (!Application.isPlaying || imageCanvas == null || !imageCanvas.activeSelf)
        {
            return;
        }

        // Stop any currently running display sequence
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
        }

        // Calculate animation durations
        float actualFadeDuration = useFading ? fadeDuration : 0f;
        float actualScaleDuration = useScaling ? scaleDuration : 0f;

        // Create hide animation sequence
        displaySequence = DOTween.Sequence();

        // Animate out (fade and/or scale)
        if (useFading)
        {
            displaySequence.Append(DOTween.To(
                () => imageComponent.color,
                x => imageComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, 0f),
                actualFadeDuration
            ));
        }

        if (useScaling)
        {
            displaySequence.Join(imageRectTransform.DOScale(startScale, actualScaleDuration));
        }

        // Cleanup when complete
        displaySequence.OnComplete(() =>
        {
            imageCanvas.SetActive(false);
            imageRectTransform.localScale = Vector3.one;
            displaySequence = null;
            onImageDisplayComplete?.Invoke();
        });
    }

    /// <summary>
    /// Immediately hide the image without animations (instant hide)
    /// </summary>
    public void HideImageImmediate()
    {
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
            displaySequence = null;
        }

        if (imageCanvas != null)
        {
            imageCanvas.SetActive(false);
        }

        if (imageRectTransform != null)
        {
            imageRectTransform.localScale = Vector3.one;
        }

        if (imageComponent != null)
        {
            imageComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }

        onImageDisplayComplete?.Invoke();
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
    /// Set the image position at runtime
    /// </summary>
    public void SetImagePosition(Vector2 newPosition)
    {
        imagePosition = newPosition;
        if (imageRectTransform != null)
        {
            imageRectTransform.anchoredPosition = newPosition;
        }
    }

    /// <summary>
    /// Set the image size at runtime
    /// </summary>
    public void SetImageSize(Vector2 newSize)
    {
        imageSize = newSize;
        if (imageRectTransform != null)
        {
            imageRectTransform.sizeDelta = newSize;
        }
    }

    /// <summary>
    /// Check if an image is currently being displayed
    /// </summary>
    public bool IsDisplaying()
    {
        return displaySequence != null && displaySequence.IsActive();
    }

    /// <summary>
    /// Get the currently displayed sprite
    /// </summary>
    public Sprite GetCurrentSprite()
    {
        return imageComponent != null ? imageComponent.sprite : null;
    }

    private void OnDestroy()
    {
        // Clean up DOTween sequences when this object is destroyed
        if (displaySequence != null && displaySequence.IsActive())
        {
            displaySequence.Kill();
        }

        // Clean up created UI
        if (imageCanvas != null)
        {
            Destroy(imageCanvas);
        }
    }
}
