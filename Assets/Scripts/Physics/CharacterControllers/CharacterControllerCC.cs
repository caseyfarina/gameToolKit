using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Platform detection modes for moving platform support
/// </summary>
public enum PlatformDetectionMode
{
    Tag,        // Only check platform tag (easiest for students)
    Layer,      // Only check platform layer
    Both        // Require both tag AND layer (most restrictive)
}

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
    [Tooltip("Acceleration and deceleration rate")]
    [SerializeField] private float speedChangeRate = 10.0f;
    [SerializeField] private float airControlFactor = 0.5f;

    [Header("Jump Settings")]
    [Tooltip("Height in meters the character can jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [Tooltip("Time required to pass before being able to jump again")]
    [SerializeField] private float jumpTimeout = 0.5f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeSpeed = 20f;
    [SerializeField] private float dodgeCooldown = 1f;
    [SerializeField] private bool allowAirDodge = false;

    [Header("Character Settings")]
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private CharacterController controller;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float terminalVelocity = -50f;
    // FIX 3: Added a customizable force for sticking to the ground
    [SerializeField] private float groundStickForce = -1.5f;

    [Header("Slope Settings")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeCheckDistance = 1f;
    [SerializeField] private float slopeSlideSpeed = 5f;

    [Header("Platform Settings")]
    [Tooltip("Detect platforms by layer, tag, or both")]
    [SerializeField] private PlatformDetectionMode platformDetectionMode = PlatformDetectionMode.Tag;
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

    // Unity TPC improvements
    private float jumpTimeoutDelta;
    private float currentSpeed;
    private float rotationVelocity;

    // Animation IDs (StringToHash optimization)
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDVerticalVelocity;
    private int _animIDIsDodging;
    private int _animIDIsWalking;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        if (characterAnimator == null && characterMesh != null)
            characterAnimator = characterMesh.GetComponentInChildren<Animator>();

        // Initialize animation IDs for StringToHash optimization
        if (characterAnimator != null)
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDVerticalVelocity = Animator.StringToHash("VerticalVelocity");
            _animIDIsDodging = Animator.StringToHash("IsDodging");
            _animIDIsWalking = Animator.StringToHash("IsWalking");
        }

        // Initialize jump timeout
        jumpTimeoutDelta = jumpTimeout;

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

    // FIX 1: Keeping input checks and visual updates in Update()
    private void Update()
    {
        UpdateDodgeCooldown();
        CheckGrounded();
        CheckSlope();
        CheckForPlatform();
        HandleDodge(); // Starts/stops dodge state, velocity applied in FixedUpdate
        HandleJump(); // Sets jumpRequested, velocity applied in FixedUpdate

        HandleRotation(); // FIX 4: Rotation is visual, stays in Update()
        UpdateAnimations();
        CheckMovementEvents();
    }

    // FIX 1: All physics-related calculations moved to FixedUpdate()
    private void FixedUpdate()
    {
        HandleMovement();
        HandleSlopeSliding();
        HandleGravity();
        ApplyPlatformMovement();
        ApplyMovement();
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
        bool wasOnSteepSlope = isOnSteepSlope;

        // If moving upward, we cannot be grounded (prevents immediate re-grounding after jump)
        if (velocity.y > 0.1f)
        {
            isGrounded = false;
            isOnSteepSlope = false;
            slopeNormal = Vector3.up;
        }
        else
        {
            // Perform sphere cast downward to detect ground AND get surface normal
            Vector3 castOrigin = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
            castOrigin.y += 0.1f; // Start slightly above to ensure we hit the ground

            RaycastHit hit;
            float castDistance = groundCheckDistance + 0.15f;
            bool sphereHit = Physics.SphereCast(
                castOrigin,
                controller.radius * 0.9f, // Slightly smaller than controller radius
                Vector3.down,
                out hit,
                castDistance,
                groundLayer
            );

            // Use CharacterController's built-in check as backup
            bool controllerGrounded = controller.isGrounded;

            // Combine both checks for reliability
            isGrounded = controllerGrounded || sphereHit;

            // Analyze surface normal to determine slope angle
            if (sphereHit)
            {
                slopeNormal = hit.normal;
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                isOnSteepSlope = slopeAngle > maxSlopeAngle;
            }
            else if (controllerGrounded)
            {
                // Controller says grounded but no sphere hit - assume flat ground
                slopeNormal = Vector3.up;
                isOnSteepSlope = false;
            }
            else
            {
                // Not grounded at all
                slopeNormal = Vector3.up;
                isOnSteepSlope = false;
            }
        }

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

        // Steep slope event
        if (isOnSteepSlope && !wasOnSteepSlope)
        {
            onSteepSlope.Invoke();
        }

        // Count down stabilization frames
        if (landingStabilizationFrames > 0)
        {
            landingStabilizationFrames--;
        }
    }

    private void CheckSlope()
    {
        // Additional forward slope check for blocking movement into walls
        // This prevents walking into steep slopes ahead
        if (lastMoveDirection != Vector3.zero && isGrounded)
        {
            Vector3 checkOrigin = transform.position + Vector3.up * (controller.height * 0.25f);
            RaycastHit forwardHit;

            if (Physics.Raycast(checkOrigin, lastMoveDirection, out forwardHit, controller.radius + slopeCheckDistance, groundLayer))
            {
                float forwardSlopeAngle = Vector3.Angle(forwardHit.normal, Vector3.up);

                // If there's a steep wall ahead, also mark as steep slope
                if (forwardSlopeAngle > maxSlopeAngle)
                {
                    isOnSteepSlope = true;
                    // Don't override slopeNormal from CheckGrounded unless this is steeper
                    if (Vector3.Angle(forwardHit.normal, Vector3.up) > Vector3.Angle(slopeNormal, Vector3.up))
                    {
                        slopeNormal = forwardHit.normal;
                    }
                }
            }
        }
    }

    private void CheckForPlatform()
    {
        wasOnPlatform = isOnPlatform;

        // Raycast downward to detect platform
        Vector3 rayStart = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);

        // Use appropriate layer mask based on detection mode
        LayerMask raycastLayer = platformDetectionMode == PlatformDetectionMode.Tag ? groundLayer : platformLayer;

        RaycastHit hit;
        bool foundPlatform = Physics.Raycast(
            rayStart,
            Vector3.down,
            out hit,
            controller.radius + groundCheckDistance + 0.1f,
            raycastLayer
        );

        // Check platform based on detection mode
        bool isPlatformValid = false;
        if (foundPlatform)
        {
            switch (platformDetectionMode)
            {
                case PlatformDetectionMode.Tag:
                    // Use groundLayer for raycast, filter by tag
                    isPlatformValid = hit.collider.CompareTag(platformTag);
                    break;
                case PlatformDetectionMode.Layer:
                    // Use platformLayer for raycast, any hit is valid
                    isPlatformValid = true;
                    break;
                case PlatformDetectionMode.Both:
                    // Use platformLayer for raycast, also check tag
                    isPlatformValid = hit.collider.CompareTag(platformTag);
                    break;
            }
        }

        if (isPlatformValid)
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

        // Execute dodge movement (Applies velocity that will be moved in FixedUpdate)
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
                float targetSpeed = moveSpeed;
                if (!isGrounded)
                {
                    targetSpeed *= airControlFactor;
                }

                // Get current horizontal speed
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
                float currentHorizontalSpeed = horizontalVelocity.magnitude;

                // Smooth acceleration to target speed (Unity TPC style)
                currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
                    Time.fixedDeltaTime * speedChangeRate);

                // Clamp to max velocity
                currentSpeed = Mathf.Min(currentSpeed, maxVelocity);

                // Apply smoothed speed in movement direction
                velocity.x = moveDirection.x * currentSpeed;
                velocity.z = moveDirection.z * currentSpeed;
            }
        }
        else
        {
            // No input - smooth deceleration when grounded (but not on steep slopes)
            if (isGrounded && !isDodging && !isOnSteepSlope)
            {
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
                float currentHorizontalSpeed = horizontalVelocity.magnitude;

                // Smooth deceleration to zero
                currentSpeed = Mathf.Lerp(currentHorizontalSpeed, 0f,
                    Time.fixedDeltaTime * speedChangeRate);

                // Apply deceleration or stop completely if very slow
                if (currentSpeed < 0.01f)
                {
                    velocity.x = 0f;
                    velocity.z = 0f;
                }
                else
                {
                    Vector3 currentDirection = horizontalVelocity.normalized;
                    velocity.x = currentDirection.x * currentSpeed;
                    velocity.z = currentDirection.z * currentSpeed;
                }
            }
        }
    }

    private void HandleSlopeSliding()
    {
        // If grounded on a steep slope, slide down
        if (isGrounded && isOnSteepSlope)
        {
            // Calculate slide direction (down the slope surface)
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;

            // Set sliding velocity directly (don't accumulate)
            velocity.x = slideDirection.x * slopeSlideSpeed;
            velocity.z = slideDirection.z * slopeSlideSpeed;
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded && jumpTimeoutDelta <= 0f)
        {
            // Height-based jump formula: velocity = sqrt(height * -2 * gravity)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false;
            isGrounded = false;  // Force not-grounded to ensure gravity applies
            jumpTimeoutDelta = jumpTimeout; // Reset timeout
            onJump.Invoke();
        }

        // Countdown jump timeout
        if (jumpTimeoutDelta > 0f)
        {
            jumpTimeoutDelta -= Time.fixedDeltaTime;
        }
    }

    // FIX 3: Updated to use groundStickForce and Time.fixedDeltaTime
    private void HandleGravity()
    {
        // Apply gravity when not grounded
        if (!isGrounded)
        {
            velocity.y += gravity * Time.fixedDeltaTime;

            // Clamp to terminal velocity
            if (velocity.y < terminalVelocity)
            {
                velocity.y = terminalVelocity;
            }
        }
        else
        {
            // When grounded, apply small downward force to stick to ground.
            // Safety: also catches any erroneous positive velocity when grounded
            if (velocity.y > 0f || velocity.y < groundStickForce)
            {
                velocity.y = groundStickForce;
            }
        }
    }

    private void ApplyPlatformMovement()
    {
        if (!isOnPlatform || currentPlatform == null)
        {
            return;
        }

        // Calculate platform movement delta
        Vector3 platformDelta = currentPlatform.position - lastPlatformPosition;
        Quaternion platformRotationDelta = currentPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);

        // Apply movement only after landing stabilization
        if (landingStabilizationFrames == 0)
        {
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
        }

        // ALWAYS store current platform state for next frame's delta calculation
        lastPlatformPosition = currentPlatform.position;
        lastPlatformRotation = currentPlatform.rotation;
    }

    // FIX 1: Using Time.fixedDeltaTime for CharacterController.Move for physics consistency
    private void ApplyMovement()
    {
        // Move character with calculated velocity
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    // Unity TPC style: SmoothDampAngle for better rotation feel
    private void HandleRotation()
    {
        // Only rotate when actively moving
        if (moveInput != Vector2.zero && lastMoveDirection != Vector3.zero)
        {
            // Calculate target angle from movement direction
            float targetAngle = Mathf.Atan2(lastMoveDirection.x, lastMoveDirection.z) * Mathf.Rad2Deg;

            // Smooth rotation using SmoothDampAngle
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                ref rotationVelocity, rotationSmoothTime);

            // Apply rotation
            transform.rotation = Quaternion.Euler(0.0f, smoothAngle, 0.0f);
        }
    }

    private void UpdateAnimations()
    {
        if (characterAnimator != null)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontalVelocity.magnitude;

            // Use StringToHash IDs for better performance
            // Only set parameters if they exist (prevents errors with incomplete Animator Controllers)
            if (HasParameter(_animIDSpeed))
                characterAnimator.SetFloat(_animIDSpeed, speed);

            if (HasParameter(_animIDGrounded))
                characterAnimator.SetBool(_animIDGrounded, isGrounded);

            if (HasParameter(_animIDVerticalVelocity))
                characterAnimator.SetFloat(_animIDVerticalVelocity, velocity.y);

            if (HasParameter(_animIDIsDodging))
                characterAnimator.SetBool(_animIDIsDodging, isDodging);

            if (HasParameter(_animIDIsWalking))
            {
                bool isWalking = speed > 0.1f && isGrounded;
                characterAnimator.SetBool(_animIDIsWalking, isWalking);
            }
        }
    }

    /// <summary>
    /// Checks if animator has a parameter with the given hash
    /// </summary>
    private bool HasParameter(int paramHash)
    {
        if (characterAnimator == null) return false;

        foreach (AnimatorControllerParameter param in characterAnimator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }
        return false;
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
    /// Changes the jump height in meters
    /// </summary>
    public void SetJumpHeight(float newHeight)
    {
        jumpHeight = newHeight;
    }

    /// <summary>
    /// Changes the jump timeout duration
    /// </summary>
    public void SetJumpTimeout(float newTimeout)
    {
        jumpTimeout = newTimeout;
    }

    /// <summary>
    /// Changes the acceleration and deceleration rate
    /// </summary>
    public void SetSpeedChangeRate(float newRate)
    {
        speedChangeRate = newRate;
    }

    /// <summary>
    /// Changes the maximum speed the character can reach
    /// </summary>
    public void SetMaxVelocity(float newMax)
    {
        maxVelocity = newMax;
    }

    /// <summary>
    /// Changes the rotation smoothing time
    /// </summary>
    public void SetRotationSmoothTime(float newSmoothTime)
    {
        rotationSmoothTime = newSmoothTime;
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

    /// <summary>
    /// Changes the gravity force applied to the character
    /// </summary>
    public void SetGravity(float newGravity)
    {
        gravity = newGravity;
    }

    /// <summary>
    /// Changes the maximum falling speed
    /// </summary>
    public void SetTerminalVelocity(float newTerminalVelocity)
    {
        terminalVelocity = newTerminalVelocity;
    }

    /// <summary>
    /// Changes how fast the character slides down steep slopes
    /// </summary>
    public void SetSlopeSlideSpeed(float newSlideSpeed)
    {
        slopeSlideSpeed = newSlideSpeed;
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
        // Get controller reference if needed (for Edit mode visualization)
        CharacterController cc = controller != null ? controller : GetComponent<CharacterController>();
        if (cc == null) return;

        // Ground check visualization
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPosition = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
        Gizmos.DrawWireSphere(checkPosition, cc.radius + groundCheckDistance);

        // Platform detection ray
        Gizmos.color = isOnPlatform ? Color.green : Color.yellow;
        Vector3 rayStart = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
        Gizmos.DrawRay(rayStart, Vector3.down * (cc.radius + groundCheckDistance + 0.1f));

        // Slope check visualization
        Gizmos.color = isOnSteepSlope ? Color.red : Color.yellow;
        if (lastMoveDirection != Vector3.zero)
        {
            Vector3 checkOrigin = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawRay(checkOrigin, lastMoveDirection * (cc.radius + slopeCheckDistance));
        }

        // Slope normal visualization
        if (slopeNormal != Vector3.up)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, slopeNormal * 2f);
        }

        // Slope slide direction visualization
        if (Application.isPlaying && isGrounded && isOnSteepSlope)
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, slideDirection * 3f);
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