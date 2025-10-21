using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// CharacterController-based humanoid character controller with slope detection, dodge mechanics, animation support, and built-in moving platform support.
/// Common use: Third-person adventure games, action platformers, or character movement systems requiring kinematic control.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterControllerCC : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
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
    [SerializeField] private CharacterController controller;

    [Header("Slope Settings")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeCheckDistance = 1f;

    [Header("Platform Settings")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private string platformTag = "movingPlatform";
    [SerializeField] private bool applyVerticalMovement = true;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform characterMesh;

    [Header("Events")]
    /// <summary>
    /// Fires every frame while the character is grounded
    /// </summary>
    public UnityEvent onGrounded;

    /// <summary>
    /// Fires when the character initiates a jump
    /// </summary>
    public UnityEvent onJump;

    /// <summary>
    /// Fires when the character lands on the ground from the air
    /// </summary>
    public UnityEvent onLanding;

    /// <summary>
    /// Fires when the character starts moving horizontally
    /// </summary>
    public UnityEvent onStartMoving;

    /// <summary>
    /// Fires when the character stops moving horizontally
    /// </summary>
    public UnityEvent onStopMoving;

    /// <summary>
    /// Fires when the character encounters a slope steeper than the maximum angle
    /// </summary>
    public UnityEvent onSteepSlope;

    /// <summary>
    /// Fires when the character initiates a dodge
    /// </summary>
    public UnityEvent onDodge;

    /// <summary>
    /// Fires when the dodge cooldown completes and dodge is ready again
    /// </summary>
    public UnityEvent onDodgeCooldownReady;

    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector3 velocity;
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

    // Platform state
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    private bool isOnPlatform;
    private bool wasOnPlatform;
    private int landingStabilizationFrames = 0;

    // Gravity settings
    private float gravity = -20f;
    private float terminalVelocity = -50f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        if (characterAnimator == null && characterMesh != null)
            characterAnimator = characterMesh.GetComponentInChildren<Animator>();

        // Auto-configure CharacterController for character control
        ConfigureCharacterController();
    }

    /// <summary>
    /// Automatically configures CharacterController settings for proper character control
    /// </summary>
    private void ConfigureCharacterController()
    {
        if (controller == null) return;

        // Set slope limit from maxSlopeAngle
        controller.slopeLimit = maxSlopeAngle;

        // Ensure skin width is reasonable for character size
        if (controller.skinWidth < 0.01f)
            controller.skinWidth = 0.08f;
    }

    /// <summary>
    /// Input System callback for movement input
    /// </summary>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Input System callback for jump input
    /// </summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && !isOnSteepSlope)
        {
            jumpRequested = true;
        }
    }

    /// <summary>
    /// Input System callback for dodge input
    /// </summary>
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

    private void Update()
    {
        UpdateDodgeCooldown();
        CheckGrounded();
        CheckSlope();
        CheckForPlatform();
        HandleDodge();
        HandleMovement();
        HandleJump();
        HandleGravity();
        ApplyPlatformMovement();
        ApplyMovement();
        HandleRotation();
        UpdateAnimations();
        CheckMovementEvents();
    }

    private void UpdateDodgeCooldown()
    {
        if (dodgeCooldownTimer > 0f)
        {
            float previousTimer = dodgeCooldownTimer;
            dodgeCooldownTimer -= Time.deltaTime;

            // Fire event when cooldown becomes ready
            if (dodgeCooldownTimer <= 0f && previousTimer > 0f)
            {
                onDodgeCooldownReady.Invoke();
            }
        }
    }

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;

        // Use CharacterController's built-in grounded check
        isGrounded = controller.isGrounded;

        // Additional sphere check for more reliable detection
        Vector3 checkPosition = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
        bool sphereCheck = Physics.CheckSphere(
            checkPosition,
            controller.radius + groundCheckDistance,
            groundLayer
        );

        // Combine both checks
        isGrounded = isGrounded || sphereCheck;

        // Landing event
        if (isGrounded && !wasGrounded)
        {
            // Zero out downward velocity on landing
            if (velocity.y < 0)
            {
                velocity.y = 0f;
            }

            onLanding.Invoke();

            // Start stabilization countdown for platform attachment
            landingStabilizationFrames = 2;
        }

        // Grounded event fires every frame while grounded
        if (isGrounded)
        {
            onGrounded.Invoke();
        }

        // Count down stabilization frames
        if (landingStabilizationFrames > 0)
        {
            landingStabilizationFrames--;
        }
    }

    private void CheckSlope()
    {
        bool wasOnSteepSlope = isOnSteepSlope;
        isOnSteepSlope = false;
        slopeNormal = Vector3.up;

        // Check for slopes in front of character
        Vector3 checkOrigin = transform.position + Vector3.up * 0.1f;

        // Forward slope check
        if (lastMoveDirection != Vector3.zero)
        {
            if (Physics.Raycast(checkOrigin, lastMoveDirection, out RaycastHit forwardHit, controller.radius + slopeCheckDistance, groundLayer))
            {
                float slopeAngle = Vector3.Angle(forwardHit.normal, Vector3.up);
                if (slopeAngle > maxSlopeAngle)
                {
                    isOnSteepSlope = true;
                    slopeNormal = forwardHit.normal;
                }
            }
        }

        // Downward slope check
        Vector3 downCheckOrigin = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius - 0.1f, 0f);
        if (Physics.Raycast(downCheckOrigin, Vector3.down, out RaycastHit downHit, slopeCheckDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(downHit.normal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                isOnSteepSlope = true;
                slopeNormal = downHit.normal;
            }
        }

        // Fire event when entering steep slope
        if (isOnSteepSlope && !wasOnSteepSlope)
        {
            onSteepSlope.Invoke();
        }
    }

    private void CheckForPlatform()
    {
        wasOnPlatform = isOnPlatform;

        // Raycast downward to detect platform
        Vector3 rayStart = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);

        RaycastHit hit;
        bool foundPlatform = Physics.Raycast(
            rayStart,
            Vector3.down,
            out hit,
            controller.radius + groundCheckDistance + 0.1f,
            platformLayer
        );

        if (foundPlatform && hit.collider.CompareTag(platformTag))
        {
            if (currentPlatform != hit.transform)
            {
                // Just stepped onto platform - initialize tracking
                currentPlatform = hit.transform;
                lastPlatformPosition = currentPlatform.position;
                lastPlatformRotation = currentPlatform.rotation;
            }
            isOnPlatform = true;
        }
        else
        {
            isOnPlatform = false;
            currentPlatform = null;
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
                // Apply dodge velocity (override horizontal velocity)
                velocity.x = dodgeDirection.x * dodgeSpeed;
                velocity.z = dodgeDirection.z * dodgeSpeed;
                // Preserve vertical velocity for gravity
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
                float currentSpeed = moveSpeed;
                if (!isGrounded)
                {
                    currentSpeed *= airControlFactor;
                }

                // Apply speed with max velocity cap
                Vector3 targetVelocity = moveDirection * currentSpeed;
                targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);

                velocity.x = targetVelocity.x;
                velocity.z = targetVelocity.z;
            }
        }
        else
        {
            // No input - stop horizontal movement when grounded
            if (isGrounded && !isDodging)
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpForce;
            jumpRequested = false;
            onJump.Invoke();
        }
    }

    private void HandleGravity()
    {
        // Apply gravity
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;

            // Clamp to terminal velocity
            if (velocity.y < terminalVelocity)
            {
                velocity.y = terminalVelocity;
            }
        }
        else
        {
            // Keep slight downward force when grounded to stick to ground
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }
        }
    }

    private void ApplyPlatformMovement()
    {
        // Skip application during landing stabilization frames to prevent jitter
        if (isOnPlatform && currentPlatform != null && landingStabilizationFrames == 0)
        {
            // Calculate platform movement delta
            Vector3 platformDelta = currentPlatform.position - lastPlatformPosition;
            Quaternion platformRotationDelta = currentPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);

            // Filter vertical movement if disabled
            if (!applyVerticalMovement)
            {
                platformDelta.y = 0f;
            }

            // Apply platform rotation to character's position relative to platform
            if (platformRotationDelta != Quaternion.identity)
            {
                Vector3 offsetFromPlatform = transform.position - currentPlatform.position;
                Vector3 rotatedOffset = platformRotationDelta * offsetFromPlatform;
                Vector3 rotationDelta = rotatedOffset - offsetFromPlatform;
                platformDelta += rotationDelta;
            }

            // Apply platform movement
            controller.Move(platformDelta);

            // Store current platform state for next frame
            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
        }
        else if (isOnPlatform && currentPlatform != null)
        {
            // Still update platform tracking even during stabilization
            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
        }
    }

    private void ApplyMovement()
    {
        // Move character with calculated velocity
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (lastMoveDirection != Vector3.zero && isGrounded)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lastMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimations()
    {
        if (characterAnimator != null)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontalVelocity.magnitude;

            characterAnimator.SetFloat("Speed", speed);
            characterAnimator.SetBool("IsGrounded", isGrounded);
            characterAnimator.SetFloat("VerticalVelocity", velocity.y);
            characterAnimator.SetBool("IsDodging", isDodging);

            bool isWalking = speed > 0.1f && isGrounded;
            characterAnimator.SetBool("IsWalking", isWalking);
        }
    }

    private void CheckMovementEvents()
    {
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
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
    /// Changes the speed of movement
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
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
    public bool IsOnPlatform => isOnPlatform;
    public Transform CurrentPlatform => currentPlatform;
    public float DodgeCooldownRemaining => dodgeCooldownTimer;
    public float CurrentSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;

    private void OnDrawGizmosSelected()
    {
        if (controller == null) return;

        // Ground check visualization
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPosition = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
        Gizmos.DrawWireSphere(checkPosition, controller.radius + groundCheckDistance);

        // Platform detection ray
        Gizmos.color = isOnPlatform ? Color.green : Color.yellow;
        Vector3 rayStart = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
        Gizmos.DrawRay(rayStart, Vector3.down * (controller.radius + groundCheckDistance + 0.1f));

        // Slope check visualization
        Gizmos.color = isOnSteepSlope ? Color.red : Color.yellow;
        if (lastMoveDirection != Vector3.zero)
        {
            Vector3 checkOrigin = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawRay(checkOrigin, lastMoveDirection * (controller.radius + slopeCheckDistance));
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

        // Platform delta visualization
        if (Application.isPlaying && isOnPlatform && currentPlatform != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 platformDelta = currentPlatform.position - lastPlatformPosition;
            Gizmos.DrawRay(transform.position, platformDelta * 10f); // Scale up for visibility
        }
    }
}
