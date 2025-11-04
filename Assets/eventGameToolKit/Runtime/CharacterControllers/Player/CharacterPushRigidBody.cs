using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Allows a CharacterController to push Rigidbody objects when colliding with them.
/// Designed for CharacterControllerCC to enable interactive physics objects like boxes and barrels.
/// Common use: Pushing crates, opening doors with physics, interacting with movable objects.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterPushRigidBody : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("Enable or disable pushing functionality")]
    [SerializeField] private bool canPush = true;

    [Tooltip("Force strength applied to pushed objects (higher = stronger push)")]
    [Range(0.5f, 10f)]
    [SerializeField] private float pushStrength = 2f;

    [Tooltip("Only push objects on these layers (leave as 'Nothing' to push all layers)")]
    [SerializeField] private LayerMask pushLayers = ~0; // Default to everything

    [Header("Push Direction Control")]
    [Tooltip("Prevent pushing objects downward when walking over them")]
    [SerializeField] private bool preventDownwardPush = true;

    [Tooltip("Minimum Y direction to prevent downward push (-0.3 is Unity standard)")]
    [Range(-1f, 0f)]
    [SerializeField] private float downwardPushThreshold = -0.3f;

    [Tooltip("Remove vertical component from push (horizontal push only)")]
    [SerializeField] private bool horizontalPushOnly = true;

    [Header("Object Filtering")]
    [Tooltip("Do not push objects with Rigidbody set to Kinematic")]
    [SerializeField] private bool ignoreKinematic = true;

    [Tooltip("Minimum mass of objects to push (0 = push all masses)")]
    [SerializeField] private float minimumMass = 0f;

    [Tooltip("Maximum mass of objects to push (0 = no maximum)")]
    [SerializeField] private float maximumMass = 0f;

    [Header("Events")]
    /// <summary>
    /// Fires when the character successfully pushes a Rigidbody object
    /// </summary>
    public UnityEvent onPushObject;

    /// <summary>
    /// Fires when the character collides with a Rigidbody but cannot push it (too heavy, wrong layer, etc.)
    /// </summary>
    public UnityEvent onPushBlocked;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            Debug.LogError($"CharacterPushRigidBody on {gameObject.name}: CharacterController component required!", this);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!canPush) return;

        PushRigidBody(hit);
    }

    private void PushRigidBody(ControllerColliderHit hit)
    {
        // Get the rigidbody attached to the collided object
        Rigidbody body = hit.collider.attachedRigidbody;

        // Exit if no rigidbody attached
        if (body == null)
        {
            return;
        }

        // Check if kinematic (static physics objects)
        if (ignoreKinematic && body.isKinematic)
        {
            onPushBlocked?.Invoke();
            return;
        }

        // Check layer mask filtering
        int bodyLayerMask = 1 << body.gameObject.layer;
        if ((bodyLayerMask & pushLayers.value) == 0)
        {
            onPushBlocked?.Invoke();
            return;
        }

        // Check mass constraints
        if (minimumMass > 0f && body.mass < minimumMass)
        {
            onPushBlocked?.Invoke();
            return;
        }

        if (maximumMass > 0f && body.mass > maximumMass)
        {
            onPushBlocked?.Invoke();
            return;
        }

        // Prevent pushing objects below us (like walking over boxes)
        if (preventDownwardPush && hit.moveDirection.y < downwardPushThreshold)
        {
            return;
        }

        // Calculate push direction
        Vector3 pushDirection = hit.moveDirection;

        // Remove vertical component if horizontal-only mode
        if (horizontalPushOnly)
        {
            pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        }

        // Apply force with strength multiplier
        body.AddForce(pushDirection * pushStrength, ForceMode.Impulse);

        // Fire push event
        onPushObject?.Invoke();
    }

    /// <summary>
    /// Enable or disable pushing functionality at runtime
    /// </summary>
    public void SetCanPush(bool enabled)
    {
        canPush = enabled;
    }

    /// <summary>
    /// Set the push strength at runtime
    /// </summary>
    public void SetPushStrength(float strength)
    {
        pushStrength = Mathf.Clamp(strength, 0.5f, 10f);
    }

    /// <summary>
    /// Set the minimum mass of objects that can be pushed
    /// </summary>
    public void SetMinimumMass(float mass)
    {
        minimumMass = Mathf.Max(0f, mass);
    }

    /// <summary>
    /// Set the maximum mass of objects that can be pushed
    /// </summary>
    public void SetMaximumMass(float mass)
    {
        maximumMass = Mathf.Max(0f, mass);
    }

    /// <summary>
    /// Toggle horizontal-only push mode
    /// </summary>
    public void SetHorizontalPushOnly(bool enabled)
    {
        horizontalPushOnly = enabled;
    }

    // Public properties for read-only access
    public bool CanPush => canPush;
    public float PushStrength => pushStrength;
    public bool IsHorizontalPushOnly => horizontalPushOnly;
}
