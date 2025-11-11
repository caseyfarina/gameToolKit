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
[RequireComponent(typeof(PlayerInput))]
public class CharacterControllerCC : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float maxVelocity = 8f;
    [Tooltip("Acceleration and deceleration rate")]
    [SerializeField] private float speedChangeRate = 10.0f;
    [SerializeField] private float airControlFactor = 0.5f;

    [Header("Sprint Settings (Optional)")]
    [Tooltip("Enable sprint functionality (hold Sprint button to run faster)")]
    [SerializeField] private bool enableSprint = false;
    [Tooltip("Speed multiplier when sprinting (2.0 = twice as fast)")]
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;

    [Header("Jump Settings")]
    [Tooltip("Height in meters the character can jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [Tooltip("Time required to pass before being able to jump again")]
    [SerializeField] private float jumpTimeout = 0.5f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("⚠️ IMPORTANT: Create a layer named 'Ground' (Edit > Project Settings > Tags and Layers)")]
    [Space(-20)]
    [Header("Then select ONLY 'Ground' in the dropdown below. Spelling and capitalization matter!")]
    [Tooltip("Layer(s) to detect as ground. Create a 'Ground' layer in your project and assign it here.")]
    [SerializeField] private LayerMask groundLayer = ~0; // Default to "Everything" until students configure

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
    [Header("⚠️ OPTIONAL: For moving platforms, create a 'MovingPlatform' tag")]
    [Space(-20)]
    [Header("(Edit > Project Settings > Tags and Layers > Tags > +)")]
    [Tooltip("Detect platforms by layer, tag, or both")]
    [SerializeField] private PlatformDetectionMode platformDetectionMode = PlatformDetectionMode.Tag;
    [SerializeField] private LayerMask platformLayer;
    [Tooltip("Tag to detect as moving platform. Create this tag in Project Settings if using moving platforms.")]
    [SerializeField] private string platformTag = "Untagged";
    [SerializeField] private bool applyVerticalMovement = true;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform characterMesh;
    [Header("⚠️ OPTIONAL: For idle emote/fidget animations after being idle")]
    [Space(-20)]
    [Header("Set to 0 to disable. Animator can use 'IdleTime' float parameter.")]
    [Tooltip("Time in seconds before IdleTime parameter starts counting. 0 = disabled. Useful for fidget/emote animations.")]
    [SerializeField] private float idleTimeBeforeEmote = 0f;

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
    private bool isSprinting;

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
    private int _animIDIdleTime;

    // Animation state tracking (prevents unnecessary updates)
    private bool _lastAnimatorGroundedState;

    // Idle time tracking for emote/fidget animations
    private float currentIdleTime;

    /// <summary>
    /// Called when component is first added or reset. Auto-configures PlayerInput component.
    /// </summary>
    private void Reset()
    {
        // Auto-configure PlayerInput component if it exists
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            // Set default map to "Player" so input works immediately
            playerInput.defaultActionMap = "Player";

            // Enable auto-switch if multiple action maps exist
            playerInput.neverAutoSwitchControlSchemes = false;

            Debug.Log("CharacterControllerCC: Auto-configured PlayerInput with 'Player' action map");
        }
    }

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
            _animIDIdleTime = Animator.StringToHash("IdleTime");
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

    /// <summary>
    /// Input System callback for sprint input (only active if enableSprint is true)
    /// </summary>
    public void OnSprint(InputValue value)
    {
        if (enableSprint)
        {
            // For a 'Value' action, this event now correctly fires when the value 
            // changes from 0 to 1 (press) AND 1 to 0 (release).
            isSprinting = value.isPressed;
            // Re-enable your Debug.Log to confirm: 
            // Debug.Log($"Sprint state changed: isSprinting = {isSprinting}");
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

        float targetSpeed = moveSpeed; // Start with base speed

        // --- Calculate Target Speed based on Sprint and Air Control ---
        if (inputDirection != Vector3.zero)
        {
            float effectiveMaxVelocity = maxVelocity;

            // Apply sprint multiplier if sprinting is enabled and active
            if (enableSprint && isSprinting && isGrounded)
            {
                targetSpeed *= sprintSpeedMultiplier;
                effectiveMaxVelocity *= sprintSpeedMultiplier; // Also increase max velocity cap
            }

            if (!isGrounded)
            {
                targetSpeed *= airControlFactor;
            }

            // --- Movement Direction Calculation ---
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z);
            lastMoveDirection = moveDirection;

            // ... (Steep slope movement block logic remains the same) ...
            bool blockMovement = false;
            if (isOnSteepSlope)
            {
                Vector3 slopePlaneDirection = Vector3.ProjectOnPlane(moveDirection, slopeNormal).normalized;
                float movementVertical = Vector3.Dot(slopePlaneDirection, Vector3.up);
                blockMovement = movementVertical > 0.01f;
            }

            if (!blockMovement)
            {
                // Get current horizontal speed
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
                float currentHorizontalSpeed = horizontalVelocity.magnitude;

                // Smooth acceleration to target speed (Unity TPC style)
                currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
                    Time.fixedDeltaTime * speedChangeRate);

                // Clamp to effective max velocity (accounts for sprint)
                currentSpeed = Mathf.Min(currentSpeed, effectiveMaxVelocity);

                // Apply smoothed speed in movement direction
                velocity.x = moveDirection.x * currentSpeed;
                velocity.z = moveDirection.z * currentSpeed;
            }
        }
        else
        {
            // No input - smooth deceleration (grounded, not dodging, not on steep slope)
            if (isGrounded && !isDodging && !isOnSteepSlope)
            {
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
                float currentHorizontalSpeed = horizontalVelocity.magnitude;

                // *** THE FIX IS HERE: Set deceleration target based on sprint state/speed ***
                // Target speed for deceleration is 0 if not moving AND not fast (or if slow).
                // If we're moving faster than base speed (due to sprint) and input is zero, 
                // we should decelerate to base speed (moveSpeed) first, then to 0.
                float decelerationTarget = 0f;

                // If current speed is greater than moveSpeed AND not sprinting, 
                // the immediate target for deceleration should be the base moveSpeed.
                // This ensures the sprint velocity is reduced even if we let go of input
                // while in a sprint.
                if (currentHorizontalSpeed > moveSpeed + 0.01f) // Use a small tolerance
                {
                    decelerationTarget = moveSpeed;
                }

                // Smooth deceleration to the calculated target (0 or moveSpeed)
                currentSpeed = Mathf.Lerp(currentHorizontalSpeed, decelerationTarget,
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
        // Don't move if controller is disabled (e.g., during checkpoint restoration)
        if (!controller.enabled)
            return;

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

            // CRITICAL FIX: Only update Grounded when it actually changes
            // Setting it every frame can retrigger transitions and prevent jump animations
            if (HasParameter(_animIDGrounded) && isGrounded != _lastAnimatorGroundedState)
            {
                characterAnimator.SetBool(_animIDGrounded, isGrounded);
                _lastAnimatorGroundedState = isGrounded;
            }

            if (HasParameter(_animIDVerticalVelocity))
                characterAnimator.SetFloat(_animIDVerticalVelocity, velocity.y);

            if (HasParameter(_animIDIsDodging))
                characterAnimator.SetBool(_animIDIsDodging, isDodging);

            if (HasParameter(_animIDIsWalking))
            {
                bool isWalking = speed > 0.1f && isGrounded;
                characterAnimator.SetBool(_animIDIsWalking, isWalking);
            }

            // Track idle time for emote/fidget animations (if enabled)
            if (HasParameter(_animIDIdleTime) && idleTimeBeforeEmote > 0f)
            {
                // Reset idle time if moving, jumping, or dodging
                if (speed > 0.1f || !isGrounded || isDodging)
                {
                    currentIdleTime = 0f;
                }
                else
                {
                    // Increment idle time when truly idle
                    currentIdleTime += Time.deltaTime;
                }

                characterAnimator.SetFloat(_animIDIdleTime, currentIdleTime);
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
    public bool IsSprinting => isSprinting && enableSprint;

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