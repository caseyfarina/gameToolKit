using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Manages health with damage and healing mechanics, firing events at critical thresholds.
/// Common use: Player or enemy health systems, destructible objects, shield mechanics, or boss health bars.
/// </summary>
public class GameHealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int lowHealthThreshold = 25;
    [SerializeField] private TextMeshProUGUI healthDisplay;

    [Header("Health Events")]
    /// <summary>
    /// Fires whenever health value changes (both damage and healing)
    /// </summary>
    public UnityEvent onHealthChanged;
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

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int LowHealthThreshold => lowHealthThreshold;
    public bool IsLowHealth => isLowHealth;
    public bool IsDead => isDead;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private void Start()
    {
        // Ensure health starts within valid range
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateDisplay();
        CheckHealthStates();
    }

    /// <summary>
    /// Take damage and reduce health
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead || damageAmount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        UpdateDisplay();
        onDamageReceived.Invoke();
        onHealthChanged.Invoke();

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

        UpdateDisplay();
        onHealthGained.Invoke();
        onHealthChanged.Invoke();

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

        UpdateDisplay();
        onHealthChanged.Invoke();

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
            UpdateDisplay();
            onHealthChanged.Invoke();
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

    private void UpdateDisplay()
    {
        if (healthDisplay != null)
        {
            healthDisplay.text = $"{currentHealth}/{maxHealth}";
        }
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