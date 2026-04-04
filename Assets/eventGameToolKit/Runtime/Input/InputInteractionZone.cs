using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Trigger zone that shows an optional visual prompt when a tagged object enters,
/// then fires an event when the player presses the Interact input action (or fallback key).
/// Creates a self-contained billboard sprite with optional hover and glow animations.
/// Common use: Door interactions, NPC conversations, item pickups, puzzle activations.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InputInteractionZone : MonoBehaviour
{
    /// <summary>How the prompt sprite is oriented in world space</summary>
    public enum PromptOrientation
    {
        FaceCamera,  // Billboard — rotates every frame to face the camera
        FixedWorld   // Uses the rotation you set in the Inspector on this GameObject
    }

    /// <summary>How the prompt animates when it appears and disappears</summary>
    public enum PromptAnimation { FadeIn, ScaleIn, Both }

    // ── Zone ────────────────────────────────────────────────────────────────
    [Header("Zone Settings")]
    [Tooltip("Tag used to identify the player. Only this tag activates the zone.")]
    [SerializeField] private string playerTag = "Player";

    // ── Input ───────────────────────────────────────────────────────────────
    [Header("Interact Input")]
    [Tooltip("The Interact action from your Input Action Asset (e.g. the 'Interact' action bound to E and Triangle).")]
    [SerializeField] private InputActionReference interactAction;

    [Tooltip("Fallback key used if no Input Action is assigned. Useful for quick prototyping.")]
    [SerializeField] private KeyCode fallbackKey = KeyCode.E;

    // ── Prompt ──────────────────────────────────────────────────────────────
    [Header("Prompt (Optional)")]
    [Tooltip("Enable to show a sprite prompt when the player is inside the zone.")]
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

    // ── Hover ───────────────────────────────────────────────────────────────
    [Header("Hover (Optional)")]
    [Tooltip("Slowly float the prompt up and down while it is visible.")]
    [SerializeField] private bool enableHover = true;

    [Tooltip("How far the prompt moves up and down in world units.")]
    [SerializeField] private float hoverHeight = 0.15f;

    [Tooltip("How many up-and-down cycles per second.")]
    [SerializeField] private float hoverSpeed = 0.8f;

    // ── Glow ────────────────────────────────────────────────────────────────
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

    // ── Events ───────────────────────────────────────────────────────────────
    [Header("Events")]
    /// <summary>
    /// Fires once when the player presses the Interact input action while inside the zone
    /// </summary>
    public UnityEvent onInteract;

    /// <summary>
    /// Fires when the player (tagged object) enters the zone
    /// </summary>
    public UnityEvent onPlayerEnter;

    /// <summary>
    /// Fires when the player (tagged object) exits the zone
    /// </summary>
    public UnityEvent onPlayerExit;

    // ── Runtime state ────────────────────────────────────────────────────────
    private bool playerInZone;
    private InputAction _interactInputAction;

    private GameObject promptObject;
    private SpriteRenderer promptRenderer;
    private Light promptLight;

    private Tween colorTween;
    private Tween scaleTween;
    private Tween hoverTween;
    private Tween glowTween;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (interactAction != null)
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
    }

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            Debug.LogWarning($"[InputInteractionZone] '{gameObject.name}': Collider is not set to Is Trigger. Players will not be detected.", this);

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
        // Orientation update
        if (promptObject != null && promptOrientation == PromptOrientation.FaceCamera && Camera.main != null)
            promptObject.transform.rotation = Camera.main.transform.rotation;

        // Fallback key input (only when no action is assigned)
        if (playerInZone && interactAction == null && Input.GetKeyDown(fallbackKey))
            onInteract.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInZone = true;
        ShowPrompt();
        onPlayerEnter.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInZone = false;
        HidePrompt();
        onPlayerExit.Invoke();
    }

    private void OnDestroy()
    {
        colorTween?.Kill();
        scaleTween?.Kill();
        hoverTween?.Kill();
        glowTween?.Kill();
    }

    // ── Input callback ────────────────────────────────────────────────────────

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInZone) return;
        onInteract.Invoke();
    }

    // ── Prompt creation ───────────────────────────────────────────────────────

    private void CreatePrompt()
    {
        promptObject = new GameObject("InteractionPrompt");
        promptObject.transform.SetParent(transform);
        promptObject.transform.localPosition = promptOffset;

        // Apply fixed world orientation at creation; FaceCamera is handled each Update
        if (promptOrientation != PromptOrientation.FaceCamera)
            promptObject.transform.rotation = Quaternion.identity;

        promptRenderer = promptObject.AddComponent<SpriteRenderer>();
        promptRenderer.sprite = promptSprite;

        // Start fully hidden
        promptRenderer.color = new Color(1f, 1f, 1f, 0f);
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

    // ── Prompt visibility ─────────────────────────────────────────────────────

    private void ShowPrompt()
    {
        if (promptObject == null) return;

        // Kill any active tweens and reset position for clean re-entry
        colorTween?.Kill();
        scaleTween?.Kill();
        hoverTween?.Kill();
        glowTween?.Kill();
        promptObject.transform.localPosition = promptOffset;

        // Appear animation
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

        // Hover loop — starts after appear animation finishes
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

        // Glow pulse loop — starts after appear animation finishes
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

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the player is currently inside the zone
    /// </summary>
    public bool IsPlayerInZone => playerInZone;

    /// <summary>
    /// Manually fire the interact event, as if the player pressed the interact button
    /// </summary>
    public void TriggerInteract()
    {
        onInteract.Invoke();
    }
}
