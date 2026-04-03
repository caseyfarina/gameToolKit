using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Allows a GameObject to be rotated by clicking and dragging the mouse.
/// Supports world or local axis constraints, mouse sensitivity, optional angle snapping, rotation limits, and damping.
/// Requires a Collider on the same GameObject.
/// Common use: Dials, levers, spinning puzzles, or orientation controls.
/// </summary>
public class InputClickRotate : MonoBehaviour
{
    public enum RotationAxis { WorldX, WorldY, WorldZ, LocalX, LocalY, LocalZ }
    public enum MouseDragAxis { Horizontal, Vertical }

    public enum LimitSpace
    {
        RelativeToStart,    // Angles measured from the object's rotation when drag began
        WorldAbsolute       // Angles measured against the world Euler angle on the chosen axis
    }

    [Header("Rotation Settings")]
    [Tooltip("Axis to rotate around")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.WorldY;

    [Tooltip("Which direction of mouse movement drives the rotation")]
    [SerializeField] private MouseDragAxis mouseAxis = MouseDragAxis.Horizontal;

    [Tooltip("Degrees of rotation per pixel of mouse movement")]
    [SerializeField] private float sensitivity = 0.5f;

    [Header("Snapping")]
    [Tooltip("Snap rotation to fixed angle increments")]
    [SerializeField] private bool snapToAngle = false;

    [Tooltip("Angle increment in degrees (e.g. 45 snaps to 0°, 45°, 90°, ...)")]
    [SerializeField] private float snapAngle = 45f;

    [Header("Limits")]
    [Tooltip("Restrict rotation within a min/max angle range")]
    [SerializeField] private bool useLimits = false;

    [Tooltip("RelativeToStart measures from the rotation captured when dragging begins. WorldAbsolute measures from the world Euler angle — works best for single-axis objects with no compound rotation.")]
    [SerializeField] private LimitSpace limitSpace = LimitSpace.RelativeToStart;

    [Tooltip("Minimum rotation angle in degrees")]
    [SerializeField] private float minAngle = -90f;

    [Tooltip("Maximum rotation angle in degrees")]
    [SerializeField] private float maxAngle = 90f;

    [Header("Damping")]
    [Tooltip("Smooth the rotation so it lags slightly behind the mouse. On release, the object snaps to the final target angle.")]
    [SerializeField] private bool enableDamping = false;

    [Tooltip("Time in seconds to reach the target rotation (smaller = snappier, larger = more lag)")]
    [SerializeField] private float dampingTime = 0.1f;

    [Header("Events")]
    /// <summary>
    /// Fires when the player starts rotating this object
    /// </summary>
    public UnityEvent onRotateStart;

    /// <summary>
    /// Fires when the player releases this object
    /// </summary>
    public UnityEvent onRotateEnd;

    /// <summary>
    /// Fires each frame while rotating, passing the object's actual applied angle in degrees (smoothed when damping is enabled)
    /// </summary>
    public UnityEvent<float> onRotated;

    private bool isRotating = false;
    private float cumulativeAngle = 0f;      // target angle (after limits and snap)
    private float appliedAngle = 0f;         // angle actually applied to the transform (smoothed when damping is on)
    private float angularVelocity = 0f;      // SmoothDamp velocity reference
    private Quaternion startRotation;
    private Vector3 rotationAxisInWorld;     // captured in world space at drag start
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("[InputClickRotate] No main camera found in scene.", this);

        if (GetComponent<Collider>() == null)
            Debug.LogError("[InputClickRotate] Requires a Collider on this GameObject.", this);
    }

    private void Update()
    {
        if (mainCamera == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && !isRotating)
            TryStartRotate(mouse.position.ReadValue());

        if (isRotating)
        {
            if (mouse.leftButton.wasReleasedThisFrame)
                EndRotate();
            else
                UpdateRotate(mouse.delta.ReadValue());
        }
    }

    private void TryStartRotate(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        if (hit.collider.gameObject != gameObject) return;

        startRotation = transform.rotation;
        cumulativeAngle = 0f;
        appliedAngle = 0f;
        angularVelocity = 0f;
        rotationAxisInWorld = GetWorldAxis();

        isRotating = true;
        onRotateStart.Invoke();
    }

