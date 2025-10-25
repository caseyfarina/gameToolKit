using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the creation, storage, and manipulation of all dialogue UI elements.
/// Decoupled from the main dialogue playback logic.
/// </summary>
[RequireComponent(typeof(ActionDialogueSequence))]
public class DialogueUIController : MonoBehaviour
{
    // Cached values (moved from main script)
    private static readonly Color ColorTranslucent = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color ColorFaded = new Color(1f, 1f, 1f, 0.5f);
    private const int MAX_VISIBLE_CHARACTERS = 99999;

    // Runtime UI references
    private Canvas canvas;
    private GameObject dialogueCanvas;
    private Image backgroundImageComponent;
    private Image leftPortrait;
    private Image rightPortrait;
    private TextMeshProUGUI dialogueTextComponent;

    // Cached visual settings
    private Sprite _backgroundImage;
    private Vector2 _backgroundPosition;
    private Vector2 _backgroundSize;
    private Vector2 _leftPosition;
    private Vector2 _rightPosition;
    private Vector2 _portraitSize;
    private Vector2 _textPosition;
    private Vector2 _textSize;
    private float _fontSize;

    // Public Accessors for UI elements
    public GameObject DialogueCanvas => dialogueCanvas;
    public Image LeftPortrait => leftPortrait;
    public Image RightPortrait => rightPortrait;
    public TextMeshProUGUI DialogueTextComponent => dialogueTextComponent;
    public Vector2 LeftPosition => _leftPosition;
    public Vector2 RightPosition => _rightPosition;
    public Vector2 TextPosition => _textPosition;


    /// <summary>
    /// Receives initial settings from ActionDialogueSequence.
    /// </summary>
    public void Setup(Sprite bg, Vector2 bgPos, Vector2 bgSize, Vector2 leftPos, Vector2 rightPos, Vector2 portraitSize, Vector2 textPos, Vector2 textSize, float fontSize)
    {
        _backgroundImage = bg;
        _backgroundPosition = bgPos;
        _backgroundSize = bgSize;
        _leftPosition = leftPos;
        _rightPosition = rightPos;
        _portraitSize = portraitSize;
        _textPosition = textPos;
        _textSize = textSize;
        _fontSize = fontSize;

        // Ensure UI is created if needed, especially in runtime
        if (dialogueCanvas == null && Application.isPlaying)
        {
            CreateDialogueUI(transform);
        }
    }

    /// <summary>
    /// Updates all cached settings (used by custom editor / runtime changes).
    /// </summary>
    public void UpdateSettings(Sprite bg, Vector2 bgPos, Vector2 bgSize, Vector2 leftPos, Vector2 rightPos, Vector2 portraitSize, Vector2 textPos, Vector2 textSize, float fontSize)
    {
        _backgroundImage = bg;
        _backgroundPosition = bgPos;
        _backgroundSize = bgSize;
        _leftPosition = leftPos;
        _rightPosition = rightPos;
        _portraitSize = portraitSize;
        _textPosition = textPos;
        _textSize = textSize;
        _fontSize = fontSize;
    }

    /// <summary>
    /// Applies cached settings to the actual RectTransforms/Components.
    /// </summary>
    public void ApplySettings()
    {
        if (dialogueCanvas == null) return;

        // Update background image
        if (backgroundImageComponent != null)
        {
            backgroundImageComponent.sprite = _backgroundImage;
            backgroundImageComponent.gameObject.SetActive(_backgroundImage != null);
            UpdateRectTransform(backgroundImageComponent.GetComponent<RectTransform>(), _backgroundPosition, _backgroundSize);
        }

        // Update portrait positions and sizes
        UpdateRectTransform(leftPortrait?.GetComponent<RectTransform>(), _leftPosition, _portraitSize);
        UpdateRectTransform(rightPortrait?.GetComponent<RectTransform>(), _rightPosition, _portraitSize);

        // Update text position, size, and font
        if (dialogueTextComponent != null)
        {
            UpdateRectTransform(dialogueTextComponent.GetComponent<RectTransform>(), _textPosition, _textSize);
            dialogueTextComponent.fontSize = _fontSize;
        }
    }

