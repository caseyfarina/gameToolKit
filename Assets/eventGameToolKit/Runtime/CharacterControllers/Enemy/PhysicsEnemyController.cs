using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum EnemyJumpMode
{
    NoJumping = 0,
    RandomIntervalJump = 1,
    CollisionJump = 2,
    CombinedJump = 3
}

/// <summary>
/// AI enemy that chases the player using physics forces with configurable jump behaviors, sprint mechanics, and robust animation support.
/// Features: Player detection, chase AI, optional sprint when close, multiple jump modes, idle animations, and grounded state tracking.
/// Common use: Patrolling enemies, chase sequences, monster AI, competitive racing opponents, or boss encounters.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PhysicsEnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 200f;
    [SerializeField] private float maxVelocity = 15f;
    [SerializeField] private bool enableAirControl = false;

    [Header("Player Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer = -1;

    [Header("Chase Behavior")]
    [SerializeField] private float minForceInterval = 1f;
    [SerializeField] private float maxForceInterval = 3f;
    [SerializeField] private bool pauseWhenPlayerClose = true;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Jump Settings")]
    [SerializeField] private EnemyJumpMode jumpMode = EnemyJumpMode.NoJumping;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float minJumpInterval = 2f;
    [SerializeField] private float maxJumpInterval = 5f;
    [SerializeField] private float jumpCooldown = 1f;

    [Header("Sprint Settings (Optional - AI only)")]
    [Tooltip("Enable sprint when chasing player (enemy runs faster when close)")]
    [SerializeField] private bool enableSprint = false;
    [Tooltip("Distance from player at which enemy starts sprinting")]
    [SerializeField] private float sprintActivationDistance = 5f;
    [Tooltip("Speed multiplier when sprinting (2.0 = twice as fast)")]
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private Transform enemyMesh;
    [Header("⚠️ OPTIONAL: For idle animations after being idle")]
    [Space(-20)]
    [Header("Set to 0 to disable. Animator can use 'IdleTime' float parameter.")]
    [Tooltip("Time in seconds before IdleTime parameter starts counting. 0 = disabled. Useful for fidget/emote animations.")]
    [SerializeField] private float idleTimeBeforeEmote = 0f;

    [Header("Events")]
    public UnityEvent onPlayerDetected;
    public UnityEvent onPlayerLost;
    public UnityEvent onChaseStart;
    public UnityEvent onForceApplied;
    public UnityEvent onReachedPlayer;
    public UnityEvent onJump;
    public UnityEvent onLanding;

    private Rigidbody rb;
    private Transform playerTransform;
    private bool isGrounded;
    private bool wasGrounded;
    private bool playerInRange;
    private bool isChasing;
    private float nextForceTime;
    private float nextJumpTime;
    private float lastJumpTime;
    private Coroutine chaseCoroutine;
    private bool isSprinting;

    // Animation IDs (StringToHash optimization)
    private int _animIDSpeed;
    private int _animIDIsGrounded;
    private int _animIDIsChasing;
    private int _animIDPlayerInRange;
    private int _animIDDistanceToPlayer;
    private int _animIDVerticalVelocity;
    private int _animIDIsMoving;
    private int _animIDIdleTime;

    // Animation state tracking (prevents unnecessary updates)
    private bool _lastAnimatorGroundedState;

    // Idle time tracking for emote/fidget animations
    private float currentIdleTime;

    public bool IsChasing => isChasing;
    public bool PlayerInRange => playerInRange;
    public bool IsSprinting => isSprinting && enableSprint;
    public float DistanceToPlayer => playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;

    private void Start()
    {
        SetupComponents();
        FindPlayer();
        SetNextForceTime();
        SetNextJumpTime();

        if (enemyAnimator == null && enemyMesh != null)
            enemyAnimator = enemyMesh.GetComponentInChildren<Animator>();

        // Initialize animation IDs for StringToHash optimization
        if (enemyAnimator != null)
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDIsGrounded = Animator.StringToHash("IsGrounded");
            _animIDIsChasing = Animator.StringToHash("IsChasing");
            _animIDPlayerInRange = Animator.StringToHash("PlayerInRange");
            _animIDDistanceToPlayer = Animator.StringToHash("DistanceToPlayer");
            _animIDVerticalVelocity = Animator.StringToHash("VerticalVelocity");
            _animIDIsMoving = Animator.StringToHash("IsMoving");
            _animIDIdleTime = Animator.StringToHash("IdleTime");
        }
    }

    private void SetupComponents()
    {
        // Get required components (guaranteed by RequireComponent)
        rb = GetComponent<Rigidbody>();
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        // Configure Rigidbody for stable upright movement
        rb.freezeRotation = false; // Allow Y rotation for turning
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Set reasonable defaults for character movement
        if (rb.mass < 0.1f) rb.mass = 1f; // Ensure reasonable mass
        if (rb.linearDamping < 0.1f) rb.linearDamping = 1f; // Add some drag for control
        rb.angularDamping = 5f; // Prevent spinning

        // Configure Capsule Collider for character (only if not already configured)
        if (capsule.height <= 1f) capsule.height = 2f;
        if (capsule.radius <= 0.1f) capsule.radius = 0.5f;
        // Don't modify center - let user position it to match their mesh
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

    private void FixedUpdate()
    {
        if (rb == null || playerTransform == null) return;

        CheckGrounded();
        CheckPlayerDetection();
        HandleChasing();
        HandleJumping();
        HandleRotation();
        UpdateAnimations();
    }

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;

        // For capsule: check at bottom of collider, not just slightly below transform
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        float capsuleBottom = transform.position.y - (capsule.height * 0.5f) + capsule.center.y;
        Vector3 checkPosition = new Vector3(transform.position.x, capsuleBottom - 0.1f, transform.position.z);

        isGrounded = Physics.CheckSphere(
            checkPosition,
            groundCheckRadius,
            groundLayer
        );

        // Check for landing
        if (isGrounded && !wasGrounded)
        {
            onLanding.Invoke();
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
            SetNextForceTime();
        }
    }

    private void StopChasing()
    {
        isChasing = false;
        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
    }

    private void HandleChasing()
    {
        if (!isChasing || !playerInRange) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (pauseWhenPlayerClose && distanceToPlayer <= stopDistance)
        {
            onReachedPlayer.Invoke();
            return;
        }

        if (Time.fixedTime >= nextForceTime)
        {
            ApplyChaseForce();
            SetNextForceTime();
        }
    }

    private void ApplyChaseForce()
    {
        if (!isGrounded && !enableAirControl) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f;

        // Calculate sprint state based on distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        isSprinting = enableSprint && isGrounded && distanceToPlayer <= sprintActivationDistance;

        // Apply sprint multiplier to force and max velocity
        float effectiveMoveForce = moveForce;
        float effectiveMaxVelocity = maxVelocity;

        if (isSprinting)
        {
            effectiveMoveForce *= sprintSpeedMultiplier;
            effectiveMaxVelocity *= sprintSpeedMultiplier;
        }

        if (rb.linearVelocity.magnitude < effectiveMaxVelocity)
        {
            rb.AddForce(directionToPlayer * effectiveMoveForce, ForceMode.Force);
            onForceApplied.Invoke();
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
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    private void HandleJumping()
    {
        if (jumpMode == EnemyJumpMode.NoJumping || !isGrounded) return;
        if (Time.fixedTime < lastJumpTime + jumpCooldown) return;

        bool shouldJump = false;

        // Mode 1: Random Interval Jump (only during chase)
        if ((jumpMode == EnemyJumpMode.RandomIntervalJump || jumpMode == EnemyJumpMode.CombinedJump) && isChasing)
        {
            if (Time.fixedTime >= nextJumpTime)
            {
                shouldJump = true;
                SetNextJumpTime();
            }
        }

        // Mode 2: Collision Jump (handled by OnCollisionEnter)
        // Mode 3: Combined (both random and collision)

        if (shouldJump)
        {
            PerformJump();
        }
    }

    private void PerformJump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.fixedTime;
            onJump.Invoke();
        }
    }

    private void SetNextForceTime()
    {
        float randomInterval = Random.Range(minForceInterval, maxForceInterval);
        nextForceTime = Time.fixedTime + randomInterval;
    }

    private void SetNextJumpTime()
    {
        float randomInterval = Random.Range(minJumpInterval, maxJumpInterval);
        nextJumpTime = Time.fixedTime + randomInterval;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Jump when hitting obstacles (not the player)
        if ((jumpMode == EnemyJumpMode.CollisionJump || jumpMode == EnemyJumpMode.CombinedJump) &&
            isGrounded &&
            Time.fixedTime >= lastJumpTime + jumpCooldown)
        {
            // Check if we hit something that's not the player
            if (!collision.gameObject.CompareTag(playerTag) &&
                !collision.gameObject.CompareTag("Ground"))
            {
                // Check if collision is roughly horizontal (hitting a wall/obstacle)
                Vector3 collisionDirection = collision.contacts[0].normal;
                if (Mathf.Abs(collisionDirection.y) < 0.5f) // Not hitting ground or ceiling
                {
                    PerformJump();
                }
            }
        }
    }

    private void UpdateAnimations()
    {
        if (enemyAnimator != null)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;

            // Use StringToHash IDs for better performance
            // Only set parameters if they exist (prevents errors with incomplete Animator Controllers)
            if (HasParameter(_animIDSpeed))
                enemyAnimator.SetFloat(_animIDSpeed, speed);

            // CRITICAL FIX: Only update IsGrounded when it actually changes
            // Setting it every frame can retrigger transitions and prevent jump animations
            if (HasParameter(_animIDIsGrounded) && isGrounded != _lastAnimatorGroundedState)
            {
                enemyAnimator.SetBool(_animIDIsGrounded, isGrounded);
                _lastAnimatorGroundedState = isGrounded;
            }

            if (HasParameter(_animIDIsChasing))
                enemyAnimator.SetBool(_animIDIsChasing, isChasing);

            if (HasParameter(_animIDPlayerInRange))
                enemyAnimator.SetBool(_animIDPlayerInRange, playerInRange);

            if (HasParameter(_animIDDistanceToPlayer))
                enemyAnimator.SetFloat(_animIDDistanceToPlayer, DistanceToPlayer);

            if (HasParameter(_animIDVerticalVelocity))
                enemyAnimator.SetFloat(_animIDVerticalVelocity, rb.linearVelocity.y);

            if (HasParameter(_animIDIsMoving))
            {
                bool isMoving = speed > 0.1f && isGrounded;
                enemyAnimator.SetBool(_animIDIsMoving, isMoving);
            }

            // Track idle time for emote/fidget animations (if enabled)
            if (HasParameter(_animIDIdleTime) && idleTimeBeforeEmote > 0f)
            {
                // Reset idle time if moving, not grounded, or player in range
                if (speed > 0.1f || !isGrounded || playerInRange)
                {
                    currentIdleTime = 0f;
                }
                else
                {
                    // Increment idle time when truly idle (grounded, not moving, no player nearby)
                    currentIdleTime += Time.deltaTime;
                }

                enemyAnimator.SetFloat(_animIDIdleTime, currentIdleTime);
            }
        }
    }

    /// <summary>
    /// Checks if animator has a parameter with the given hash
    /// </summary>
    private bool HasParameter(int paramHash)
    {
        if (enemyAnimator == null) return false;

        foreach (AnimatorControllerParameter param in enemyAnimator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Changes the force applied when moving toward the player
    /// </summary>
    public void SetMoveForce(float newForce)
    {
        moveForce = newForce;
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
    /// Sets the minimum and maximum time between movement impulses
    /// </summary>
    public void SetForceInterval(float minInterval, float maxInterval)
    {
        minForceInterval = minInterval;
        maxForceInterval = maxInterval;
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
        Gizmos.color = playerInRange ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (pauseWhenPlayerClose)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }

        // Ground check visualization
        Gizmos.color = isGrounded ? Color.green : Color.red;
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            float capsuleBottom = transform.position.y - (capsule.height * 0.5f) + capsule.center.y;
            Vector3 checkPosition = new Vector3(transform.position.x, capsuleBottom - 0.1f, transform.position.z);
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }

        if (playerTransform != null && playerInRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}