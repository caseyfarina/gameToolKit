using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages health with damage and healing mechanics, firing events at critical thresholds.
/// Does NOT handle display - wire onHealthChanged to GameUIManager for visual updates.
///
/// MULTI-SCENE SUPPORT: Optionally assign an IntVariable asset to persist health across scene loads.
/// If no IntVariable is assigned, health is stored locally (single-scene behavior).
///
/// Common use: Player or enemy health systems, destructible objects, shield mechanics, or boss health bars.
/// </summary>
public class GameHealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int lowHealthThreshold = 25;

    [Header("Multi-Scene Persistence (Optional)")]
    [Tooltip("Optional: Assign an IntVariable asset to persist health across scene loads. Leave empty for single-scene games.")]
    [SerializeField] private IntVariable healthVariable;

    // Local storage for single-scene mode
    private int localHealth;

    [Header("Health Events")]
    /// <summary>
    /// Fires whenever health value changes (both damage and healing), passing current and max health as parameters
    /// </summary>
    public UnityEvent<int, int> onHealthChanged;
    /// <summary>
    /// Fires when damage is taken
    /// </summary>
    public UnityEvent onDamageReceived;
    /// <summary>
    /// Fires when health is gained through healing
    /// </summary>
    public UnityEvent onHealthGained;
    /// <summary>
    /// Fires when health drops to or below the low health threshold
    /// </summary>
    public UnityEvent onLowHealthReached;
    /// <summary>
    /// Fires when health recovers above the low health threshold
    /// </summary>
    public UnityEvent onLowHealthRecovered;
    /// <summary>
    /// Fires when health reaches zero
    /// </summary>
    public UnityEvent onDeath;
    /// <summary>
    /// Fires when health is restored above zero after death
    /// </summary>
    public UnityEvent onRevived;

    private bool isLowHealth = false;
    private bool isDead = false;

    /// <summary>
    /// Current health value. Uses IntVariable if assigned, otherwise local storage.
    /// </summary>
    private int currentHealth
    {
        get => healthVariable != null ? healthVariable.Value : localHealth;
        set
        {
            if (healthVariable != null)
                healthVariable.Value = value;
            else
                localHealth = value;
        }
    }

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int LowHealthThreshold => lowHealthThreshold;
    public bool IsLowHealth => isLowHealth;
    public bool IsDead => isDead;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private void Start()
    {
        // Initialize health based on mode
        if (healthVariable != null)
        {
            // Multi-scene mode: IntVariable handles its own initialization from defaultValue
            // Only set starting health if the variable hasn't been modified yet
            // (i.e., this is the first scene load, not a scene transition)
        }
        else
        {
            // Single-scene mode: use local starting health
            localHealth = startingHealth;
        }

        // Ensure health is within valid range
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        CheckHealthStates();
        // Fire initial health event
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Take damage and reduce health
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead || damageAmount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        onDamageReceived.Invoke();
        onHealthChanged.Invoke(currentHealth, maxHealth);

        // Check if we crossed the low health threshold going down
        if (previousHealth > lowHealthThreshold && currentHealth <= lowHealthThreshold && !isDead)
        {
            isLowHealth = true;
            onLowHealthReached.Invoke();
        }

        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            onDeath.Invoke();
        }
    }

    /// <summary>
    /// Heal and increase health
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isDead || healAmount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        onHealthGained.Invoke();
        onHealthChanged.Invoke(currentHealth, maxHealth);

        CheckHealthStates();

        // Check if we recovered from low health
        if (previousHealth <= lowHealthThreshold && currentHealth > lowHealthThreshold)
        {
            isLowHealth = false;
            onLowHealthRecovered.Invoke();
        }
    }

    /// <summary>
    /// Set health to specific value
    /// </summary>
    public void SetHealth(int newHealth)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        onHealthChanged.Invoke(currentHealth, maxHealth);

        // Determine if this was damage or healing
        if (currentHealth < previousHealth)
        {
            onDamageReceived.Invoke();
        }
        else if (currentHealth > previousHealth)
        {
            onHealthGained.Invoke();
        }

        CheckHealthStates();

        // Check threshold crossings
        if (previousHealth > lowHealthThreshold && currentHealth <= lowHealthThreshold && !isDead)
        {
            isLowHealth = true;
            onLowHealthReached.Invoke();
        }
        else if (previousHealth <= lowHealthThreshold && currentHealth > lowHealthThreshold)
        {
            isLowHealth = false;
            onLowHealthRecovered.Invoke();
        }

        // Check for death/revival
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            onDeath.Invoke();
        }
        else if (currentHealth > 0 && isDead)
        {
            isDead = false;
            onRevived.Invoke();
        }
    }

    /// <summary>
    /// Restore to full health
    /// </summary>
    public void FullHeal()
    {
        SetHealth(maxHealth);
    }

    /// <summary>
    /// Set maximum health and adjust current health if needed
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        if (newMaxHealth <= 0) return;

        maxHealth = newMaxHealth;

        // Don't let current health exceed new max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
            onHealthChanged.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Set the low health threshold
    /// </summary>
    public void SetLowHealthThreshold(int newThreshold)
    {
        lowHealthThreshold = Mathf.Clamp(newThreshold, 0, maxHealth);
        CheckHealthStates();
    }

    private void CheckHealthStates()
    {
        isLowHealth = currentHealth <= lowHealthThreshold && currentHealth > 0;
        isDead = currentHealth <= 0;
    }

    /// <summary>
    /// For testing - add damage over time
    /// </summary>
    public void StartDamageOverTime(int damagePerSecond, float duration)
    {
        StartCoroutine(DamageOverTimeCoroutine(damagePerSecond, duration));
    }

    private System.Collections.IEnumerator DamageOverTimeCoroutine(int damagePerSecond, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !isDead)
        {
            yield return new WaitForSeconds(1f);
            TakeDamage(damagePerSecond);
            elapsed += 1f;
        }
    }
}