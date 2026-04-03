using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Allows a GameObject to be clicked and dragged by the player.
/// Constrains movement to a chosen plane with optional grid snapping, positional limits, and damping.
/// Requires a Collider on the same GameObject.
/// Common use: Puzzle pieces, sliders, object placement, drag-to-sort mechanics.
/// </summary>
public class InputClickDrag : MonoBehaviour
{
    public enum DragPlane
    {
        CameraFacing,   // Plane perpendicular to the camera at the object's depth
        WorldXZ,        // Horizontal ground plane — Y stays fixed
        WorldXY,        // Vertical frontal plane — Z stays fixed
        WorldYZ         // Vertical side plane — X stays fixed
    }

    public enum GrabMode
    {
        MaintainOffset, // Object keeps its offset from the exact click point
        SnapToCenter    // Object center snaps directly to the cursor
    }

    [Header("Drag Settings")]
    [Tooltip("The plane the object slides along while being dragged")]
    [SerializeField] private DragPlane dragPlane = DragPlane.CameraFacing;

    [Tooltip("MaintainOffset keeps the grab point relative to the object center; SnapToCenter pulls the center to the cursor")]
    [SerializeField] private GrabMode grabMode = GrabMode.MaintainOffset;

    [Header("Snapping")]
    [Tooltip("Snap the dragged position to a world-space grid")]
    [SerializeField] private bool snapToGrid = false;

    [Tooltip("World-space grid cell size in units")]
    [SerializeField] private float snapSize = 1f;

    [Header("Limits")]
    [Tooltip("Restrict movement within world-space bounds")]
    [SerializeField] private bool useLimits = false;

    [Tooltip("Minimum world-space position on each axis")]
    [SerializeField] private Vector3 minLimit = new Vector3(-10f, -10f, -10f);

    [Tooltip("Maximum world-space position on each axis")]
    [SerializeField] private Vector3 maxLimit = new Vector3(10f, 10f, 10f);

    [Header("Damping")]
    [Tooltip("Smooth the object's movement so it lags slightly behind the cursor. On release, the object snaps to the final target position.")]
    [SerializeField] private bool enableDamping = false;

    [Tooltip("Time in seconds to reach the cursor position (smaller = snappier, larger = more lag)")]
    [SerializeField] private float dampingTime = 0.1f;

    [Header("Events")]
    /// <summary>
    /// Fires when the player starts dragging this object
    /// </summary>
    public UnityEvent onDragStart;

    /// <summary>
    /// Fires when the player releases this object
    /// </summary>
    public UnityEvent onDragEnd;

    /// <summary>
    /// Fires each frame while dragging, passing the object's actual world-space position (smoothed when damping is enabled)
    /// </summary>
    public UnityEvent<Vector3> onDragged;

    private bool isDragging = false;
    private Vector3 grabOffset;       // world-space offset from plane hit point to object center
    private Vector3 dragPlanePoint;   // a point on the drag plane (captured at grab start)
    private Vector3 dragPlaneNormal;  // normal of the drag plane (captured at grab start)
    private Vector3 targetPosition;   // where the object should end up (before damping)
    private Vector3 dampingVelocity;  // SmoothDamp velocity reference
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("[InputClickDrag] No main camera found in scene.", this);

        if (GetComponent<Collider>() == null)
            Debug.LogError("[InputClickDrag] Requires a Collider on this GameObject.", this);
    }

    private void Update()
    {
        if (mainCamera == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && !isDragging)
            TryStartDrag(mouse.position.ReadValue());

        if (isDragging)
        {
            if (mouse.leftButton.wasReleasedThisFrame)
                EndDrag();
            else
                UpdateDrag(mouse.position.ReadValue());
        }
    }

    private void TryStartDrag(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        if (hit.collider.gameObject != gameObject) return;

        // Record the drag plane at the object's current position
        dragPlanePoint = transform.position;
        dragPlaneNormal = dragPlane switch
        {
            DragPlane.CameraFacing => -mainCamera.transform.forward,
            DragPlane.WorldXZ      => Vector3.up,
            DragPlane.WorldXY      => Vector3.forward,
            DragPlane.WorldYZ      => Vector3.right,
            _                      => -mainCamera.transform.forward
        };

        // Find where the ray hits the drag plane
        Plane plane = new Plane(dragPlaneNormal, dragPlanePoint);
        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 hitPoint = ray.GetPoint(enter);
        grabOffset = grabMode == GrabMode.MaintainOffset ? (transform.position - hitPoint) : Vector3.zero;

        targetPosition = transform.position;
        dampingVelocity = Vector3.zero;

        isDragging = true;
        onDragStart.Invoke();
    }

    private void UpdateDrag(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(dragPlaneNormal, dragPlanePoint);

        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 newPos = ray.GetPoint(enter) + grabOffset;
        newPos = ApplySnap(newPos);
        newPos = ApplyLimits(newPos);
        targetPosition = newPos;

        if (enableDamping)
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref dampingVelocity, dampingTime);
        else
            transform.position = targetPosition;

        onDragged.Invoke(transform.position);
    }

    private void EndDrag()
    {
        if (enableDamping)
        {
            transform.position = targetPosition;
            dampingVelocity = Vector3.zero;
        }
        isDragging = false;
        onDragEnd.Invoke();
    }

    private Vector3 ApplySnap(Vector3 pos)
    {
        if (!snapToGrid || snapSize <= 0f) return pos;
        return new Vector3(
            Mathf.Round(pos.x / snapSize) * snapSize,
            Mathf.Round(pos.y / snapSize) * snapSize,
            Mathf.Round(pos.z / snapSize) * snapSize
        );
    }

    private Vector3 ApplyLimits(Vector3 pos)
    {
        if (!useLimits) return pos;
        return new Vector3(
            Mathf.Clamp(pos.x, minLimit.x, maxLimit.x),
            Mathf.Clamp(pos.y, minLimit.y, maxLimit.y),
            Mathf.Clamp(pos.z, minLimit.z, maxLimit.z)
        );
    }

    /// <summary>
    /// Cancels a drag in progress, snapping to the target position and firing onDragEnd
    /// </summary>
    public void CancelDrag()
    {
        if (isDragging) EndDrag();
    }

    /// <summary>
    /// Returns true if this object is currently being dragged
    /// </summary>
    public bool IsDragging => isDragging;
}
