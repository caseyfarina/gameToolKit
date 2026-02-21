using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// CharacterController-based first-person character controller with mouse look, slope detection,
/// animation support, and built-in moving platform support.
///
/// SPAWN SYSTEM: On Awake(), this controller checks for any ISpawnPointProvider in the scene
/// (such as GameCheckpointManager) and spawns at that position BEFORE physics initializes.
/// This eliminates race conditions between spawn systems and physics.
///
/// LOOK SYSTEM: Camera pitch (up/down) rotates the camera Transform only, while yaw (left/right)
/// rotates the character body. Sensitivity is automatically adjusted based on active control scheme.
///
/// Common use: First-person shooters, walking simulators, exploration games, horror games.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class CharacterControllerFP : MonoBehaviour
{
    [Header("Movement Settings")]
    /// <summary>
    /// Base movement speed in units per second
    /// </summary>
    [SerializeField] private float moveSpeed = 6f;

    /// <summary>
    /// Maximum horizontal velocity the character can reach
    /// </summary>
    [SerializeField] private float maxVelocity = 6f;

    /// <summary>
    /// How quickly the character accelerates and decelerates
    /// </summary>
    [Tooltip("Acceleration and deceleration rate")]
    [SerializeField] private float speedChangeRate = 10.0f;

    /// <summary>
    /// Multiplier for movement control while airborne (0 = no air control, 1 = full)
    /// </summary>
    [SerializeField] private float airControlFactor = 0.5f;

    [Header("Sprint Settings (Optional)")]
    /// <summary>
    /// Enable sprint functionality (hold Sprint button to run faster)
    /// </summary>
    [Tooltip("Enable sprint functionality (hold Sprint button to run faster)")]
    [SerializeField] private bool enableSprint = false;

    /// <summary>
    /// Speed multiplier when sprinting (2.0 = twice as fast)
    /// </summary>
    [Tooltip("Speed multiplier when sprinting (2.0 = twice as fast)")]
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;

    [Header("Look Settings")]
    /// <summary>
    /// Mouse look sensitivity. Higher values = faster camera rotation with mouse input
    /// </summary>
    [Tooltip("Mouse look sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;

    /// <summary>
    /// Gamepad look sensitivity. Higher values = faster camera rotation with gamepad stick
    /// </summary>
    [Tooltip("Gamepad right stick look sensitivity")]
    [SerializeField] private float gamepadSensitivity = 100f;

    /// <summary>
    /// Maximum vertical look angle in degrees. Prevents looking past straight up/down
    /// </summary>
    [Tooltip("Maximum vertical look angle in degrees (e.g., 80 = can look almost straight up/down)")]
    [Range(30f, 90f)]
    [SerializeField] private float verticalLookLimit = 80f;

    /// <summary>
    /// Invert the Y axis for look input (push up to look down)
    /// </summary>
    [Tooltip("Invert the Y axis for look input")]
    [SerializeField] private bool invertY = false;

    [Header("Cursor Settings")]
    /// <summary>
    /// Lock and hide the cursor when the game starts
    /// </summary>
    [Tooltip("Lock and hide the cursor when the game starts")]
    [SerializeField] private bool lockCursorOnStart = true;

    /// <summary>
    /// Key to toggle cursor lock/unlock (useful for menus and pause screens)
    /// </summary>
    [Tooltip("Key to toggle cursor lock/unlock")]
    [SerializeField] private KeyCode cursorToggleKey = KeyCode.Escape;

    [Header("Reticle")]
    /// <summary>
    /// Optional crosshair texture drawn at screen center when the cursor is locked.
    /// Used with InputMouseInteraction (CenterScreen mode) so the player can see where they are aiming.
    /// </summary>
    [Tooltip("Crosshair texture drawn at screen center when cursor is locked (optional)")]
    [SerializeField] private Texture2D reticleTexture;

    /// <summary>
    /// Size of the reticle in pixels (width and height)
    /// </summary>
    [Tooltip("Size of the reticle in pixels")]
    [SerializeField] private float reticleSize = 32f;

    [Header("Jump Settings")]
    /// <summary>
    /// Height in meters the character can jump
    /// </summary>
    [Tooltip("Height in meters the character can jump")]
    [SerializeField] private float jumpHeight = 1.2f;

    /// <summary>
    /// Time required to pass before being able to jump again
    /// </summary>
    [Tooltip("Time required to pass before being able to jump again")]
    [SerializeField] private float jumpTimeout = 0.5f;

    /// <summary>
    /// Distance below the character to check for ground
    /// </summary>
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Ground Detection")]
    /// <summary>
    /// Layer(s) to detect as ground. Create a 'Ground' layer and assign it here
    /// </summary>
    [Tooltip("Layer(s) to detect as ground. Create a 'Ground' layer and assign it here.")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Gravity Settings")]
    /// <summary>
    /// Gravity acceleration applied to the character (negative = downward)
    /// </summary>
    [SerializeField] private float gravity = -20f;

    /// <summary>
    /// Maximum falling speed (negative value)
    /// </summary>
    [SerializeField] private float terminalVelocity = -50f;

    /// <summary>
    /// Downward force applied when grounded to keep the character on the ground
    /// </summary>
    [SerializeField] private float groundStickForce = -1.5f;

    [Header("Slope Settings")]
    /// <summary>
    /// Maximum angle in degrees the character can walk up
    /// </summary>
    [SerializeField] private float maxSlopeAngle = 45f;

    /// <summary>
    /// How far ahead to check for slopes
    /// </summary>
    [SerializeField] private float slopeCheckDistance = 1f;

    /// <summary>
    /// Speed at which the character slides down steep slopes
    /// </summary>
    [SerializeField] private float slopeSlideSpeed = 5f;

    [Header("Platform Settings")]
    /// <summary>
    /// How to detect moving platforms (by tag, layer, or both)
    /// </summary>
    [Tooltip("Detect platforms by layer, tag, or both")]
    [SerializeField] private PlatformDetectionMode platformDetectionMode = PlatformDetectionMode.Tag;

    /// <summary>
    /// Layer mask for platform detection when using Layer or Both mode
    /// </summary>
    [SerializeField] private LayerMask platformLayer;

    /// <summary>
    /// Tag to detect as moving platform
    /// </summary>
    [Tooltip("Tag to detect as moving platform.")]
    [SerializeField] private string platformTag = "Untagged";

    /// <summary>
    /// Whether to follow platform vertical movement
    /// </summary>
    [SerializeField] private bool applyVerticalMovement = true;

    [Header("Spawn Settings")]
    /// <summary>
    /// Check for spawn point providers (checkpoints, spawn managers) on Awake
    /// </summary>
    [Tooltip("Check for spawn point providers (checkpoints, spawn managers) on Awake")]
    [SerializeField] private bool useSpawnPointProviders = true;

    [Header("Camera")]
    /// <summary>
    /// The camera Transform used for first-person look. If null, auto-finds Camera.main
    /// </summary>
    [Tooltip("Camera Transform for first-person look. If null, auto-finds Camera.main.")]
    [SerializeField] private Transform playerCamera;

    [Header("Animation")]
    /// <summary>
    /// Optional Animator for first-person arm/weapon animations
    /// </summary>
    [SerializeField] private Animator characterAnimator;

    [Header("Events")]
    /// <summary>
    /// Fires every frame the character is grounded
    /// </summary>
    public UnityEvent onGrounded;

    /// <summary>
    /// Fires when the character jumps
    /// </summary>
    public UnityEvent onJump;

    /// <summary>
    /// Fires when the character lands on the ground after being airborne
    /// </summary>
    public UnityEvent onLanding;

    /// <summary>
    /// Fires when the character starts moving from a standstill
    /// </summary>
    public UnityEvent onStartMoving;

    /// <summary>
    /// Fires when the character stops moving
    /// </summary>
    public UnityEvent onStopMoving;

    /// <summary>
    /// Fires when the character is on a slope steeper than maxSlopeAngle
    /// </summary>
    public UnityEvent onSteepSlope;

    /// <summary>
    /// Fires when the character is teleported. Passes the destination position
    /// </summary>
    public UnityEvent<Vector3> onTeleport;

    /// <summary>
    /// Fires when the character uses a spawn point. Passes the spawn position
    /// </summary>
    public UnityEvent<Vector3> onSpawnPointUsed;

    /// <summary>
    /// Fires when cursor lock state changes. Passes true if cursor is now locked
    /// </summary>
    public UnityEvent<bool> onCursorLockChanged;

    // Core state
    private CharacterController controller;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpRequested;
    private bool isMoving;
    private Vector3 lastMoveDirection;
    private bool isOnSteepSlope;
    private Vector3 slopeNormal = Vector3.up;
    private bool isSprinting;
    private bool isCursorLocked;
    private bool _inputEnabled = true;

    // Camera state
    private float cameraPitch;

    // Platform state
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    private bool isOnPlatform;
    private bool wasOnPlatform;
    private int landingStabilizationFrames = 0;

    // Jump timing
    private float jumpTimeoutDelta;

    // Animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDVerticalVelocity;
    private int _animIDIsWalking;
    private int _animIDIsSprinting;
    private bool _lastAnimatorGroundedState;

    #region Initialization

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Check for spawn point BEFORE Start() runs and before physics initializes
        if (useSpawnPointProviders)
        {
            CheckForSpawnPoint();
        }
    }

    /// <summary>
    /// Checks for any ISpawnPointProvider in the scene and spawns at that position.
    /// Called in Awake() to set position before physics runs.
    /// </summary>
    private void CheckForSpawnPoint()
    {
        // Search through all MonoBehaviours for spawn point providers
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour is ISpawnPointProvider provider && provider.HasSpawnPoint)
            {
                // Found a valid spawn point - use it
                Debug.Log($"CharacterControllerFP: Found spawn point at {provider.SpawnPosition}");

                // Disable CharacterController to set position directly
                controller.enabled = false;
                transform.position = provider.SpawnPosition;
                transform.rotation = provider.SpawnRotation;
                controller.enabled = true;

                // Notify the provider that we used the spawn point
                provider.OnSpawnPointUsed();

                // Fire event
                onSpawnPointUsed?.Invoke(provider.SpawnPosition);

                // Only use first valid provider
                return;
            }
        }

        // No spawn point found - stay at current position (scene default)
    }

    private void Reset()
    {
        PlayerInput pi = GetComponent<PlayerInput>();
        if (pi != null)
        {
            pi.defaultActionMap = "Player";
            pi.neverAutoSwitchControlSchemes = false;
        }
    }

    private void Start()
    {
        if (playerCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("CharacterControllerFP: No camera assigned and Camera.main not found. Look controls will not work.");
            }
        }

        if (characterAnimator != null)
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDVerticalVelocity = Animator.StringToHash("VerticalVelocity");
            _animIDIsWalking = Animator.StringToHash("IsWalking");
            _animIDIsSprinting = Animator.StringToHash("IsSprinting");
        }

        jumpTimeoutDelta = jumpTimeout;
        ConfigureCharacterController();

        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void ConfigureCharacterController()
    {
        if (controller == null) return;
        controller.slopeLimit = maxSlopeAngle;
        if (controller.skinWidth < 0.01f)
            controller.skinWidth = 0.08f;
    }

    #endregion

    #region Cursor Management

    /// <summary>
    /// Locks and hides the cursor. Call this when gameplay resumes or game starts
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isCursorLocked = true;
        onCursorLockChanged?.Invoke(true);
    }

    /// <summary>
    /// Unlocks and shows the cursor. Call this for menus, pause screens, or inventory
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isCursorLocked = false;
        onCursorLockChanged?.Invoke(false);
    }

    /// <summary>
    /// Toggles the cursor between locked and unlocked states
    /// </summary>
    public void ToggleCursor()
    {
        if (isCursorLocked)
            UnlockCursor();
        else
            LockCursor();
    }

    /// <summary>
    /// Enables or disables all player input (look, move, jump, cursor toggle).
    /// Use this during cutscenes, dialogue decisions, or UI interactions to freeze the controller.
    /// Gravity and platform attachment continue to apply so the player does not float.
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    private void HandleCursorToggle()
    {
        if (!_inputEnabled) return;
        if (Input.GetKeyDown(cursorToggleKey))
        {
            ToggleCursor();
        }
    }

    #endregion

    #region Teleportation

    /// <summary>
    /// Teleports the character to a new position. Use for portals, respawns, cutscenes
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        TeleportTo(position, transform.rotation);
    }

    /// <summary>
    /// Teleports the character to a new position and rotation
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        velocity = Vector3.zero;
        currentPlatform = null;
        isOnPlatform = false;

        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;

        // Reset camera pitch to match new rotation
        cameraPitch = 0f;

        Debug.Log($"CharacterControllerFP: Teleported to {position}");
        onTeleport?.Invoke(position);
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Called by PlayerInput when Move action fires. Receives WASD/stick input
    /// </summary>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called by PlayerInput when Look action fires. Receives mouse/stick input
    /// </summary>
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called by PlayerInput when Jump action fires
    /// </summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && !isOnSteepSlope)
        {
            jumpRequested = true;
        }
    }

    /// <summary>
    /// Called by PlayerInput when Sprint action fires
    /// </summary>
    public void OnSprint(InputValue value)
    {
        if (enableSprint)
        {
            isSprinting = value.isPressed;
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        HandleCursorToggle();
        HandleLook();
        CheckGrounded();
        CheckSlope();
        CheckForPlatform();
        HandleJump();
        UpdateAnimations();
        CheckMovementEvents();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleSlopeSliding();
        HandleGravity();
        ApplyPlatformMovement();
        ApplyMovement();
    }

    #endregion

    #region Look Handling

    private void HandleLook()
    {
        if (!_inputEnabled) return;
        if (playerCamera == null) return;
        if (lookInput == Vector2.zero) return;

        // Determine sensitivity based on active control scheme
        float sensitivity;
        bool isGamepad = playerInput != null &&
                         playerInput.currentControlScheme != null &&
                         playerInput.currentControlScheme == "Gamepad";

        if (isGamepad)
        {
            sensitivity = gamepadSensitivity * Time.deltaTime;
        }
        else
        {
            // Mouse input is already frame-rate independent (delta), so no Time.deltaTime
            sensitivity = mouseSensitivity;
        }

        float yawInput = lookInput.x * sensitivity;
        float pitchInput = lookInput.y * sensitivity;

        if (invertY)
        {
            pitchInput = -pitchInput;
        }

        // Yaw: rotate the character body left/right
        transform.Rotate(Vector3.up, yawInput);

        // Pitch: rotate the camera up/down, clamped to prevent over-rotation
        cameraPitch -= pitchInput;
        cameraPitch = Mathf.Clamp(cameraPitch, -verticalLookLimit, verticalLookLimit);

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    #endregion

    #region Ground and Slope Detection

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;

        if (velocity.y > 0.1f)
        {
            isGrounded = false;
            isOnSteepSlope = false;
            slopeNormal = Vector3.up;
        }
        else
        {
            Vector3 castOrigin = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
            castOrigin.y += 0.1f;

            RaycastHit hit;
            float castDistance = groundCheckDistance + 0.15f;
            bool sphereHit = Physics.SphereCast(
                castOrigin,
                controller.radius * 0.9f,
                Vector3.down,
                out hit,
                castDistance,
                groundLayer
            );

            bool controllerGrounded = controller.isGrounded;
            isGrounded = controllerGrounded || sphereHit;

            if (sphereHit)
            {
                slopeNormal = hit.normal;
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                isOnSteepSlope = slopeAngle > maxSlopeAngle;
            }
            else
            {
                slopeNormal = Vector3.up;
                isOnSteepSlope = false;
            }
        }

        if (isGrounded && !wasGrounded)
        {
            if (velocity.y < 0) velocity.y = 0f;
            onLanding.Invoke();
            landingStabilizationFrames = 2;
        }

        if (isGrounded) onGrounded.Invoke();
        if (isOnSteepSlope && !wasGrounded) onSteepSlope.Invoke();
        if (landingStabilizationFrames > 0) landingStabilizationFrames--;
    }

    private void CheckSlope()
    {
        if (lastMoveDirection != Vector3.zero && isGrounded)
        {
            Vector3 checkOrigin = transform.position + Vector3.up * (controller.height * 0.25f);
            RaycastHit forwardHit;

            if (Physics.Raycast(checkOrigin, lastMoveDirection, out forwardHit, controller.radius + slopeCheckDistance, groundLayer))
            {
                float forwardSlopeAngle = Vector3.Angle(forwardHit.normal, Vector3.up);
                if (forwardSlopeAngle > maxSlopeAngle)
                {
                    isOnSteepSlope = true;
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

        Vector3 rayStart = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
        LayerMask raycastLayer = platformDetectionMode == PlatformDetectionMode.Tag ? groundLayer : platformLayer;

        RaycastHit hit;
        bool foundPlatform = Physics.Raycast(rayStart, Vector3.down, out hit,
            controller.radius + groundCheckDistance + 0.1f, raycastLayer);

        bool isPlatformValid = false;
        if (foundPlatform)
        {
            switch (platformDetectionMode)
            {
                case PlatformDetectionMode.Tag:
                    isPlatformValid = hit.collider.CompareTag(platformTag);
                    break;
                case PlatformDetectionMode.Layer:
                    isPlatformValid = true;
                    break;
                case PlatformDetectionMode.Both:
                    isPlatformValid = hit.collider.CompareTag(platformTag);
                    break;
            }
        }

        if (isPlatformValid)
        {
            if (currentPlatform != hit.transform)
            {
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

    #endregion

    #region Movement Handling

    private void HandleMovement()
    {
        if (!_inputEnabled) return;
        // Movement is always character-relative (forward/strafe)
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = (right * moveInput.x + forward * moveInput.y).normalized;
        float targetSpeed = moveSpeed;

        if (targetDirection != Vector3.zero)
        {
            float effectiveMaxVelocity = maxVelocity;

            if (enableSprint && isSprinting && isGrounded)
            {
                targetSpeed *= sprintSpeedMultiplier;
                effectiveMaxVelocity *= sprintSpeedMultiplier;
            }

            if (!isGrounded) targetSpeed *= airControlFactor;

            Vector3 targetVelocity = targetDirection * targetSpeed;
            Vector3 currentHorizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

            Vector3 newHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetVelocity,
                speedChangeRate * Time.fixedDeltaTime
            );

            if (newHorizontalVelocity.magnitude > effectiveMaxVelocity)
            {
                newHorizontalVelocity = newHorizontalVelocity.normalized * effectiveMaxVelocity;
            }

            velocity.x = newHorizontalVelocity.x;
            velocity.z = newHorizontalVelocity.z;
            lastMoveDirection = targetDirection;
        }
        else
        {
            Vector3 currentHorizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 newHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                Vector3.zero,
                speedChangeRate * Time.fixedDeltaTime
            );

            velocity.x = newHorizontalVelocity.x;
            velocity.z = newHorizontalVelocity.z;
        }
    }

    private void HandleJump()
    {
        if (!_inputEnabled) return;
        if (isGrounded)
        {
            jumpTimeoutDelta = jumpTimeout;

            if (jumpRequested && !isOnSteepSlope)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpRequested = false;
                onJump.Invoke();
            }
        }
        else
        {
            if (jumpTimeoutDelta > 0f) jumpTimeoutDelta -= Time.deltaTime;
            jumpRequested = false;
        }
    }

    private void HandleSlopeSliding()
    {
        if (isGrounded && isOnSteepSlope)
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            velocity += slideDirection * slopeSlideSpeed * Time.fixedDeltaTime;
        }
    }

    private void HandleGravity()
    {
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = groundStickForce;
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
            if (velocity.y < terminalVelocity) velocity.y = terminalVelocity;
        }
    }

    private void ApplyPlatformMovement()
    {
        if (isOnPlatform && currentPlatform != null && landingStabilizationFrames <= 0)
        {
            Vector3 platformDelta = currentPlatform.position - lastPlatformPosition;

            if (platformDelta.x != 0f || platformDelta.z != 0f)
            {
                controller.Move(new Vector3(platformDelta.x, 0f, platformDelta.z));
            }

            if (applyVerticalMovement && platformDelta.y != 0f)
            {
                controller.Move(new Vector3(0f, platformDelta.y, 0f));
            }

            Quaternion rotationDelta = currentPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);
            if (rotationDelta != Quaternion.identity)
            {
                Vector3 rotatedOffset = rotationDelta * (transform.position - currentPlatform.position);
                Vector3 newPosition = currentPlatform.position + rotatedOffset;
                controller.Move(newPosition - transform.position);
                transform.rotation = rotationDelta * transform.rotation;
            }

            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
        }
    }

    private void ApplyMovement()
    {
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    #endregion

    #region Animation

    private void UpdateAnimations()
    {
        if (characterAnimator == null) return;

        float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (HasParameter(_animIDSpeed))
            characterAnimator.SetFloat(_animIDSpeed, speed);

        if (HasParameter(_animIDGrounded) && isGrounded != _lastAnimatorGroundedState)
        {
            characterAnimator.SetBool(_animIDGrounded, isGrounded);
            _lastAnimatorGroundedState = isGrounded;
        }

        if (HasParameter(_animIDVerticalVelocity))
            characterAnimator.SetFloat(_animIDVerticalVelocity, velocity.y);

        if (HasParameter(_animIDIsWalking))
            characterAnimator.SetBool(_animIDIsWalking, speed > 0.1f && isGrounded);

        if (HasParameter(_animIDIsSprinting))
            characterAnimator.SetBool(_animIDIsSprinting, isSprinting && enableSprint && speed > 0.1f && isGrounded);
    }

    private bool HasParameter(int paramHash)
    {
        if (characterAnimator == null) return false;
        foreach (AnimatorControllerParameter param in characterAnimator.parameters)
        {
            if (param.nameHash == paramHash) return true;
        }
        return false;
    }

    private void CheckMovementEvents()
    {
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        bool currentlyMoving = horizontalVelocity.magnitude > 0.1f;

        if (currentlyMoving && !isMoving) onStartMoving.Invoke();
        else if (!currentlyMoving && isMoving) onStopMoving.Invoke();

        isMoving = currentlyMoving;
    }

    #endregion

    #region Public Setters

    /// <summary>
    /// Sets the movement speed
    /// </summary>
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;

    /// <summary>
    /// Sets the jump height in meters
    /// </summary>
    public void SetJumpHeight(float newHeight) => jumpHeight = newHeight;

    /// <summary>
    /// Sets the jump timeout duration
    /// </summary>
    public void SetJumpTimeout(float newTimeout) => jumpTimeout = newTimeout;

    /// <summary>
    /// Sets the acceleration/deceleration rate
    /// </summary>
    public void SetSpeedChangeRate(float newRate) => speedChangeRate = newRate;

    /// <summary>
    /// Sets the maximum horizontal velocity
    /// </summary>
    public void SetMaxVelocity(float newMax) => maxVelocity = newMax;

    /// <summary>
    /// Sets the mouse look sensitivity
    /// </summary>
    public void SetMouseSensitivity(float newSensitivity) => mouseSensitivity = newSensitivity;

    /// <summary>
    /// Sets the gamepad look sensitivity
    /// </summary>
    public void SetGamepadSensitivity(float newSensitivity) => gamepadSensitivity = newSensitivity;

    /// <summary>
    /// Sets whether the Y axis is inverted for look input
    /// </summary>
    public void SetInvertY(bool invert) => invertY = invert;

    /// <summary>
    /// Sets the gravity acceleration
    /// </summary>
    public void SetGravity(float newGravity) => gravity = newGravity;

    /// <summary>
    /// Sets the maximum falling speed
    /// </summary>
    public void SetTerminalVelocity(float newTerminalVelocity) => terminalVelocity = newTerminalVelocity;

    /// <summary>
    /// Sets the slope slide speed
    /// </summary>
    public void SetSlopeSlideSpeed(float newSlideSpeed) => slopeSlideSpeed = newSlideSpeed;

    /// <summary>
    /// Resets all velocity to zero
    /// </summary>
    public void ResetVelocity() => velocity = Vector3.zero;

    #endregion

    #region Public Properties

    /// <summary>
    /// Whether the character is currently on the ground
    /// </summary>
    public bool IsGrounded => isGrounded;

    /// <summary>
    /// Whether the character is currently moving horizontally
    /// </summary>
    public bool IsMoving => isMoving;

    /// <summary>
    /// Whether the character is on a slope steeper than maxSlopeAngle
    /// </summary>
    public bool IsOnSteepSlope => isOnSteepSlope;

    /// <summary>
    /// Whether the character is standing on a detected moving platform
    /// </summary>
    public bool IsOnPlatform => isOnPlatform;

    /// <summary>
    /// The Transform of the current moving platform, or null
    /// </summary>
    public Transform CurrentPlatform => currentPlatform;

    /// <summary>
    /// Current horizontal speed of the character
    /// </summary>
    public float CurrentSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;

    /// <summary>
    /// Whether the character is currently sprinting
    /// </summary>
    public bool IsSprinting => isSprinting && enableSprint;

    /// <summary>
    /// Current velocity vector of the character
    /// </summary>
    public Vector3 Velocity => velocity;

    /// <summary>
    /// Whether the cursor is currently locked
    /// </summary>
    public bool IsCursorLocked => isCursorLocked;

    /// <summary>
    /// Current camera pitch angle in degrees
    /// </summary>
    public float CameraPitch => cameraPitch;

    #endregion

    #region Reticle

    private void OnGUI()
    {
        if (reticleTexture == null || !isCursorLocked) return;

        float x = (Screen.width - reticleSize) * 0.5f;
        float y = (Screen.height - reticleSize) * 0.5f;
        GUI.DrawTexture(new Rect(x, y, reticleSize, reticleSize), reticleTexture);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        CharacterController cc = controller != null ? controller : GetComponent<CharacterController>();
        if (cc == null) return;

        // Ground check sphere
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPosition = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
        Gizmos.DrawWireSphere(checkPosition, cc.radius + groundCheckDistance);

        // Platform detection ray
        Gizmos.color = isOnPlatform ? Color.green : Color.yellow;
        Vector3 rayStart = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
        Gizmos.DrawRay(rayStart, Vector3.down * (cc.radius + groundCheckDistance + 0.1f));

        // Slope normal
        if (slopeNormal != Vector3.up)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, slopeNormal * 2f);
        }

        // Slide direction on steep slope
        if (Application.isPlaying && isGrounded && isOnSteepSlope)
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, slideDirection * 3f);
        }

        // Camera look direction
        if (playerCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerCamera.position, playerCamera.forward * 3f);
        }
    }

    #endregion
}
