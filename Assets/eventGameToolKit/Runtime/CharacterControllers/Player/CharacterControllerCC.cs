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
/// CharacterController-based humanoid character controller with slope detection, dodge mechanics, 
/// animation support, and built-in moving platform support.
/// 
/// SPAWN SYSTEM: On Awake(), this controller checks for any ISpawnPointProvider in the scene
/// (such as GameCheckpointManager) and spawns at that position BEFORE physics initializes.
/// This eliminates race conditions between spawn systems and physics.
/// 
/// Common use: Third-person adventure games, action platformers, or character movement systems.
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

    [Header("Ground Detection")]
    [Tooltip("Layer(s) to detect as ground. Create a 'Ground' layer and assign it here.")]
    [SerializeField] private LayerMask groundLayer = ~0;

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
    [SerializeField] private float groundStickForce = -1.5f;

    [Header("Slope Settings")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeCheckDistance = 1f;
    [SerializeField] private float slopeSlideSpeed = 5f;

    [Header("Platform Settings")]
    [Tooltip("Detect platforms by layer, tag, or both")]
    [SerializeField] private PlatformDetectionMode platformDetectionMode = PlatformDetectionMode.Tag;
    [SerializeField] private LayerMask platformLayer;
    [Tooltip("Tag to detect as moving platform.")]
    [SerializeField] private string platformTag = "Untagged";
    [SerializeField] private bool applyVerticalMovement = true;

    [Header("Spawn Settings")]
    [Tooltip("Check for spawn point providers (checkpoints, spawn managers) on Awake")]
    [SerializeField] private bool useSpawnPointProviders = true;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform characterMesh;
    [Tooltip("Time in seconds before IdleTime parameter starts counting. 0 = disabled.")]
    [SerializeField] private float idleTimeBeforeEmote = 0f;

    [Header("Events")]
    public UnityEvent onGrounded;
    public UnityEvent onJump;
    public UnityEvent onLanding;
    public UnityEvent onStartMoving;
    public UnityEvent onStopMoving;
    public UnityEvent onSteepSlope;
    public UnityEvent onDodge;
    public UnityEvent onDodgeCooldownReady;
    public UnityEvent<Vector3> onTeleport;
    public UnityEvent<Vector3> onSpawnPointUsed;

    // Core state
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

    // Jump timing
    private float jumpTimeoutDelta;
    private float currentSpeed;
    private float rotationVelocity;

    // Animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDVerticalVelocity;
    private int _animIDIsDodging;
    private int _animIDIsWalking;
    private int _animIDIdleTime;
    private bool _lastAnimatorGroundedState;
    private float currentIdleTime;

    #region Initialization

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
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
        // Find all spawn point providers in the scene
        // This includes checkpoint managers, spawn point selectors, etc.
        ISpawnPointProvider[] providers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None) 
            as ISpawnPointProvider[];
        
        // More reliable approach: search through all MonoBehaviours
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        
        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour is ISpawnPointProvider provider && provider.HasSpawnPoint)
            {
                // Found a valid spawn point - use it
                Debug.Log($"CharacterControllerCC: Found spawn point at {provider.SpawnPosition}");
                
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
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.defaultActionMap = "Player";
            playerInput.neverAutoSwitchControlSchemes = false;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (characterAnimator == null && characterMesh != null)
            characterAnimator = characterMesh.GetComponentInChildren<Animator>();

        if (characterAnimator != null)
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDVerticalVelocity = Animator.StringToHash("VerticalVelocity");
            _animIDIsDodging = Animator.StringToHash("IsDodging");
            _animIDIsWalking = Animator.StringToHash("IsWalking");
            _animIDIdleTime = Animator.StringToHash("IdleTime");
        }

        jumpTimeoutDelta = jumpTimeout;
        ConfigureCharacterController();
    }

    private void ConfigureCharacterController()
    {
        if (controller == null) return;
        controller.slopeLimit = maxSlopeAngle;
        if (controller.skinWidth < 0.01f)
            controller.skinWidth = 0.08f;
    }

    #endregion

    #region Teleportation

    /// <summary>
    /// Teleports the character to a new position. Use for portals, respawns, cutscenes.
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        TeleportTo(position, transform.rotation);
    }

    /// <summary>
    /// Teleports the character to a new position and rotation.
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        velocity = Vector3.zero;
        isDodging = false;
        dodgeRequested = false;
        currentPlatform = null;
        isOnPlatform = false;

        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;

        Debug.Log($"CharacterControllerCC: Teleported to {position}");
        onTeleport?.Invoke(position);
    }

    #endregion

    #region Input Callbacks

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
            if (allowAirDodge || isGrounded)
            {
                dodgeRequested = true;
            }
        }
    }

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
        UpdateDodgeCooldown();
        CheckGrounded();
        CheckSlope();
        CheckForPlatform();
        HandleDodge();
        HandleJump();
        HandleRotation();
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

    #region Ground and Slope Detection

    private void UpdateDodgeCooldown()
    {
        if (dodgeCooldownTimer > 0f)
        {
            float previousTimer = dodgeCooldownTimer;
            dodgeCooldownTimer -= Time.deltaTime;

            if (dodgeCooldownTimer <= 0f && previousTimer > 0f)
            {
                onDodgeCooldownReady.Invoke();
            }
        }
    }

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

    private void HandleDodge()
    {
        if (dodgeRequested)
        {
            dodgeRequested = false;
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            if (inputDirection != Vector3.zero)
            {
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
                dodgeDirection = lastMoveDirection.normalized;
            }
            else
            {
                dodgeDirection = transform.forward;
            }

            if (isOnSteepSlope) return;

            dodgeStartPosition = transform.position;
            isDodging = true;
            dodgeCooldownTimer = dodgeCooldown;
            onDodge.Invoke();
        }

        if (isDodging)
        {
            if (isOnSteepSlope)
            {
                isDodging = false;
                return;
            }

            float distanceTraveled = Vector3.Distance(dodgeStartPosition, transform.position);
            if (distanceTraveled < dodgeDistance)
            {
                velocity.x = dodgeDirection.x * dodgeSpeed;
                velocity.z = dodgeDirection.z * dodgeSpeed;
            }
            else
            {
                isDodging = false;
            }
        }
    }

    private void HandleMovement()
    {
        if (isDodging) return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float targetSpeed = moveSpeed;

        if (inputDirection != Vector3.zero)
        {
            float effectiveMaxVelocity = maxVelocity;

            if (enableSprint && isSprinting && isGrounded)
            {
                targetSpeed *= sprintSpeedMultiplier;
                effectiveMaxVelocity *= sprintSpeedMultiplier;
            }

            if (!isGrounded) targetSpeed *= airControlFactor;

            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 targetDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z).normalized;
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

    private void HandleRotation()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDirection != Vector3.zero)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 targetDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z).normalized;
            float targetRotation = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetRotation,
                ref rotationVelocity,
                rotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }
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

        if (HasParameter(_animIDIsDodging))
            characterAnimator.SetBool(_animIDIsDodging, isDodging);

        if (HasParameter(_animIDIsWalking))
            characterAnimator.SetBool(_animIDIsWalking, speed > 0.1f && isGrounded);

        if (HasParameter(_animIDIdleTime) && idleTimeBeforeEmote > 0f)
        {
            if (speed > 0.1f || !isGrounded || isDodging)
                currentIdleTime = 0f;
            else
                currentIdleTime += Time.deltaTime;

            characterAnimator.SetFloat(_animIDIdleTime, currentIdleTime);
        }
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

    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
    public void SetJumpHeight(float newHeight) => jumpHeight = newHeight;
    public void SetJumpTimeout(float newTimeout) => jumpTimeout = newTimeout;
    public void SetSpeedChangeRate(float newRate) => speedChangeRate = newRate;
    public void SetMaxVelocity(float newMax) => maxVelocity = newMax;
    public void SetRotationSmoothTime(float newSmoothTime) => rotationSmoothTime = newSmoothTime;
    public void SetDodgeDistance(float newDistance) => dodgeDistance = newDistance;
    public void SetDodgeSpeed(float newSpeed) => dodgeSpeed = newSpeed;
    public void SetDodgeCooldown(float newCooldown) => dodgeCooldown = newCooldown;
    public void SetGravity(float newGravity) => gravity = newGravity;
    public void SetTerminalVelocity(float newTerminalVelocity) => terminalVelocity = newTerminalVelocity;
    public void SetSlopeSlideSpeed(float newSlideSpeed) => slopeSlideSpeed = newSlideSpeed;
    public void ResetVelocity() => velocity = Vector3.zero;

    #endregion

    #region Public Properties

    public bool IsGrounded => isGrounded;
    public bool IsMoving => isMoving;
    public bool IsOnSteepSlope => isOnSteepSlope;
    public bool IsDodging => isDodging;
    public bool IsOnPlatform => isOnPlatform;
    public Transform CurrentPlatform => currentPlatform;
    public float DodgeCooldownRemaining => dodgeCooldownTimer;
    public float CurrentSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;
    public bool IsSprinting => isSprinting && enableSprint;
    public Vector3 Velocity => velocity;

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        CharacterController cc = controller != null ? controller : GetComponent<CharacterController>();
        if (cc == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPosition = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
        Gizmos.DrawWireSphere(checkPosition, cc.radius + groundCheckDistance);

        Gizmos.color = isOnPlatform ? Color.green : Color.yellow;
        Vector3 rayStart = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
        Gizmos.DrawRay(rayStart, Vector3.down * (cc.radius + groundCheckDistance + 0.1f));

        if (slopeNormal != Vector3.up)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, slopeNormal * 2f);
        }

        if (Application.isPlaying && isGrounded && isOnSteepSlope)
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, slideDirection * 3f);
        }

        if (Application.isPlaying && isDodging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, dodgeDirection * dodgeDistance);
        }
    }

    #endregion
}
