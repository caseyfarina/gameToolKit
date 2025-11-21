using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AI enemy that chases the player using CharacterController with configurable jump behaviors and built-in platform support.
/// Common use: Patrolling enemies, chase sequences, monster AI, or competitive racing opponents.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class EnemyControllerCC : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float maxVelocity = 15f;
    [SerializeField] private bool enableAirControl = false;

    [Header("Player Detection (Gizmo: Yellow/Red = Detection, Orange = Leash)")]
    [Tooltip("Distance at which enemy starts chasing player (Gizmo: Yellow sphere, Red when player detected)")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("Distance beyond which enemy gives up chase (0 = auto-calculate as detection + 2) (Gizmo: Orange sphere)")]
    [SerializeField] private float leashRange = 0f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer = -1;

    [Header("Chase Behavior (Gizmo: Blue = Stop Distance)")]
    [SerializeField] private float minMoveInterval = 1f;
    [SerializeField] private float maxMoveInterval = 3f;
    [SerializeField] private bool pauseWhenPlayerClose = true;
    [Tooltip("How close to get to player before stopping (Gizmo: Blue sphere when pause enabled)")]
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Return Behavior (Gizmo: Magenta Cross = Origin, Green Line = Path)")]
    [Tooltip("When chase ends, enemy returns to starting position")]
    [SerializeField] private bool returnToOrigin = true;
    [Tooltip("Movement speed when returning to origin")]
    [SerializeField] private float returnSpeed = 5f;

    [Header("Patrol Behavior (Gizmo: Cyan Cross = Waypoint, White = Path, Yellow = Target)")]
    [Tooltip("Enable patrolling between origin and waypoint")]
    [SerializeField] private bool enablePatrol = false;
    [Tooltip("Waypoint to patrol to (patrols between origin and this point) (Gizmo: Cyan cross marker)")]
    [SerializeField] private Transform patrolWaypoint;
    [Tooltip("Minimum movement speed while patrolling (randomized on spawn)")]
    [SerializeField] private float minPatrolSpeed = 2f;
    [Tooltip("Maximum movement speed while patrolling (randomized on spawn)")]
    [SerializeField] private float maxPatrolSpeed = 4f;
    [Tooltip("Minimum time to pause at each waypoint (randomized each arrival)")]
    [SerializeField] private float minPatrolPause = 0.5f;
    [Tooltip("Maximum time to pause at each waypoint (randomized each arrival)")]
    [SerializeField] private float maxPatrolPause = 2f;

    [Header("Ground Detection (Gizmo: Green/Red Sphere = Grounded Check)")]
    [Tooltip("Distance below character to check for ground (Gizmo: Green sphere when grounded, Red when airborne)")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Jump Settings")]
    [Tooltip("NoJumping = never jumps | RandomJumpDuringChase = jumps randomly only when chasing player | RandomJumpAlways = jumps randomly at all times | CollisionJump = jumps when hitting obstacles | CombinedJump = both random and collision")]
    [SerializeField] private EnemyJumpMode jumpMode = EnemyJumpMode.NoJumping;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float minJumpInterval = 2f;
    [SerializeField] private float maxJumpInterval = 5f;
    [SerializeField] private float jumpCooldown = 1f;

    [Header("Platform Settings")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private string platformTag = "movingPlatform";
    [SerializeField] private bool applyVerticalMovement = true;

    [Header("Animation")]
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private Transform enemyMesh;

    [Header("Events")]
    /// <summary>
    /// Fires when the player enters the detection range
    /// </summary>
    public UnityEvent onPlayerDetected;

    /// <summary>
    /// Fires when the player exits the detection range
    /// </summary>
    public UnityEvent onPlayerLost;

    /// <summary>
    /// Fires when the enemy starts chasing the player
    /// </summary>
    public UnityEvent onChaseStart;

    /// <summary>
    /// Fires each time movement is applied toward the player
    /// </summary>
    public UnityEvent onMoveApplied;

    /// <summary>
    /// Fires when the enemy reaches the stop distance from the player
    /// </summary>
    public UnityEvent onReachedPlayer;

    /// <summary>
    /// Fires when the enemy jumps
    /// </summary>
    public UnityEvent onJump;

    /// <summary>
    /// Fires when the enemy lands on the ground
    /// </summary>
    public UnityEvent onLanding;

    /// <summary>
    /// Fires when the enemy starts returning to origin position
    /// </summary>
    public UnityEvent onReturnStart;

    /// <summary>
    /// Fires when the enemy reaches the origin position
    /// </summary>
    public UnityEvent onReturnComplete;

    /// <summary>
    /// Fires when the enemy reaches the patrol waypoint
    /// </summary>
    public UnityEvent onReachedWaypoint;

    /// <summary>
    /// Fires when the enemy reaches the origin during patrol
    /// </summary>
    public UnityEvent onReachedOriginDuringPatrol;

    private CharacterController controller;
    private Transform playerTransform;
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool playerInRange;
    private bool isChasing;
    private bool isReturning;
    private bool isPatrolling;
    private bool isMovingToWaypoint; // True = moving to waypoint, False = moving to origin
    private float patrolPauseTimer;
    private float currentPatrolSpeed; // Randomized patrol speed
    private float currentPatrolPause; // Randomized pause duration
    private float nextMoveTime;
    private float nextJumpTime;
    private float lastJumpTime;
    private Vector3 originPosition;
    private Quaternion originRotation;

    // Platform state
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    private bool isOnPlatform;
    private int landingStabilizationFrames = 0;

    // Gravity settings
    private float gravity = -20f;
    private float terminalVelocity = -50f;

    // Collision tracking for jump
    private bool hitObstacle;

    public bool IsChasing => isChasing;
    public bool IsReturning => isReturning;
    public bool IsPatrolling => isPatrolling;
    public bool PlayerInRange => playerInRange;
    public float DistanceToPlayer => playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;
    public float DistanceToOrigin => Vector3.Distance(transform.position, originPosition);
    public float DistanceToWaypoint => patrolWaypoint != null ? Vector3.Distance(transform.position, patrolWaypoint.position) : float.MaxValue;
    public bool IsOnPlatform => isOnPlatform;
    public Transform CurrentPlatform => currentPlatform;
    public Vector3 OriginPosition => originPosition;
    public Transform PatrolWaypoint => patrolWaypoint;
    public float EffectiveLeashRange => leashRange > 0f ? leashRange : detectionRange + 2f;

    private void Start()
    {
        // Store starting position
        originPosition = transform.position;
        originRotation = transform.rotation;

        SetupComponents();
        FindPlayer();
        SetNextMoveTime();
        SetNextJumpTime();

        // Randomize patrol speed and pause on spawn
        currentPatrolSpeed = Random.Range(minPatrolSpeed, maxPatrolSpeed);

        if (enemyAnimator == null && enemyMesh != null)
            enemyAnimator = enemyMesh.GetComponentInChildren<Animator>();

        // Start patrolling if enabled and waypoint is set
        if (enablePatrol && patrolWaypoint != null)
        {
            StartPatrolling();
        }
    }

    /// <summary>
    /// Automatically configures CharacterController settings for enemy control
    /// </summary>
    private void SetupComponents()
    {
        controller = GetComponent<CharacterController>();

        // Set reasonable defaults for character movement
        if (controller.height <= 1f) controller.height = 2f;
        if (controller.radius <= 0.1f) controller.radius = 0.5f;
        if (controller.skinWidth < 0.01f) controller.skinWidth = 0.08f;

        // Configure slope limit (enemies should handle most slopes)
        controller.slopeLimit = 45f;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"No GameObject found with tag '{playerTag}'. Enemy will not function properly.");
        }
    }

    private void Update()
    {
        if (controller == null || playerTransform == null) return;

        CheckGrounded();
        CheckForPlatform();
        CheckPlayerDetection();
        HandleChasing();
        HandleReturning();
        HandlePatrolling();
        HandleGravity();
        ApplyPlatformMovement();
        ApplyMovement(); // OnControllerColliderHit fires here, setting hitObstacle=true
        HandleJumping();  // MOVED AFTER ApplyMovement so it can react to collisions this frame
        HandleRotation();
        UpdateAnimations();

        // Note: hitObstacle is reset in HandleJumping after processing, not here
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

        // Count down stabilization frames
        if (landingStabilizationFrames > 0)
        {
            landingStabilizationFrames--;
        }
    }

    private void CheckForPlatform()
    {
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

    private void CheckPlayerDetection()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasPlayerInRange = playerInRange;

        // Use detection range to START chase
        if (!isChasing)
        {
            playerInRange = distanceToPlayer <= detectionRange;
        }
        else
        {
            // Use leash range to END chase (prevents flickering at boundary)
            float effectiveLeashRange = EffectiveLeashRange;
            playerInRange = distanceToPlayer <= effectiveLeashRange;
        }

        if (playerInRange && !wasPlayerInRange)
        {
            onPlayerDetected.Invoke();
            StartChasing();
        }
        else if (!playerInRange && wasPlayerInRange)
        {
            onPlayerLost.Invoke();
            StopChasing();
        }
    }

    private void StartChasing()
    {
        if (!isChasing)
        {
            isChasing = true;
            onChaseStart.Invoke();
            SetNextMoveTime();
        }
    }

    /// <summary>
    /// Stops the enemy from chasing the player
    /// </summary>
    private void StopChasing()
    {
        isChasing = false;

        // Resume patrolling if enabled, otherwise return to origin
        if (enablePatrol && patrolWaypoint != null)
        {
            // Return to origin first, then resume patrol
            isReturning = true;
            onReturnStart?.Invoke();
        }
        else if (returnToOrigin)
        {
            isReturning = true;
            onReturnStart?.Invoke();
        }
    }

    private void StartPatrolling()
    {
        isPatrolling = true;
        isMovingToWaypoint = true;  // Start by moving to waypoint
        patrolPauseTimer = 0f;
    }

    private void StopPatrolling()
    {
        isPatrolling = false;
    }

    private void HandleChasing()
    {
        if (!isChasing || !playerInRange) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (pauseWhenPlayerClose && distanceToPlayer <= stopDistance)
        {
            onReachedPlayer.Invoke();
            // Stop horizontal movement when close
            velocity.x = 0f;
            velocity.z = 0f;
            return;
        }

        if (Time.time >= nextMoveTime)
        {
            ApplyChaseMovement();
            SetNextMoveTime();
        }
    }

    private void HandleReturning()
    {
        if (!isReturning) return;

        // If player re-enters detection range, stop returning and start chasing again
        if (playerInRange && Vector3.Distance(transform.position, playerTransform.position) <= detectionRange)
        {
            isReturning = false;
            StartChasing();
            return;
        }

        float distanceToOrigin = Vector3.Distance(transform.position, originPosition);

        // Check if we've reached origin (within small threshold)
        if (distanceToOrigin < 0.5f)
        {
            // Stop returning
            isReturning = false;

            // Zero out horizontal velocity
            velocity.x = 0f;
            velocity.z = 0f;

            // Snap to origin position and rotation
            transform.position = new Vector3(originPosition.x, transform.position.y, originPosition.z);
            transform.rotation = originRotation;

            // Fire return complete event
            onReturnComplete?.Invoke();

            // Resume patrol if enabled
            if (enablePatrol && patrolWaypoint != null)
            {
                StartPatrolling();
            }

            return;
        }

        // Move toward origin
        if (isGrounded || enableAirControl)
        {
            Vector3 directionToOrigin = (originPosition - transform.position).normalized;
            directionToOrigin.y = 0f;

            // Apply return speed
            Vector3 targetVelocity = directionToOrigin * returnSpeed;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
        }
    }

    private void HandlePatrolling()
    {
        if (!isPatrolling || patrolWaypoint == null) return;

        // If player re-enters detection range, stop patrolling and start chasing
        if (playerInRange && Vector3.Distance(transform.position, playerTransform.position) <= detectionRange)
        {
            StopPatrolling();
            StartChasing();
            return;
        }

        // Handle pause timer
        if (patrolPauseTimer > 0f)
        {
            patrolPauseTimer -= Time.deltaTime;

            // Stop movement while pausing
            velocity.x = 0f;
            velocity.z = 0f;

            // Debug: Log when pausing
            // Debug.Log($"Patrol pausing: {patrolPauseTimer:F2}s remaining");
            return;
        }

        // Determine target position
        Vector3 targetPosition = isMovingToWaypoint ? patrolWaypoint.position : originPosition;

        // Use horizontal (XZ) distance only, ignore Y-axis height differences
        Vector3 horizontalPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 horizontalTarget = new Vector3(targetPosition.x, 0f, targetPosition.z);
        float distanceToTarget = Vector3.Distance(horizontalPosition, horizontalTarget);

        // Debug: Log patrol state
        // Debug.Log($"Patrolling to {(isMovingToWaypoint ? "Waypoint" : "Origin")}, horizontal distance: {distanceToTarget:F2}");

        // Check if we've reached the current target (horizontal distance only)
        if (distanceToTarget < 0.5f)
        {
            // Snap to target position
            transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);

            // Zero out horizontal velocity
            velocity.x = 0f;
            velocity.z = 0f;

            // Fire appropriate event
            if (isMovingToWaypoint)
            {
                onReachedWaypoint?.Invoke();
                // Debug.Log("Reached waypoint, switching to origin");
            }
            else
            {
                onReachedOriginDuringPatrol?.Invoke();
                // Debug.Log("Reached origin, switching to waypoint");
            }

            // Start pause timer with randomized duration
            currentPatrolPause = Random.Range(minPatrolPause, maxPatrolPause);
            patrolPauseTimer = currentPatrolPause;

            // Switch direction BEFORE return
            isMovingToWaypoint = !isMovingToWaypoint;

            // Debug: Log direction switch
            // Debug.Log($"Switched direction, now moving to: {(isMovingToWaypoint ? "Waypoint" : "Origin")}");
            return;
        }

        // Move toward target (only if grounded or air control enabled)
        if (isGrounded || enableAirControl)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0f;

            // Apply randomized patrol speed
            Vector3 targetVelocity = directionToTarget * currentPatrolSpeed;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;

            // Debug.Log($"Moving toward target - isGrounded: {isGrounded}, velocity: ({velocity.x:F2}, {velocity.z:F2})");
        }
        else
        {
            // Debug: Log if not moving due to grounded check
            // Debug.Log($"NOT MOVING - isGrounded: {isGrounded}, enableAirControl: {enableAirControl}");
        }
    }

    private void ApplyChaseMovement()
    {
        if (!isGrounded && !enableAirControl) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f;

        // Apply speed with max velocity cap
        Vector3 targetVelocity = directionToPlayer * moveSpeed;
        float currentHorizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (currentHorizontalSpeed < maxVelocity)
        {
            targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
            onMoveApplied.Invoke();
        }
    }

    private void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;

        if (isChasing && playerTransform != null)
        {
            // Look at player when chasing
            targetDirection = (playerTransform.position - transform.position).normalized;
        }
        else if (isReturning)
        {
            // Look toward origin when returning
            targetDirection = (originPosition - transform.position).normalized;
        }
        else if (isPatrolling && patrolWaypoint != null && patrolPauseTimer <= 0f)
        {
            // Look toward current patrol target
            Vector3 patrolTarget = isMovingToWaypoint ? patrolWaypoint.position : originPosition;
            targetDirection = (patrolTarget - transform.position).normalized;
        }

        targetDirection.y = 0f;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJumping()
    {
        if (jumpMode == EnemyJumpMode.NoJumping)
        {
            return;
        }

        if (!isGrounded)
        {
            // Debug.Log("HandleJumping: Not grounded, skipping");
            return;
        }

        if (Time.time < lastJumpTime + jumpCooldown)
        {
            // Debug.Log($"HandleJumping: Still on cooldown, {(lastJumpTime + jumpCooldown - Time.time):F2}s remaining");
            return;
        }

        bool shouldJump = false;

        // Mode 1: Random Jump During Chase (only when chasing player)
        if (jumpMode == EnemyJumpMode.RandomJumpDuringChase && isChasing)
        {
            if (Time.time >= nextJumpTime)
            {
                shouldJump = true;
                SetNextJumpTime();
                // Debug.Log("HandleJumping: Random jump during chase triggered");
            }
        }

        // Mode 2: Random Jump Always (jumps at all times - patrol, chase, return, idle)
        if (jumpMode == EnemyJumpMode.RandomJumpAlways)
        {
            if (Time.time >= nextJumpTime)
            {
                shouldJump = true;
                SetNextJumpTime();
                // Debug.Log("HandleJumping: Random jump always triggered");
            }
        }

        // Mode 3: Collision Jump (detected via OnControllerColliderHit)
        if ((jumpMode == EnemyJumpMode.CollisionJump || jumpMode == EnemyJumpMode.CombinedJump) && hitObstacle)
        {
            shouldJump = true;
            hitObstacle = false; // Reset flag after processing
            // Debug.Log("HandleJumping: Collision jump triggered (hitObstacle was true)");
        }

        if (shouldJump)
        {
            PerformJump();
        }
    }

    private void PerformJump()
    {
        if (isGrounded)
        {
            velocity.y = jumpForce;
            lastJumpTime = Time.time;
            hitObstacle = false; // Ensure flag is cleared
            // Debug.Log($"Enemy jumped! Jump force: {jumpForce}, isGrounded: {isGrounded}");
            onJump.Invoke();
        }
        // else
        // {
        //     Debug.LogWarning("Jump attempted but enemy is not grounded!");
        // }
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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Detect obstacle collisions for jump mode
        if ((jumpMode == EnemyJumpMode.CollisionJump || jumpMode == EnemyJumpMode.CombinedJump) &&
            isGrounded &&
            Time.time >= lastJumpTime + jumpCooldown)
        {
            // Check if we hit something that's not the player
            // Skip the Ground tag check since it's often not defined
            if (!hit.gameObject.CompareTag(playerTag))
            {
                // Check if collision is roughly horizontal (hitting a wall/obstacle)
                // This filters out ground collisions which have y-normal close to 1.0
                Vector3 collisionNormal = hit.normal;
                if (Mathf.Abs(collisionNormal.y) < 0.5f) // Not hitting ground or ceiling
                {
                    hitObstacle = true;
                    // Debug.Log($"Obstacle detected: {hit.gameObject.name}, normal: {collisionNormal}, setting hitObstacle=true");
                }
            }
        }
    }

    private void UpdateAnimations()
    {
        if (enemyAnimator != null)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontalVelocity.magnitude;

            enemyAnimator.SetFloat("Speed", speed);
            enemyAnimator.SetBool("IsGrounded", isGrounded);
            enemyAnimator.SetBool("IsChasing", isChasing);
            enemyAnimator.SetBool("IsReturning", isReturning);
            enemyAnimator.SetBool("IsPatrolling", isPatrolling);
            enemyAnimator.SetBool("PlayerInRange", playerInRange);
            enemyAnimator.SetFloat("DistanceToPlayer", DistanceToPlayer);
            enemyAnimator.SetFloat("DistanceToOrigin", DistanceToOrigin);
            enemyAnimator.SetFloat("DistanceToWaypoint", DistanceToWaypoint);
            enemyAnimator.SetFloat("VerticalVelocity", velocity.y);

            bool isMoving = speed > 0.1f && isGrounded;
            enemyAnimator.SetBool("IsMoving", isMoving);
        }
    }

    private void SetNextMoveTime()
    {
        float randomInterval = Random.Range(minMoveInterval, maxMoveInterval);
        nextMoveTime = Time.time + randomInterval;
    }

    private void SetNextJumpTime()
    {
        float randomInterval = Random.Range(minJumpInterval, maxJumpInterval);
        nextJumpTime = Time.time + randomInterval;
    }

    /// <summary>
    /// Changes the speed of movement toward the player
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    /// <summary>
    /// Changes the maximum speed the enemy can reach
    /// </summary>
    public void SetMaxVelocity(float newMax)
    {
        maxVelocity = newMax;
    }

    /// <summary>
    /// Changes how far the enemy can detect the player
    /// </summary>
    public void SetDetectionRange(float newRange)
    {
        detectionRange = newRange;
    }

    /// <summary>
    /// Sets the minimum and maximum time between movement applications
    /// </summary>
    public void SetMoveInterval(float minInterval, float maxInterval)
    {
        minMoveInterval = minInterval;
        maxMoveInterval = maxInterval;
    }

    /// <summary>
    /// Enables or disables the ability to control movement while in the air
    /// </summary>
    public void EnableAirControl(bool enable)
    {
        enableAirControl = enable;
    }

    /// <summary>
    /// Stops the enemy from chasing the player
    /// </summary>
    public void PauseChasing()
    {
        StopChasing();
    }

    /// <summary>
    /// Resumes chasing if the player is in range
    /// </summary>
    public void ResumeChasing()
    {
        if (playerInRange)
        {
            StartChasing();
        }
    }

    /// <summary>
    /// Sets the leash range at runtime (distance before giving up chase)
    /// </summary>
    public void SetLeashRange(float range)
    {
        leashRange = Mathf.Max(0f, range);
    }

    /// <summary>
    /// Sets whether the enemy should return to origin after chase ends
    /// </summary>
    public void SetReturnToOrigin(bool shouldReturn)
    {
        returnToOrigin = shouldReturn;
    }

    /// <summary>
    /// Sets the speed at which the enemy returns to origin
    /// </summary>
    public void SetReturnSpeed(float speed)
    {
        returnSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// Manually sets a new origin position for the enemy
    /// </summary>
    public void SetOriginPosition(Vector3 newOrigin)
    {
        originPosition = newOrigin;
    }

    /// <summary>
    /// Forces the enemy to immediately return to origin
    /// </summary>
    public void ForceReturnToOrigin()
    {
        isChasing = false;
        isReturning = true;
    }

    /// <summary>
    /// Enables or disables patrol behavior at runtime
    /// </summary>
    public void SetPatrolEnabled(bool enabled)
    {
        enablePatrol = enabled;

        if (enabled && patrolWaypoint != null && !isChasing && !isReturning)
        {
            StartPatrolling();
        }
        else if (!enabled && isPatrolling)
        {
            StopPatrolling();
        }
    }

    /// <summary>
    /// Sets the patrol waypoint at runtime
    /// </summary>
    public void SetPatrolWaypoint(Transform waypoint)
    {
        patrolWaypoint = waypoint;
    }

    /// <summary>
    /// Sets the patrol speed range at runtime (min and max)
    /// </summary>
    public void SetPatrolSpeedRange(float min, float max)
    {
        minPatrolSpeed = Mathf.Max(0f, min);
        maxPatrolSpeed = Mathf.Max(minPatrolSpeed, max);
        // Re-randomize current speed
        currentPatrolSpeed = Random.Range(minPatrolSpeed, maxPatrolSpeed);
    }

    /// <summary>
    /// Sets the patrol pause range at runtime (min and max)
    /// </summary>
    public void SetPatrolPauseRange(float min, float max)
    {
        minPatrolPause = Mathf.Max(0f, min);
        maxPatrolPause = Mathf.Max(minPatrolPause, max);
    }

    /// <summary>
    /// Forces the enemy to immediately start patrolling
    /// </summary>
    public void ForceStartPatrol()
    {
        if (patrolWaypoint == null)
        {
            Debug.LogWarning($"EnemyControllerCC on {gameObject.name}: Cannot start patrol without a waypoint!", this);
            return;
        }

        isChasing = false;
        isReturning = false;
        StartPatrolling();
    }

    private void OnDrawGizmosSelected()
    {
        // GIZMO COLOR LEGEND:
        // Yellow/Red Sphere = Detection Range (Yellow = idle, Red = player detected)
        // Orange Sphere = Leash Range (max chase distance)
        // Blue Sphere = Stop Distance (how close to get to player)
        // Magenta Cross = Origin Position (starting point)
        // Green Line = Return Path (when returning to origin)
        // Cyan Cross = Patrol Waypoint
        // White Line = Patrol Path (between origin and waypoint)
        // Yellow Line = Current Patrol Target (active direction)
        // Green/Red Sphere (bottom) = Ground Check (Green = grounded, Red = airborne)
        // Cyan Line = Line to Player (when in detection range)

        // Detection range (yellow/red when player in range)
        Gizmos.color = playerInRange ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Leash range (orange - different from detection)
        float effectiveLeashRange = EffectiveLeashRange;
        if (effectiveLeashRange > detectionRange)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange, semi-transparent
            Gizmos.DrawWireSphere(transform.position, effectiveLeashRange);
        }

        // Stop distance
        if (pauseWhenPlayerClose)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }

        // Origin position marker (magenta cross)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            float crossSize = 0.5f;
            Gizmos.DrawLine(originPosition + Vector3.left * crossSize, originPosition + Vector3.right * crossSize);
            Gizmos.DrawLine(originPosition + Vector3.forward * crossSize, originPosition + Vector3.back * crossSize);
            Gizmos.DrawLine(originPosition, originPosition + Vector3.up * 2f); // Vertical line

            // Line to origin when returning
            if (isReturning)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, originPosition);
            }
        }

        // Patrol waypoint visualization
        if (enablePatrol && patrolWaypoint != null)
        {
            // Waypoint marker (cyan cross)
            Gizmos.color = Color.cyan;
            float waypointCrossSize = 0.5f;
            Vector3 waypointPos = patrolWaypoint.position;
            Gizmos.DrawLine(waypointPos + Vector3.left * waypointCrossSize, waypointPos + Vector3.right * waypointCrossSize);
            Gizmos.DrawLine(waypointPos + Vector3.forward * waypointCrossSize, waypointPos + Vector3.back * waypointCrossSize);
            Gizmos.DrawLine(waypointPos, waypointPos + Vector3.up * 2f);

            // Patrol path line (white dashed-looking line from origin to waypoint)
            if (Application.isPlaying)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(originPosition, waypointPos);

                // Draw current target line when patrolling
                if (isPatrolling)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 currentTarget = isMovingToWaypoint ? waypointPos : originPosition;
                    Gizmos.DrawLine(transform.position, currentTarget);
                }
            }
            else
            {
                // In edit mode, show the patrol path
                Gizmos.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent white
                Vector3 editOrigin = transform.position; // Use current position as origin in edit mode
                Gizmos.DrawLine(editOrigin, waypointPos);
            }
        }

        // Ground check visualization (Green = grounded, Red = airborne)
        CharacterController cc = controller ?? GetComponent<CharacterController>();
        if (cc != null)
        {
            // Calculate ground check position
            Vector3 checkPosition = transform.position - new Vector3(0f, cc.height * 0.5f - cc.radius, 0f);
            float checkRadius = cc.radius + groundCheckDistance;

            // Color based on grounded state (in play mode) or yellow in edit mode
            if (Application.isPlaying)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
            }
            else
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow semi-transparent in edit mode
            }

            // Draw the ground check sphere
            Gizmos.DrawWireSphere(checkPosition, checkRadius);

            // Draw a line down to show the check direction
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawLine(transform.position, checkPosition);
        }

        // Line to player
        if (playerTransform != null && playerInRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        // Platform detection ray
        if (controller != null)
        {
            Gizmos.color = isOnPlatform ? Color.green : Color.yellow;
            Vector3 rayStart = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
            Gizmos.DrawRay(rayStart, Vector3.down * (controller.radius + groundCheckDistance + 0.1f));
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
