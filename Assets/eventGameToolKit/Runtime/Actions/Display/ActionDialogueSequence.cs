using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
/// Decision choice data structure for player decisions at the end of dialogue.
/// Each choice represents a clickable button with optional image and event callback.
/// </summary>
[System.Serializable]
public struct DecisionChoice
{
    /// <summary>
    /// Text to display on the button
    /// </summary>
    [Tooltip("Text to display on the button")]
    public string choiceText;

    /// <summary>
    /// Optional image to display with this choice (shown on left side of button)
    /// </summary>
    [Tooltip("Optional image to display with this choice")]
    public Sprite choiceImage;

    /// <summary>
    /// Event fired when this choice is selected by the player
    /// </summary>
    [Tooltip("Event fired when this choice is selected")]
    public UnityEvent onChoiceSelected;
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

    [Tooltip("Allow player to click or press any key to advance to next dialogue line")]
    [SerializeField] private bool enableClickThrough = false;

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

    [Tooltip("Text alignment for dialogue text")]
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Left;

    [Tooltip("Text color for dialogue text")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Optional custom font for all dialogue and decision text (leave empty for default)")]
    [SerializeField] private TMP_FontAsset customFont;

    [Header("Decision System (Optional)")]
    [Tooltip("Enable player decision at the end of dialogue")]
    [SerializeField] private bool enableDecision = false;

    [Tooltip("Array of choices for the player to select from")]
    [SerializeField] private DecisionChoice[] decisionChoices = new DecisionChoice[0];

    [Tooltip("Position of the decision panel on screen")]
    [SerializeField] private Vector2 decisionPanelPosition = new Vector2(0f, -200f);

    [Tooltip("Size of each decision button")]
    [SerializeField] private Vector2 decisionButtonSize = new Vector2(400f, 100f);

    [Tooltip("Vertical spacing between decision buttons")]
    [SerializeField] private float decisionButtonSpacing = 20f;

    [Tooltip("Size of optional images displayed with each choice")]
    [SerializeField] private Vector2 decisionImageSize = new Vector2(80f, 80f);

    [Tooltip("Font size for decision button text")]
    [SerializeField] private float decisionFontSize = 36f;

    [Tooltip("Background opacity for decision buttons (0 = transparent, 1 = opaque)")]
    [Range(0f, 1f)]
    [SerializeField] private float decisionButtonOpacity = 0.9f;

    [Header("Dialogue Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueComplete;
    public UnityEvent<int> onLineChanged;

    /// <summary>
    /// Fires when the decision panel is displayed
    /// </summary>
    public UnityEvent onDecisionStart;


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
            fontSize,
            textAlignment,
            textColor,
            customFont
        );
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartDialogue();
        }
    }

