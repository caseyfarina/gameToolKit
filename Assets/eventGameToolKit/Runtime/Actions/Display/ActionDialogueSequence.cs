using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening; // Add DOTween namespace

/// <summary>
/// Dialogue line data structure containing text, timing, portrait, and positioning
/// </summary>
[System.Serializable]
public struct DialogueLine
{
    public enum Orientation { Left, Right }

    [Tooltip("Position of character image (left or right side of screen)")]
    public Orientation orientation;

    [Tooltip("The dialogue text to display")]
    [TextArea(2, 5)]
    public string dialogueText;

    [Tooltip("How long this dialogue line stays on screen (in seconds)")]
    public float displayTime;

    [Tooltip("Character image for this line (optional)")]
    public Sprite characterImage;
}

/// <summary>
/// Streamlined dialogue system that handles sequential playback using a separate UI controller and DOTween for animation.
/// </summary>
public class ActionDialogueSequence : MonoBehaviour
{
    // Keeping enums here for public access and Inspector use
    public enum ImageAnimation { None, SlideUpFromBottom, SlideInFromSide, FadeIn, PopIn }
    public enum TextAnimation { None, TypeOn, FadeIn, SlideUpFromBottom }

    [Header("Dependencies")]
    [Tooltip("Reference to the UI manager that creates and holds the dialogue elements.")]
    // Assign this in the Inspector if UI is persistent, or leave null to autogenerate
    [SerializeField] private DialogueUIController uiController;


    [Header("Dialogue Content")]
    [Tooltip("Array of dialogue lines to display in sequence")]
    [SerializeField] private DialogueLine[] dialogueLines = new DialogueLine[0];

    [Header("Playback Settings")]
    [Tooltip("Start playing dialogue automatically when scene starts")]
    [SerializeField] private bool playOnStart = false;

    [Tooltip("Loop back to beginning after last line (if false, fires onComplete)")]
    [SerializeField] private bool loop = false;

    [Header("Animation Settings - General")]
    [SerializeField] private ImageAnimation imageAnimation = ImageAnimation.SlideUpFromBottom;
    [SerializeField] private TextAnimation textAnimation = TextAnimation.TypeOn;

    [Header("Image Animation Settings")]
    [SerializeField] private float imageFadeInDuration = 0.2f;
    [SerializeField] private float imageFadeOutDuration = 0.2f;
    [SerializeField] private float slideDistance = 500f;
    [SerializeField] private Ease imageSlideEasing = Ease.OutQuad; // Using DOTween Ease

    [Header("Text Animation Settings")]
    [SerializeField] private float textFadeInDuration = 0.2f;
    [SerializeField] private float textFadeOutDuration = 0.2f;
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float textSlideDistance = 100f;
    [SerializeField] private Ease textSlideEasing = Ease.OutQuad; // Using DOTween Ease


    [Header("Visual Settings (Passed to UI Controller)")]
    // NOTE: UI positions/sprites are now handled by the DialogueUIController
    [SerializeField] private Sprite backgroundImage;
    [SerializeField] private Vector2 backgroundPosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 backgroundSize = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 leftPosition = new Vector2(-400f, -100f);
    [SerializeField] private Vector2 rightPosition = new Vector2(400f, -100f);
    [SerializeField] private Vector2 portraitSize = new Vector2(300f, 300f);
    [SerializeField] private Vector2 textPosition = new Vector2(0f, -300f);
    [SerializeField] private Vector2 textSize = new Vector2(1200f, 200f);
    [SerializeField] private float fontSize = 48f;


