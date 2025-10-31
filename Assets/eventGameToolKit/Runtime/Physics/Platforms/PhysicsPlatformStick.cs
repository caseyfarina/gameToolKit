using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Makes physics objects stick to moving platforms while preserving independent movement capability.
/// Common use: Players on elevators, objects on conveyor belts, characters on moving trains, or rotating platforms.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PhysicsPlatformStick : MonoBehaviour
{
    [Header("Platform Detection")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private string platformTag = "movingPlatform";

    [Header("Movement Settings")]
    [Tooltip("Apply vertical platform movement (enable for elevators, disable for horizontal-only platforms)")]
    [SerializeField] private bool applyVerticalMovement = true;

    [Header("Capsule Settings (Optional)")]
    [Tooltip("If using CapsuleCollider, leave null to auto-find")]
    [SerializeField] private CapsuleCollider capsuleCollider;

    private Rigidbody rb;
    private Rigidbody platformRb;
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    private Vector3 platformPositionDelta;
    private Vector3 platformVelocity;
    private bool isOnPlatform;
    private bool wasOnPlatform;
    private int landingStabilizationFrames = 0;

    private void OnEnable()
    {
        #if UNITY_EDITOR
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        #endif
    }

    private void OnDisable()
    {
        #if UNITY_EDITOR
        SceneView.duringSceneGui -= OnSceneGUI;
        #endif
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Auto-find capsule collider if not set
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }
    }

    private void FixedUpdate()
    {
        CheckForPlatform();

        // Detect landing on platform (transition from not on platform to on platform)
        if (isOnPlatform && !wasOnPlatform)
        {
            // Start stabilization countdown (wait 2 physics frames after landing)
            landingStabilizationFrames = 2;
        }

        // Count down stabilization frames
        if (landingStabilizationFrames > 0)
        {
            landingStabilizationFrames--;
        }

        if (isOnPlatform && currentPlatform != null)
        {
            // Calculate platform movement delta
            Vector3 platformDelta = currentPlatform.position - lastPlatformPosition;
            Quaternion platformRotationDelta = currentPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);

            // Store delta for application
            platformPositionDelta = platformDelta;

            // Filter vertical movement if disabled
            if (!applyVerticalMovement)
            {
                platformPositionDelta.y = 0f;
            }

            // Apply platform rotation to character's position relative to platform
            if (platformRotationDelta != Quaternion.identity)
            {
                Vector3 offsetFromPlatform = transform.position - currentPlatform.position;
                Vector3 rotatedOffset = platformRotationDelta * offsetFromPlatform;
                Vector3 rotationDelta = rotatedOffset - offsetFromPlatform;
                platformPositionDelta += rotationDelta;
            }

            // Store current platform state for next frame
            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
        }
        else
        {
            platformPositionDelta = Vector3.zero;
        }

        wasOnPlatform = isOnPlatform;
    }

    private void LateUpdate()
    {
        // Apply platform movement AFTER all physics updates
        // Skip application during landing stabilization frames to prevent jitter
        if (isOnPlatform && platformPositionDelta != Vector3.zero && landingStabilizationFrames == 0)
        {
            transform.position += platformPositionDelta;
        }
    }

    private void CheckForPlatform()
    {
        // Determine raycast start position
        Vector3 rayStart = transform.position;

        // If using capsule collider, start from bottom of capsule
        if (capsuleCollider != null)
        {
            float capsuleBottom = transform.position.y - (capsuleCollider.height * 0.5f) + capsuleCollider.center.y;
            rayStart = new Vector3(transform.position.x, capsuleBottom, transform.position.z);
        }

        RaycastHit hit;
        bool foundPlatform = Physics.Raycast(
            rayStart,
            Vector3.down,
            out hit,
            groundCheckDistance,
            platformLayer
        );

        if (foundPlatform && hit.collider.CompareTag(platformTag))
        {
            if (currentPlatform != hit.transform)
            {
                // Just stepped onto platform - initialize tracking
                currentPlatform = hit.transform;
                platformRb = currentPlatform.GetComponent<Rigidbody>();
                lastPlatformPosition = currentPlatform.position;
                lastPlatformRotation = currentPlatform.rotation;
                platformVelocity = Vector3.zero;
            }
            isOnPlatform = true;
        }
        else
        {
            isOnPlatform = false;
            currentPlatform = null;
            platformRb = null;
            platformVelocity = Vector3.zero;
        }
    }

    #if UNITY_EDITOR
    private void OnSceneGUI(SceneView sceneView)
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        // Draw the main raycast
        Handles.color = isOnPlatform ? Color.green : Color.yellow;
        Vector3 rayStart = transform.position;
        Vector3 rayEnd = rayStart + Vector3.down * groundCheckDistance;
        
        // Draw the main line
        Handles.DrawDottedLine(rayStart, rayEnd, 2f);

        // Draw sphere at check point
        float sphereSize = 0.1f;
        Handles.SphereHandleCap(
            0,
            rayEnd,
            Quaternion.identity,
            sphereSize * 2f,
            EventType.Repaint
        );

        // Draw detection area
        Handles.color = new Color(1f, 1f, 0f, 0.2f);
        Handles.DrawSolidDisc(rayEnd, Vector3.up, sphereSize);

        // Add labels
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        string statusText = isOnPlatform ? "On Platform" : "No Platform";
        Handles.Label(rayStart + Vector3.up * 0.1f, statusText, style);
    }
    #endif

    /// <summary>
    /// Returns true if the object is currently on a moving platform.
    /// </summary>
    public bool IsOnPlatform => isOnPlatform;

    /// <summary>
    /// Returns the transform of the current platform, or null if not on a platform.
    /// </summary>
    public Transform CurrentPlatform => currentPlatform;

    /// <summary>
    /// Returns the position delta of the current platform this frame.
    /// </summary>
    public Vector3 PlatformPositionDelta => platformPositionDelta;
}