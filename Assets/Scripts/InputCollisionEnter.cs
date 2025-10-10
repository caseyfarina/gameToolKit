using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects 3D collisions with tagged objects, measuring impact strength and applying timing restrictions.
/// Common use: Impact damage systems, collision-triggered events, breakable objects, or force-based interactions.
/// </summary>
[RequireComponent(typeof(Collider))]
[HelpURL("https://docs.unity3d.com/Manual/CollidersOverview.html")]
[AddComponentMenu("Teaching/Input/Input Collision Enter")]
public class InputCollisionEnter : MonoBehaviour
{
    // Timing mode enumeration
    public enum TimingMode
    {
        None,           // No timing restrictions
        Cooldown,       // Time-based cooldown between events
        InitialContact  // Only fire once per object until it leaves and returns
    }

    [Header("Detection Settings")]
    [SerializeField]
    [Tooltip("Tag of objects that will trigger collision events")]
    private string collisionObjectTag = "Player";

    [SerializeField]
    [Tooltip("Invert tag detection - trigger on everything EXCEPT the specified tag")]
    private bool invertTagDetection = false;

    [SerializeField]
    [Tooltip("Minimum collision impact magnitude required to trigger events (0 = any collision)")]
    [Range(0f, 20f)]
    private float minimumImpactStrength = 0f;

    [Header("Timing Controls")]
    [SerializeField]
    [Tooltip("Choose timing mode: Cooldown prevents rapid firing, Initial Contact fires once per object")]
    private TimingMode timingMode = TimingMode.None;

    [SerializeField]
    [Tooltip("Cooldown time in seconds between collision events (only used with Cooldown mode)")]
    [Range(0.1f, 5f)]
    private float collisionCooldown = 0.5f;

    [Header("Debug Settings")]
    [SerializeField]
    [Tooltip("Master toggle for all debug features")]
    private bool enableDebug = false;

    [SerializeField]
    [Tooltip("Show debug messages in console")]
    private bool debugConsoleLogging = true;

    [SerializeField]
    [Tooltip("Display velocity statistics to help set threshold values")]
    private bool debugVelocityStats = false;

    [SerializeField]
    [Tooltip("Show impact strength as 3D text in scene")]
    private bool debugShow3DText = false;

    [SerializeField]
    [Tooltip("Font size for 3D debug text")]
    [Range(10, 100)]
    private int debugTextFontSize = 50;

    [SerializeField]
    [Tooltip("Duration in seconds for 3D debug text")]
    [Range(0.5f, 5f)]
    private float debugTextDuration = 1f;

    [Header("Events")]
    [Space(10)]
    [Tooltip("Called when an object with the specified tag collides with sufficient force")]
    /// <summary>
    /// Fires when an object with the target tag collides with sufficient force and timing restrictions are met
    /// </summary>
    public UnityEvent onCollisionEnter;

    [Tooltip("Optional: Pass the collision impact strength as a float parameter")]
    /// <summary>
    /// Fires when collision occurs, passing the impact strength as a float parameter
    /// </summary>
    public UnityEvent<float> onCollisionEnterWithStrength;

    [Tooltip("Optional: Pass the colliding GameObject")]
    /// <summary>
    /// Fires when collision occurs, passing the colliding GameObject as a parameter
    /// </summary>
    public UnityEvent<GameObject> onCollisionEnterWithObject;

    // Private variables for timing control
    private float lastCollisionTime = -999f;
    private HashSet<Collider> contactedObjects;

    // Velocity statistics for threshold tuning
    private float minVelocitySeen = float.MaxValue;
    private float maxVelocitySeen = 0f;
    private float totalVelocity = 0f;
    private int velocityCount = 0;

    // Component references
    private new Collider collider;
    private Rigidbody rigidBody;

    void Awake()
    {
        // Cache component references
        collider = GetComponent<Collider>();
        rigidBody = GetComponent<Rigidbody>();

        // Initialize HashSet only if using InitialContact mode
        if (timingMode == TimingMode.InitialContact)
        {
            contactedObjects = new HashSet<Collider>();
        }
    }

    void Start()
    {
        ValidateSetup();
    }