    [Header("Dialogue Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueComplete;
    public UnityEvent<int> onLineChanged;


    // Playback state
    private Coroutine dialogueCoroutine;
    private int currentLineIndex = -1;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool skipRequested = false;

    // Static/Cached values
    private Dictionary<float, WaitForSeconds> cachedWaits = new Dictionary<float, WaitForSeconds>();
    private const int MAX_VISIBLE_CHARACTERS = 99999;
    public float CharactersPerSecond => charactersPerSecond; // Public accessor for Typewriter effect

    // Public properties
    public bool IsPlaying => isPlaying;
    public bool IsTyping => isTyping;
    public int CurrentLineIndex => currentLineIndex;
    public int TotalLines => dialogueLines.Length;

    // Accessors for UI settings (used by DialogueUIController/Animators)
    public float ImageFadeInDuration => imageFadeInDuration;
    public float ImageFadeOutDuration => imageFadeOutDuration;
    public float TextFadeInDuration => textFadeInDuration;
    public float TextFadeOutDuration => textFadeOutDuration;
    public float SlideDistance => slideDistance;
    public float TextSlideDistance => textSlideDistance;
    public Ease ImageSlideEasing => imageSlideEasing;
    public Ease TextSlideEasing => textSlideEasing;


    private void Awake()
    {
        // Auto-create UI Controller if none is provided
        if (uiController == null)
        {
            uiController = gameObject.AddComponent<DialogueUIController>();
        }

        // Pass initial/serialized settings to the UI controller
        uiController.Setup(
            backgroundImage,
            backgroundPosition,
            backgroundSize,
            leftPosition,
            rightPosition,
            portraitSize,
            textPosition,
            textSize,
            fontSize
        );
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartDialogue();
        }
    }

    private void OnDestroy()
    {
        // Stop all animations when destroyed
        DOTween.Kill(this);

        // UI cleanup is delegated to the UI Controller if it exists
        if (uiController != null)
        {
            uiController.CleanupUI();
        }
    }

    /// <summary>
    /// Starts the dialogue sequence from the beginning
    /// </summary>
    public void StartDialogue()
    {
        if (dialogueLines.Length == 0)
        {
            Debug.LogWarning("No dialogue lines configured!");
            return;
        }

        if (isPlaying)
        {
            StopDialogue();
        }

        // --- CRITICAL FIX: Ensure UI is created before attempting to use it ---
        if (uiController.DialogueCanvas == null)
        {
            // Pass 'transform' as the parent
            uiController.CreateDialogueUI(transform);
        }
        // --------------------------------------------------------------------

        currentLineIndex = -1;
        isPlaying = true;
        skipRequested = false;

        uiController.SetCanvasActive(true);
        onDialogueStart?.Invoke();

        dialogueCoroutine = StartCoroutine(PlayDialogueSequence());
    }

    /// <summary>
    /// Stops the dialogue sequence
    /// </summary>
    public void StopDialogue()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        isPlaying = false;
        isTyping = false;
        uiController.SetCanvasActive(false);

        // Kill any active DOTween animations targeting this object or UI elements
        DOTween.Kill(this);
    }

    /// <summary>
    /// Advances to the next dialogue line (or skips typewriter if currently typing)
    /// </summary>
    public void NextDialogue()
    {
        if (!isPlaying) return;

        // If currently typing, skip the type-on effect immediately
        if (isTyping)
        {
            uiController.DialogueTextComponent.maxVisibleCharacters = MAX_VISIBLE_CHARACTERS;
            isTyping = false;
            // The main Coroutine loop will pick up 'skipRequested' in the next iteration.
        }
        else
        {
            skipRequested = true;
        }
    }

    private IEnumerator PlayDialogueSequence()
    {
        while (isPlaying)
        {
            currentLineIndex++;

            // Check if we've reached the end
            if (currentLineIndex >= dialogueLines.Length)
            {
                if (loop)
                {
                    currentLineIndex = 0;
                }
                else
                {
                    // Dialogue complete
                    isPlaying = false;
                    onDialogueComplete?.Invoke();
                    uiController.SetCanvasActive(false);
                    yield break;
                }
            }

            // Display current line
            DialogueLine currentLine = dialogueLines[currentLineIndex];
            onLineChanged?.Invoke(currentLineIndex);

            yield return StartCoroutine(DisplayDialogueLine(currentLine));
        }
    }