    private void Update()
    {
        // Handle click-through input when enabled
        if (enableClickThrough && isPlaying)
        {
            // Check for mouse click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                NextDialogue();
            }
            // Check for any key press (keyboard)
            else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                NextDialogue();
            }
            // Check for gamepad south button (A/X)
            else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                NextDialogue();
            }
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
        decisionMade = true; // Force decision to complete if active

        // Clean up decision panel if it exists
        if (decisionPanel != null)
        {
            Destroy(decisionPanel);
            decisionButtons.Clear();
        }

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
                    // Dialogue complete - check if we should show decision
                    isPlaying = false;

                    if (enableDecision && decisionChoices.Length > 0)
                    {
                        // Show decision panel and wait for player choice
                        yield return StartCoroutine(ShowDecisionPanel());
                    }
                    else
                    {
                        // No decision - complete normally
                        onDialogueComplete?.Invoke();
                        uiController.SetCanvasActive(false);
                    }

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
                textComp.color = textColor;
                break;

            case TextAnimation.TypeOn:
                textComp.color = textColor;
                textComp.maxVisibleCharacters = 0;
                yield return StartCoroutine(TypewriterEffect(line.dialogueText));
                break;

            case TextAnimation.FadeIn:
                textComp.maxVisibleCharacters = MAX_VISIBLE_CHARACTERS;
                // Start with transparent version of textColor
                textComp.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                seq.Append(DOTween.To(() => textComp.color, x => textComp.color = x,
                    textColor, textFadeInDuration).SetEase(Ease.Linear));
                break;

            case TextAnimation.SlideUpFromBottom:
                textComp.color = textColor;
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
        uiController.Setup(backgroundImage, backgroundPosition, backgroundSize, leftPosition, rightPosition, portraitSize, textPosition, textSize, fontSize, textAlignment, textColor, customFont);
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
        uiController.UpdateSettings(backgroundImage, backgroundPosition, backgroundSize, leftPosition, rightPosition, portraitSize, textPosition, textSize, fontSize, textAlignment, textColor, customFont);
        uiController.ApplySettings();

        // Check if we're previewing decision (lineIndex == dialogueLines.Length)
        bool isDecisionPreview = enableDecision && decisionChoices.Length > 0 && lineIndex == dialogueLines.Length;

        if (isDecisionPreview)
        {
            // Hide dialogue content, show decision panel
            uiController.UpdateContentForPreview("", null, DialogueLine.Orientation.Left);

            // Clean up existing decision preview
            if (decisionPanel != null)
            {
                DestroyImmediate(decisionPanel);
                decisionButtons.Clear();
            }

            // Create decision preview
            CreateDecisionPanelPreview();
        }
        else
        {
            // Clean up decision preview if it exists
            if (decisionPanel != null)
            {
                DestroyImmediate(decisionPanel);
                decisionButtons.Clear();
            }

            // Show dialogue line content
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
    }

    private WaitForSeconds GetWait(float duration)
    {
        if (!cachedWaits.ContainsKey(duration))
        {
            cachedWaits[duration] = new WaitForSeconds(duration);
        }
        return cachedWaits[duration];
    }

    // ================================= DECISION SYSTEM =================================

    private GameObject decisionPanel;
    private List<GameObject> decisionButtons = new List<GameObject>();
    private bool decisionMade = false;
    private int selectedDecisionIndex = 0;

    /// <summary>
    /// Shows the decision panel with all choices and waits for player selection
    /// </summary>
    private IEnumerator ShowDecisionPanel()
    {
        onDecisionStart?.Invoke();
        decisionMade = false;
        selectedDecisionIndex = 0;

        // Create decision panel
        CreateDecisionPanel();

        // Highlight first button
        UpdateDecisionHighlight();

        // Wait for player to make a choice
        while (!decisionMade)
        {
            // Handle keyboard/gamepad navigation
            HandleDecisionInput();
            yield return null;
        }

        // Clean up decision UI
        CleanupDecisionPanel();

        // Fire dialogue complete after decision is made
        onDialogueComplete?.Invoke();
        uiController.SetCanvasActive(false);
    }

    /// <summary>
    /// Creates the decision panel with buttons for each choice
    /// </summary>
    private void CreateDecisionPanel()
    {
        if (uiController.DialogueCanvas == null) return;

        // Create panel container
        decisionPanel = new GameObject("DecisionPanel");
        decisionPanel.transform.SetParent(uiController.DialogueCanvas.transform, false);

        RectTransform panelRect = decisionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = decisionPanelPosition;

        // Calculate total height needed for all buttons
        float totalHeight = (decisionButtonSize.y * decisionChoices.Length) + (decisionButtonSpacing * (decisionChoices.Length - 1));
        panelRect.sizeDelta = new Vector2(decisionButtonSize.x, totalHeight);

        // Create buttons for each choice
        for (int i = 0; i < decisionChoices.Length; i++)
        {
            GameObject buttonObj = CreateDecisionButton(decisionPanel.transform, decisionChoices[i], i);
            decisionButtons.Add(buttonObj);

            // Position button
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            float yOffset = (totalHeight / 2f) - (decisionButtonSize.y / 2f) - (i * (decisionButtonSize.y + decisionButtonSpacing));
            buttonRect.anchoredPosition = new Vector2(0f, yOffset);
        }

        // Fade in decision panel
        CanvasGroup canvasGroup = decisionPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.3f);
    }

    /// <summary>
    /// Creates a single decision button with text, optional image, and click handler
    /// </summary>
    private GameObject CreateDecisionButton(Transform parent, DecisionChoice choice, int choiceIndex)
    {
        // Create button GameObject
        GameObject buttonObj = new GameObject($"DecisionButton_{choiceIndex}");
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = decisionButtonSize;

        // Add button background image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, decisionButtonOpacity);

        // Add Button component
        Button button = buttonObj.AddComponent<Button>();

        // Create color block for hover/press effects
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, decisionButtonOpacity);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, Mathf.Min(1f, decisionButtonOpacity + 0.1f));
        colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, decisionButtonOpacity);
        button.colors = colors;

        // Set up click handler
        int capturedIndex = choiceIndex; // Capture for closure
        button.onClick.AddListener(() => OnDecisionSelected(capturedIndex));

        // Add optional image (left side)
        if (choice.choiceImage != null)
        {
            GameObject imageObj = new GameObject("ChoiceImage");
            imageObj.transform.SetParent(buttonObj.transform, false);

            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0f, 0.5f);
            imageRect.anchorMax = new Vector2(0f, 0.5f);
            imageRect.pivot = new Vector2(0f, 0.5f);
            imageRect.anchoredPosition = new Vector2(10f, 0f);
            imageRect.sizeDelta = decisionImageSize;

            Image choiceImageComponent = imageObj.AddComponent<Image>();
            choiceImageComponent.sprite = choice.choiceImage;
        }

        // Add text
        GameObject textObj = new GameObject("ChoiceText");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        // Offset text based on whether there's an image
        float textOffset = choice.choiceImage != null ? decisionImageSize.x + 20f : 10f;
        textRect.offsetMin = new Vector2(textOffset, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = choice.choiceText;
        textComponent.fontSize = decisionFontSize;
        textComponent.color = textColor;
        textComponent.alignment = textAlignment;

        // Apply custom font if specified
        if (customFont != null)
        {
            textComponent.font = customFont;
        }

        return buttonObj;
    }

    /// <summary>
    /// Called when a decision button is clicked
    /// </summary>
    private void OnDecisionSelected(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= decisionChoices.Length) return;

        // Fire the choice's UnityEvent
        decisionChoices[choiceIndex].onChoiceSelected?.Invoke();

        // Mark decision as made
        decisionMade = true;
    }

    /// <summary>
    /// Cleans up the decision panel UI
    /// </summary>
    private void CleanupDecisionPanel()
    {
        if (decisionPanel != null)
        {
            // Fade out before destroying
            CanvasGroup canvasGroup = decisionPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, 0.2f)
                    .OnComplete(() =>
                    {
                        if (decisionPanel != null)
                        {
                            Destroy(decisionPanel);
                        }
                    });
            }
            else
            {
                Destroy(decisionPanel);
            }
        }

        decisionButtons.Clear();
    }

    /// <summary>
    /// Handles keyboard/gamepad input for decision navigation
    /// </summary>
    private void HandleDecisionInput()
    {
        // Check for UI input actions
        if (Keyboard.current != null)
        {
            // Up arrow or W key
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                selectedDecisionIndex--;
                if (selectedDecisionIndex < 0)
                    selectedDecisionIndex = decisionChoices.Length - 1;
                UpdateDecisionHighlight();
            }
            // Down arrow or S key
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                selectedDecisionIndex++;
                if (selectedDecisionIndex >= decisionChoices.Length)
                    selectedDecisionIndex = 0;
                UpdateDecisionHighlight();
            }
            // Enter or Space to submit
            else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnDecisionSelected(selectedDecisionIndex);
            }
        }

        // Check for gamepad input
        if (Gamepad.current != null)
        {
            // D-pad or left stick up
            if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame)
            {
                selectedDecisionIndex--;
                if (selectedDecisionIndex < 0)
                    selectedDecisionIndex = decisionChoices.Length - 1;
                UpdateDecisionHighlight();
            }
            // D-pad or left stick down
            else if (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame)
            {
                selectedDecisionIndex++;
                if (selectedDecisionIndex >= decisionChoices.Length)
                    selectedDecisionIndex = 0;
                UpdateDecisionHighlight();
            }
            // South button (A on Xbox, X on PlayStation) to submit
            else if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                OnDecisionSelected(selectedDecisionIndex);
            }
        }
    }

    /// <summary>
    /// Updates the visual highlight on the currently selected decision button
    /// </summary>
    private void UpdateDecisionHighlight()
    {
        for (int i = 0; i < decisionButtons.Count; i++)
        {
            if (decisionButtons[i] == null) continue;

            Image buttonImage = decisionButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                if (i == selectedDecisionIndex)
                {
                    // Highlighted - brighter color
                    buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                }
                else
                {
                    // Normal - darker color
                    buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                }
            }
        }
    }

    /// <summary>
    /// Creates decision panel for preview (similar to CreateDecisionPanel but for edit mode)
    /// </summary>
    private void CreateDecisionPanelPreview()
    {
        if (uiController.DialogueCanvas == null) return;
        if (decisionChoices.Length == 0) return;

        // Create panel container
        decisionPanel = new GameObject("DecisionPanel_PREVIEW");
        decisionPanel.transform.SetParent(uiController.DialogueCanvas.transform, false);
        decisionPanel.hideFlags = HideFlags.DontSave;

        RectTransform panelRect = decisionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = decisionPanelPosition;

        // Calculate total height needed for all buttons
        float totalHeight = (decisionButtonSize.y * decisionChoices.Length) + (decisionButtonSpacing * (decisionChoices.Length - 1));
        panelRect.sizeDelta = new Vector2(decisionButtonSize.x, totalHeight);

        // Create buttons for each choice
        for (int i = 0; i < decisionChoices.Length; i++)
        {
            GameObject buttonObj = CreateDecisionButtonPreview(decisionPanel.transform, decisionChoices[i], i);
            decisionButtons.Add(buttonObj);

            // Position button
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            float yOffset = (totalHeight / 2f) - (decisionButtonSize.y / 2f) - (i * (decisionButtonSize.y + decisionButtonSpacing));
            buttonRect.anchoredPosition = new Vector2(0f, yOffset);
        }

        // Full opacity for preview
        CanvasGroup canvasGroup = decisionPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Creates a single decision button for preview (no click handler needed)
    /// </summary>
    private GameObject CreateDecisionButtonPreview(Transform parent, DecisionChoice choice, int choiceIndex)
    {
        // Create button GameObject
        GameObject buttonObj = new GameObject($"DecisionButton_PREVIEW_{choiceIndex}");
        buttonObj.transform.SetParent(parent, false);
        buttonObj.hideFlags = HideFlags.DontSave;

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = decisionButtonSize;

        // Add button background image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, decisionButtonOpacity);

        // Add optional image (left side)
        if (choice.choiceImage != null)
        {
            GameObject imageObj = new GameObject("ChoiceImage_PREVIEW");
            imageObj.transform.SetParent(buttonObj.transform, false);
            imageObj.hideFlags = HideFlags.DontSave;

            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0f, 0.5f);
            imageRect.anchorMax = new Vector2(0f, 0.5f);
            imageRect.pivot = new Vector2(0f, 0.5f);
            imageRect.anchoredPosition = new Vector2(10f, 0f);
            imageRect.sizeDelta = decisionImageSize;

            Image choiceImageComponent = imageObj.AddComponent<Image>();
            choiceImageComponent.sprite = choice.choiceImage;
        }

        // Add text
        GameObject textObj = new GameObject("ChoiceText_PREVIEW");
        textObj.transform.SetParent(buttonObj.transform, false);
        textObj.hideFlags = HideFlags.DontSave;

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        // Offset text based on whether there's an image
        float textOffset = choice.choiceImage != null ? decisionImageSize.x + 20f : 10f;
        textRect.offsetMin = new Vector2(textOffset, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = string.IsNullOrEmpty(choice.choiceText) ? $"Choice {choiceIndex + 1}" : choice.choiceText;
        textComponent.fontSize = decisionFontSize;
        textComponent.color = textColor;
        textComponent.alignment = textAlignment;

        // Apply custom font if specified
        if (customFont != null)
        {
            textComponent.font = customFont;
        }

        return buttonObj;
    }
}