    /// <summary>
    /// Validates the GameObject setup for collision detection
    /// </summary>
    private void ValidateSetup()
    {
        if (collider == null)
        {
            Debug.LogError($"InputCollisionEnter on {gameObject.name}: Missing Collider component!", this);
            return;
        }

        if (collider.isTrigger)
        {
            Debug.LogWarning($"InputCollisionEnter on {gameObject.name}: Collider is set as Trigger. " +
                           "For collision detection, uncheck 'Is Trigger'. " +
                           "Use triggers for zone/area detection instead.", this);
        }

        if (rigidBody == null)
        {
            // Check if there's a Rigidbody in parent hierarchy
            rigidBody = GetComponentInParent<Rigidbody>();

            if (rigidBody == null)
            {
                Debug.LogWarning($"InputCollisionEnter on {gameObject.name}: No Rigidbody found! " +
                               "Add a Rigidbody component (can be Kinematic) for collision detection.", this);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check tag (with invert option)
        bool tagMatches = collision.gameObject.CompareTag(collisionObjectTag);
        if (invertTagDetection ? tagMatches : !tagMatches)
            return;

        // Calculate impact strength
        float impactStrength = collision.relativeVelocity.magnitude;

        // Update velocity statistics if debug is enabled
        if (enableDebug && debugVelocityStats)
        {
            UpdateVelocityStats(impactStrength);
        }

        // Check minimum impact strength
        if (impactStrength < minimumImpactStrength)
        {
            DebugLog($"Collision too weak: {collision.gameObject.name} impact {impactStrength:F2} < required {minimumImpactStrength:F2}");
            return;
        }

        // Apply timing mode restrictions
        if (!CheckTimingRestrictions(collision.collider, impactStrength))
            return;

        // Show 3D debug text if enabled
        if (enableDebug && debugShow3DText && collision.contacts.Length > 0)
        {
            Show3DDebugText(collision.contacts[0].point, impactStrength);
        }

        // Log successful collision
        DebugLog($"Collision detected: {collision.gameObject.name} → {gameObject.name} (Impact: {impactStrength:F2})");

        // Invoke all relevant events
        onCollisionEnter?.Invoke();
        onCollisionEnterWithStrength?.Invoke(impactStrength);
        onCollisionEnterWithObject?.Invoke(collision.gameObject);
    }

    /// <summary>
    /// Checks if collision should trigger based on timing mode
    /// </summary>
    private bool CheckTimingRestrictions(Collider other, float impactStrength)
    {
        switch (timingMode)
        {
            case TimingMode.Cooldown:
                float currentTime = Time.time;
                if (currentTime - lastCollisionTime < collisionCooldown)
                {
                    DebugLog($"Collision ignored (cooldown): {collisionCooldown:F2}s remaining");
                    return false;
                }
                lastCollisionTime = currentTime;
                break;

            case TimingMode.InitialContact:
                if (contactedObjects == null)
                {
                    contactedObjects = new HashSet<Collider>();
                }

                if (!contactedObjects.Add(other))  // Add returns false if already present
                {
                    DebugLog($"Collision ignored (already contacted): {other.name}");
                    return false;
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// Updates velocity statistics for threshold tuning
    /// </summary>
    private void UpdateVelocityStats(float velocity)
    {
        minVelocitySeen = Mathf.Min(minVelocitySeen, velocity);
        maxVelocitySeen = Mathf.Max(maxVelocitySeen, velocity);
        totalVelocity += velocity;
        velocityCount++;

        float average = totalVelocity / velocityCount;
        float suggestedThreshold = average * 0.5f;

        Debug.Log($"[VELOCITY STATS] Current: {velocity:F2} | Min: {minVelocitySeen:F2} | " +
                 $"Max: {maxVelocitySeen:F2} | Avg: {average:F2} | " +
                 $"Suggested Threshold: {suggestedThreshold:F2}", this);
    }

    /// <summary>
    /// Creates 3D text showing impact strength at collision point
    /// </summary>
    private void Show3DDebugText(Vector3 position, float impactStrength)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Don't create debug text if this GameObject is inactive
        if (!gameObject.activeInHierarchy)
            return;

        GameObject textObj = new GameObject($"ImpactText_{impactStrength:F2}");
        textObj.transform.position = position + Vector3.up * 0.5f;

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = $"{impactStrength:F2}";
        textMesh.fontSize = debugTextFontSize;
        textMesh.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(impactStrength / 10f));
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Scale down for better world-space appearance
        textObj.transform.localScale = Vector3.one * 0.5f;

        // Only start coroutine if GameObject is active
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateDebugText(textObj, debugTextDuration));
        }
        else
        {
            // Fallback: destroy immediately if can't animate
            Destroy(textObj, debugTextDuration);
        }
#endif
    }

    /// <summary>
    /// Animates debug text (fade and float up)
    /// </summary>
    private IEnumerator AnimateDebugText(GameObject textObj, float duration)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (textObj == null) yield break;

        TextMesh textMesh = textObj.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            Destroy(textObj);
            yield break;
        }