    private IEnumerator DisplayDialogueLine(DialogueLine line)
    {
        // 1. Setup and Animate IN
        float imageInDuration = GetImageInDuration();
        float textInDuration = GetTextInDuration(line.dialogueText);
        float maxInDuration = Mathf.Max(imageInDuration, textInDuration);

        // Use a List of Coroutines to run simultaneously
        List<Coroutine> inAnimations = new List<Coroutine>();
        inAnimations.Add(StartCoroutine(AnimatePortraitsIn(line)));
        inAnimations.Add(StartCoroutine(AnimateTextIn(line)));

        // Wait for the longest animation to finish
        yield return new WaitForSeconds(maxInDuration);

        // 2. Wait for Display Time (Wait for Input/Skip)
        float waitTime = Mathf.Max(0f, line.displayTime - maxInDuration);
        float elapsed = 0f;

        while (elapsed < waitTime)
        {
            if (skipRequested)
            {
                skipRequested = false;
                break; // Skip wait
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. Animate OUT
        float imageOutDuration = GetImageOutDuration();
        float textOutDuration = GetTextOutDuration();
        float maxOutDuration = Mathf.Max(imageOutDuration, textOutDuration);

        List<Coroutine> outAnimations = new List<Coroutine>();
        outAnimations.Add(StartCoroutine(AnimatePortraitsOut()));
        outAnimations.Add(StartCoroutine(AnimateTextOut()));

        // Wait for the longest OUT animation to finish
        yield return new WaitForSeconds(maxOutDuration);
    }

    // ================================= ANIMATION HELPERS (Now using DOTween) =================================

    private IEnumerator AnimatePortraitsIn(DialogueLine line)
    {
        // Determine which portrait to use
        Image targetPortrait = line.orientation == DialogueLine.Orientation.Left ? uiController.LeftPortrait : uiController.RightPortrait;
        Image otherPortrait = line.orientation == DialogueLine.Orientation.Left ? uiController.RightPortrait : uiController.LeftPortrait;

        // Reset positions and kill existing tweens
        targetPortrait.GetComponent<RectTransform>().anchoredPosition = line.orientation == DialogueLine.Orientation.Left ? uiController.LeftPosition : uiController.RightPosition;
        targetPortrait.transform.localScale = Vector3.one;

        // Hide other portrait (clean up)
        otherPortrait.DOKill(true);
        otherPortrait.color = Color.clear;

        // Set sprite and activate
        targetPortrait.sprite = line.characterImage;
        if (line.characterImage != null)
        {
            targetPortrait.gameObject.SetActive(true);
        }
        else
        {
            targetPortrait.gameObject.SetActive(false);
            yield break;
        }

        float duration = GetImageInDuration();
        Sequence seq = DOTween.Sequence();
        RectTransform rect = targetPortrait.GetComponent<RectTransform>();

        // DOTween-based animation logic
        switch (imageAnimation)
        {
            case ImageAnimation.None:
                targetPortrait.color = Color.white;
                break;

            case ImageAnimation.FadeIn:
                // FIX: Set color channels (RGB) to white before fading the alpha.
                targetPortrait.color = new Color(1f, 1f, 1f, 0f); // Set to transparent white
                seq.Append(DOTween.To(() => targetPortrait.color, x => targetPortrait.color = x,
                    Color.white, duration).SetEase(Ease.Linear));
                break;

            case ImageAnimation.SlideUpFromBottom:
                targetPortrait.color = Color.white;
                Vector2 finalPosBottom = rect.anchoredPosition;
                Vector2 startPosBottom = finalPosBottom - new Vector2(0f, slideDistance);
                rect.anchoredPosition = startPosBottom;
                seq.Append(DOTween.To(() => rect.anchoredPosition, x => rect.anchoredPosition = x,
                    finalPosBottom, duration).SetEase(imageSlideEasing));
                break;

            case ImageAnimation.SlideInFromSide:
                targetPortrait.color = Color.white;
                float direction = line.orientation == DialogueLine.Orientation.Left ? -1f : 1f;
                Vector2 finalPosSide = rect.anchoredPosition;
                Vector2 startPosSide = finalPosSide - new Vector2(slideDistance * direction, 0f);
                rect.anchoredPosition = startPosSide;
                seq.Append(DOTween.To(() => rect.anchoredPosition, x => rect.anchoredPosition = x,
                    finalPosSide, duration).SetEase(imageSlideEasing));
                break;

            case ImageAnimation.PopIn:
                targetPortrait.color = Color.white;
                rect.localScale = Vector3.zero;
                seq.Append(rect.DOScale(Vector3.one, duration).SetEase(Ease.OutBack)); // PopIn uses a distinct ease
                break;
        }

        if (imageAnimation != ImageAnimation.None)
        {
            yield return seq.WaitForCompletion();
        }
    }

    private IEnumerator AnimatePortraitsOut()
    {
        Image[] portraits = new Image[] { uiController.LeftPortrait, uiController.RightPortrait };
        Sequence seq = DOTween.Sequence().SetTarget(this);

        if (imageAnimation == ImageAnimation.None)
        {
            // Just ensure they are off
            foreach (var p in portraits) p.color = Color.clear;
            yield break;
        }

        foreach (var p in portraits)
        {
            if (p.color.a > 0f) // Only fade out visible portraits
            {
                Color targetColor = new Color(p.color.r, p.color.g, p.color.b, 0f);
                seq.Join(DOTween.To(() => p.color, x => p.color = x, targetColor, imageFadeOutDuration).SetEase(Ease.OutQuad));
                // Clean up PopIn scale reset after fade
                if (imageAnimation == ImageAnimation.PopIn)
                {
                    seq.AppendCallback(() => p.transform.localScale = Vector3.one);
                }
            }
        }

        yield return seq.WaitForCompletion();
    }

    private IEnumerator AnimateTextIn(DialogueLine line)
    {
        TextMeshProUGUI textComp = uiController.DialogueTextComponent;
        textComp.text = line.dialogueText;
        RectTransform textRect = textComp.GetComponent<RectTransform>();
        Vector2 finalPosition = uiController.TextPosition;

        // Reset
        textComp.DOKill(true);
        textRect.anchoredPosition = finalPosition;
        textComp.maxVisibleCharacters = MAX_VISIBLE_CHARACTERS;

        Sequence seq = DOTween.Sequence();

        switch (textAnimation)
        {
            case TextAnimation.None:
                textComp.color = Color.white;
                break;

            case TextAnimation.TypeOn:
                textComp.color = Color.white;
                textComp.maxVisibleCharacters = 0;
                yield return StartCoroutine(TypewriterEffect(line.dialogueText));
                break;

            case TextAnimation.FadeIn:
                textComp.maxVisibleCharacters = MAX_VISIBLE_CHARACTERS;
                // FIX: Set color channels (RGB) to white before fading the alpha.
                textComp.color = new Color(1f, 1f, 1f, 0f); // Set to transparent white
                seq.Append(DOTween.To(() => textComp.color, x => textComp.color = x,
                    Color.white, textFadeInDuration).SetEase(Ease.Linear));
                break;

            case TextAnimation.SlideUpFromBottom:
                textComp.color = Color.white;
                Vector2 startPos = finalPosition - new Vector2(0f, textSlideDistance);
                textRect.anchoredPosition = startPos;
                seq.Append(DOTween.To(() => textRect.anchoredPosition, x => textRect.anchoredPosition = x,
                    finalPosition, imageFadeInDuration).SetEase(textSlideEasing));
                break;
        }

        if (textAnimation != TextAnimation.None && textAnimation != TextAnimation.TypeOn)
        {
            yield return seq.WaitForCompletion();
        }
    }

    private IEnumerator AnimateTextOut()
    {
        TextMeshProUGUI textComp = uiController.DialogueTextComponent;
        RectTransform textRect = textComp.GetComponent<RectTransform>();

        if (textAnimation == TextAnimation.None)
        {
            textComp.text = "";
            yield break;
        }

        // Fade out
        Color fadeOutColor = new Color(textComp.color.r, textComp.color.g, textComp.color.b, 0f);
        yield return DOTween.To(() => textComp.color, x => textComp.color = x,
            fadeOutColor, textFadeOutDuration).SetEase(Ease.OutQuad).WaitForCompletion();

        // Reset
        if (textAnimation == TextAnimation.SlideUpFromBottom)
        {
            textRect.anchoredPosition = uiController.TextPosition;
        }
        textComp.text = "";
    }

    private IEnumerator TypewriterEffect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            uiController.DialogueTextComponent.maxVisibleCharacters = 0;
            yield break;
        }

        isTyping = true;
        int totalCharacters = text.Length;
        float duration = totalCharacters / charactersPerSecond;
        TextMeshProUGUI textComp = uiController.DialogueTextComponent;

        textComp.DOKill(true);
        textComp.maxVisibleCharacters = 0;

        // Initialize variable to null first to avoid CS0165 error
        Tween typeTween = null;

        // Use DOTween to animate maxVisibleCharacters for typewriter effect
        typeTween = DOTween.To(
            () => textComp.maxVisibleCharacters,
            x => textComp.maxVisibleCharacters = x,
            totalCharacters,
            duration
        )
        .SetEase(Ease.Linear)
        .OnUpdate(() =>
        {
            // Check for skip during the DOTween type-on
            if (skipRequested)
            {
                // If skipped, complete the tween instantly
                typeTween.Complete();
                skipRequested = false;
            }
        })
        .SetTarget(this);

        yield return typeTween.WaitForCompletion();

        isTyping = false;
    }

