using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Modifier key required to be held when clicking.
/// </summary>
public enum ModifierKey
{
    None,
    Shift,
    Ctrl,
    Alt
}

/// <summary>
/// Detects mouse clicks and hover events on 3D objects with optional visual feedback.
/// Raycasts from the mouse cursor position each frame (requires a free/visible cursor).
/// For first-person games with a locked cursor, use InputFPMouseInteraction instead.
/// Common use: Clickable buttons, interactive objects, hover tooltips, or selection systems.
/// </summary>
public class InputMouseInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Which mouse button to detect (0=Left, 1=Right, 2=Middle)")]
    [SerializeField] private int mouseButton = 0;
    [Tooltip("Modifier key that must be held for click events to fire (hover is unaffected)")]
    [SerializeField] private ModifierKey requiredModifier = ModifierKey.None;
    [Tooltip("When enabled, releasing the mouse button always clears click state even if the modifier was released first. Recommended: on.")]
    [SerializeField] private bool resetOnModifierRelease = true;
    [SerializeField] private bool enableHover = true;
    [SerializeField] private bool enableClick = true;

    [Header("Visual Feedback")]
    [Tooltip("Material to use when hovering (optional)")]
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private bool scaleOnHover = false;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);

    [Header("Scale Animation")]
    [Tooltip("Enable smooth scale animation instead of instant")]
    [SerializeField] private bool useScaleEasing = false;
    [SerializeField] private float scaleAnimationDuration = 0.2f;
    [SerializeField] private AnimationCurve scaleEasingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Click Events")]
    /// <summary>
    /// Fires when the mouse button is pressed and released on the same object
    /// </summary>
    public UnityEvent onMouseClick;
    /// <summary>
    /// Fires when the mouse button is pressed down on this object
    /// </summary>
    public UnityEvent onMouseDown;
    /// <summary>
    /// Fires when the mouse button is released over this object
    /// </summary>
    public UnityEvent onMouseUp;

    [Header("Hover Events")]
    /// <summary>
    /// Fires when the mouse cursor first enters this object's collider
    /// </summary>
    public UnityEvent onMouseEnter;
    /// <summary>
    /// Fires when the mouse cursor leaves this object's collider
    /// </summary>
    public UnityEvent onMouseExit;
    /// <summary>
    /// Fires continuously each frame while the mouse cursor is over this object
    /// </summary>
    public UnityEvent onMouseHover;

    private bool isHovering = false;
    private bool wasClicked = false;
    private Material originalMaterial;
    private Vector3 originalScale;
    private Renderer objectRenderer;
    private Tween scaleTween;

    // Raycast-based state
    private bool wasHitLastFrame = false;
    private bool isMouseDown = false;

    public bool IsHovering => isHovering;
    public bool WasClicked => wasClicked;

    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        originalScale = transform.localScale;

        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"InputMouseInteraction on {gameObject.name} requires a Collider component!");
        }
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        bool isHit = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide)
                     && hit.collider.gameObject == gameObject;

        // Hover state
        if (enableHover)
        {
            if (isHit && !wasHitLastFrame)
            {
                isHovering = true;
                ApplyHoverEffects();
                onMouseEnter.Invoke();
                if (showDebugInfo) Debug.Log($"Mouse entered: {gameObject.name}");
            }
            else if (!isHit && wasHitLastFrame)
            {
                isHovering = false;
                RemoveHoverEffects();
                onMouseExit.Invoke();
                if (showDebugInfo) Debug.Log($"Mouse exited: {gameObject.name}");
            }

            if (isHit)
            {
                onMouseHover.Invoke();
            }
        }

        // Click state
        if (enableClick)
        {
            ButtonControl button = GetMouseButton();

            if (IsModifierHeld())
            {
                if (isHit && button.wasPressedThisFrame)
                {
                    isMouseDown = true;
                    wasClicked = true;
                    onMouseDown.Invoke();
                    if (showDebugInfo) Debug.Log($"Mouse down on: {gameObject.name}");
                }

                if (button.wasReleasedThisFrame)
                {
                    if (isHit)
                    {
                        onMouseUp.Invoke();
                        if (showDebugInfo) Debug.Log($"Mouse up on: {gameObject.name}");

                        if (isMouseDown)
                        {
                            onMouseClick.Invoke();
                            if (showDebugInfo) Debug.Log($"Mouse clicked: {gameObject.name}");
                        }
                    }
                    isMouseDown = false;
                }
            }
            else if (resetOnModifierRelease && button.wasReleasedThisFrame)
            {
                isMouseDown = false;
            }
        }

        wasHitLastFrame = isHit;
    }

    private ButtonControl GetMouseButton()
    {
        return mouseButton switch
        {
            0 => Mouse.current.leftButton,
            1 => Mouse.current.rightButton,
            2 => Mouse.current.middleButton,
            _ => Mouse.current.leftButton
        };
    }

    private bool IsModifierHeld()
    {
        if (Keyboard.current == null) return requiredModifier == ModifierKey.None;
        return requiredModifier switch
        {
            ModifierKey.None  => true,
            ModifierKey.Shift => Keyboard.current.shiftKey.isPressed,
            ModifierKey.Ctrl  => Keyboard.current.ctrlKey.isPressed,
            ModifierKey.Alt   => Keyboard.current.altKey.isPressed,
            _                 => true
        };
    }

    #region Visual Effects

    private void ApplyHoverEffects()
    {
        if (hoverMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = hoverMaterial;
        }

        if (scaleOnHover)
        {
            Vector3 targetScale = Vector3.Scale(originalScale, hoverScale);
            AnimateScale(targetScale);
        }
    }

    private void RemoveHoverEffects()
    {
        if (originalMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = originalMaterial;
        }

        if (scaleOnHover)
        {
            AnimateScale(originalScale);
        }
    }

    private void AnimateScale(Vector3 targetScale)
    {
        if (scaleTween != null && scaleTween.IsActive())
        {
            scaleTween.Kill();
        }

        if (useScaleEasing)
        {
            scaleTween = transform.DOScale(targetScale, scaleAnimationDuration)
                .SetEase(scaleEasingCurve)
                .SetUpdate(true);
        }
        else
        {
            transform.localScale = targetScale;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Enable mouse interaction
    /// </summary>
    public void EnableInteraction()
    {
        enableClick = true;
        enableHover = true;
    }

    /// <summary>
    /// Disable mouse interaction
    /// </summary>
    public void DisableInteraction()
    {
        enableClick = false;
        enableHover = false;
        RemoveHoverEffects();
    }

    /// <summary>
    /// Enable only click events
    /// </summary>
    public void EnableClickOnly()
    {
        enableClick = true;
        enableHover = false;
        RemoveHoverEffects();
    }

    /// <summary>
    /// Enable only hover events
    /// </summary>
    public void EnableHoverOnly()
    {
        enableClick = false;
        enableHover = true;
    }

    /// <summary>
    /// Set which mouse button to detect
    /// </summary>
    public void SetMouseButton(int buttonIndex)
    {
        mouseButton = Mathf.Clamp(buttonIndex, 0, 2);
    }

    /// <summary>
    /// Change hover material at runtime
    /// </summary>
    public void SetHoverMaterial(Material newMaterial)
    {
        hoverMaterial = newMaterial;
    }

    /// <summary>
    /// Reset the clicked state
    /// </summary>
    public void ResetClickState()
    {
        wasClicked = false;
    }

    /// <summary>
    /// Enable or disable scale easing animation
    /// </summary>
    public void SetScaleEasing(bool enableEasing)
    {
        useScaleEasing = enableEasing;
    }

    /// <summary>
    /// Change the scale animation duration
    /// </summary>
    public void SetScaleAnimationDuration(float duration)
    {
        scaleAnimationDuration = Mathf.Max(0.01f, duration);
    }

    /// <summary>
    /// Set custom scale animation curve
    /// </summary>
    public void SetScaleEasingCurve(AnimationCurve curve)
    {
        if (curve != null)
        {
            scaleEasingCurve = curve;
        }
    }

    #endregion

    #region Student Helper Methods

    /// <summary>
    /// Simple method for students - simulate a click
    /// </summary>
    public void SimulateClick()
    {
        onMouseClick.Invoke();
        if (showDebugInfo) Debug.Log($"Simulated click on: {gameObject.name}");
    }

    /// <summary>
    /// Check if object is currently being interacted with
    /// </summary>
    public bool IsBeingInteractedWith()
    {
        return isHovering || wasClicked;
    }

    #endregion

    #region Debug Methods

    [Header("Debug Tools")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0)
            {
                Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);

                GUILayout.BeginArea(new Rect(guiPos.x - 75, guiPos.y - 50, 150, 100));
                GUILayout.Box($"{gameObject.name}");
                GUILayout.Label($"Hovering: {isHovering}");
                GUILayout.Label($"Clicked: {wasClicked}");
                GUILayout.Label($"Button: {mouseButton}");

                if (GUILayout.Button("Test Click"))
                {
                    SimulateClick();
                }

                GUILayout.EndArea();
            }
        }
    }

    #endregion

    private void OnDisable()
    {
        if (isHovering)
        {
            isHovering = false;
            RemoveHoverEffects();
        }
        wasHitLastFrame = false;
        isMouseDown = false;

        if (scaleTween != null && scaleTween.IsActive())
        {
            scaleTween.Kill();
        }
    }

    private void OnDestroy()
    {
        if (scaleTween != null && scaleTween.IsActive())
        {
            scaleTween.Kill();
        }

        if (objectRenderer != null && objectRenderer.material != originalMaterial)
        {
            if (Application.isPlaying)
            {
                Destroy(objectRenderer.material);
            }
        }
    }
}
