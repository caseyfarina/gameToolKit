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

    [Header("Player Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer = -1;

    [Header("Chase Behavior")]
    [SerializeField] private float minMoveInterval = 1f;
    [SerializeField] private float maxMoveInterval = 3f;
    [SerializeField] private bool pauseWhenPlayerClose = true;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Jump Settings")]
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

    private CharacterController controller;
    private Transform playerTransform;
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool playerInRange;
    private bool isChasing;
    private float nextMoveTime;
    private float nextJumpTime;
    private float lastJumpTime;

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
    public bool PlayerInRange => playerInRange;
    public float DistanceToPlayer => playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;
    public bool IsOnPlatform => isOnPlatform;
    public Transform CurrentPlatform => currentPlatform;

    private void Start()
    {
        SetupComponents();
        FindPlayer();
        SetNextMoveTime();
        SetNextJumpTime();

        if (enemyAnimator == null && enemyMesh != null)
            enemyAnimator = enemyMesh.GetComponentInChildren<Animator>();
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
        HandleJumping();
        HandleGravity();
        ApplyPlatformMovement();
        ApplyMovement();
        HandleRotation();
        UpdateAnimations();

        // Reset obstacle hit flag each frame
        hitObstacle = false;
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

        playerInRange = distanceToPlayer <= detectionRange;

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
        if (playerTransform != null && isChasing)
        {
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0f;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleJumping()
    {
        if (jumpMode == EnemyJumpMode.NoJumping || !isGrounded) return;
        if (Time.time < lastJumpTime + jumpCooldown) return;

        bool shouldJump = false;

        // Mode 1: Random Interval Jump (only during chase)
        if ((jumpMode == EnemyJumpMode.RandomIntervalJump || jumpMode == EnemyJumpMode.CombinedJump) && isChasing)
        {
            if (Time.time >= nextJumpTime)
            {
                shouldJump = true;
                SetNextJumpTime();
            }
        }

        // Mode 2: Collision Jump (detected via OnControllerColliderHit)
        if ((jumpMode == EnemyJumpMode.CollisionJump || jumpMode == EnemyJumpMode.CombinedJump) && hitObstacle)
        {
            shouldJump = true;
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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Detect obstacle collisions for jump mode
        if ((jumpMode == EnemyJumpMode.CollisionJump || jumpMode == EnemyJumpMode.CombinedJump) &&
            isGrounded &&
            Time.time >= lastJumpTime + jumpCooldown)
        {
            // Check if we hit something that's not the player or ground
            if (!hit.gameObject.CompareTag(playerTag) &&
                !hit.gameObject.CompareTag("Ground"))
            {
                // Check if collision is roughly horizontal (hitting a wall/obstacle)
                Vector3 collisionNormal = hit.normal;
                if (Mathf.Abs(collisionNormal.y) < 0.5f) // Not hitting ground or ceiling
                {
                    hitObstacle = true;
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
            enemyAnimator.SetBool("PlayerInRange", playerInRange);
            enemyAnimator.SetFloat("DistanceToPlayer", DistanceToPlayer);
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

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = playerInRange ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Stop distance
        if (pauseWhenPlayerClose)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }

        // Ground check visualization
        if (controller != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 checkPosition = transform.position - new Vector3(0f, controller.height * 0.5f - controller.radius, 0f);
            Gizmos.DrawWireSphere(checkPosition, controller.radius + groundCheckDistance);
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
