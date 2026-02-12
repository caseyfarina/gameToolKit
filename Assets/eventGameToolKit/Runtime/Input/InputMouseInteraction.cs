using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Detects mouse clicks and hover events on 3D objects with optional visual feedback.
/// Supports two detection modes: ScreenMouse (free cursor) and CenterScreen (locked cursor / FPS).
/// Common use: Clickable buttons, interactive objects, hover tooltips, or selection systems.
/// </summary>
public class InputMouseInteraction : MonoBehaviour
{
    /// <summary>
    /// How mouse interaction is detected.
    /// ScreenMouse raycasts from the mouse position. CenterScreen raycasts from the camera center (for FPS / locked cursor).
    /// </summary>
    public enum DetectionMode
    {
        ScreenMouse,
        CenterScreen
    }

    /// <summary>
    /// Controls cursor visibility. Does NOT affect CursorLockMode.
    /// </summary>
    public enum CursorAppearance
    {
        Invisible,
        Visible
    }

    [Header("Detection Settings")]
    [Tooltip("ScreenMouse: raycasts from mouse position. CenterScreen: raycasts from camera center (for FPS).")]
    [SerializeField] private DetectionMode detectionMode = DetectionMode.ScreenMouse;
    [Tooltip("Which mouse button to detect (0=Left, 1=Right, 2=Middle)")]
    [SerializeField] private int mouseButton = 0;
    [SerializeField] private bool enableHover = true;
    [SerializeField] private bool enableClick = true;

    [Header("CenterScreen Settings")]
    [Tooltip("Camera to use for raycasting. Falls back to Camera.main if not set.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Maximum distance for the interaction raycast")]
    [SerializeField] private float maxRaycastDistance = 100f;
    [Tooltip("Which layers can be interacted with")]
    [SerializeField] private LayerMask interactionLayer = ~0;

    [Header("Cursor Appearance")]
    [Tooltip("Controls cursor visibility. Does not affect CursorLockMode.")]
    [SerializeField] private CursorAppearance cursorAppearance = CursorAppearance.Visible;
    [Tooltip("Optional custom cursor texture (only when Visible)")]
    [SerializeField] private Texture2D customCursorTexture;
    [Tooltip("Hotspot offset for custom cursor texture")]
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

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

        ApplyCursorSettings();
    }

    private void OnEnable()
    {
        ApplyCursorSettings();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Camera cam = GetCamera();
        if (cam == null) return;

        // Build ray based on detection mode
        Ray ray;
        if (detectionMode == DetectionMode.CenterScreen)
        {
            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        }

        // Raycast
        bool isHit = Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, interactionLayer, QueryTriggerInteraction.Collide)
                     && hit.collider.gameObject == gameObject;

        // Hover state
        if (enableHover)
        {
            if (isHit && !wasHitLastFrame)
            {
                // Enter
                isHovering = true;
                ApplyHoverEffects();
                onMouseEnter.Invoke();
                if (showDebugInfo) Debug.Log($"Mouse entered: {gameObject.name}");
            }
            else if (!isHit && wasHitLastFrame)
            {
                // Exit
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

    private Camera GetCamera()
    {
        if (targetCamera != null) return targetCamera;
        if (Camera.main != null) return Camera.main;
        return null;
    }

    #region Cursor Management

    private void ApplyCursorSettings()
    {
        // In CenterScreen mode, another system (e.g. CharacterControllerFP) owns
        // Cursor.visible and CursorLockMode. Only apply custom cursor texture.
        if (detectionMode == DetectionMode.CenterScreen)
        {
            if (cursorAppearance == CursorAppearance.Visible && customCursorTexture != null)
            {
                Cursor.SetCursor(customCursorTexture, cursorHotspot, CursorMode.Auto);
            }
            return;
        }

        // ScreenMouse mode: this script owns cursor visibility
        if (cursorAppearance == CursorAppearance.Invisible)
        {
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
            if (customCursorTexture != null)
            {
                Cursor.SetCursor(customCursorTexture, cursorHotspot, CursorMode.Auto);
            }
        }
    }

    private void RestoreCursorSettings()
    {
        // Always clear custom cursor texture
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Only restore visibility in ScreenMouse mode — in CenterScreen mode
        // another system (e.g. CharacterControllerFP) owns Cursor.visible
        if (detectionMode == DetectionMode.ScreenMouse)
        {
            Cursor.visible = true;
        }
    }

    #endregion

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
                .SetUpdate(true); // Unscaled time for UI responsiveness
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
            Camera cam = GetCamera();
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
                GUILayout.Label($"Mode: {detectionMode}");

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
        // Clean up hover effects
        if (isHovering)
        {
            isHovering = false;
            RemoveHoverEffects();
        }
        wasHitLastFrame = false;
        isMouseDown = false;

        // Kill any running scale tween
        if (scaleTween != null && scaleTween.IsActive())
        {
            scaleTween.Kill();
        }

        // Restore cursor
        RestoreCursorSettings();
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
