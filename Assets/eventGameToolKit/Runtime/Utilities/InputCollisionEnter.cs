using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects 3D collisions with tagged objects, measuring impact strength and applying timing restrictions.
/// Common use: Impact damage systems, collision-triggered events, breakable objects, or force-based interactions.
/// </summary>
// No [RequireComponent(typeof(Collider))]: it forces a 3D collider, and Unity then
// refuses to add a Collider2D, making 2D collisions impossible. Start() validates that
// some collider is present instead.
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
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
    private HashSet<GameObject> contactedObjects;

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
            contactedObjects = new HashSet<GameObject>();
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
        // Either dimension is acceptable; the matching collision callback fires for whichever
        // collider type is attached.
        Collider2D collider2D = GetComponent<Collider2D>();

        if (collider == null && collider2D == null)
        {
            Debug.LogError($"InputCollisionEnter on {gameObject.name}: needs a Collider (3D) or Collider2D (2D)!", this);
            return;
        }

        bool isTrigger = collider != null ? collider.isTrigger
                                          : collider2D.isTrigger;
        if (isTrigger)
        {
            Debug.LogWarning($"InputCollisionEnter on {gameObject.name}: Collider is set as Trigger. " +
                           "For collision detection, uncheck 'Is Trigger'. " +
                           "Use triggers for zone/area detection instead.", this);
        }

        // A collision needs a body on one side of the pair. Check both dimensions before
        // warning, or a perfectly valid 2D setup gets told it is broken.
        bool hasBody = rigidBody != null || GetComponent<Rigidbody2D>() != null;
        if (!hasBody)
        {
            rigidBody = GetComponentInParent<Rigidbody>();
            hasBody = rigidBody != null || GetComponentInParent<Rigidbody2D>() != null;

            if (!hasBody)
            {
                Debug.LogWarning($"InputCollisionEnter on {gameObject.name}: No Rigidbody found! " +
                               "Add a Rigidbody or Rigidbody2D (can be Kinematic) for collision detection.", this);
            }
        }
    }

    // Unity dispatches the 3D and 2D collision callbacks independently: an object with a
    // Collider only ever hears the 3D one, a Collider2D only the 2D one. Both feed the
    // same handler, so behaviour is identical in either dimension.
    private void OnCollisionEnter(Collision collision)
    {
        Vector3 point = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        HandleCollision(collision.gameObject, collision.relativeVelocity.magnitude,
                        point, collision.contacts.Length > 0);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 point = collision.contactCount > 0 ? (Vector3)collision.GetContact(0).point : transform.position;
        HandleCollision(collision.gameObject, collision.relativeVelocity.magnitude,
                        point, collision.contactCount > 0);
    }

    private void HandleCollision(GameObject other, float impactStrength,
                                 Vector3 contactPoint, bool hasContact)
    {
        // Check tag (with invert option)
        bool tagMatches = other.CompareTag(collisionObjectTag);
        if (invertTagDetection ? tagMatches : !tagMatches)
            return;

        // Update velocity statistics if debug is enabled
        if (enableDebug && debugVelocityStats)
        {
            UpdateVelocityStats(impactStrength);
        }

        // Check minimum impact strength
        if (impactStrength < minimumImpactStrength)
        {
            DebugLog($"Collision too weak: {other.name} impact {impactStrength:F2} < required {minimumImpactStrength:F2}");
            return;
        }

        // Apply timing mode restrictions
        if (!CheckTimingRestrictions(other, impactStrength))
            return;

        // Show 3D debug text if enabled
        if (enableDebug && debugShow3DText && hasContact)
        {
            Show3DDebugText(contactPoint, impactStrength);
        }

        // Log successful collision
        DebugLog($"Collision detected: {other.name} → {gameObject.name} (Impact: {impactStrength:F2})");

        // Invoke all relevant events
        onCollisionEnter?.Invoke();
        onCollisionEnterWithStrength?.Invoke(impactStrength);
        onCollisionEnterWithObject?.Invoke(other);
    }

    /// <summary>
    /// Checks if collision should trigger based on timing mode
    /// </summary>
    private bool CheckTimingRestrictions(GameObject other, float impactStrength)
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
                    contactedObjects = new HashSet<GameObject>();
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
            // Tracking is by GameObject, so this works for 2D and 3D alike.
            if (contactedObjects.Remove(obj))
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

        return contactedObjects.Contains(obj);
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
            contactedObjects = new HashSet<GameObject>();
        }
        else if (timingMode != TimingMode.InitialContact && contactedObjects != null)
        {
            contactedObjects = null;
        }
    }
#endif
}