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
/// AI enemy that chases the player using physics forces with configurable jump behaviors.
/// Common use: Patrolling enemies, chase sequences, monster AI, or competitive racing opponents.
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

    [Header("Animation")]
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private Transform enemyMesh;

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

    public bool IsChasing => isChasing;
    public bool PlayerInRange => playerInRange;
    public float DistanceToPlayer => playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;

    private void Start()
    {
        SetupComponents();
        FindPlayer();
        SetNextForceTime();
        SetNextJumpTime();

        if (enemyAnimator == null && enemyMesh != null)
            enemyAnimator = enemyMesh.GetComponentInChildren<Animator>();
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

        if (rb.linearVelocity.magnitude < maxVelocity)
        {
            rb.AddForce(directionToPlayer * moveForce, ForceMode.Force);
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

            enemyAnimator.SetFloat("Speed", speed);
            enemyAnimator.SetBool("IsGrounded", isGrounded);
            enemyAnimator.SetBool("IsChasing", isChasing);
            enemyAnimator.SetBool("PlayerInRange", playerInRange);
            enemyAnimator.SetFloat("DistanceToPlayer", DistanceToPlayer);
            enemyAnimator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

            bool isMoving = speed > 0.1f && isGrounded;
            enemyAnimator.SetBool("IsMoving", isMoving);
        }
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