    private void UpdateRectTransform(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect != null)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }


    /// <summary>
    /// Creates the canvas and UI elements for the dialogue system
    /// </summary>
    public void CreateDialogueUI(Transform parent)
    {
        // Destroy existing before creating new in editor mode
        CleanupUI();

        // 1. Create Canvas container
        dialogueCanvas = new GameObject("DialogueCanvas");
        dialogueCanvas.transform.SetParent(parent);

        canvas = dialogueCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = dialogueCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        dialogueCanvas.AddComponent<GraphicRaycaster>();

        // 2. Create background
        if (_backgroundImage != null)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(dialogueCanvas.transform);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = _backgroundPosition;
            bgRect.sizeDelta = _backgroundSize;

            backgroundImageComponent = bgObj.AddComponent<Image>();
            backgroundImageComponent.sprite = _backgroundImage;
            backgroundImageComponent.color = ColorTranslucent;
        }

        // 3. Create portraits
        leftPortrait = CreatePortrait("LeftPortrait", _leftPosition);
        rightPortrait = CreatePortrait("RightPortrait", _rightPosition);

        // 4. Create dialogue text
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(dialogueCanvas.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = _textPosition;
        textRect.sizeDelta = _textSize;

        dialogueTextComponent = textObj.AddComponent<TextMeshProUGUI>();
        dialogueTextComponent.fontSize = _fontSize;
        dialogueTextComponent.alignment = TextAlignmentOptions.Center;
        dialogueTextComponent.color = Color.clear;
        dialogueTextComponent.text = "";

        // Hide canvas initially for runtime
        if (Application.isPlaying)
        {
            dialogueCanvas.SetActive(false);
        }
    }

    private Image CreatePortrait(string name, Vector2 position)
    {
        GameObject portraitObj = new GameObject(name);
        portraitObj.transform.SetParent(dialogueCanvas.transform);

        RectTransform rect = portraitObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = _portraitSize;

        Image image = portraitObj.AddComponent<Image>();
        image.color = Color.clear; // Start invisible
        image.raycastTarget = false; // Portraits shouldn't block rays

        return image;
    }

    /// <summary>
    /// Sets the active state of the root canvas object.
    /// </summary>
    public void SetCanvasActive(bool active)
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(active);
        }
    }

    /// <summary>
    /// Used by the custom editor to show sample content.
    /// </summary>
    public void UpdateContentForPreview(string text, Sprite image, DialogueLine.Orientation orientation)
    {
        if (dialogueTextComponent == null) return;

        // Update Text
        dialogueTextComponent.text = string.IsNullOrEmpty(text) ? "Sample dialogue text" : text;
        dialogueTextComponent.color = Color.white;
        dialogueTextComponent.maxVisibleCharacters = MAX_VISIBLE_CHARACTERS;

        // Hide both portraits first
        leftPortrait.color = Color.clear;
        rightPortrait.color = Color.clear;

        // Update Image
        if (image != null)
        {
            Image targetImage = orientation == DialogueLine.Orientation.Left ? leftPortrait : rightPortrait;
            targetImage.sprite = image;
            targetImage.color = Color.white;
            targetImage.transform.localScale = Vector3.one;
            targetImage.GetComponent<RectTransform>().anchoredPosition = orientation == DialogueLine.Orientation.Left ? _leftPosition : _rightPosition;
        }
        else
        {
            dialogueTextComponent.color = ColorFaded;
        }
    }

    /// <summary>
    /// Cleans up the created UI (used in OnDestroy and DestroyPreviewCanvas).
    /// </summary>
    public void CleanupUI()
    {
        if (dialogueCanvas != null)
        {
            // Use Destroy or DestroyImmediate depending on application state
            if (Application.isPlaying)
            {
                Destroy(dialogueCanvas);
            }
            else
            {
                // Must destroyImmediate in editor to clean up immediately
                DestroyImmediate(dialogueCanvas);
            }
        }

        // Reset references
        dialogueCanvas = null;
        leftPortrait = null;
        rightPortrait = null;
        dialogueTextComponent = null;
        backgroundImageComponent = null;
    }
}