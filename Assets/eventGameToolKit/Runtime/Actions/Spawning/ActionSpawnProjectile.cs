using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

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

    [Header("Cooldown")]
    [Tooltip("Minimum time between spawns in seconds (0 = no cooldown)")]
    [SerializeField] private float cooldownTime = 0f;

    private float lastSpawnTime = -Mathf.Infinity;

    [Header("Multi-Shot")]
    [Tooltip("Number of projectiles to spawn simultaneously (1 = single shot, higher = shotgun/burst)")]
    [SerializeField] private int projectileCount = 1;

    [Header("Accuracy")]
    [Tooltip("Random angle deviation in degrees (0 = perfect accuracy, higher = more spread)")]
    [SerializeField] private float spreadAngle = 0f;

    [Header("Spawn Animation (Optional)")]
    [Tooltip("Animate projectile scale on spawn using DOTween")]
    [SerializeField] private bool animateSpawn = false;

    [Tooltip("Duration of spawn scale animation in seconds")]
    [SerializeField] private float spawnScaleDuration = 0.2f;

    [Tooltip("Starting scale multiplier (animates to 1.0)")]
    [SerializeField] private float spawnStartScale = 0.1f;

    [Header("Events")]
    /// <summary>
    /// Fires when a projectile is spawned, passing the spawned GameObject as a parameter
    /// </summary>
    public UnityEvent<GameObject> onProjectileSpawned;

    /// <summary>
    /// Fires when spawn is attempted but is on cooldown
    /// </summary>
    public UnityEvent onCooldownActive;

    /// <summary>
    /// Spawns a projectile moving forward in this object's local Z direction
    /// </summary>
    public void SpawnProjectile()
    {
        // Check cooldown
        if (cooldownTime > 0f && Time.time < lastSpawnTime + cooldownTime)
        {
            onCooldownActive?.Invoke();
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"ActionSpawnProjectile on {gameObject.name}: No projectile prefab assigned!", this);
            return;
        }

        // Update cooldown timer
        lastSpawnTime = Time.time;

        // Spawn multiple projectiles if configured
        int count = Mathf.Max(1, projectileCount);
        for (int i = 0; i < count; i++)
        {
            SpawnSingleProjectile();
        }
    }

    /// <summary>
    /// Internal method to spawn a single projectile with spread
    /// </summary>
    private void SpawnSingleProjectile()
    {
        // Calculate spawn position with offset in local space
        Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnOffset);

        // Spawn projectile with spawner's rotation
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, transform.rotation);

        // Calculate direction with spread
        Vector3 direction = transform.forward;
        if (spreadAngle > 0f)
        {
            // Apply random spread in a cone
            Quaternion spread = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f
            );
            direction = spread * direction;
        }

        // Add velocity component to make it move forward
        ProjectileVelocity velocity = projectile.GetComponent<ProjectileVelocity>();
        if (velocity == null)
        {
            velocity = projectile.AddComponent<ProjectileVelocity>();
        }

        // Set the direction (with spread) and speed
        velocity.Initialize(direction, projectileSpeed);

        // Apply DOTween spawn animation if enabled
        if (animateSpawn)
        {
            Vector3 originalScale = projectile.transform.localScale;
            projectile.transform.localScale = originalScale * spawnStartScale;
            projectile.transform.DOScale(originalScale, spawnScaleDuration).SetEase(Ease.OutBack);
        }

        // Set up auto-destroy if lifetime is configured
        if (projectileLifetime > 0f)
        {
            Destroy(projectile, projectileLifetime);
        }

        // Fire event with spawned projectile reference
        onProjectileSpawned?.Invoke(projectile);
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

    /// <summary>
    /// Sets the cooldown time at runtime
    /// </summary>
    public void SetCooldownTime(float cooldown)
    {
        cooldownTime = Mathf.Max(0f, cooldown);
    }

    /// <summary>
    /// Sets the spread angle at runtime (controls accuracy)
    /// </summary>
    public void SetSpreadAngle(float angle)
    {
        spreadAngle = Mathf.Max(0f, angle);
    }

    /// <summary>
    /// Sets the number of projectiles to spawn simultaneously
    /// </summary>
    public void SetProjectileCount(int count)
    {
        projectileCount = Mathf.Max(1, count);
    }

    /// <summary>
    /// Resets the cooldown timer, allowing immediate spawning
    /// </summary>
    public void ResetCooldown()
    {
        lastSpawnTime = -Mathf.Infinity;
    }

    /// <summary>
    /// Checks if the spawner is currently on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return cooldownTime > 0f && Time.time < lastSpawnTime + cooldownTime;
    }

    /// <summary>
    /// Gets the remaining cooldown time in seconds (0 if ready to fire)
    /// </summary>
    public float GetRemainingCooldown()
    {
        if (cooldownTime <= 0f) return 0f;
        float remaining = (lastSpawnTime + cooldownTime) - Time.time;
        return Mathf.Max(0f, remaining);
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
    private bool isInitialized = false;

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
        isInitialized = true;
        velocity = direction.normalized * speed;

        if (usePhysics && rb != null)
        {
            // Set Rigidbody velocity directly for physics-based movement
            rb.linearVelocity = velocity;
        }
    }

    void Update()
    {
        // Safety check: warn if not initialized
        if (!isInitialized)
        {
            Debug.LogWarning($"ProjectileVelocity on {gameObject.name} was not initialized! Call Initialize() first or use ActionSpawnProjectile to spawn.", this);
            return;
        }

        // Only use transform-based movement if no Rigidbody
        if (!usePhysics)
        {
            transform.position += velocity * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // Safety check: only update if initialized
        if (!isInitialized) return;

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
        if (!isInitialized)
        {
            Debug.LogWarning($"ProjectileVelocity on {gameObject.name}: Cannot set velocity before initialization!", this);
            return;
        }

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

    /// <summary>
    /// Checks if the projectile has been properly initialized
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
}