    // ================================= DURATION GETTERS =================================

    private float GetImageInDuration()
    {
        switch (imageAnimation)
        {
            case ImageAnimation.FadeIn:
            case ImageAnimation.SlideUpFromBottom:
            case ImageAnimation.SlideInFromSide:
            case ImageAnimation.PopIn:
                return imageFadeInDuration;
            default:
                return 0f;
        }
    }

    private float GetImageOutDuration()
    {
        if (imageAnimation != ImageAnimation.None)
            return imageFadeOutDuration;
        return 0f;
    }

    private float GetTextInDuration(string text)
    {
        switch (textAnimation)
        {
            case TextAnimation.TypeOn:
                return text.Length / charactersPerSecond;
            case TextAnimation.FadeIn:
                return textFadeInDuration;
            case TextAnimation.SlideUpFromBottom:
                return imageFadeInDuration; // Consistency with Image Slide duration
            default:
                return 0f;
        }
    }

    private float GetTextOutDuration()
    {
        if (textAnimation != TextAnimation.None)
            return textFadeOutDuration;
        return 0f;
    }

    // ================================= RUNTIME SETTERS =================================
    // (Retained for runtime control, logic is now applied via Getters/DOTween)

    public void SetImageAnimation(ImageAnimation animation) => imageAnimation = animation;
    public void SetTextAnimation(TextAnimation animation) => textAnimation = animation;
    public void SetTypewriterSpeed(float speed) => charactersPerSecond = Mathf.Max(1f, speed);
    public void SetImageFadeInDuration(float duration) => imageFadeInDuration = Mathf.Max(0.1f, duration);
    public void SetImageFadeOutDuration(float duration) => imageFadeOutDuration = Mathf.Max(0.1f, duration);
    public void SetTextFadeInDuration(float duration) => textFadeInDuration = Mathf.Max(0.1f, duration);
    public void SetTextFadeOutDuration(float duration) => textFadeOutDuration = Mathf.Max(0.1f, duration);
    public void SetLoop(bool enableLoop) => loop = enableLoop;

