using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Rigidbody-based humanoid character controller with slope detection, dodge mechanics, and animation support.
/// Common use: Third-person adventure games, action platformers, physics-based combat, or character movement systems.
/// </summary>
public class PhysicsCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float maxVelocity = 8f;
    [SerializeField] private float airControlFactor = 0.5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeSpeed = 20f;
    [SerializeField] private float dodgeCooldown = 1f;
    [SerializeField] private bool allowAirDodge = false;

    [Header("Character Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("Slope Settings")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeCheckDistance = 1f;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform characterMesh;

    [Header("Events")]
    public UnityEvent onGrounded;
    public UnityEvent onJump;
    public UnityEvent onLanding;
    public UnityEvent onStartMoving;
    public UnityEvent onStopMoving;
    public UnityEvent onSteepSlope;
    public UnityEvent onDodge;
    public UnityEvent onDodgeCooldownReady;

    private Rigidbody rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpRequested;
    private bool isMoving;
    private Vector3 lastMoveDirection;
    private bool isOnSteepSlope;
    private Vector3 slopeNormal = Vector3.up;

    // Dodge state
    private bool dodgeRequested;
    private bool isDodging;
    private float dodgeCooldownTimer;
    private Vector3 dodgeDirection;
    private Vector3 dodgeStartPosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        if (characterAnimator == null && characterMesh != null)
            characterAnimator = characterMesh.GetComponentInChildren<Animator>();

        // Auto-configure Rigidbody for character controller
        ConfigureRigidbody();
    }

    /// <summary>
    /// Automatically configures Rigidbody settings for proper character control
    /// </summary>
    private void ConfigureRigidbody()
    {
        if (rb == null) return;

        // Freeze all rotation axes to prevent physics from rotating character
        rb.freezeRotation = true;

        // Use Continuous collision detection to prevent tunneling at high speeds
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Interpolate for smooth camera following
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ensure constraints are properly set
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && !isOnSteepSlope)
        {
            jumpRequested = true;
        }
    }

    public void OnDodge(InputValue value)
    {
        if (value.isPressed && dodgeCooldownTimer <= 0f && !isDodging)
        {
            // Check if air dodge is allowed
            if (allowAirDodge || isGrounded)
            {
                dodgeRequested = true;
            }
        }
    }

    private void FixedUpdate()
    {
        UpdateDodgeCooldown();
        CheckGrounded();
        CheckSlope();
        HandleDodge();
        HandleMovement();
        HandleJump();
        HandleRotation();
        UpdateAnimations();
        CheckMovementEvents();
    }

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;

        float capsuleBottom = transform.position.y - (capsuleCollider.height * 0.5f) + capsuleCollider.center.y;
        Vector3 checkPosition = new Vector3(transform.position.x, capsuleBottom, transform.position.z);

        isGrounded = Physics.CheckSphere(
            checkPosition,
            capsuleCollider.radius + groundCheckDistance,
            groundLayer
        );

        if (isGrounded && !wasGrounded)
        {
            // Zero out downward velocity on landing to prevent bouncing
            if (rb.linearVelocity.y < 0)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }

            onLanding.Invoke();
        }

        if (isGrounded)
        {
            onGrounded.Invoke();
        }
    }

    private void CheckSlope()
    {
        bool wasOnSteepSlope = isOnSteepSlope;
        isOnSteepSlope = false;
        slopeNormal = Vector3.up;

        // Use SphereCast from capsule bottom for more reliable slope detection
        float capsuleBottom = transform.position.y - (capsuleCollider.height * 0.5f) + capsuleCollider.center.y;
        Vector3 sphereCastOrigin = new Vector3(transform.position.x, capsuleBottom + 0.1f, transform.position.z);

        // Check downward for slopes below character
        if (Physics.SphereCast(sphereCastOrigin, capsuleCollider.radius * 0.9f, Vector3.down, out RaycastHit hit, slopeCheckDistance, groundLayer))
        {
            slopeNormal = hit.normal;
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            isOnSteepSlope = slopeAngle > maxSlopeAngle;
        }

        // Additional check: use capsule's collision contacts for ground directly touching
        if (isGrounded)
        {
            // Check all collision points on the capsule
            Collider[] overlaps = Physics.OverlapSphere(sphereCastOrigin, capsuleCollider.radius + 0.1f, groundLayer);
            foreach (Collider col in overlaps)
            {
                // Raycast from slightly above to hit the surface
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit contactHit, 2f, groundLayer))
                {
                    float contactAngle = Vector3.Angle(contactHit.normal, Vector3.up);
                    if (contactAngle > maxSlopeAngle)
                    {
                        isOnSteepSlope = true;
                        slopeNormal = contactHit.normal;
                        break;
                    }
                }
            }
        }

        // Fire event when entering steep slope
        if (isOnSteepSlope && !wasOnSteepSlope)
        {
            onSteepSlope.Invoke();
        }
    }

    private void UpdateDodgeCooldown()
    {
        if (dodgeCooldownTimer > 0f)
        {
            float previousTimer = dodgeCooldownTimer;
            dodgeCooldownTimer -= Time.fixedDeltaTime;

            // Fire event when cooldown becomes ready
            if (dodgeCooldownTimer <= 0f && previousTimer > 0f)
            {
                onDodgeCooldownReady.Invoke();
            }
        }
    }

    private void HandleDodge()
    {
        // Start dodge
        if (dodgeRequested)
        {
            dodgeRequested = false;

            // Determine dodge direction based on movement input or facing direction
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            if (inputDirection != Vector3.zero)
            {
                // Dodge in movement direction (camera-relative)
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();

                dodgeDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z).normalized;
            }
            else if (lastMoveDirection != Vector3.zero)
            {
                // Dodge in facing direction if no input
                dodgeDirection = lastMoveDirection.normalized;
            }
            else
            {
                // Default dodge forward
                dodgeDirection = transform.forward;
            }

            // If on steep slope, cancel dodge to prevent climbing
            if (isOnSteepSlope)
            {
                return;
            }

            dodgeStartPosition = transform.position;
            isDodging = true;
            dodgeCooldownTimer = dodgeCooldown;
            onDodge.Invoke();
        }

        // Execute dodge movement
        if (isDodging)
        {
            // Cancel dodge if on steep slope
            if (isOnSteepSlope)
            {
                isDodging = false;
                return;
            }

            float distanceTraveled = Vector3.Distance(dodgeStartPosition, transform.position);

            if (distanceTraveled < dodgeDistance)
            {
                // Apply dodge velocity
                Vector3 dodgeVelocity = dodgeDirection * dodgeSpeed;
                dodgeVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity
                rb.linearVelocity = dodgeVelocity;
            }
            else
            {
                // End dodge
                isDodging = false;
            }
        }
    }

    private void HandleMovement()
    {
        // Don't apply normal movement during dodge
        if (isDodging)
            return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDirection != Vector3.zero)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z);
            lastMoveDirection = moveDirection;

            // Check if trying to move uphill on steep slope
            bool blockMovement = false;
            if (isOnSteepSlope)
            {
                // Project move direction onto slope plane
                Vector3 slopePlaneDirection = Vector3.ProjectOnPlane(moveDirection, slopeNormal).normalized;

                // Check if movement is upward (dot product with up vector is positive)
                float movementVertical = Vector3.Dot(slopePlaneDirection, Vector3.up);

                // Block if trying to move uphill, allow if moving downhill or sideways
                blockMovement = movementVertical > 0.01f;
            }

            if (!blockMovement)
            {
                float currentForce = moveForce;
                if (!isGrounded)
                {
                    currentForce *= airControlFactor;
                }

                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                if (horizontalVelocity.magnitude < maxVelocity)
                {
                    rb.AddForce(moveDirection * currentForce, ForceMode.Force);
                }
            }
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
            onJump.Invoke();
        }
    }

    private void HandleRotation()
    {
        if (lastMoveDirection != Vector3.zero && isGrounded)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lastMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimations()
    {
        if (characterAnimator != null)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;

            characterAnimator.SetFloat("Speed", speed);
            characterAnimator.SetBool("IsGrounded", isGrounded);
            characterAnimator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
            characterAnimator.SetBool("IsDodging", isDodging);

            bool isWalking = speed > 0.1f && isGrounded;
            characterAnimator.SetBool("IsWalking", isWalking);
        }
    }

    private void CheckMovementEvents()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        bool currentlyMoving = horizontalVelocity.magnitude > 0.1f;

        if (currentlyMoving && !isMoving)
        {
            onStartMoving.Invoke();
        }
        else if (!currentlyMoving && isMoving)
        {
            onStopMoving.Invoke();
        }

        isMoving = currentlyMoving;
    }

    /// <summary>
    /// Changes the force applied when moving
    /// </summary>
    public void SetMoveForce(float newForce)
    {
        moveForce = newForce;
    }

    /// <summary>
    /// Changes the force applied when jumping
    /// </summary>
    public void SetJumpForce(float newForce)
    {
        jumpForce = newForce;
    }

    /// <summary>
    /// Changes the maximum speed the character can reach
    /// </summary>
    public void SetMaxVelocity(float newMax)
    {
        maxVelocity = newMax;
    }

    /// <summary>
    /// Changes how far the dodge movement travels
    /// </summary>
    public void SetDodgeDistance(float newDistance)
    {
        dodgeDistance = newDistance;
    }

    /// <summary>
    /// Changes how fast the dodge movement is
    /// </summary>
    public void SetDodgeSpeed(float newSpeed)
    {
        dodgeSpeed = newSpeed;
    }

    /// <summary>
    /// Changes the cooldown time between dodges
    /// </summary>
    public void SetDodgeCooldown(float newCooldown)
    {
        dodgeCooldown = newCooldown;
    }

    public bool IsGrounded => isGrounded;
    public bool IsMoving => isMoving;
    public bool IsOnSteepSlope => isOnSteepSlope;
    public bool IsDodging => isDodging;
    public float DodgeCooldownRemaining => dodgeCooldownTimer;
    public float CurrentSpeed => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

    private void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null) return;

        // Ground check visualization
        Gizmos.color = isGrounded ? Color.green : Color.red;
        float capsuleBottom = transform.position.y - (capsuleCollider.height * 0.5f) + capsuleCollider.center.y;
        Vector3 checkPosition = new Vector3(transform.position.x, capsuleBottom, transform.position.z);
        Gizmos.DrawWireSphere(checkPosition, capsuleCollider.radius + groundCheckDistance);

        // Slope check visualization
        Gizmos.color = isOnSteepSlope ? Color.red : Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * slopeCheckDistance);

        // Forward wall check visualization
        if (lastMoveDirection != Vector3.zero)
        {
            Gizmos.color = isOnSteepSlope ? Color.red : Color.yellow;
            Gizmos.DrawRay(transform.position, lastMoveDirection * (capsuleCollider.radius + 0.1f));
        }

        // Slope normal visualization
        if (slopeNormal != Vector3.up)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, slopeNormal * 2f);
        }

        // Dodge visualization
        if (Application.isPlaying && isDodging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, dodgeDirection * dodgeDistance);
            Gizmos.DrawWireSphere(dodgeStartPosition + dodgeDirection * dodgeDistance, 0.5f);
        }
    }
}