    private void UpdateRotate(Vector2 mouseDelta)
    {
        float rawDelta = mouseAxis == MouseDragAxis.Horizontal ? mouseDelta.x : mouseDelta.y;
        cumulativeAngle += rawDelta * sensitivity;

        // Apply relative-to-start limits before snap
        if (useLimits && limitSpace == LimitSpace.RelativeToStart)
            cumulativeAngle = Mathf.Clamp(cumulativeAngle, minAngle, maxAngle);

        // Apply snap
        if (snapToAngle && snapAngle > 0f)
        {
            cumulativeAngle = Mathf.Round(cumulativeAngle / snapAngle) * snapAngle;
            // Re-clamp after snap so a snap step can't overshoot the limit
            if (useLimits && limitSpace == LimitSpace.RelativeToStart)
                cumulativeAngle = Mathf.Clamp(cumulativeAngle, minAngle, maxAngle);
        }

        // Smooth toward target or apply directly
        if (enableDamping)
            appliedAngle = Mathf.SmoothDamp(appliedAngle, cumulativeAngle, ref angularVelocity, dampingTime);
        else
            appliedAngle = cumulativeAngle;

        transform.rotation = Quaternion.AngleAxis(appliedAngle, rotationAxisInWorld) * startRotation;

        // World-absolute limits: clamp the actual world Euler angle post-rotation
        if (useLimits && limitSpace == LimitSpace.WorldAbsolute)
        {
            float worldEuler = GetWorldEulerOnAxis();
            float clamped = Mathf.Clamp(worldEuler, minAngle, maxAngle);
            if (!Mathf.Approximately(clamped, worldEuler))
            {
                Vector3 euler = transform.eulerAngles;
                SetEulerAxis(ref euler, clamped);
                transform.rotation = Quaternion.Euler(euler);
                float correction = worldEuler - clamped;
                cumulativeAngle -= correction;
                appliedAngle -= correction;
            }
        }

        onRotated.Invoke(appliedAngle);
    }

    private void EndRotate()
    {
        if (enableDamping)
        {
            appliedAngle = cumulativeAngle;
            transform.rotation = Quaternion.AngleAxis(appliedAngle, rotationAxisInWorld) * startRotation;
            angularVelocity = 0f;
        }
        isRotating = false;
        onRotateEnd.Invoke();
    }

    private Vector3 GetWorldAxis()
    {
        return rotationAxis switch
        {
            RotationAxis.WorldX => Vector3.right,
            RotationAxis.WorldY => Vector3.up,
            RotationAxis.WorldZ => Vector3.forward,
            RotationAxis.LocalX => transform.right,
            RotationAxis.LocalY => transform.up,
            RotationAxis.LocalZ => transform.forward,
            _                   => Vector3.up
        };
    }

    private float GetWorldEulerOnAxis()
    {
        Vector3 euler = transform.eulerAngles;
        float raw = rotationAxis switch
        {
            RotationAxis.WorldX or RotationAxis.LocalX => euler.x,
            RotationAxis.WorldY or RotationAxis.LocalY => euler.y,
            RotationAxis.WorldZ or RotationAxis.LocalZ => euler.z,
            _                                          => 0f
        };
        // Normalize from Unity's [0, 360) to (-180, 180]
        if (raw > 180f) raw -= 360f;
        return raw;
    }

    private void SetEulerAxis(ref Vector3 euler, float value)
    {
        switch (rotationAxis)
        {
            case RotationAxis.WorldX: case RotationAxis.LocalX: euler.x = value; break;
            case RotationAxis.WorldY: case RotationAxis.LocalY: euler.y = value; break;
            case RotationAxis.WorldZ: case RotationAxis.LocalZ: euler.z = value; break;
        }
    }

    /// <summary>
    /// Cancels a rotation in progress, snapping to the target angle and firing onRotateEnd
    /// </summary>
    public void CancelRotation()
    {
        if (isRotating) EndRotate();
    }

    /// <summary>
    /// Returns true if this object is currently being rotated
    /// </summary>
    public bool IsRotating => isRotating;

    /// <summary>
    /// Returns the current applied angle in degrees from the drag start rotation (smoothed when damping is enabled)
    /// </summary>
    public float GetCurrentAngle() => appliedAngle;
}
