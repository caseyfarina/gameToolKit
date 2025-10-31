using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects mouse clicks and hover events on 3D objects with optional visual feedback.
/// Common use: Clickable buttons, interactive objects, hover tooltips, or selection systems.
/// </summary>
public class InputMouseInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Which mouse button to detect (0=Left, 1=Right, 2=Middle)")]
    [SerializeField] private int mouseButton = 0;
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
    private Coroutine scaleCoroutine;

    public bool IsHovering => isHovering;
    public bool WasClicked => wasClicked;

    private void Start()
    {
        // Get renderer for visual feedback
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        // Store original scale
        originalScale = transform.localScale;

        // Ensure we have a collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"InputMouseInteraction on {gameObject.name} requires a Collider component!");
        }
    }

    #region Mouse Event Handlers

    private void OnMouseEnter()
    {
        if (!enableHover) return;

        isHovering = true;
        ApplyHoverEffects();
        onMouseEnter.Invoke();
        Debug.Log($"Mouse entered: {gameObject.name}");
    }

    private void OnMouseExit()
    {
        if (!enableHover) return;

        isHovering = false;
        RemoveHoverEffects();
        onMouseExit.Invoke();
        Debug.Log($"Mouse exited: {gameObject.name}");
    }

    private void OnMouseOver()
    {
        if (!enableHover) return;

        onMouseHover.Invoke();
    }

    private void OnMouseDown()
    {
        if (!enableClick) return;

        if (Input.GetMouseButtonDown(mouseButton))
        {
            wasClicked = true;
            onMouseDown.Invoke();
            Debug.Log($"Mouse down on: {gameObject.name}");
        }
    }

    private void OnMouseUp()
    {
        if (!enableClick) return;

        if (Input.GetMouseButtonUp(mouseButton))
        {
            onMouseUp.Invoke();
            Debug.Log($"Mouse up on: {gameObject.name}");
        }
    }

    private void OnMouseUpAsButton()
    {
        if (!enableClick) return;

        // This fires only if mouse was pressed AND released on the same object
        onMouseClick.Invoke();
        Debug.Log($"Mouse clicked: {gameObject.name}");
    }

    #endregion

    #region Visual Effects

    private void ApplyHoverEffects()
    {
        // Change material if specified
        if (hoverMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = hoverMaterial;
        }

        // Scale effect if enabled
        if (scaleOnHover)
        {
            Vector3 targetScale = Vector3.Scale(originalScale, hoverScale);
            AnimateScale(targetScale);
        }
    }

    private void RemoveHoverEffects()
    {
        // Restore original material
        if (originalMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = originalMaterial;
        }

        // Restore original scale
        if (scaleOnHover)
        {
            AnimateScale(originalScale);
        }
    }

    private void AnimateScale(Vector3 targetScale)
    {
        // Stop any existing scale animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }

        if (useScaleEasing && Application.isPlaying && gameObject.activeInHierarchy)
        {
            scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
        }
        else
        {
            // Instant scale change (or can't start coroutine)
            transform.localScale = targetScale;
        }
    }

    private System.Collections.IEnumerator ScaleCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for UI responsiveness
            float progress = elapsed / scaleAnimationDuration;
            float easedProgress = scaleEasingCurve.Evaluate(progress);

            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }

        // Ensure we end at exactly the target scale
        transform.localScale = targetScale;
        scaleCoroutine = null;
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
        scaleAnimationDuration = Mathf.Max(0.01f, duration); // Minimum duration to prevent issues
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
        Debug.Log($"Simulated click on: {gameObject.name}");
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
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0) // Only show if in front of camera
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
        // Clean up effects when disabled
        RemoveHoverEffects();

        // Stop any running scale animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        // Stop any running scale animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        // Clean up material instance if created
        if (objectRenderer != null && objectRenderer.material != originalMaterial)
        {
            if (Application.isPlaying)
            {
                Destroy(objectRenderer.material);
            }
        }
    }
}