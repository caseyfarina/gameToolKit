using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns a projectile that moves forward in the spawner's local Z direction at a constant speed.
/// Designed to be placed on a child GameObject of the player or any rotating object.
/// Common use: Bullet spawners, magic projectiles, arrows, rockets, or any forward-firing projectile system.
/// </summary>
public class ActionSpawnProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab to spawn (will move forward automatically)")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Speed at which projectile moves forward (units per second)")]
    [SerializeField] private float projectileSpeed = 10f;

    [Tooltip("Lifetime of projectile in seconds (0 = never destroy)")]
    [SerializeField] private float projectileLifetime = 5f;

    [Tooltip("Offset from spawner position (useful for spawning ahead of player)")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Events")]
    /// <summary>
    /// Fires when a projectile is spawned
    /// </summary>
    public UnityEvent onProjectileSpawned;

    /// <summary>
    /// Spawns a projectile moving forward in this object's local Z direction
    /// </summary>
    public void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"ActionSpawnProjectile on {gameObject.name}: No projectile prefab assigned!", this);
            return;
        }

        // Calculate spawn position with offset in local space
        Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnOffset);

        // Spawn projectile with spawner's rotation
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, transform.rotation);

        // Add velocity component to make it move forward
        ProjectileVelocity velocity = projectile.GetComponent<ProjectileVelocity>();
        if (velocity == null)
        {
            velocity = projectile.AddComponent<ProjectileVelocity>();
        }

        // Set the forward direction and speed
        velocity.Initialize(transform.forward, projectileSpeed);

        // Set up auto-destroy if lifetime is configured
        if (projectileLifetime > 0f)
        {
            Destroy(projectile, projectileLifetime);
        }

        // Fire event
        onProjectileSpawned?.Invoke();
    }

    /// <summary>
    /// Sets the projectile speed at runtime
    /// </summary>
    public void SetProjectileSpeed(float speed)
    {
        projectileSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// Sets the projectile lifetime at runtime
    /// </summary>
    public void SetProjectileLifetime(float lifetime)
    {
        projectileLifetime = Mathf.Max(0f, lifetime);
    }

    // Gizmo to show spawn point and direction
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // Show spawn position with offset
        Vector3 spawnPos = transform.position + transform.TransformDirection(spawnOffset);
        Gizmos.DrawWireSphere(spawnPos, 0.2f);

        // Show forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(spawnPos, transform.forward * 2f);
    }
}

/// <summary>
/// Simple component that moves an object forward at constant velocity.
/// Automatically added to spawned projectiles by ActionSpawnProjectile.
/// Works with or without Rigidbody components.
/// </summary>
public class ProjectileVelocity : MonoBehaviour
{
    private Vector3 velocity;
    private Rigidbody rb;
    private bool usePhysics;

    void Awake()
    {
        // Check if this projectile has a Rigidbody
        rb = GetComponent<Rigidbody>();
        usePhysics = rb != null;

        if (usePhysics)
        {
            // Configure Rigidbody for projectile behavior
            rb.useGravity = false;  // Projectiles typically don't use gravity
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;  // Prevents tunneling through objects
        }
    }

    /// <summary>
    /// Initializes the projectile with a direction and speed
    /// </summary>
    public void Initialize(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;

        if (usePhysics && rb != null)
        {
            // Set Rigidbody velocity directly for physics-based movement
            rb.linearVelocity = velocity;
        }
    }

    void Update()
    {
        // Only use transform-based movement if no Rigidbody
        if (!usePhysics)
        {
            transform.position += velocity * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // Maintain constant velocity for physics-based projectiles
        // (prevents slowdown from drag or collisions)
        if (usePhysics && rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// Sets a new velocity direction and speed
    /// </summary>
    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;

        if (usePhysics && rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// Gets the current velocity
    /// </summary>
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    /// <summary>
    /// Enables or disables gravity for physics-based projectiles
    /// </summary>
    public void SetUseGravity(bool useGravity)
    {
        if (usePhysics && rb != null)
        {
            rb.useGravity = useGravity;
        }
    }
}
