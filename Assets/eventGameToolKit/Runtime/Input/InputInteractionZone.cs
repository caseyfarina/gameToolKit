using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using DG.Tweening;

/// <summary>
/// Interaction zone with two modes: Proximity (player walks into a trigger collider and
/// presses the Interact action) or Mouse (cursor hovers over this object and clicks).
/// Both modes show an optional billboard prompt sprite with hover float and glow animations.
/// Common use: Doors, NPCs, item pickups, puzzle activations, point-and-click interactions.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InputInteractionZone : MonoBehaviour
{
    /// <summary>How interaction is initiated</summary>
    public enum InteractionMode
    {
        Proximity,  // Player walks into a trigger collider, then presses the Interact button
        Mouse       // Cursor hovers over this object; click to interact
    }

    /// <summary>How the prompt sprite is oriented in world space</summary>
    public enum PromptOrientation
    {
        FaceCamera,  // Billboard — rotates every frame to face the camera
        FixedWorld   // Uses the rotation you set in the Inspector on this GameObject
    }

    /// <summary>How the prompt animates when it appears and disappears</summary>
    public enum PromptAnimation { FadeIn, ScaleIn, Both }

    // ── Mode ─────────────────────────────────────────────────────────────────
    [Header("Mode")]
    [Tooltip("Proximity: player enters trigger collider then presses Interact. Mouse: cursor hovers over this object then clicks.")]
    [SerializeField] private InteractionMode interactionMode = InteractionMode.Proximity;

    // ── Proximity settings ────────────────────────────────────────────────────
    [Header("Proximity Settings")]
    [Tooltip("Tag used to identify the player. Only objects with this tag activate the zone.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Interact Input")]
    [Tooltip("The Interact action from your Input Action Asset (e.g. the Interact action bound to E and Triangle).")]
    [SerializeField] private InputActionReference interactAction;

    [Tooltip("Keyboard fallback used when no Input Action is assigned.")]
    [SerializeField] private KeyCode fallbackKey = KeyCode.E;

    // ── Mouse settings ─────────────────────────────────────────────────────────
    [Header("Mouse Settings")]
    [Tooltip("Which mouse button triggers the interaction (0 = Left, 1 = Right, 2 = Middle).")]
    [SerializeField] private int mouseButton = 0;

    [Tooltip("Camera used for raycasting. Falls back to Camera.main if not assigned.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Maximum distance the raycast reaches.")]
    [SerializeField] private float maxRaycastDistance = 100f;

    [Tooltip("Which layers the raycast detects.")]
    [SerializeField] private LayerMask interactionLayer = ~0;

    // ── Prompt ────────────────────────────────────────────────────────────────
    [Header("Prompt (Optional)")]
    [Tooltip("Enable to show a sprite prompt when interaction is available.")]
    [SerializeField] private bool showPrompt = true;

    [Tooltip("The icon sprite to display (e.g. a button prompt image).")]
    [SerializeField] private Sprite promptSprite;

    [Tooltip("Position of the prompt relative to this GameObject's center.")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);

    [Tooltip("Uniform scale of the prompt sprite in world units.")]
    [SerializeField] private float promptSize = 1f;

    [Tooltip("FaceCamera: billboard, always faces the player. FixedWorld: uses the rotation set in the Inspector.")]
    [SerializeField] private PromptOrientation promptOrientation = PromptOrientation.FaceCamera;

    [Tooltip("Animation style when the prompt appears and disappears.")]
    [SerializeField] private PromptAnimation promptAnimation = PromptAnimation.Both;

    [Tooltip("Duration of the appear/disappear animation in seconds.")]
    [SerializeField] private float animationDuration = 0.2f;

    // ── Hover ─────────────────────────────────────────────────────────────────
    [Header("Hover (Optional)")]
    [Tooltip("Slowly float the prompt up and down while it is visible.")]
    [SerializeField] private bool enableHover = true;

    [Tooltip("How far the prompt moves up and down in world units.")]
    [SerializeField] private float hoverHeight = 0.15f;

    [Tooltip("How many up-and-down cycles per second.")]
    [SerializeField] private float hoverSpeed = 0.8f;

    // ── Glow ──────────────────────────────────────────────────────────────────
    [Header("Glow (Optional)")]
    [Tooltip("Add a pulsing Point Light to the prompt for a glow effect.")]
    [SerializeField] private bool enableGlow = false;

    [Tooltip("Color of the glow light.")]
    [SerializeField] private Color glowColor = Color.white;

    [Tooltip("Peak intensity of the glow light at full pulse.")]
    [SerializeField] private float glowIntensity = 2f;

    [Tooltip("Radius of the glow light in world units.")]
    [SerializeField] private float glowRange = 3f;

    [Tooltip("Time in seconds for one full glow pulse (ramp up + ramp down).")]
    [SerializeField] private float glowPulseDuration = 1f;

    // ── Events ────────────────────────────────────────────────────────────────
    [Header("Events")]
    /// <summary>
    /// Fires when the player presses Interact (Proximity mode) or clicks (Mouse mode)
    /// </summary>
    public UnityEvent onInteract;

    /// <summary>
    /// Fires when the player enters the zone (Proximity) or the cursor enters this object (Mouse)
    /// </summary>
    public UnityEvent onEnter;

    /// <summary>
    /// Fires when the player exits the zone (Proximity) or the cursor leaves this object (Mouse)
    /// </summary>
    public UnityEvent onExit;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private bool isInteractable;        // true when prompt is shown and interaction is available
    private bool wasHoveredLastFrame;   // mouse mode only

    private InputAction _interactInputAction;

    private GameObject promptObject;
    private SpriteRenderer promptRenderer;
    private Light promptLight;

    private Tween colorTween;
    private Tween scaleTween;
    private Tween hoverTween;
    private Tween glowTween;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (interactionMode == InteractionMode.Proximity && interactAction != null)
        {
            _interactInputAction = interactAction.action;
            if (_interactInputAction != null)
            {
                _interactInputAction.performed += OnInteractPerformed;
                if (!_interactInputAction.enabled)
                    _interactInputAction.Enable();
            }
        }
    }

    private void OnDisable()
    {
        if (_interactInputAction != null)
            _interactInputAction.performed -= OnInteractPerformed;

        // Clean up active prompt if disabled mid-interaction
        if (isInteractable)
        {
            isInteractable = false;
            HidePrompt();
        }

        wasHoveredLastFrame = false;
    }

    private void Start()
    {
        if (interactionMode == InteractionMode.Proximity)
        {
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
                Debug.LogWarning($"[InputInteractionZone] '{gameObject.name}': Collider is not set to Is Trigger. Players will not be detected in Proximity mode.", this);
        }

        if (showPrompt)
        {
            if (promptSprite != null)
                CreatePrompt();
            else
                Debug.LogWarning($"[InputInteractionZone] '{gameObject.name}': Show Prompt is enabled but no Prompt Sprite is assigned.", this);
        }
    }

    private void Update()
    {
        // Keep prompt facing camera
        if (promptObject != null && promptOrientation == PromptOrientation.FaceCamera && Camera.main != null)
            promptObject.transform.rotation = Camera.main.transform.rotation;

        if (interactionMode == InteractionMode.Mouse)
            HandleMouseMode();
        else
            HandleProximityFallbackKey();
    }

    // ── Proximity ─────────────────────────────────────────────────────────────

    private void HandleProximityFallbackKey()
    {
        if (!isInteractable) return;
        if (_interactInputAction == null && Input.GetKeyDown(fallbackKey))
            onInteract.Invoke();
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!isInteractable) return;
        onInteract.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (interactionMode != InteractionMode.Proximity) return;
        if (!other.CompareTag(playerTag)) return;

        isInteractable = true;
        ShowPrompt();
        onEnter.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (interactionMode != InteractionMode.Proximity) return;
        if (!other.CompareTag(playerTag)) return;

        isInteractable = false;
        HidePrompt();
        onExit.Invoke();
    }

    // ── Mouse mode ─────────────────────────────────────────────────────────────

    private void HandleMouseMode()
    {
        if (Mouse.current == null) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        bool isHit = Physics.Raycast(ray, out _, maxRaycastDistance, interactionLayer, QueryTriggerInteraction.Collide)
                     && IsMouseOverThisObject(cam);

        if (isHit && !wasHoveredLastFrame)
        {
            isInteractable = true;
            ShowPrompt();
            onEnter.Invoke();
        }
        else if (!isHit && wasHoveredLastFrame)
        {
            isInteractable = false;
            HidePrompt();
            onExit.Invoke();
        }

        if (isHit && GetMouseButton().wasPressedThisFrame)
            onInteract.Invoke();

        wasHoveredLastFrame = isHit;
    }

    private bool IsMouseOverThisObject(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, interactionLayer, QueryTriggerInteraction.Collide)
               && hit.collider.gameObject == gameObject;
    }

    private ButtonControl GetMouseButton()
    {
        return mouseButton switch
        {
            1 => Mouse.current.rightButton,
            2 => Mouse.current.middleButton,
            _ => Mouse.current.leftButton
        };
    }

    // ── Prompt creation ────────────────────────────────────────────────────────

    private void CreatePrompt()
    {
        promptObject = new GameObject("InteractionPrompt");
        promptObject.transform.SetParent(transform);
        promptObject.transform.localPosition = promptOffset;

        if (promptOrientation != PromptOrientation.FaceCamera)
            promptObject.transform.rotation = Quaternion.identity;

        promptRenderer = promptObject.AddComponent<SpriteRenderer>();
        promptRenderer.sprite = promptSprite;

        // ScaleIn starts opaque at zero scale; FadeIn/Both start transparent at full scale
        bool fadeInvolved = promptAnimation == PromptAnimation.FadeIn || promptAnimation == PromptAnimation.Both;
        promptRenderer.color = fadeInvolved ? new Color(1f, 1f, 1f, 0f) : Color.white;
        promptObject.transform.localScale = promptAnimation == PromptAnimation.FadeIn
            ? Vector3.one * promptSize
            : Vector3.zero;

        if (enableGlow)
        {
            promptLight = promptObject.AddComponent<Light>();
            promptLight.type = LightType.Point;
            promptLight.color = glowColor;
            promptLight.intensity = 0f;
            promptLight.range = glowRange;
            promptLight.shadows = LightShadows.None;
        }
    }

    // ── Prompt visibility ──────────────────────────────────────────────────────

    private void ShowPrompt()
    {
        if (promptObject == null) return;

        colorTween?.Kill();
        scaleTween?.Kill();
        hoverTween?.Kill();
        glowTween?.Kill();
        promptObject.transform.localPosition = promptOffset;

        if (promptAnimation == PromptAnimation.FadeIn || promptAnimation == PromptAnimation.Both)
        {
            colorTween = DOTween.To(
                () => promptRenderer.color,
                x => promptRenderer.color = x,
                Color.white,
                animationDuration
            ).SetUpdate(true);
        }

        if (promptAnimation == PromptAnimation.ScaleIn || promptAnimation == PromptAnimation.Both)
        {
            scaleTween = promptObject.transform
                .DOScale(Vector3.one * promptSize, animationDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        if (enableHover)
        {
            float halfPeriod = hoverSpeed > 0f ? 0.5f / hoverSpeed : 0.5f;
            hoverTween = promptObject.transform
                .DOLocalMoveY(promptOffset.y + hoverHeight, halfPeriod)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetDelay(animationDuration);
        }

        if (enableGlow && promptLight != null)
        {
            glowTween = DOTween.To(
                () => promptLight.intensity,
                x => promptLight.intensity = x,
                glowIntensity,
                glowPulseDuration * 0.5f
            ).SetEase(Ease.InOutSine)
             .SetLoops(-1, LoopType.Yoyo)
             .SetUpdate(true)
             .SetDelay(animationDuration);
        }
    }

    private void HidePrompt()
    {
        if (promptObject == null) return;

        hoverTween?.Kill();
        glowTween?.Kill();
        colorTween?.Kill();
        scaleTween?.Kill();

        if (promptLight != null)
            promptLight.intensity = 0f;

        if (promptAnimation == PromptAnimation.FadeIn || promptAnimation == PromptAnimation.Both)
        {
            colorTween = DOTween.To(
                () => promptRenderer.color,
                x => promptRenderer.color = x,
                new Color(1f, 1f, 1f, 0f),
                animationDuration
            ).SetUpdate(true);
        }

        if (promptAnimation == PromptAnimation.ScaleIn || promptAnimation == PromptAnimation.Both)
        {
            scaleTween = promptObject.transform
                .DOScale(Vector3.zero, animationDuration)
                .SetUpdate(true);
        }
    }

    private void OnDestroy()
    {
        colorTween?.Kill();
        scaleTween?.Kill();
        hoverTween?.Kill();
        glowTween?.Kill();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when interaction is available — player is in zone (Proximity) or cursor is hovering (Mouse)
    /// </summary>
    public bool IsInteractable => isInteractable;

    /// <summary>
    /// Manually fire the interact event, as if the player pressed the interact button
    /// </summary>
    public void TriggerInteract() => onInteract.Invoke();
}