        Vector3 startPos = textObj.transform.position;
        Color startColor = textMesh.color;
        float elapsed = 0f;

        while (elapsed < duration && textObj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Float up and fade out
            textObj.transform.position = startPos + Vector3.up * (t * 0.5f);

            // Check if textMesh still exists before modifying color
            if (textMesh != null)
            {
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            }

            // Face camera
            if (Camera.main != null)
            {
                textObj.transform.LookAt(Camera.main.transform);
                textObj.transform.rotation = Quaternion.LookRotation(-textObj.transform.forward);
            }

            yield return null;
        }

        if (textObj != null)
            Destroy(textObj);
#else
        yield break;
#endif
    }

    /// <summary>
    /// Conditional debug logging
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebug && debugConsoleLogging)
        {
            Debug.Log($"[InputCollisionEnter] {message}", this);
        }
    }

    /// <summary>
    /// Resets contact tracking (useful for testing or level resets)
    /// </summary>
    public void ResetContactTracking()
    {
        contactedObjects?.Clear();
        lastCollisionTime = -999f;

        if (enableDebug && debugConsoleLogging)
        {
            Debug.Log($"[InputCollisionEnter] Contact tracking reset for {gameObject.name}", this);
        }
    }

    /// <summary>
    /// Resets velocity statistics
    /// </summary>
    public void ResetVelocityStats()
    {
        minVelocitySeen = float.MaxValue;
        maxVelocitySeen = 0f;
        totalVelocity = 0f;
        velocityCount = 0;

        if (enableDebug && debugConsoleLogging)
        {
            Debug.Log($"[InputCollisionEnter] Velocity statistics reset for {gameObject.name}", this);
        }
    }

    /// <summary>
    /// Gets current velocity statistics (for testing or UI display)
    /// </summary>
    public (float min, float max, float average, int count) GetVelocityStats()
    {
        float avg = velocityCount > 0 ? totalVelocity / velocityCount : 0f;
        return (minVelocitySeen, maxVelocitySeen, avg, velocityCount);
    }

    /// <summary>
    /// Manually trigger the collision event (useful for testing or external systems)
    /// </summary>
    [ContextMenu("Test Trigger Event")]
    public void TriggerEvent()
    {
        TriggerEvent(5f, gameObject);
    }

    /// <summary>
    /// Manually trigger the collision event with parameters
    /// </summary>
    public void TriggerEvent(float impactStrength, GameObject collidingObject)
    {
        DebugLog($"Manually triggering collision event on {gameObject.name}");
        onCollisionEnter?.Invoke();
        onCollisionEnterWithStrength?.Invoke(impactStrength);
        onCollisionEnterWithObject?.Invoke(collidingObject);
    }

    /// <summary>
    /// Clear a specific object from contact tracking
    /// </summary>
    public void ClearSpecificContact(GameObject obj)
    {
        if (obj != null && contactedObjects != null)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null && contactedObjects.Remove(col))
            {
                DebugLog($"Cleared {obj.name} from contact tracking");
            }
        }
    }

    /// <summary>
    /// Check if an object is currently in contact (for InitialContact mode)
    /// </summary>
    public bool IsObjectInContact(GameObject obj)
    {
        if (obj == null || contactedObjects == null || timingMode != TimingMode.InitialContact)
            return false;

        Collider col = obj.GetComponent<Collider>();
        return col != null && contactedObjects.Contains(col);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only validation when values change
    /// </summary>
    private void OnValidate()
    {
        // Ensure timing mode and HashSet are synchronized
        if (timingMode == TimingMode.InitialContact && contactedObjects == null && Application.isPlaying)
        {
            contactedObjects = new HashSet<Collider>();
        }
        else if (timingMode != TimingMode.InitialContact && contactedObjects != null)
        {
            contactedObjects = null;
        }
    }
#endif
}