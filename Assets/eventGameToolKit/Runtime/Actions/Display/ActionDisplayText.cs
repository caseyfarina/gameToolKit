using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Displays text messages on screen with fade and typewriter effects.
/// Creates its own Canvas and TextMeshProUGUI at runtime - no manual UI setup required.
/// Common use: Tutorial hints, dialogue systems, notification messages, score popups, or objective updates.
/// </summary>
public class ActionDisplayText : MonoBehaviour
{
    [Header("Text Settings")]
    [Tooltip("Default text to display (optional)")]
    [SerializeField] private string defaultText = "";

    [Tooltip("Position of the text on screen (0,0 = center)")]
    [SerializeField] private Vector2 textPosition = Vector2.zero;

    [Tooltip("Size of the text box in pixels")]
    [SerializeField] private Vector2 textSize = new Vector2(800, 200);

    [Tooltip("Font size")]
    [SerializeField] private float fontSize = 48f;

    [Tooltip("Text alignment")]
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Left;

    [Tooltip("Text color")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Font to use for displayed text")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Display Duration")]
    [Tooltip("How long the text stays visible on screen (in seconds)")]
    [SerializeField] private float timeOnScreen = 3f;

    [Header("Fade Animation")]
    [Tooltip("Should text fade in/out or appear instantly?")]
    [SerializeField] private bool useFading = true;

    [Tooltip("Duration of fade in/out animations")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Typewriter Effect")]
    [Tooltip("Should text appear one character at a time (typewriter effect)?")]
    [SerializeField] private bool useTypewriter = false;

    [Tooltip("How many characters appear per second (typewriter speed)")]
    [SerializeField] private float charactersPerSecond = 20f;

    [Header("Events")]
    /// <summary>
    /// Fires when the text starts displaying
    /// </summary>
    public UnityEvent onTextDisplayStart;

    /// <summary>
    /// Fires when the text finishes displaying and hides
    /// </summary>
    public UnityEvent onTextDisplayComplete;

    // Runtime UI references
    private Canvas canvas;
    private GameObject textCanvas;
    private TextMeshProUGUI textComponent;
    private RectTransform textRectTransform;
    private Coroutine displayCoroutine;
    private Tween displaySequence;
    private Color originalColor;

    private void Start()
    {
        // Create UI at runtime
        if (Application.isPlaying)
        {
            CreateTextUI();
        }
    }

    /// <summary>
    /// Creates the canvas and text element at runtime
    /// </summary>
    private void CreateTextUI()
    {
        // Create Canvas container
        textCanvas = new GameObject("TextCanvas");
        textCanvas.transform.SetParent(transform);

        canvas = textCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = textCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        textCanvas.AddComponent<GraphicRaycaster>();

        // Create TextMeshProUGUI GameObject
        GameObject textObj = new GameObject("DisplayText");
        textObj.transform.SetParent(textCanvas.transform);

        textRectTransform = textObj.AddComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textRectTransform.pivot = new Vector2(0.5f, 0.5f);
        textRectTransform.anchoredPosition = textPosition;
        textRectTransform.sizeDelta = textSize;

        textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = fontSize;
        textComponent.alignment = textAlignment;
        textComponent.color = new Color(textColor.r, textColor.g, textColor.b, 0f); // Start invisible

        if (font != null)
        {
            textComponent.font = font;
        }

        textComponent.text = "";

        // Store original color
        originalColor = textColor;

        // Hide canvas initially
        textCanvas.SetActive(false);
    }

    /// <summary>
    /// Display text on screen for the configured duration (uses timeOnScreen parameter)
    /// Text will automatically hide after the duration with animations
    /// </summary>
    public void DisplayTextTimed(string message)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("DisplayTextTimed can only be called at runtime!");
            return;
        }

        if (textComponent == null)
        {
            Debug.LogWarning("Text component is missing! Canvas may not have been created.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("No message provided to display!");
            return;
        }

        // Stop any currently running display
        StopCurrentDisplay();

        // Show canvas
        textCanvas.SetActive(true);

        // Start animation sequence with auto-hide
        displayCoroutine = StartCoroutine(DisplayTextSequence(message, true));
    }

    /// <summary>
    /// Display text on screen indefinitely (stays visible until HideText is called)
    /// Text will play fade-in/typewriter animations but will NOT auto-hide
    /// </summary>
    public void DisplayText(string message)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("DisplayText can only be called at runtime!");
            return;
        }

        if (textComponent == null)
        {
            Debug.LogWarning("Text component is missing! Canvas may not have been created.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("No message provided to display!");
            return;
        }

        // Stop any currently running display
        StopCurrentDisplay();

        // Show canvas
        textCanvas.SetActive(true);

        // Start animation sequence WITHOUT auto-hide
        displayCoroutine = StartCoroutine(DisplayTextSequence(message, false));
    }

    /// <summary>
    /// Display the default text (if set) for the configured duration
    /// </summary>
    public void DisplayDefaultTextTimed()
    {
        if (!string.IsNullOrEmpty(defaultText))
        {
            DisplayTextTimed(defaultText);
        }
        else
        {
            Debug.LogWarning("No default text set!");
        }
    }

    /// <summary>
    /// Display the default text (if set) indefinitely (stays until HideText is called)
    /// </summary>
    public void DisplayDefaultText()
    {
        if (!string.IsNullOrEmpty(defaultText))
        {
            DisplayText(defaultText);
        }
        else
        {
            Debug.LogWarning("No default text set!");
        }
    }

    /// <summary>
    /// Display text with custom duration (for advanced use)
    /// </summary>
    public void DisplayTextTimed(string message, float customDuration)
    {
        float originalDuration = timeOnScreen;
        timeOnScreen = customDuration;
        DisplayTextTimed(message);
        timeOnScreen = originalDuration;
    }

    private IEnumerator DisplayTextSequence(string message, bool autoHide)
    {
        // Fire start event
        onTextDisplayStart?.Invoke();

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

        // Fade in
        if (useFading)
        {
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

            displaySequence = DOTween.To(
                () => textComponent.color,
                x => textComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a),
                fadeDuration
            );

            yield return displaySequence.WaitForCompletion();
        }
        else
        {
            textComponent.color = originalColor;
        }

        // Typewriter effect (if enabled)
        if (useTypewriter)
        {
            yield return StartCoroutine(TypewriterText(message));
        }

        // If autoHide is true, wait and then fade out
        if (autoHide)
        {
            // Wait for display time (minus fade and typewriter durations)
            float waitTime = Mathf.Max(0f, timeOnScreen - (useFading ? fadeDuration * 2f : 0f) - typewriterDuration);
            yield return new WaitForSeconds(waitTime);

            // Fade out
            if (useFading)
            {
                displaySequence = DOTween.To(
                    () => textComponent.color,
                    x => textComponent.color = x,
                    new Color(originalColor.r, originalColor.g, originalColor.b, 0f),
                    fadeDuration
                );

                yield return displaySequence.WaitForCompletion();
            }

            // Hide and cleanup
            textCanvas.SetActive(false);
            textComponent.text = "";
            textComponent.maxVisibleCharacters = 99999;
            displayCoroutine = null;
            displaySequence = null;

            onTextDisplayComplete?.Invoke();
        }
        else
        {
            // For indefinite display, just cleanup the coroutine reference
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

    /// <summary>
    /// Hide the currently displayed text with fade-out animation
    /// Use this to manually hide text displayed with DisplayText()
    /// </summary>
    public void HideText()
    {
        if (!Application.isPlaying || textCanvas == null || !textCanvas.activeSelf)
        {
            return;
        }

        // Stop any currently running display
        StopCurrentDisplay();

        // Start hide coroutine
        StartCoroutine(HideTextSequence());
    }

    private IEnumerator HideTextSequence()
    {
        // Fade out
        if (useFading)
        {
            displaySequence = DOTween.To(
                () => textComponent.color,
                x => textComponent.color = x,
                new Color(originalColor.r, originalColor.g, originalColor.b, 0f),
                fadeDuration
            );

            yield return displaySequence.WaitForCompletion();
        }

        // Hide and cleanup
        textCanvas.SetActive(false);
        textComponent.text = "";
        textComponent.maxVisibleCharacters = 99999;
        displaySequence = null;

        onTextDisplayComplete?.Invoke();
    }

    /// <summary>
    /// Immediately hide the text without animations (instant hide)
    /// </summary>
    public void HideTextImmediate()
    {
        StopCurrentDisplay();

        if (textCanvas != null)
        {
            textCanvas.SetActive(false);
        }

        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.maxVisibleCharacters = 99999;
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }

        onTextDisplayComplete?.Invoke();
    }

    private void StopCurrentDisplay()
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
    /// Set the default text to use with DisplayDefaultText()
    /// </summary>
    public void SetDefaultText(string newDefaultText)
    {
        defaultText = newDefaultText;
    }

    /// <summary>
    /// Set the text position at runtime
    /// </summary>
    public void SetTextPosition(Vector2 newPosition)
    {
        textPosition = newPosition;
        if (textRectTransform != null)
        {
            textRectTransform.anchoredPosition = newPosition;
        }
    }

    /// <summary>
    /// Set the text size at runtime
    /// </summary>
    public void SetTextSize(Vector2 newSize)
    {
        textSize = newSize;
        if (textRectTransform != null)
        {
            textRectTransform.sizeDelta = newSize;
        }
    }

    /// <summary>
    /// Set the font size at runtime
    /// </summary>
    public void SetFontSize(float newFontSize)
    {
        fontSize = newFontSize;
        if (textComponent != null)
        {
            textComponent.fontSize = newFontSize;
        }
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

    private void OnDestroy()
    {
        // Clean up DOTween sequences when this object is destroyed
        StopCurrentDisplay();

        // Clean up created UI
        if (textCanvas != null)
        {
            Destroy(textCanvas);
        }
    }
}