    // ================================= PREVIEW FUNCTIONS (Simplified) =================================

    /// <summary>
    /// Creates and shows the canvas preview in edit mode (called by custom editor)
    /// </summary>
    public void CreatePreviewCanvas(int lineIndex = 0)
    {
        if (uiController == null)
        {
            // If the UI Controller was destroyed or not created yet
            uiController = gameObject.AddComponent<DialogueUIController>();
        }

        // Pass settings and create UI structure
        uiController.Setup(backgroundImage, backgroundPosition, backgroundSize, leftPosition, rightPosition, portraitSize, textPosition, textSize, fontSize);
        uiController.CreateDialogueUI(transform);

        // Update with sample content
        UpdatePreviewCanvas(lineIndex);
        uiController.SetCanvasActive(true);
    }

    /// <summary>
    /// Destroys the canvas preview in edit mode (called by custom editor)
    /// </summary>
    public void DestroyPreviewCanvas()
    {
        if (uiController != null)
        {
            uiController.CleanupUI();
        }
    }

    /// <summary>
    /// Updates the preview canvas with current settings (called by custom editor)
    /// </summary>
    public void UpdatePreviewCanvas(int lineIndex = 0)
    {
        if (uiController == null || uiController.DialogueCanvas == null)
            return;

        // Apply visual settings updates
        uiController.UpdateSettings(backgroundImage, backgroundPosition, backgroundSize, leftPosition, rightPosition, portraitSize, textPosition, textSize, fontSize);
        uiController.ApplySettings();

        // Apply content updates
        if (dialogueLines.Length > 0 && lineIndex >= 0 && lineIndex < dialogueLines.Length)
        {
            DialogueLine selectedLine = dialogueLines[lineIndex];
            uiController.UpdateContentForPreview(selectedLine.dialogueText, selectedLine.characterImage, selectedLine.orientation);
        }
        else
        {
            uiController.UpdateContentForPreview("Add dialogue lines to preview content", null, DialogueLine.Orientation.Left);
        }
    }

    private WaitForSeconds GetWait(float duration)
    {
        if (!cachedWaits.ContainsKey(duration))
        {
            cachedWaits[duration] = new WaitForSeconds(duration);
        }
        return cachedWaits[duration];
    }
}